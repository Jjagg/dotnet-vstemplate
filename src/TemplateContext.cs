namespace VSTemplate
{
    public class TemplateContext
    {
        public string TemplateJsonContent { get; }
        public TemplateProperties TemplateJsonProps { get; }
        public VSTemplate VSTemplate { get; }

        public TemplateContext(string templateJsonContent, TemplateProperties templateJsonProps, VSTemplate vsTemplate)
        {
            TemplateJsonContent = templateJsonContent;
            TemplateJsonProps = templateJsonProps;
            VSTemplate = vsTemplate;
        }
    }
}
