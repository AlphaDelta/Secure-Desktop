using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SecureDesktop
{
    public partial class DesktopAgent : Form
    {
        IntPtr Desktop = IntPtr.Zero;
        public DesktopAgent(IntPtr Process, IntPtr Desktop)
        {
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
                try
                {
                    while (!this.IsDisposed)
                    {
                        Thread.Sleep(500);
                        if (!WinAPI.GetExitCodeProcess(Process, out code) || code != 259) { break; }
                    }
                }
                catch { }
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
