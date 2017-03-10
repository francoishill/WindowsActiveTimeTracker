using System;
using System.IO;

namespace WindowsActiveTimeTracker
{
    class Program
    {
        private static readonly TimeSpan m_TimeResolution = TimeSpan.FromSeconds(3);
        private static string m_StatsFilePath;


        static void Main(string[] args)
        {
            m_StatsFilePath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "WindowsActiveTimeTracker",
               "stats.json");

            var recorder = new StatsRecorder(m_TimeResolution, m_StatsFilePath);

            try
            {
                recorder.Initialize();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Init failed with exception: {exception.Message}. Stack: {exception.StackTrace}");
                Environment.Exit(1);
            }

            recorder.StartRecording();
        }
    }
}
