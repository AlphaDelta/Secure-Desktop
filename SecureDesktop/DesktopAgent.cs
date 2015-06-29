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
                Gma.UserActivityMonitor.HookManager.KeyUp += KeyboardHookUp;
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
                        Thread.Sleep(100);
                        if (!GetExitCodeProcess(Process, out code) || code != 259) { break; }
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
                //UInt32 arraySize = 120;
                //UInt32 arrayBytesSize = arraySize * sizeof(UInt32);
                //UInt32[] processIds = new UInt32[arraySize];
                //UInt32 bytesCopied;

                //if (!EnumProcesses(processIds, arrayBytesSize, out bytesCopied)) MessageBox.Show("Could not walk processes", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //else
                //{
                //    uint num = bytesCopied >> 2;
                //    for (int i = 0; i < num; i++)
                //        if (MessageBox.Show("Proc id: " + processIds[i], "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.OK) break;
                //}

                //IntPtr snapshot = IntPtr.Zero;
                //snapshot = CreateToolhelp32Snapshot((uint)SnapshotFlags.Process | (uint)SnapshotFlags.Thread, 0);

                //PROCESSENTRY32 proc = new PROCESSENTRY32();
                //proc.dwSize = (UInt32)Marshal.SizeOf(typeof(PROCESSENTRY32));
                //if (Process32First(snapshot, ref proc))
                //{
                //    do
                //    {
                //        if (proc.szExeFile != "notepad++.exe") continue;

                //        StringBuilder sb = new StringBuilder();
                //        sb.AppendLine();
                //        THREADENTRY32 proct = new THREADENTRY32();
                //        proct.dwSize = (UInt32)Marshal.SizeOf(typeof(THREADENTRY32));
                //        if (Thread32First(snapshot, ref proct))
                //        {
                //            do
                //            {
                //                if (proct.th32OwnerProcessID == proc.th32ProcessID) sb.AppendLine(proct.th32ThreadID.ToString() + ":" + GetThreadDesktop(proct.th32ThreadID) + ":" + Marshal.GetLastWin32Error());
                //            } while (Thread32Next(snapshot, ref proct));
                //        }

                //        if (MessageBox.Show("Proc id: " + proc.th32ProcessID + "\nProc name: " + proc.szExeFile + "\nThreads:" + sb.ToString(), "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.OK) break;
                //    } while (Process32Next(snapshot, ref proc));
                //}
                //else
                //{
                //    throw new ApplicationException(string.Format("Failed with win32 error code {0}", Marshal.GetLastWin32Error()));
                //}
                //List<uint> Procs = new List<uint>();

                //THREADENTRY32 proct = new THREADENTRY32();
                //proct.dwSize = (UInt32)Marshal.SizeOf(typeof(THREADENTRY32));
                //if (Thread32First(snapshot, ref proct))
                //{
                //    do
                //    {
                //        if (GetThreadDesktop(proct.th32ThreadID) == Desktop)
                //        {
                //            bool flag = true;
                //            foreach (uint i in Procs) if (i == proct.th32OwnerProcessID) { flag = false; break; }
                //            if (flag) Procs.Add(proct.th32OwnerProcessID);
                //        }
                //    } while (Thread32Next(snapshot, ref proct));
                //}

                ////foreach (uint i in Procs) MessageBox.Show(i.ToString());

                //PROCESSENTRY32 proc = new PROCESSENTRY32();
                //proc.dwSize = (UInt32)Marshal.SizeOf(typeof(PROCESSENTRY32));
                //if (Process32First(snapshot, ref proc))
                //{
                //    do
                //    {
                //        bool flag = false;
                //        foreach (uint i in Procs) if (i == proc.th32ProcessID) { flag = true; break; }
                //        if (flag)
                //            if (MessageBox.Show("Proc id: " + proc.th32ProcessID + "\nProc name: " + proc.szExeFile, "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != System.Windows.Forms.DialogResult.OK) break;
                //    } while (Process32Next(snapshot, ref proc));
                //}
                //else
                //{
                //    throw new ApplicationException(string.Format("Failed with win32 error code {0}", Marshal.GetLastWin32Error()));
                //}

                //CloseHandle(snapshot);
            }
        }

        void KeyboardHookUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) ctrl = false;
        }

        public delegate void Action();
    }
}
