using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopAgent
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F,
            NoHeaps = 0x40000000
        }
        //inner struct used only internally
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            const int MAX_PATH = 260;
            internal UInt32 dwSize;
            internal UInt32 cntUsage;
            internal UInt32 th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal UInt32 th32ModuleID;
            internal UInt32 cntThreads;
            internal UInt32 th32ParentProcessID;
            internal Int32 pcPriClassBase;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct THREADENTRY32
        {
            internal UInt32 dwSize;
            internal UInt32 cntUsage;
            internal UInt32 th32ThreadID;
            internal UInt32 th32OwnerProcessID;
            internal UInt32 tpBasePri;
            internal UInt32 tpDeltaPri;
            internal UInt32 dwFlags;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr CreateToolhelp32Snapshot([In]UInt32 dwFlags, [In]UInt32 th32ProcessID);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32First([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32Next([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

        [DllImport("kernel32.dll")]
        static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        static void Main(string[] args)
        {
            Console.Write("Desktop handle (" + GetThreadDesktop(GetCurrentThreadId()) + "): ");
            IntPtr Desktop = (IntPtr)uint.Parse(Console.ReadLine());

            IntPtr snapshot = IntPtr.Zero;
            snapshot = CreateToolhelp32Snapshot((uint)SnapshotFlags.Process | (uint)SnapshotFlags.Thread, 0);
            List<uint> Procs = new List<uint>();

            THREADENTRY32 proct = new THREADENTRY32();
            proct.dwSize = (UInt32)Marshal.SizeOf(typeof(THREADENTRY32));
            if (Thread32First(snapshot, ref proct))
            {
                do
                {
                    if (GetThreadDesktop(proct.th32ThreadID) == Desktop)
                    {
                        bool flag = true;
                        foreach (uint i in Procs) if (i == proct.th32OwnerProcessID) { flag = false; break; }
                        if (flag) Procs.Add(proct.th32OwnerProcessID);
                    }
                } while (Thread32Next(snapshot, ref proct));
            }

            //foreach (uint i in Procs) MessageBox.Show(i.ToString());

            PROCESSENTRY32 proc = new PROCESSENTRY32();
            proc.dwSize = (UInt32)Marshal.SizeOf(typeof(PROCESSENTRY32));
            if (Process32First(snapshot, ref proc))
            {
                do
                {
                    bool flag = false;
                    foreach (uint i in Procs) if (i == proc.th32ProcessID) { flag = true; break; }
                    if (flag)
                        Console.WriteLine("Proc id: " + proc.th32ProcessID + "\nProc name: " + proc.szExeFile);
                } while (Process32Next(snapshot, ref proc));
            }
            else
            {
                throw new ApplicationException(string.Format("Failed with win32 error code {0}", Marshal.GetLastWin32Error()));
            }

            CloseHandle(snapshot);

            Console.ReadKey();
        }
    }
}
