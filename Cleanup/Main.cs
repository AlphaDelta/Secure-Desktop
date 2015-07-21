using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Cleanup
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();

            foreach (ProcessInfo proc in Program.ProcList)
                listBox1.Items.Add(proc.Name);

            if (Program.ViewOnly)
            {
                btnTerminate.Text = "Ok";
                btnTerminate.Enabled = true;
                label1.Text = "Processes open in this desktop:";
                return;
            }

            Timer t = new Timer();
            int time = 5;
            btnTerminate.Text = time.ToString();
            t.Tick += delegate
            {
                time--;

                if (time < 1)
                {
                    btnTerminate.Text = "Terminate";
                    btnTerminate.Enabled = true;
                    t.Stop();
                }
                else
                    btnTerminate.Text = time.ToString();
            };
            t.Interval = 1000;
            t.Start();

            WinAPI.FLASHWINFO fInfo = new WinAPI.FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = this.Handle;
            fInfo.dwFlags = WinAPI.FLASHW_CAPTION;
            fInfo.uCount = 6;
            fInfo.dwTimeout = 50;

            WinAPI.FlashWindowEx(ref fInfo);
        }

        private void btnTerminate_Click(object sender, EventArgs e)
        {
            if (Program.ViewOnly)
            {
                this.Close();
                return;
            }

            if (MessageBox.Show("Are you sure you would like to terminate these processes?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != System.Windows.Forms.DialogResult.Yes)
                return;

            uint code;
            foreach (ProcessInfo info in Program.ProcList)
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

            this.Close();
        }
    }
}
