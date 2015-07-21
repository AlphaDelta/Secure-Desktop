using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SecureDesktop
{
    public partial class DesktopAgent : Form
    {
        IntPtr Desktop = IntPtr.Zero;
        string cleanup = "";
        public int ERROR = -1;
        Taskbar tb;
        public DesktopAgent(IntPtr Process, IntPtr Desktop, string location, Taskbar tb)
        {
            cleanup = location + "cleanup.exe";
            if (Process == IntPtr.Zero) this.Close();
            if (Desktop == IntPtr.Zero) this.Close();
            this.Desktop = Desktop;
            InitializeComponent();

            //CenterToScreen();
            this.FormBorderStyle = FormBorderStyle.None;

            //this.TopMost = true;
            //this.TopLevel = true;
            this.ShowInTaskbar = false;

            this.tb = tb;
            //Console.WriteLine("{0}:{1}, {2}:{3}", tb.Bounds.Left, tb.Bounds.Top, tb.Bounds.Width, tb.Bounds.Height);
            //this.Left = tb.Bounds.Left;
            //SetSize(tb.Bounds.Width, tb.Bounds.Height);

            Gma.UserActivityMonitor.HookManager.KeyDown += KeyboardHookDown;
            Gma.UserActivityMonitor.HookManager.KeyUp += KeyboardHookUp;

            FormClosing += delegate
            {
                Gma.UserActivityMonitor.HookManager.KeyDown -= KeyboardHookDown;
                Gma.UserActivityMonitor.HookManager.KeyUp -= KeyboardHookUp;
            };

            //this.Opacity = 0;

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

                if (File.Exists(cleanup))
                {
                    IntPtr hProc = IntPtr.Zero;

                    WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                    si.lpDesktop = "securedesktop";
                    si.dwFlags |= 0x00000020;
                    WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                    WinAPI.CreateProcess(null, cleanup + " -flag", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                    hProc = pi.hProcess;
                    //if (!this.IsDisposed) this.Invoke((Action)delegate { this.Close(); });
                    try
                    {
                        while (!this.IsDisposed)
                        {
                            Thread.Sleep(500);
                            if (!WinAPI.GetExitCodeProcess(hProc, out code) || code != 259) { break; }
                        }
                    }
                    catch { }
                }
                else ERROR = 1;
                if(!this.IsDisposed) this.Invoke((Action)delegate { this.Close(); });
            };
            bg.RunWorkerAsync();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Tick += delegate { Update(); };
            timer.Interval = 1000;
            timer.Start();

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Update();
        }
        //C:\Users\Administrator.AUD122024G\Git\Secure-Desktop\Cleanup\bin\Debug
        bool ctrl = false, shift = false, alt = false;
        const string taskmgr = @"C:\Windows\System32\taskmgr.exe";
        void KeyboardHookDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.PrintScreen && !ctrl) e.SuppressKeyPress = true;
            else if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) ctrl = true;
            else if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) shift = true;
            else if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu) alt = true;
            else if (e.KeyCode == Keys.K && ctrl && alt) this.Close();
            else if (e.KeyCode == Keys.E && ctrl && alt)
                MessageBox.Show("Desktop handle: " + Desktop.ToString());
            else if (e.KeyCode == Keys.T && ctrl && alt)
                WinAPI.SetWindowPos(this.Handle, WinAPI.HWND_TOPMOST, 0, 0, 0, 0, WinAPI.SWP_NOMOVE | WinAPI.SWP_NOSIZE | WinAPI.SWP_SHOWWINDOW);
            else if (e.KeyCode == Keys.V && ctrl && alt)
            {
                if (File.Exists(cleanup))
                {
                    WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                    si.lpDesktop = "securedesktop";
                    si.dwFlags |= 0x00000020;
                    WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                    WinAPI.CreateProcess(null, cleanup + " -view", IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                }
            }
            else if (e.KeyCode == Keys.Escape && ctrl && shift) //Task manager wont open by default so we'll have to do it manually and supress it
            {
                e.SuppressKeyPress = true;

                if (File.Exists(taskmgr))
                {
                    WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                    si.lpDesktop = "securedesktop";
                    si.dwFlags |= 0x00000020;
                    WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
                    WinAPI.CreateProcess(null, taskmgr, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi);
                }
            }
        }

        void KeyboardHookUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey) ctrl = false;
            else if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) shift = false;
            else if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu) alt = false;
        }

        public delegate void Action();

        Bitmap bitmap = null;
        SolidBrush background = new SolidBrush(Color.FromArgb(0x88, 0x00, 0x00, 0x00));
        Pen edge = new Pen(Color.FromArgb(0xAA, 0x00, 0x00, 0x00));
        Font font = new Font("Microsoft Sans Serif", 10.25f);//8.25f);
        Color shadow = Color.FromArgb(0x99, 0x00, 0x00, 0x00);
        new public void Update()
        {
            if (bitmap == null) bitmap = new Bitmap(tb.Bounds.Width, tb.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //bitmap = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                g.FillRectangle(background, 0, 0, tb.Bounds.Width, tb.Bounds.Height);

                if(tb.Position == TaskbarPosition.Bottom)
                    g.DrawLine(edge, 0, 0, tb.Bounds.Width, 0);
                else if (tb.Position == TaskbarPosition.Top)
                    g.DrawLine(edge, 0, tb.Bounds.Height - 1, tb.Bounds.Width, tb.Bounds.Height - 1);
                else if (tb.Position == TaskbarPosition.Left)
                    g.DrawLine(edge, tb.Bounds.Width - 1, 0, tb.Bounds.Width - 1, tb.Bounds.Height - 1);
                else if (tb.Position == TaskbarPosition.Right)
                    g.DrawLine(edge, 0, 0, 0, tb.Bounds.Height - 1);

                //g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                //int midy = (int)Math.Round(tb.Bounds.Height / 2f);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                if (tb.Position == TaskbarPosition.Bottom || tb.Position == TaskbarPosition.Top)
                {
                    string time = DateTime.Now.ToString("h:mm:ss tt");
                    Rectangle textbounds = new Rectangle(10, 0, tb.Bounds.Width - 20, tb.Bounds.Height);
                    Rectangle shadowbounds = new Rectangle(10, 1, tb.Bounds.Width - 20, tb.Bounds.Height);
                    TextRenderer.DrawText(g, "SecureDesktop", font, shadowbounds, shadow, (TextFormatFlags.VerticalCenter | TextFormatFlags.Left));
                    TextRenderer.DrawText(g, "SecureDesktop", font, textbounds, Color.White, (TextFormatFlags.VerticalCenter | TextFormatFlags.Left));

                    TextRenderer.DrawText(g, time, font, shadowbounds, shadow, (TextFormatFlags.VerticalCenter | TextFormatFlags.Right));
                    TextRenderer.DrawText(g, time, font, textbounds, Color.White, (TextFormatFlags.VerticalCenter | TextFormatFlags.Right));
                }
                else
                {
                    string time = DateTime.Now.ToString("h:mm tt");
                    Rectangle textbounds = new Rectangle(0, 10, tb.Bounds.Width, tb.Bounds.Height - 20);
                    Rectangle shadowbounds = new Rectangle(0, 11, tb.Bounds.Width, tb.Bounds.Height - 20);
                    TextRenderer.DrawText(g, "Secure Desktop", font, shadowbounds, shadow, (TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak));
                    TextRenderer.DrawText(g, "Secure Desktop", font, textbounds, Color.White, (TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak));

                    TextRenderer.DrawText(g, time, font, shadowbounds, shadow, (TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.WordBreak));
                    TextRenderer.DrawText(g, time, font, textbounds, Color.White, (TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.WordBreak));
                }
            }

            IntPtr screenDc = WinAPI.GetDC(IntPtr.Zero);
            IntPtr memDc = WinAPI.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = WinAPI.SelectObject(memDc, hBitmap);

                Size size = new Size(bitmap.Width, bitmap.Height);
                Point pointSource = new Point(0, 0);
                Point topPos = new Point(tb.Bounds.Left, tb.Bounds.Top);
                WinAPI.BLENDFUNCTION blend = new WinAPI.BLENDFUNCTION();
                blend.BlendOp = WinAPI.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 0xFF;
                blend.AlphaFormat = WinAPI.AC_SRC_ALPHA;

                WinAPI.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, WinAPI.ULW_ALPHA);
            }
            finally
            {
                WinAPI.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    WinAPI.SelectObject(memDc, oldBitmap);
                    WinAPI.DeleteObject(hBitmap);
                }
                WinAPI.DeleteDC(memDc);

                //WinAPI.SetWindowPos(this.Handle, WinAPI.HWND_TOPMOST, 0, 0, 0, 0, WinAPI.SWP_NOMOVE | WinAPI.SWP_NOSIZE | WinAPI.SWP_SHOWWINDOW);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WinAPI.WS_EX_LAYERED | WinAPI.WS_EX_TRANSPARENT | WinAPI.WS_EX_TOOLWINDOW | WinAPI.WS_EX_TOPMOST;
                return cp;
            }
        }
    }
}
