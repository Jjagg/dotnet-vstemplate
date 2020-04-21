using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Setup.Configuration;
using CommandHandler = System.CommandLine.Invocation.CommandHandler;
using CustomParameter = VSTemplate.VSTemplateTemplateContentCustomParametersCustomParameter;

namespace VSTemplate
{
    internal partial class Program
    {
        private static Task<int> Main(string[] args)
        {
            var root = new RootCommand
            {
                new Option<string>(new string[] { "-s", "--source" })
                {
                    Name = "Source",
                    Description = "NuGet package to generate a vsix file for.",
                    Required = true
                }.LegalFilePathsOnly(),
                new Option<string>("--vsix", "Output .vsix package path.").LegalFilePathsOnly(),
                new Option<bool>(new string[] { "-f", "--force" }, "Set to overwrite vsix at output path if it exists."),
                new Option<string>(new string[] { "-o", "--obj" }, () => "obj")
                {
                    Name = "Obj",
                    Description = "Intermediate output folder. Defaults to './obj'."
                }.LegalFilePathsOnly(),

                new Option<string>("--more-info", "VSIX MoreInfo property."),
                new Option<string>("--license-file", "Path to the license file.").LegalFilePathsOnly(),
                new Option<string>("--release-notes", "Path to file or URL to site of release notes."),
                new Option<string>("--package-icon", "Path to image to use for VSIX icon. (32x32)").LegalFilePathsOnly(),
                new Option<string>("--preview-img", "Path to image to use for VSIX preview image. (200x200)").LegalFilePathsOnly(),
                new Option<string[]>("--package-tags", "List of tags for the VSIX Tags property.")
                {
                    Argument = new Argument<string[]> { Arity = ArgumentArity.OneOrMore }
                },
                new Option<string>("--getting-started", "VSIX GettingStartedGuide property.").LegalFilePathsOnly(),

                new Option<string[]>("--template-icon", () => new string[0], "Mapping for icons for the individual templates.")
                {
                    Argument = new Argument<string[]> { Arity = ArgumentArity.OneOrMore, }
                },
                new Option<string[]>("--language-tag", () => new string[0], "Mapping for language tags for the individual templates. This value can be extracted from the manifest.json file, see the docs.")
                {
                    Argument = new Argument<string[]> { Arity = ArgumentArity.OneOrMore, }
                },
                new Option<string[]>("--platform-tags", () => new string[0], "Mapping for platform tags for the individual templates.")
                {
                    Argument = new Argument<string[]> { Arity = ArgumentArity.OneOrMore, }
                },
                new Option<string[]>("--type-tags", () => new string[0], "Project type tags to add to the vstemplate.")
                {
                    Argument = new Argument<string[]> { Arity = ArgumentArity.OneOrMore, }
                }
            }.WithHandler(CommandHandler.Create(Delegate.CreateDelegate(typeof(PackVsixDelegate), null, typeof(Program).GetMethod(nameof(PackVsix)))));

            var cmdConfig = new CommandLineConfiguration(
                new[] { root },
                responseFileHandling: ResponseFileHandling.ParseArgsAsSpaceSeparated);
            var parser = new Parser(cmdConfig);

            return parser.InvokeAsync(args);
        }

        private delegate Task<int> PackVsixDelegate(
            string source,
            string vsix,
            bool force,
            string obj,
            string moreInfo,
            string licenseFile,
            string releaseNotes,
            string packageIcon,
            string previewImg,
            string[] packageTags,
            string gettingStarted,
            string[] templateIcon,
            string[] languageTag,
            string[] platformTags,
            string[] typeTags);

