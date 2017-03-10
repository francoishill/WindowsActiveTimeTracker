using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsActiveTimeTracker
{
    public class StatsRecorder
    {
        private readonly TimeSpan m_TimeResolution;
        private readonly StatFileManager m_StatFileManager;
        private const string FileSplitToken = "[===]";

        private readonly Dictionary<string, TimeSpan> m_ApplicationStats;
        private bool m_BusySaving;
        private string m_LastStatsFilePath;

        public StatsRecorder(TimeSpan timeResolution, string statFilesDirectory)
        {
            m_TimeResolution = timeResolution;
            m_StatFileManager = new StatFileManager(statFilesDirectory);

            m_ApplicationStats = new Dictionary<string, TimeSpan>();
        }

        public void StartRecording()
        {
            while (true)
            {
                try
                {
                    RecordCurrentWindowStat();
                    Console.Out.WriteLine($"Successfully recorded current stat");
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Failed to record current stat, exception: {exception.Message}. Stack: {exception.StackTrace}");
                }
                Thread.Sleep(m_TimeResolution);
            }
        }

        private void RecordCurrentWindowStat()
        {
            var lastInputTime = GetLastInputTime();
            if (lastInputTime > 30)
            {
                Console.Out.WriteLine($"Last (user) input time in seconds is {lastInputTime}, skip recording stat.");
                return;
            }

            var windowTitle = GetForegroundWindowTitle().Trim();

            var currentStatsFilePath = m_StatFileManager.GetFilePath();
            if (currentStatsFilePath != m_LastStatsFilePath)
            {
                //The file path either changes or it is on startup
                Console.Out.WriteLine($"Stats file path changed so reloading. Current filepath is '{currentStatsFilePath}' and previous/last filepath is '{m_LastStatsFilePath ?? ""}'");
                ReloadFromFileLines();
            }

            if (!m_ApplicationStats.ContainsKey(windowTitle))
            {
                m_ApplicationStats.Add(windowTitle, TimeSpan.FromMilliseconds(0));
            }
            m_ApplicationStats[windowTitle] = m_ApplicationStats[windowTitle] + m_TimeResolution;

            SaveFileAsync();
        }

        private void ReloadFromFileLines()
        {
            m_ApplicationStats.Clear();

            var statsFilePath = m_StatFileManager.GetFilePath();
            ReadFileLines(statsFilePath)
                .ToList()
                .ForEach(stat =>
                {
                    m_ApplicationStats.Add(stat.Title, TimeSpan.FromSeconds(stat.Seconds));
                });
            m_LastStatsFilePath = statsFilePath;
        }

        private FileLine[] ReadFileLines(string statsFilePath)
        {
            if (!File.Exists(statsFilePath))
            {
                return new FileLine[] { };
            }

            return File.ReadLines(statsFilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(FileLine.FromString)
                .Where(fileLine => !string.IsNullOrWhiteSpace(fileLine.Title))
                .ToArray();
        }

        private async void SaveFileAsync()
        {
            await Task.Run(() => SaveFile());
        }

        private void SaveFile()
        {
            if (m_BusySaving) return;
            m_BusySaving = true;

            try
            {
                var statFilePath = m_StatFileManager.GetFilePath();

                var lines = m_ApplicationStats
                    .Select(kv => new FileLine(kv.Key, kv.Value.TotalSeconds).ToStringLine());

                var parentDir = Path.GetDirectoryName(statFilePath);
                Directory.CreateDirectory(parentDir);

                File.WriteAllLines(statFilePath, lines);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to save file, exception: {exception.Message}. Stack: {exception.StackTrace}");
            }
            finally
            {
                m_BusySaving = false;
            }
        }

        private string GetForegroundWindowTitle()
        {
            var hwnd = WindowsDLLs.GetForegroundWindow();
            var titleLength = WindowsDLLs.GetWindowTextLength(hwnd);
            var titleSB = new StringBuilder(titleLength + 1);
            WindowsDLLs.GetWindowText(hwnd, titleSB, titleSB.Capacity);
            return titleSB.ToString();
        }

        private uint GetLastInputTime()
        {
            uint idleTime = 0;
            var lastInputInfo = new WindowsDLLs.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            var envTicks = (uint)Environment.TickCount;

            if (WindowsDLLs.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        private class FileLine
        {
            public string Title;
            public double Seconds;

            public FileLine(string title, double seconds)
            {
                Title = title;
                Seconds = seconds;
            }

            public static FileLine FromString(string line)
            {
                var indexSplitToken = line.IndexOf(FileSplitToken);
                if (indexSplitToken == -1)
                {
                    throw new Exception($"Unable to find split token '{FileSplitToken}' in line '{line}'");
                }

                var title = line.Substring(0, indexSplitToken).Trim();
                var secondsStr = line.Substring(indexSplitToken + 1);

                double parsedSeconds;
                if (!double.TryParse(line.Substring(indexSplitToken + FileSplitToken.Length), out parsedSeconds))
                {
                    throw new Exception($"Unable to parse seconds '{secondsStr}' as double.");
                }

                return new FileLine(title, parsedSeconds);
            }

            public string ToStringLine()
            {
                return $"{Title}{FileSplitToken}{Seconds}";
            }
        }
    }
}
