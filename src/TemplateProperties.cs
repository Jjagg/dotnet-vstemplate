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
            var jd = JsonDocument.Parse(text);
            var root = jd.RootElement;

            var props = new TemplateProperties();

            props.Name = root.GetProperty("name").GetString();
            props.Description = root.GetProperty("description").GetString();
            props.Author = root.GetProperty("author").GetString();
            props.DefaultName = root.GetProperty("defaultName").GetString();
            props.Identity = root.GetProperty("identity").GetString();
            props.GroupIdentity = root.GetProperty("groupIdentity").GetString();
            var language = root.GetProperty("tags").GetProperty("language").GetString();
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