        public static async Task<int> PackVsix(
            string source,
            string vsix,
            bool force,
            string obj,
            string moreInfo,
            string licenseFile,
            string releaseNotes,
            string packageIcon,
            string previewImg,
            string[] packageTags,
            string gettingStarted,
            string[] templateIcon,
            string[] languageTag,
            string[] platformTags,
            string[] typeTags)
        {
            var l = new Logger();
            if (!File.Exists(source))
            {
                l.LogError($"Source file '{source}' does not exist.");
                return 1;
            }


            l.Log($"Generating VSIX template package for '{source}'.");

            var templateIconMappings = templateIcon?.Select(s => new TemplatePropertyMapping(s));
            var languageTagMappings = languageTag?.Select(s => new TemplatePropertyMapping(s));
            var platformTagsMappings = platformTags?.Select(s => new TemplatePropertyMapping(s));
            var typeTagsMappings = typeTags?.Select(s => new TemplatePropertyMapping(s));

            l.Log("Parsing NuGet metadata.");

            var pkg = await NuPackage.Open(source);
            var metadata = pkg.Metadata;
            var vsixProps = VsixProperties.FromNuspec(metadata);

            var sourceFolder = Path.GetDirectoryName(source);
            vsix = vsix ?? Path.Combine(sourceFolder, vsixProps.Id + ".vsix");

            if (File.Exists(vsix) && !force)
            {
                l.LogError($"File exists at output path '{vsix}'. Set the --force flag to overwrite it.");
                return 1;
            }

            if (moreInfo != null) vsixProps.MoreInfo = moreInfo;
            if (licenseFile != null) vsixProps.License = licenseFile;
            if (releaseNotes != null) vsixProps.ReleaseNotes = releaseNotes;
            if (packageIcon != null) vsixProps.Icon = packageIcon;
            if (previewImg != null) vsixProps.PreviewImage = previewImg;
            if (packageTags != null)
            {
                var splitPackageTags = packageTags.SelectMany(ts => ts.Split(new char[0], StringSplitOptions.RemoveEmptyEntries));
                vsixProps.Tags = string.Join(';', splitPackageTags);
            }
            if (gettingStarted != null) vsixProps.GettingStartedGuide = gettingStarted;

            var archive = pkg.Archive;
            var templateJsonFiles = archive.Entries.Where(e => e.FullName.EndsWith(".template.config/template.json"));

            l.Log("Generating .vstemplate files.");

            var templateContexts = new List<TemplateContext>();

            var tags = new List<string>();

            foreach (var templateJsonFile in templateJsonFiles)
            {
                var templateJsonContent = new StreamReader(templateJsonFile.Open()).ReadToEnd();

                // TODO apply props overrides
                var props = TemplateProperties.ParseTemplateJson(templateJsonContent);

                props.Icon = MapTemplateProperties(props.Identity, templateIconMappings, tags)?.FirstOrDefault();
                if (languageTagMappings != null && languageTagMappings.Any())
                    props.LanguageTag = MapTemplateProperties(props.Identity, languageTagMappings, tags)?.FirstOrDefault();
                props.PlatformTags = MapTemplateProperties(props.Identity, platformTagsMappings, tags);
                props.ProjectTypeTags = MapTemplateProperties(props.Identity, typeTagsMappings, tags);

                var vsTemplate = CreateVSTemplate(true, props);

                var context = new TemplateContext(templateJsonContent, props, vsTemplate);
                templateContexts.Add(context);
            }

            var di = Directory.CreateDirectory(obj);
            // clear obj directory
            foreach (var fi in di.GetFiles()) fi.Delete();
            foreach (var fi in di.GetDirectories()) fi.Delete(true);
 
            l.Log("Creating VSIX project.");
            l.Indent();

            var vsixDir = Path.Combine(obj, "Vsix");
            Directory.CreateDirectory(vsixDir);

            var vsixProject = VsixProject.Create(vsixDir, vsixProps);

            var vsixProjectPath = Path.Combine(vsixDir, "Vsix.csproj");

            var sourceFileName = Path.GetFileName(source);
            File.Copy(source, Path.Combine(vsixDir, sourceFileName));
            vsixProject.AddNupkg(sourceFileName);

            foreach (var ctx in templateContexts)
            {
                var zipFileName = ctx.TemplateJsonProps.Identity + ".zip";

                var tmpZipFolder = Path.Combine(obj, "zip", ctx.TemplateJsonProps.Identity);
                Directory.CreateDirectory(tmpZipFolder);

                File.WriteAllText(Path.Combine(tmpZipFolder, "template.json"), ctx.TemplateJsonContent);
                ctx.VSTemplate.Write(Path.Combine(tmpZipFolder, "template.vstemplate"));

                if (ctx.TemplateJsonProps.Icon != null)
                    File.Copy(ctx.TemplateJsonProps.Icon, Path.Combine(tmpZipFolder, ctx.TemplateJsonProps.IconZipPath));

                ZipFile.CreateFromDirectory(tmpZipFolder, Path.Combine(vsixDir, zipFileName));
                vsixProject.AddTemplateZip(zipFileName, Path.Combine("Templates", ctx.TemplateJsonProps.Identity));

                l.Log($"Added template '{ctx.TemplateJsonProps.Identity}'.");
            }

            vsixProject.Write(vsixProjectPath);

            l.Dedent();

            if (!BuildVsix(l, obj, vsixProjectPath))
            {
                l.LogError("Failed to build vsix.");
                return 1;
            }

            var builtVsixPath = Path.Combine(vsixDir, "bin", "Vsix.vsix");
            File.Copy(builtVsixPath, vsix, force);

            l.Log($"VSIX generated at {Path.GetFullPath(vsix)}.");

            return 0;
        }

        private static string[] MapTemplateProperties(string identity, IEnumerable<TemplatePropertyMapping> mappings, List<string> reusableValues)
        {
            if (mappings is null)
                return null;

            reusableValues.Clear();
            reusableValues.AddRange(mappings.Where(m => m.AppliesTo(identity)).Select(m => m.Value));
            return reusableValues.ToArray<string>();
        }

