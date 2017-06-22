using Microsoft.Owin.FileSystems;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class ETagProvider
    {
        private IFileSystem fileSystem;
        public ETagProvider(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }
        public async Task<string> GetETag(IFileInfo fileInfo)
        {
            IFileInfo crcFileInfo;

            if (fileSystem.TryGetFileInfo($"{fileInfo.PhysicalPath}.crc", out crcFileInfo))
            {
                using (Stream stream = crcFileInfo.CreateReadStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var line = await reader.ReadLineAsync();

                        return line;
                    }
                }
            }
            return null;
        }
    }
}
