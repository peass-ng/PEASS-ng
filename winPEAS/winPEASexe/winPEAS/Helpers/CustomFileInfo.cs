namespace winPEAS.Helpers
{
    internal class CustomFileInfo
    {
        public string Filename { get; }
        public string Extension { get; }
        public string FullPath { get; }
        public long Size { get; }
        public bool IsDirectory { get; }

        public CustomFileInfo(string filename, string extension, string fullPath, long size, bool isDirectory)
        {
            Filename = filename;
            Extension = extension;
            FullPath = fullPath;
            Size = size;
            IsDirectory = isDirectory;
        }
    }
}
