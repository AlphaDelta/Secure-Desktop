using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cleanup
{
    static class Program
    {
        public static List<ProcessInfo>
            ProcList = new List<ProcessInfo>(),
            AutoProcList = new List<ProcessInfo>();
        public static bool ViewOnly = false;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                MessageBox.Show("This program is a necessary part of SecureDesktop and is dangerous for it to be run by a user, didn't your parents teach you not to run random executables?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ViewOnly = (args[0] == "-view");

            IntPtr Desktop = WinAPI.GetThreadDesktop(WinAPI.GetCurrentThreadId());

            IntPtr snapshot = IntPtr.Zero;
            snapshot = WinAPI.CreateToolhelp32Snapshot((uint)WinAPI.SnapshotFlags.Process | (uint)WinAPI.SnapshotFlags.Thread, 0);
            List<uint> Procs = new List<uint>();

            WinAPI.THREADENTRY32 proct = new WinAPI.THREADENTRY32();
            proct.dwSize = (UInt32)Marshal.SizeOf(typeof(WinAPI.THREADENTRY32));
            if (WinAPI.Thread32First(snapshot, ref proct))
            {
                do
                {
                    if (WinAPI.GetThreadDesktop(proct.th32ThreadID) == Desktop)
                    {
                        bool flag = true;
                        foreach (uint i in Procs) if (i == proct.th32OwnerProcessID) { flag = false; break; }
                        if (flag) Procs.Add(proct.th32OwnerProcessID);
                    }
                } while (WinAPI.Thread32Next(snapshot, ref proct));
            }

            WinAPI.PROCESSENTRY32 proc = new WinAPI.PROCESSENTRY32();
            proc.dwSize = (UInt32)Marshal.SizeOf(typeof(WinAPI.PROCESSENTRY32));
            if (WinAPI.Process32First(snapshot, ref proc))
            {
                do
                {
                    bool flag = false;
                    foreach (uint i in Procs) if (i == proc.th32ProcessID) { flag = true; break; }
                    //TODO: Check if ctfmon.exe closes correctly, if not remove it from the filter
                    //   -  It does not close correctly on Windows 8, check on Windows 7 later. Possibly due to the lack of DESKTOP_ENUMERATE?
                    //TODO: Rework this to rely on pointers rather than names so as to prevent malicious programs from bypassing cleanup
                    if (flag)
                    {
                        string name = proc.szExeFile.ToLower();
                        if (name == "ctfmon.exe" || name == "jpnime.exe")
                            AutoProcList.Add(new ProcessInfo(proc.th32ProcessID, proc.szExeFile));
                        else if (name != "cleanup.exe")
                            ProcList.Add(new ProcessInfo(proc.th32ProcessID, proc.szExeFile));
                    }
                } while (WinAPI.Process32Next(snapshot, ref proc));
            }
            else
            {
                throw new ApplicationException(string.Format("Failed with win32 error code {0}", Marshal.GetLastWin32Error()));
            }

            WinAPI.CloseHandle(snapshot);

            if (ProcList.Count > 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            }
            if (AutoProcList.Count > 0)
            {
                uint code;
                foreach (ProcessInfo info in Program.AutoProcList)
                {
                    IntPtr handle = WinAPI.OpenProcess(WinAPI.ProcessAccessFlags.Terminate | WinAPI.ProcessAccessFlags.QueryInformation, false, (int)info.ID);
                    if (handle == null || handle == IntPtr.Zero)
                    {
                        MessageBox.Show("Could not open handle for " + info.Name + ", please manually terminate this process before continuing", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    if (WinAPI.GetExitCodeProcess(handle, out code) && code == 259)
                        WinAPI.TerminateProcess(handle, 1);
                }
            }
        }
    }

    public class ProcessInfo
    {
        public uint ID;
        public string Name;

        public ProcessInfo(uint ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;
        }
    }
}
