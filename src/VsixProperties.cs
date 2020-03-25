using System.Linq;
using NuGet.Packaging;

namespace VSTemplate
{
    public class VsixProperties
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string License { get; set; }
        public string MoreInfo { get; set; }
        public string ReleaseNotes { get; set; }
        public string Icon { get; set; }
        public string PreviewImage { get; set; }
        public string GettingStartedGuide { get; set; }

        public static VsixProperties FromNuspec(IPackageMetadata metadata) => new VsixProperties
        {
            Id = metadata.Id,
            Version = metadata.Version.ToFullString(),
            Publisher = metadata.Authors.FirstOrDefault(),
            Description = metadata.Description,
            DisplayName = metadata.Title,
            Tags = metadata.Tags?.Replace(' ', ';'),

            // License = metadata.License // needs to be copied
            MoreInfo = metadata.Repository?.Url,
            // ReleaseNotes = metadata.ReleaseNotes // needs to be put in a file
            // Icon = metadata.Icon // needs to be copied
            // PreviewImage = metadata.Icon // needs to be copied
        };
    }
}
