namespace ResXManager.Model
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;

    public abstract class XmlFile
    {
        private static readonly Encoding _utf8WithBom = new UTF8Encoding(true);
        private static readonly Encoding _utf8WithoutBom = new UTF8Encoding(false);

        public Encoding Encoding { get; private set; } = _utf8WithBom;

        public DateTime FileTime { get; private set; }

        public string FilePath { get; }

        public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;

        protected XmlFile(string filePath)
        {
            FilePath = filePath;
        }

        public bool IsBufferOutdated => FileTime != File.GetLastWriteTime(FilePath);

        public bool IsWritable
        {
            get
            {
                try
                {
                    if ((File.GetAttributes(FilePath) & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                        return false;

                    using (File.Open(FilePath, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                return false;
            }
        }

        protected XDocument LoadFromFile()
        {
            using var stream = File.OpenRead(FilePath);
            using var reader = new StreamReader(stream, _utf8WithoutBom, true);
            var result = XDocument.Load(reader);
            Encoding = reader.CurrentEncoding;
            FileTime = File.GetLastWriteTime(FilePath);
            return result;
        }

        protected XDocument? TryLoadFromFile()
        {
            try
            {
                if (File.Exists(FilePath))
                    return LoadFromFile();
            }
            catch
            {
                // load failed, just return null.
            }

            return null;
        }

        protected void SaveToFile(XDocument document)
        {
            using (var stream = File.Open(FilePath, FileMode.Create))
            {
                using var writer = new StreamWriter(stream, Encoding);
                document.Save(writer);
            }

            FileTime = File.GetLastWriteTime(FilePath);
        }
    }
}