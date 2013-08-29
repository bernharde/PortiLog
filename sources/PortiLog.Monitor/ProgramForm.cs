using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;

namespace PortiLog.Monitor
{
    public partial class ProgramForm : Form
    {
        long position = 0;

        public ProgramForm()
        {
            InitializeComponent();

            try
            {
                this.Bounds = Properties.Settings.Default.FormBounds;
                this.WindowState = Properties.Settings.Default.WindowsState;
                Program.LogFileName = Properties.Settings.Default.Filename;

                Program.ReloadRecent();
            }
            catch// (Exception ex)
            {
                // i had some exceptions here..
            }

        }

        void UpdateRecentMenuItems()
        {
            btnRecent.DropDownItems.Clear();

            int i = 0;
            foreach (var recent in Program.Recent)
            {
                i++;
                ToolStripMenuItem item = new ToolStripMenuItem(string.Format("{0} {1}", i, recent));
                item.Tag = recent;
                item.Click += item_Click;
                btnRecent.DropDownItems.Add(item);
            }
        }

        void item_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var recent = (string)item.Tag;

            Program.SelectLogFile(recent);
            UpdateRecentMenuItems();
            UpdateView();
        }

        Exception _lastException;

        bool UpdateView()
        {
            string filename = Program.LogFileName;
            if (string.IsNullOrEmpty(filename))
                return false;

            this.Text = filename + " - PortiLog.Monitor";

            try
            {
                using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (position == stream.Length)
                    {
                        return true;
                    }
                    else if (position > stream.Length)
                    {
                        this.txtFile.Text = "";
                        position = 0;
                    }

                    stream.Position = position;

                    using (StreamReader s = new StreamReader(stream))
                    {
                        string logtext = s.ReadToEnd();
                        int logtextLength = logtext.Length;
                        logtext = logtext.TrimEnd((char)20);

                        this.txtFile.AppendText(logtext);
                       
                        position = stream.Position;

                        Application.DoEvents();
                    }
                }

                // reset last exception
                _lastException = null;
                lblStatus.Text = "Ok";

                return true;
            }
            catch (Exception ex)
            {
                // only write an exception once
                if (_lastException == null || ex.GetType() != _lastException.GetType())
                {
                    lblStatus.Text = string.Format("Error occured: {0}\r\n", ex.Message);
                }
                _lastException = ex;
                return false;
            }
        }

        void ProgramForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                if(this.WindowState == FormWindowState.Normal)
                    Properties.Settings.Default.FormBounds = this.Bounds;
                Properties.Settings.Default.WindowsState = this.WindowState;
                Properties.Settings.Default.Save();
            }
            catch //(Exception ex)
            {
            }
        }

        void reloadToolStripButton_Click(object sender, EventArgs e)
        {
            this.txtFile.Text = "";
            this.position = 0;
            UpdateView();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            UpdateView();
        }

        void btnStartStopUpdates_Click(object sender, EventArgs e)
        {
            if (btnAutoUpdates.Checked)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }

            btnAutoUpdates.Checked = !btnAutoUpdates.Checked;
        }

        void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFile();
        }


        void btnOpenInPackages_Click(object sender, EventArgs e)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var packages = Path.Combine(appData, "packages");
            openFileDialog.InitialDirectory = packages;
            OpenFile();
        }

        void OpenFile()
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenFile(openFileDialog.FileName);
            }
        }

        void OpenFile(string filename)
        {
            Program.SelectLogFile(filename);

            txtFile.Text = string.Empty;

            UpdateRecentMenuItems();
            UpdateView();
        }

        void timerStartup_Tick(object sender, EventArgs e)
        {
            timerStartup.Stop();

            UpdateRecentMenuItems();
            UpdateView();
        }

        void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtFile.Copy();
        }

        void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("https://portilog.codeplex.com/");
            Process.Start(sInfo);
        }

        void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}