# dotnet-vstemplate [![Build status](https://github.com/Jjagg/dotnet-vstemplate/workflows/ci/badge.svg)](https://github.com/Jjagg/dotnet-vstemplate/actions)

<img align="right" width="100px" height="100px" src="img/icon.png">

`dotnet-vstemplate` is a .NET Core tool for packing templates for the .NET Core Template Engine into VSIX project template packages.
Generated VSIX packages support VS2017 and up.

## Help

```bash
Usage:
  dotnet-vstemplate [options]

Options:
  -s, --source <source>                  NuGet package to generate a vsix file for.
  --vsix <vsix>                          Output .vsix package path.
  -f, --force                            Set to overwrite vsix at output path if it exists.
  -o, --obj <obj>                        Intermediate output folder. Defaults to './obj'. [default: obj]
  --vsix-version <vsix-version>          Override the version of the vsix package. Defaults to the version of the
                                         source nupkg (strips pre-release version; anything after a '-').
  --more-info <more-info>                VSIX MoreInfo property.
  --license-file <license-file>          Path to the license file.
  --release-notes <release-notes>        Path to file or URL to site of release notes.
  --package-icon <package-icon>          Path to image to use for VSIX icon. (32x32)
  --preview-img <preview-img>            Path to image to use for VSIX preview image. (200x200)
  --package-tags <package-tags>          List of tags for the VSIX Tags property.
  --getting-started <getting-started>    VSIX GettingStartedGuide property.
  --template-icon <template-icon>        Mapping for icons for the individual templates.
  --language-tag <language-tag>          Mapping for language tags for the individual templates. This value can be
                                         extracted from the manifest.json file, see the docs.
  --platform-tags <platform-tags>        Mapping for platform tags for the individual templates.
  --type-tags <type-tags>                Project type tags to add to the vstemplate.
  --default-name <default-name>          Mapping for the default project name for the individual templates. This value
                                         can be extracted from the manifest.json file, see the docs.
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
  ```

## VSIX properties

dotnet-vstemplate maps metadata to VSIX properties. For most properties
there is a command line option to override the VSIX property.
Below you'll find the mapping from NuGet metadata to VSIX metadata, the command
line option to override the property and the default value that's used when the
property is not in the NuGet metadata and no command line option for the property
is specified.

- .nuspec reference: https://docs.microsoft.com/en-us/nuget/reference/nuspec
- VSIX reference: https://docs.microsoft.com/en-us/visualstudio/extensibility/vsix-extension-schema-2-0-reference?view=vs-2019

| .nuspec       | .vsix               | Option            | Default |
| ------------- | ------------------- | ----------------- | ------- |
| id            | Id                  | /                 | NA      |
| version       | Version             | --vsix-version    | NA      |
| /             | Language            | /                 | en-us   |
| authors       | Publisher           | /                 | NA      |
| title         | DisplayName         | /                 | /       |
| description   | Description         | /                 | NA      |
| /             | MoreInfo            | --more-info       | /       |
| /             | License             | --license-file    | /       |
| /             | ReleaseNotes        | --release-notes   | /       |
| icon          | Icon                | --package-icon    | /       |
| icon          | PreviewImage        | --preview-img     | /       |
| tags          | Tags                | --package-tags    | /       |
| /             | GettingStartedGuide | --getting-started | /       |

## Template property mapping

For individual templates, dotnet-vstemplate maps template.json properties to vstemplate properties.
Just like the VSIX properties most can be overridden with a command line option.

These command line options apply per template. To set a property for a certain template you can use the following format: `[template-id]=[value]` (e.g. `--language-tag MyTemplate.CSharp=csharp`).
If you omit the template id the value will be applied to all templates in the pack.
You can use wildcards `*` in the template-id.
These command line arguments can all take in multiple values, e.g. `--template-icon MyTemplate.One*=icon1.png MyTemplate.Two*=icon2.png`.

- template.json reference: https://github.com/dotnet/templating/wiki/Reference-for-template.json
- vstemplate TemplateData reference: https://docs.microsoft.com/en-us/visualstudio/extensibility/templatedata-element-visual-studio-templates?view=vs-2019
- VS2019+ template tags: https://docs.microsoft.com/en-us/visualstudio/ide/template-tags?view=vs-2019

| template.json | vstemplate      | Option          | Default |
| ------------- | --------------- | --------------- | ------- |
| name          | Name            | /               | NA      |
| description   | Description     | /               | NA      |
| /             | Icon            | --template-icon | /       |
| defaultName   | DefaultName     | --default-name  | /       |
| tags/language | LanguageTag     | --language-tag  | csharp  |
| /             | PlatformTag     | --platform-tags | /       |
| /             | ProjectTypeTag  | --project-tags  | /       |

Supported tags/language in template.json for automatically mapping to vstemplate C#, F# and VB.

## Automation

dotnet-vstemplate supports response files, i.e. passing a file with command line options.
This makes it easy to maintain command line options to run dotnet-vstemplate with.
To use a response file pass the `@<response file>` options

```bash
vstemplate @settings.rsp
```

For an example check out the usage in [MonoGame](https://monogame.net), where the VSIX packaging is
completely automated:

- [Call to dotnet-vstemplate in a CAKE build script](https://github.com/MonoGame/MonoGame/blob/687756238f4a660448526c3bf16e3db8bda8e7e2/build.cake#L224-L227)
- [Response file that is passed to dotnet-vstemplate](https://github.com/MonoGame/MonoGame/blob/687756238f4a660448526c3bf16e3db8bda8e7e2/Templates/VisualStudio/settings.rsp)