        public static bool BuildVsix(Logger l, string logDir, string projectFile)
        {
            var query = new SetupConfiguration();
            var setupHelper = (ISetupHelper)query;

            var instances = new List<VSInstance>();

            ForEachVsInstance(inst =>
            {
                var version = inst.GetInstallationVersion();
                var instance = new VSInstance
                {
                    Name = inst.GetDisplayName(),
                    Version = version,
                    IntVersion = setupHelper.ParseVersion(version),
                    Path = inst.GetInstallationPath(),
                    MSBuildPath = inst.ResolvePath("MSBuild\\Current\\Bin\\MSBuild.exe")
                };

                instances.Add(instance);
            });

            instances.Sort((left, right) => Math.Sign((long)left.IntVersion - (long)right.IntVersion));
            var newestInstance = instances.Last();

            if (!newestInstance.IsVs2017OrNewer())
            {
                l.LogError("Did not find a VS2017+ installation.");
                return false;
            }

            l.Log("Building VSIX");

            var buildLogPath = Path.Combine(logDir, "build.log");
            var vsixDir = Path.GetDirectoryName(projectFile);
            var buildExitCode = MSBuildBuild(buildLogPath, newestInstance.MSBuildPath, vsixDir);

            if (buildExitCode != 0)
            {
                l.LogError($"VSIX build failed, check the MSBuild output at {Path.GetFullPath(buildLogPath)}.");
                return false;
            }

            return true;
        }

        private static int MSBuildBuild(string logPath, string msbuildPath, string workingDir)
            => RunCommand(logPath, msbuildPath, workingDir, "-restore");

        private static int RunCommand(string logPath, string command, string workingDir, string arguments = "")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var exitCode = -1;
            using (var logFile = File.CreateText(logPath))
            {
                var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (s, e) => logFile.WriteLine(e.Data);
                process.ErrorDataReceived += (s, e) => logFile.WriteLine(e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            return exitCode;
        }

        private class VSInstance
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public ulong IntVersion { get; set; }
            public string Path { get; set; }
            public string MSBuildPath { get; set; }

            internal bool IsVs2017OrNewer()
            {
                var majorVersionSpan = Version.AsSpan(0, Version.IndexOf('.'));
                var majorVersion = int.Parse(majorVersionSpan);
                return majorVersion >= 15;
            }
        }

        private static void ForEachVsInstance(Action<ISetupInstance> action)
        {
            var query = new SetupConfiguration();
            var query2 = (ISetupConfiguration2)query;
            var e = query2.EnumAllInstances();

            var helper = (ISetupHelper)query;

            int fetched;
            var instances = new ISetupInstance[1];
            do
            {
                e.Next(1, instances, out fetched);
                if (fetched > 0)
                {
                    action(instances[0]);
                }
            }
            while (fetched > 0);
        }

        private static VSTemplate CreateVSTemplate(bool force, TemplateProperties props)
        {
            var tmpl = new VSTemplate();
            tmpl.Version = "3.0.0";
            tmpl.Type = "ProjectGroup";

            var td = tmpl.TemplateData = new TemplateData();
            td.Name = props.Name;
            td.Description = props.Description;
            td.Icon = props.IconZipPath;
            td.ProjectType = "CSharp";
            td.DefaultName = props.DefaultName;
            td.LanguageTag = props.LanguageTag;
            if (props.PlatformTags != null)
            {
                foreach (var pt in props.PlatformTags)
                    td.PlatformTag.Add(pt);
            }
            if (props.ProjectTypeTags != null)
            {
                foreach (var pt in props.ProjectTypeTags)
                    td.ProjectTypeTag.Add(pt);
            }

            //td.CreateNewFolder = true;
            //td.ProvideDefaultName = true;
            //td.LocationField = TemplateDataLocationField.Enabled;
            //td.EnableLocationBrowseButton = true;

            var tc = tmpl.TemplateContent = new VSTemplateTemplateContent();
            tc.CustomParameters.Add(new CustomParameter { Name = "$language$", Value = "CSharp" });
            tc.CustomParameters.Add(new CustomParameter { Name = "$uistyle$", Value = "none" });
            tc.CustomParameters.Add(new CustomParameter { Name = "$groupid$", Value = props.GroupIdentity });

            var we = tmpl.WizardExtension = new VSTemplateWizardExtension();
            we.Assembly = "Microsoft.VisualStudio.TemplateEngine.Wizard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            we.FullClassName = "Microsoft.VisualStudio.TemplateEngine.Wizard.TemplateEngineWizard";

            return tmpl;
        }
    }

    public static class CommandLineExtensions
    {
        public static Command WithHandler(this Command command, System.CommandLine.Invocation.ICommandHandler handler)
        {
            command.Handler = handler;
            return command;
        }
    }
}
