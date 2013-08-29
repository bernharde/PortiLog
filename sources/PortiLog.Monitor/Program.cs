using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PortiLog.Monitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {

                string logFilename = args[0];
                if (string.IsNullOrEmpty(logFilename))
                    return;

                if (!File.Exists(logFilename))
                    return;

                SelectLogFile(logFilename);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProgramForm());
        }


        public static string LogFileName;

        public static void SelectLogFile(string fileName)
        {
            Program.LogFileName = fileName;

            ReloadRecent();

            Recent.RemoveAll(r => string.Equals(r, fileName, StringComparison.InvariantCultureIgnoreCase));

            Recent.Insert(0, fileName);

            if (Recent.Count > 20)
                Recent.RemoveAt(20);

            Properties.Settings.Default.Filename = Program.LogFileName;
            Properties.Settings.Default.Recent = Recent.ToArray();
            Properties.Settings.Default.Save();
        }

        public static List<string> Recent = new List<string>();

        public static void ReloadRecent()
        {
            Program.Recent.Clear();
            var recent = Properties.Settings.Default.Recent;
            if (recent != null && recent.Length > 0)
                Program.Recent.AddRange(Properties.Settings.Default.Recent);
        }
    }
}