using System.IO.Compression;

namespace MVD.Util
{
    public static class BZipUnpacker
    {
        public static void Unpack(string filename, string targetDir)
        {
            ZipFile.ExtractToDirectory(filename, targetDir, true);
        }
    }
}
