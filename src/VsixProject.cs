using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace VSTemplate
{
    public class VsixProject
    {
        private static Lazy<string> _vsixProjectTemplate =
            new Lazy<string>(() => ReadEmbeddedResource("tmpl.vsix-template.csproj.txt"));

        private static Lazy<string> _vsixManifestTemplate =
            new Lazy<string>(() => ReadEmbeddedResource("tmpl.source.extension.vsixmanifest"));

        private static string _pkgDefTemplate =
@"[$RootKey$\TemplateEngine\Templates\{0}]
""InstalledPath""=""$PackageFolder$""";

        private static string ReadEmbeddedResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ns = assembly.GetName().Name;
            var resourcePath = ns + "." + path;

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }


        private readonly string _rootDir;
        private readonly XDocument _doc;
        private readonly XElement _itemGroup;

        //private static readonly string _msbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";
        private static readonly string _msbuildNs = "";
        private static readonly XName _itemGroupName = XName.Get("ItemGroup", _msbuildNs);
        private static readonly XName _contentName = XName.Get("Content", _msbuildNs);
        private static readonly XName _includeInVsixName = XName.Get("IncludeInVSIX", _msbuildNs);
        private static readonly XName _targetPathName = XName.Get("TargetPath", _msbuildNs);
        private static readonly XName _vsixSubPathName = XName.Get("VSIXSubPath", _msbuildNs);
        private static readonly XName _includeAttrName = XName.Get("Include", _msbuildNs);

        private static readonly string _vsixManifestNs = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        private static readonly XName _metadataName = XName.Get("Metadata", _vsixManifestNs);
        private static readonly XName _identityName = XName.Get("Identity", _vsixManifestNs);
        private static readonly XName _idAttrName = XName.Get("Id");
        private static readonly XName _languageAttrName = XName.Get("Language");
        private static readonly XName _versionAttrName = XName.Get("Version");
        private static readonly XName _publisherAttrName = XName.Get("Publisher");
        private static readonly XName _displayNameName = XName.Get("DisplayName", _vsixManifestNs);
        private static readonly XName _descriptionName = XName.Get("Description", _vsixManifestNs);
        private static readonly XName _moreInfoName = XName.Get("MoreInfo", _vsixManifestNs);
        private static readonly XName _tagsName = XName.Get("Tags", _vsixManifestNs);
        private static readonly XName _licenseName = XName.Get("License", _vsixManifestNs);
        private static readonly XName _releaseNotesName = XName.Get("ReleaseNotes", _vsixManifestNs);
        private static readonly XName _iconName = XName.Get("Icon", _vsixManifestNs);
        private static readonly XName _previewImageName = XName.Get("PreviewImage", _vsixManifestNs);
        private static readonly XName _gettingStartedGuideName = XName.Get("GettingStartedGuide", _vsixManifestNs);

        private VsixProject(string rootDir, XDocument doc)
        {
            _rootDir = rootDir;
            _doc = doc;

            _itemGroup = doc.Root.Descendants(_itemGroupName)
                                     .Where(i => i.IsEmpty)
                                     .First();
        }

        public void AddNupkg(string fileName)
        {
            var content = new XElement(_contentName);
            content.SetAttributeValue(_includeAttrName, fileName);

            var includeInVsix = new XElement(_includeInVsixName, true);
            content.Add(includeInVsix);

            _itemGroup.Add(content);
        }

        public void AddTemplateZip(string fileName, string vsixSubFolder)
        {
            var content = new XElement(_contentName);
            content.SetAttributeValue(_includeAttrName, fileName);

            content.AddElement(_vsixSubPathName, vsixSubFolder);
            content.AddElement(_includeInVsixName, "true");
            content.AddElement(_targetPathName, "template.zip");
            _itemGroup.Add(content);
        }

        private bool AddExternalContent(string path, string localName, Logger l)
        {
            var success = true;

            try
            {
                File.Copy(path, Path.Combine(_rootDir, localName));
                RegisterContent(localName);
            }
            catch (FileNotFoundException)
            {
                l.LogWarning($"File not found '{Path.GetFullPath(path)}'.");
                success = false;
            }

            return success;
        }

        private void RegisterContent(string localName)
        {
            var content = new XElement(_contentName);
            content.SetAttributeValue(_includeAttrName, localName);
            content.AddElement(_includeInVsixName, "true");
            _itemGroup.Add(content);
        }

        public void Write(string file)
        {
            _doc.Save(file);
        }

        public static VsixProject Create(string rootDir, VsixProperties props, Logger l)
        {
            var vsixProject = _vsixProjectTemplate.Value;
            var doc = XDocument.Parse(vsixProject);
            var project = new VsixProject(rootDir, doc);
            project.WriteManifest(props, l);
            project.WritePkgDef(props, l);

            return project;
        }

        private void WriteManifest(VsixProperties props, Logger l)
        {
            // The elements here need to be in this exact order or schema validation for the manifest fails.

            var doc = XDocument.Parse(_vsixManifestTemplate.Value);
            var metadata = doc.Descendants(_metadataName).First();

            var identity = new XElement(_identityName);
            identity.SetAttributeValue(_idAttrName, props.Id);
            identity.SetAttributeValue(_versionAttrName, props.Version);
            identity.SetAttributeValue(_publisherAttrName, props.Publisher);
            identity.SetAttributeValue(_languageAttrName, "en-us");
            metadata.Add(identity);

            if (props.DisplayName != null) metadata.AddElement(_displayNameName, props.DisplayName);
            if (props.Description != null) metadata.AddElement(_descriptionName, props.Description);
            if (props.MoreInfo != null) metadata.AddElement(_moreInfoName, props.MoreInfo);

            // License
            if (props.License != null)
            {
                var ext = Path.GetExtension(props.License);
                var localName = "license" + ext;
                if (AddExternalContent(props.License, localName, l))
                    metadata.AddElement(_licenseName, localName);
            }

            // GettingStartedGuide - http(s) URL or local .html file
            if (props.GettingStartedGuide != null)
            {
                var isWebUrl = Uri.TryCreate(props.GettingStartedGuide, UriKind.RelativeOrAbsolute, out var uri) && uri.Scheme.StartsWith("http");
                if (isWebUrl)
                {
                    metadata.AddElement(_gettingStartedGuideName, props.GettingStartedGuide);
                }
                else
                {
                    var ext = Path.GetExtension(props.GettingStartedGuide);
                    var localName = "getting-started" + ext;
                    if (AddExternalContent(props.GettingStartedGuide, localName, l))
                    metadata.AddElement(_gettingStartedGuideName, localName);
                }
            }

            // ReleaseNotes - http(s) URL or local file
            if (props.ReleaseNotes != null)
            {
                var isWebUrl = Uri.TryCreate(props.ReleaseNotes, UriKind.RelativeOrAbsolute, out var uri) && uri.Scheme.StartsWith("http");
                if (isWebUrl)
                {
                    metadata.AddElement(_releaseNotesName, props.ReleaseNotes);
                }
                else
                {
                    var ext = Path.GetExtension(props.ReleaseNotes);
                    var localName = "release-notes" + ext;
                    if (AddExternalContent(props.ReleaseNotes, localName, l))
                        metadata.AddElement(_releaseNotesName, localName);
                }
            }

            // Icon
            if (props.Icon != null)
            {
                var ext = Path.GetExtension(props.Icon);
                var localName = "icon" + ext;
                if (AddExternalContent(props.Icon, localName, l))
                    metadata.AddElement(_iconName, localName);
            }

            // PreviewImage
            if (props.PreviewImage != null)
            {
                var ext = Path.GetExtension(props.PreviewImage);
                if (props.PreviewImage == props.Icon)
                {
                    var localName = "icon" + ext;
                    metadata.AddElement(_previewImageName, localName);
                }
                else
                {
                    var localName = "preview" + ext;
                    if (AddExternalContent(props.PreviewImage, localName, l))
                        metadata.AddElement(_previewImageName, localName);
                }
            }

            if (props.Tags != null) metadata.AddElement(_tagsName, props.Tags);

            var path = Path.Combine(_rootDir, "source.extension.vsixmanifest");
            doc.Save(path);
        }

        private void WritePkgDef(VsixProperties props, Logger l)
        {
            var content = string.Format(_pkgDefTemplate, props.Id);
            var path = Path.Combine(_rootDir, "template.pkgdef");
            File.WriteAllText(path, content);
        }
    }

    public static class XmlExtensions
    {
        public static void AddElement(this XElement parent, XName name, string value)
        {
            var child = new XElement(name, value);
            parent.Add(child);
        }
    }
}
