using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace VSTemplate
{
    public sealed class NuPackage : IDisposable
    {
        public ZipArchive Archive { get; }
        public IPackageMetadata Metadata { get; }

        private NuPackage(ZipArchive archive, IPackageMetadata metadata)
        {
            Archive = archive;
            Metadata = metadata;
        }

        public void Dispose()
        {
            Archive.Dispose();
        }

        public static async Task<NuPackage> Open(string path)
        {
            var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);
            IPackageMetadata metadata;

            using var fs = File.OpenRead(path);
            using var packageReader = new PackageArchiveReader(fs);
            using var nuspecStream = await packageReader.GetNuspecAsync(CancellationToken.None);

            var manifest = Manifest.ReadFrom(nuspecStream, false);
            metadata = manifest.Metadata;

            return new NuPackage(archive, metadata);
        }
    }
}
