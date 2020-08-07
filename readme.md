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

## Package property mapping

- .nuspec reference: https://docs.microsoft.com/en-us/nuget/reference/nuspec
- VSIX reference: https://docs.microsoft.com/en-us/visualstudio/extensibility/vsix-extension-schema-2-0-reference?view=vs-2019

| .nuspec       | .vsix               | Argument          | Default |
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

- template.json reference: https://github.com/dotnet/templating/wiki/Reference-for-template.json
- .vstemplate TemplateData reference: https://docs.microsoft.com/en-us/visualstudio/extensibility/templatedata-element-visual-studio-templates?view=vs-2019
- VS2019+ template tags: https://docs.microsoft.com/en-us/visualstudio/ide/template-tags?view=vs-2019

These properties apply per template. To set a property for a certain template you can use the following format: `[template-id]=[value]` (e.g. `--language-tag MyTemplate.CSharp=csharp`).
If you omit the template id the value will be applied to all templates in the pack.
You can use wildcards `*` in the template-id.
These command line arguments can all take in multiple values, e.g. `--template-icon MyTemplate.One*=icon1.png MyTemplate.Two*=icon2.png`.


| template.json | .vstemplate     | Argument        | Default |
| ------------- | --------------- | --------------- | ------- |
| name          | Name            | /               | NA      |
| description   | Description     | /               | NA      |
| /             | Icon            | --template-icon | /       |
| defaultName   | DefaultName     | --default-name  | /       |
| tags/language | LanguageTag     | --language-tag  | csharp  |
| /             | PlatformTag     | --platform-tags | /       |
| /             | ProjectTypeTag  | --project-tags  | /       |

Supported tags/language properties are C#, F# and VB.


