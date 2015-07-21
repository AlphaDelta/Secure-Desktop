using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SecureDesktop
{
    class Program
    {
        static volatile bool workdone = false;
        static void Main(string[] args)
        {
            Taskbar tb = new Taskbar(); //Get this first so that if we crash we wont be stuck in desktop limbo!

            IntPtr hOldDesktop = WinAPI.GetThreadDesktop(WinAPI.GetCurrentThreadId());

            IntPtr hNewDesktop = WinAPI.CreateDesktop("securedesktop",
            IntPtr.Zero, IntPtr.Zero, 0, (uint)WinAPI.DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);

            WinAPI.SwitchDesktop(hNewDesktop);

            IntPtr hProc = IntPtr.Zero;
            BackgroundWorker bg = new BackgroundWorker();
            int ERROR = -1;
            DesktopAgent sf = null;
            bg.DoWork += delegate
            {
                WinAPI.SetThreadDesktop(hNewDesktop);

                WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                si.lpDesktop = "securedesktop";
                si.dwFlags |= 0x00000020;
                WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                //CreateProcess(null, @"C:\Program Files (x86)\Notepad++\notepad++.exe -nosession -notabbar C:\Windows\System32\drivers\etc\hosts", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                WinAPI.CreateProcess(null, @"C:\Windows\notepad.exe", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                //WinAPI.CreateProcess(null, @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe -no-remote -private ""about:blank""", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                hProc = pi.hProcess;

                sf = new DesktopAgent(hProc, hNewDesktop, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\", tb);
                //sf.FormClosing += (sender, e) => { passwd = passwordTextBox.Text; };

                Application.Run(sf);
                ERROR = sf.ERROR;
                workdone = true;
            };
            bg.RunWorkerAsync();

            while (!workdone)
            {
                System.Threading.Thread.Sleep(100);
                //if(sf != null && !sf.IsDisposed) WinAPI.SetWindowPos(sf.Handle, new IntPtr(-1), tb.Bounds.Left, tb.Bounds.Top, tb.Bounds.Width, tb.Bounds.Height, 0);
            }

            WinAPI.SwitchDesktop(hOldDesktop);

            if (hProc != IntPtr.Zero) WinAPI.TerminateProcess(hProc, 0);
            WinAPI.CloseDesktop(hNewDesktop);

            switch (ERROR)
            {
                case 1:
                    MessageBox.Show("The desktop agent could not locate the cleanup binary, it is unsafe to continue to use Secure Desktop until the problem is corrected by redownloading or updating Secure Desktop.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
    }
}
