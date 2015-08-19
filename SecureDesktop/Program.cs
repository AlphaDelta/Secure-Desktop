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
            if (args.Length < 1)
            {
                Console.WriteLine("Please specify a file to run");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("The file you specified could not be found");
                return;
            }

            string procline = String.Join(" ", args);

            Taskbar tb = new Taskbar(); //Get this first so that if we crash we wont be stuck in desktop limbo!

            IntPtr hOldDesktop = WinAPI.GetThreadDesktop(WinAPI.GetCurrentThreadId());

            IntPtr hNewDesktop = WinAPI.CreateDesktop("securedesktop",
            IntPtr.Zero, IntPtr.Zero, 0, (uint)WinAPI.DESKTOP_ACCESS.GENERIC_ALL, IntPtr.Zero);

            int ERROR = -1;
            IntPtr hProc = IntPtr.Zero;
            Exception da_ex = null, sd_ex = null;
            try
            {
                WinAPI.SwitchDesktop(hNewDesktop);

                BackgroundWorker bg = new BackgroundWorker();
                DesktopAgent sf = null;
                bg.DoWork += delegate
                {
                    WinAPI.SetThreadDesktop(hNewDesktop);
                    try
                    {
                        WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                        si.lpDesktop = "securedesktop";
                        si.dwFlags |= 0x00000020;
                        WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                        bool cpdone = WinAPI.CreateProcess(null, procline, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                        hProc = pi.hProcess;

                        if (cpdone)
                        {
                            sf = new DesktopAgent(hProc, hNewDesktop, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\", tb);

                            Application.Run(sf);
                            ERROR = sf.ERROR;
                        }
                        else
                        {
                            ERROR = 4;
                        }
                    }
                    catch (Exception e) { ERROR = 2; da_ex = e; }
                    finally { workdone = true; }
                };
                bg.RunWorkerAsync();

                while (!workdone)
                {
                    System.Threading.Thread.Sleep(100);
                    //if(sf != null && !sf.IsDisposed) WinAPI.SetWindowPos(sf.Handle, new IntPtr(-1), tb.Bounds.Left, tb.Bounds.Top, tb.Bounds.Width, tb.Bounds.Height, 0);
                }
            }
            catch (Exception e) { ERROR = 3; sd_ex = e; }
            finally
            {
                WinAPI.SwitchDesktop(hOldDesktop);

                if (hProc != IntPtr.Zero) WinAPI.TerminateProcess(hProc, 0);
                WinAPI.CloseDesktop(hNewDesktop);
            }

            switch (ERROR)
            {
                case 1:
                    MessageBox.Show("The desktop agent could not locate the cleanup binary, it is unsafe to continue to use Secure Desktop until the problem is corrected by redownloading or updating Secure Desktop.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case 2:
                    if (da_ex != null)
                        MessageBox.Show("The desktop agent crashed;\r\n" + da_ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case 3:
                    if (sd_ex != null)
                        MessageBox.Show("Secure Desktop crashed;\r\n" + sd_ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case 4:
                    MessageBox.Show(String.Format("Failed to start process with error code '{0:X8}'", Marshal.GetLastWin32Error()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
    }
}
