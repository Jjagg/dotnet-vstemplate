using System.IO;
using System.Text.Json;

namespace VSTemplate
{
    public class TemplateProperties
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DefaultName { get; set; }
        public string Identity { get; set; }
        public string GroupIdentity { get; set; }
        public string LanguageTag { get; set; }
        public string[] PlatformTags { get; set; }
        public string[] ProjectTypeTags { get; set; }
        public string Icon { get; set; }

        public string IconZipPath => Icon is null ? null : "icon" + Path.GetExtension(Icon);

        public static TemplateProperties Default => new TemplateProperties
        {
            Author = "Author",
            Name = "Name",
            Description = "Description",
            DefaultName = "MyProject",
            LanguageTag = "csharp",
            PlatformTags = new string[] { "windows" },
            ProjectTypeTags = new string[] { "console" }
        };

        public static TemplateProperties ParseTemplateJson(string text)
        {
            JsonElement? GetProp(JsonElement root, string name)
                => root.TryGetProperty(name, out var prop) ? (JsonElement?) prop : null;
            string GetPropStr(JsonElement root, string name)
                => root.TryGetProperty(name, out var prop) ? prop.GetString() : null;

            var jd = JsonDocument.Parse(text);
            var root = jd.RootElement;

            var props = new TemplateProperties();

            props.Name = GetPropStr(root, "name");
            props.Description = GetPropStr(root, "description");
            props.Author = GetPropStr(root, "author");
            props.DefaultName = GetPropStr(root, "defaultName");
            props.Identity = GetPropStr(root, "identity");
            props.GroupIdentity = GetPropStr(root, "groupIdentity");
            var tags = GetProp(root, "tags");
            var language = tags == null ? null : GetPropStr(tags.Value, "language");
            props.LanguageTag = language switch
            {
                "C#" => "csharp",
                "F#" => "fsharp",
                "VB" => "visualbasic",
                _ => "csharp"
            };

            return props;
        }
    }
}
