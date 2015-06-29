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

namespace SecureDesktop
{
    public partial class DesktopAgent : Form
    {
        IntPtr Desktop = IntPtr.Zero;
        string Bin = "";
        public int ERROR = -1;
        public DesktopAgent(IntPtr Process, IntPtr Desktop, string location)
        {
            Bin = location;
            if (Process == IntPtr.Zero) this.Close();
            if (Desktop == IntPtr.Zero) this.Close();
            this.Desktop = Desktop;
            InitializeComponent();

            CenterToScreen();

            Gma.UserActivityMonitor.HookManager.KeyDown += KeyboardHookDown;
            Gma.UserActivityMonitor.HookManager.KeyUp += KeyboardHookUp;

            FormClosing += delegate
            {
                Gma.UserActivityMonitor.HookManager.KeyDown -= KeyboardHookDown;
                Gma.UserActivityMonitor.HookManager.KeyUp -= KeyboardHookUp;
            };

            this.Opacity = 0;

            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += delegate
            {
                uint code = 1;
                try
                {
                    while (!this.IsDisposed)
                    {
                        Thread.Sleep(500);
                        if (!WinAPI.GetExitCodeProcess(Process, out code) || code != 259) { break; }
                    }
                }
                catch { }

                if (File.Exists(Bin + ""))
                {
                    IntPtr hProc = IntPtr.Zero;

                    WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                    si.lpDesktop = "securedesktop";
                    si.dwFlags |= 0x00000020;
                    WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                    WinAPI.CreateProcess(null, @"C:\Windows\notepad.exe", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                    hProc = pi.hProcess;

                    try
                    {
                        while (!this.IsDisposed)
                        {
                            Thread.Sleep(500);
                            if (!WinAPI.GetExitCodeProcess(Process, out code) || code != 259) { break; }
                        }
                    }
                    catch { }
                }
                else ERROR = 1;
                if(!this.IsDisposed) this.Invoke((Action)delegate { this.Close(); });
            };
            bg.RunWorkerAsync();
        }

        bool ctrl = false;
        void KeyboardHookDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) ctrl = true;
            else if (e.KeyCode == Keys.K && ctrl) this.Close();
            else if (e.KeyCode == Keys.E && ctrl)
            {
                MessageBox.Show("Desktop handle: " + Desktop.ToString());
            }
        }

        void KeyboardHookUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) ctrl = false;
        }

        public delegate void Action();
    }
}
