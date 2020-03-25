namespace VSTemplate
{
    public partial class NameDescriptionIcon
    {
        public static implicit operator NameDescriptionIcon(string value) => new NameDescriptionIcon() { Value = value };
    }
}
