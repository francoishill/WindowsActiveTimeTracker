using System;
using System.IO;

namespace WindowsActiveTimeTracker
{
    class Program
    {
        private static readonly TimeSpan m_TimeResolution = TimeSpan.FromSeconds(3);
        private static string m_StatFilesDirectory;


        static void Main(string[] args)
        {
            m_StatFilesDirectory = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "WindowsActiveTimeTracker");

            var recorder = new StatsRecorder(m_TimeResolution, m_StatFilesDirectory);

            recorder.StartRecording();
        }
    }
}
