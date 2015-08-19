using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SecureDesktop_GUI
{
    static class Program
    {
        public static string location = "";

        [STAThread]
        static void Main()
        {
            location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!CheckValidity()) return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
        }

        public static bool CheckValidity()
        {
            if (!File.Exists(location + "\\Cleanup.exe"))
            {
                MessageBox.Show("Missing cleanup executable", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!File.Exists(location + "\\SecureDesktop.exe"))
            {
                MessageBox.Show("Missing Secure Desktop executable", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }
    }
}
