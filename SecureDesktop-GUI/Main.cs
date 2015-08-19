using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SecureDesktop_GUI
{
    public partial class Main : Form
    {
        BackgroundWorker bg = new BackgroundWorker();
        Stopwatch stop = new Stopwatch();
        public Main()
        {
            InitializeComponent();

            this.Icon = Properties.Resources.icon;

            this.AllowDrop = true;
            this.DragEnter += delegate(object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;

                    LoadFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0], true);
                }
            };
            this.DragLeave += delegate { UnloadFile(); };
            this.DragDrop += delegate(object sender, DragEventArgs e)
            {
                LoadFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
            };

            bg.DoWork += delegate
            {
                IntPtr hProc = IntPtr.Zero;



                WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                WinAPI.CreateProcess(null, String.Format("\"{0}\" \"{1}\"", Program.location + "\\SecureDesktop.exe", fileloc), IntPtr.Zero, IntPtr.Zero, false, WinAPI.CREATE_NO_WINDOW, IntPtr.Zero, null, ref si, out pi);
                hProc = pi.hProcess;
                //if (!this.IsDisposed) this.Invoke((Action)delegate { this.Close(); });
                try
                {
                    uint code;
                    while (!this.IsDisposed)
                    {
                        Thread.Sleep(500);
                        if (!WinAPI.GetExitCodeProcess(hProc, out code) || code != 259) { break; }
                    }
                }
                catch { }
            };
            bg.RunWorkerCompleted += delegate
            {
                btnRun.Enabled = true;
                stop.Stop();

                labelTime.Text = String.Format("Secure Desktop ran for {0:0}m{1:0}s", stop.Elapsed.TotalMinutes, stop.Elapsed.Seconds);
            };
        }

        Image GetIcon(string file)
        {
            Icon ficon;
            WinAPI.SHFILEINFO shinfo = new WinAPI.SHFILEINFO();
            IntPtr ptr = WinAPI.SHGetFileInfo(file, WinAPI.FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), WinAPI.SHGFI_SYSICONINDEX);
            if (ptr == IntPtr.Zero) ficon = Icon.ExtractAssociatedIcon(file);
            else
            {
                int iconIndex = shinfo.iIcon;
                Guid iImageListGuid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
                WinAPI.IImageList iml;
                int hres = WinAPI.SHGetImageList(0x04, ref iImageListGuid, out iml);
                IntPtr hIcon = IntPtr.Zero;
                hres = iml.GetIcon(iconIndex, 1, ref hIcon);
                ficon = System.Drawing.Icon.FromHandle(hIcon);
            }

            Image img;
            Icon temp = new Icon(ficon, 128, 128);
            img = temp.ToBitmap();
            temp.Dispose();
            ficon.Dispose();
            return img;
        }

        Image oldicon = Properties.Resources.icon128_gray;
        string oldfile = "Please select or drop a file", olddate = "2015-01-01";
        string fileloc = "";
        void LoadFile(string file, bool preview = false)
        {
            icon.Image = GetIcon(file);

            labelFile.Text = Path.GetFileName(file);
            FileInfo info = new FileInfo(file);
            labelDate.Text = info.LastWriteTime.ToString("yyyy-MM-dd");

            if (!preview)
            {
                labelTime.Text = "Waiting to run...";

                oldicon = icon.Image;
                oldfile = labelFile.Text;
                olddate = labelDate.Text;

                fileloc = file;
                btnRun.Enabled = true;
            }
        }

        void UnloadFile()
        {
            icon.Image = oldicon;
            labelFile.Text = oldfile;
            labelDate.Text = olddate;

            //btnRun.Enabled = false;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!File.Exists(fileloc))
            {
                MessageBox.Show("The selected file no longer exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Program.CheckValidity()) return;

            btnRun.Enabled = false;
            stop.Start();
            bg.RunWorkerAsync();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuFileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) { ofd.Dispose(); return; }

            LoadFile(ofd.FileName);

            ofd.Dispose();
        }

        private void menuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuHelpAbout_Click(object sender, EventArgs e)
        {
            new About().ShowDialog(); //It should automatically dispose on close
        }
    }
}
