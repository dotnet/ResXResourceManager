namespace ResXManager.Tests
{
    using System;
    using System.IO;

    /// <summary>
    /// Based on example on MSDN: https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo
    /// </summary>
    internal static class FileHelper
    {
        public static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(Path.GetFullPath(sourceDirectory));
            var diTarget = new DirectoryInfo(Path.GetFullPath(targetDirectory));

            CopyDirectory(diSource, diTarget);
        }

        public static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
        {
            if (string.Equals(source.FullName, target.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!source.Exists)
            {
                return;
            }

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                CopyDirectory(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
