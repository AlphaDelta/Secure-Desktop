using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cleanup
{
    static class Program
    {
        public static List<ProcessInfo> ProcList = new List<ProcessInfo>();
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
                    //TODO: Rework this to rely on pointers rather than names so as to prevent malicious programs from bypassing cleanup
                    if (flag && proc.szExeFile != "ctfmon.exe" && proc.szExeFile != "Cleanup.exe")
                        //Console.WriteLine("Proc id: " + proc.th32ProcessID + "\nProc name: " + proc.szExeFile);
                        ProcList.Add(new ProcessInfo(proc.th32ProcessID, proc.szExeFile));
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
