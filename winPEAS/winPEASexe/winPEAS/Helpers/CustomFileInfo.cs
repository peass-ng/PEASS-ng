namespace winPEAS.Helpers
{
    internal class CustomFileInfo
    {
        public string Filename { get; set; }
        public string Extension { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get { return string.IsNullOrEmpty(Filename); } }

        public CustomFileInfo(string filename, string extension, string fullPath)
        {
            Filename = filename;
            Extension = extension;
            FullPath = fullPath;
        }
    }
}
