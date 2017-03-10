using System;
using System.IO;

namespace WindowsActiveTimeTracker
{
    public class StatFileManager
    {
        private readonly string m_StatFilesDirectory;

        public StatFileManager(string statFilesDirectory)
        {
            m_StatFilesDirectory = statFilesDirectory;
        }

        public string GetFilePath()
        {
            var fileName = DateTime.Now.ToString("yyyy-MM-dd") + "-stats.txt";
            return Path.Combine(m_StatFilesDirectory, fileName);
        }
    }
}
