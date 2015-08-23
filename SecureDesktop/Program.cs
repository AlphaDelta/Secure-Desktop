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

            StringBuilder sb = new StringBuilder();
            string procline = String.Format("\"{0}\"", String.Join("\" \"", args));
            string ext = Path.GetExtension(args[0]).ToLower();

            //if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
            {
                int i = 0;
                for (; i < 10; i++)
                {
                    if (ext == ".dll")
                    {
                        procline = String.Format("{0} {1}", @"rundll32", procline);
                        break;
                    }
                    else if (ext != ".exe")
                    {
                        string file = "";
                        if (!ResolveExtension(ext, ref file)) break;
                        procline = String.Format("\"{0}\" {1}", file, procline);
                        ext = Path.GetExtension(file).ToLower();
                    }
                    else break;
                }
                if (i == 10)
                {
                    Console.WriteLine("Could not locate default program");
                    return;
                }
            }

            /* Entropy collection */
            int[] entropy = new int[ISAAC.SIZE];
            int ei = 0;

            WinAPI.MEMORYSTATUSEX memStatus = new WinAPI.MEMORYSTATUSEX();
            if (WinAPI.GlobalMemoryStatusEx(memStatus))
            {
                entropy[0] = (int)memStatus.ullAvailPhys;
                entropy[1] = (int)memStatus.ullAvailVirtual;
                entropy[2] = (int)memStatus.ullAvailPageFile;
                ei = 2;
            }

            WinAPI.POINT pt;
            if (WinAPI.GetCursorPos(out pt))
            {
                entropy[ei + 1] = pt.X;
                entropy[ei + 2] = pt.Y;
                ei += 2;
            }

            uint spc, bps, nofc, tnoc;
            if (WinAPI.GetDiskFreeSpace(null, out spc, out bps, out nofc, out tnoc))
            {
                entropy[ei + 1] = (int)spc;
                entropy[ei + 2] = (int)bps;
                entropy[ei + 3] = (int)nofc;
                entropy[ei + 4] = (int)tnoc;
                ei += 4;
            }

            ISAAC csprng = new ISAAC(entropy);

            for (int i = 0; i < 3; i++) csprng.Isaac();

            StringBuilder desktopname = new StringBuilder(16);
            const int min = 0x61;
            const int max = 0x7A;
            const int diff = max - min;
            for (int i = 0; i < 16; i++)
                desktopname.Append(
                    (char)(((int)Math.Abs(csprng.rsl[i]) % diff) + min)
                    );

            string dname = desktopname.ToString();

            Taskbar tb = new Taskbar(); //Get this first so that if we crash we wont be stuck in desktop limbo!

            IntPtr hOldDesktop = WinAPI.GetThreadDesktop(WinAPI.GetCurrentThreadId());

            IntPtr hNewDesktop = WinAPI.CreateDesktop(dname,
            IntPtr.Zero, IntPtr.Zero, 0, (uint)WinAPI.DESKTOP_ACCESS.CUSTOM_SECURE, IntPtr.Zero);

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
                        si.lpDesktop = dname;
                        si.dwFlags |= 0x00000020;
                        WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                        bool cpdone = WinAPI.CreateProcess(null, procline, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                        hProc = pi.hProcess;

                        if (cpdone)
                        {
                            sf = new DesktopAgent(hProc, hNewDesktop, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\", tb, dname);

                            Application.Run(sf);
                            ERROR = sf.ERROR;
                        }
                        else
                            ERROR = 4;
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

        static bool ResolveExtension(string ext, ref string def)
        {

            uint length = 0;
            uint ret = WinAPI.AssocQueryString(WinAPI.AssocF.None, WinAPI.AssocStr.Executable, ext, null, null, ref length);
            if (ret == WinAPI.S_FALSE)
            {
                StringBuilder sb = new StringBuilder((int)length);
                ret = WinAPI.AssocQueryString(WinAPI.AssocF.None, WinAPI.AssocStr.Executable, ext, null, sb, ref length);
                if (ret == WinAPI.S_OK)
                {
                    def = sb.ToString();
                    return true;
                }
            }
            return false;
        }
    }
}
