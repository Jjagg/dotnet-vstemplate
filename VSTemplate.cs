using System.IO;
using System.Xml.Serialization;

namespace VSTemplate
{
    public partial class VSTemplate
    {
        public bool Write(string path, bool force = false)
        {
            using var fs = File.Create(path);
            return Write(fs, force);
        }

        public bool Write(Stream stream, bool force = false)
        {
            var serializer = new XmlSerializer(typeof(VSTemplate));
            var filemode = force ? FileMode.Create : FileMode.CreateNew;

            try
            {
                serializer.Serialize(stream, this);
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
    }
}
