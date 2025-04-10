using System;
using System.IO;
using System.Text;

namespace System.IO
{
    public static class DirectoryEx
    {
        public static string[] GetFilesIncludingSubDirs(string path)
        {
            var files = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            foreach (var dir in dirs)
            {
                var filesInDir = GetFilesIncludingSubDirs(dir);

                var tempFiles = new string[files.Length + filesInDir.Length];
                files.CopyTo(tempFiles, 0);
                filesInDir.CopyTo(tempFiles, files.Length);

                files = tempFiles;
            }

            return files;
        }
    }
}