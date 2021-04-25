//-----------------------------------------------------------------------
// <copyright file="ScreenCapture.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A class for handling screen capture functionality. Imported from the Auto Screen Capture project.</summary>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    public class ScreenCapture
    {
        private const int MAX_CHARS = 48000;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern void GetWindowRect(IntPtr hWnd, out Rectangle rect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public string ActiveWindowTitle { get; set; }

        public string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle;
                int chars = MAX_CHARS;

                StringBuilder buffer = new StringBuilder(chars);

                handle = GetForegroundWindow();

                if (GetWindowText(handle, buffer, chars) > 0)
                {
                    // Make sure to strip out the backslash if it's in the window title.
                    return buffer.ToString().Replace(@"\", string.Empty);
                }

                return "(system)";
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Bitmap GetActiveWindowBitmap()
        {
            try
            {
                GetWindowRect(GetForegroundWindow(), out Rectangle rect);

                int width = rect.Width - rect.X;
                int height = rect.Height - rect.Y;

                if (width > 0 && height > 0)
                {
                    Bitmap bmp = new Bitmap(width, height);

                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(new System.Drawing.Point(rect.X, rect.Y), new System.Drawing.Point(0, 0), new System.Drawing.Size(width, height));
                    }

                    return bmp;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SetApplicationFocus(string applicationFocus)
        {
            if (string.IsNullOrEmpty(applicationFocus)) return;

            Process[] process = Process.GetProcessesByName(applicationFocus);

            foreach (var item in process)
            {
                var proc = Process.GetProcessById(item.Id);

                IntPtr handle = proc.MainWindowHandle;
                SetForegroundWindow(handle);

                var placement = GetPlacement(proc.MainWindowHandle);

                if (placement.showCmd == ShowWindowCommands.Minimized)
                {
                    ShowWindowAsync(proc.MainWindowHandle, (int)ShowWindowCommands.Normal);
                }
            }
        }

        public BitmapSource TakeScreenshot(int x, int y, int width, int height, bool clipboard, bool captureActiveWindow, out Bitmap bitmap)
        {
            BitmapSource bitmapSource = null;

            try
            {
                if (!string.IsNullOrEmpty(ActiveWindowTitle) && !GetActiveWindowTitle().ToLower().Contains(ActiveWindowTitle.ToLower()))
                {
                    bitmap = null;
                    return null;
                }

                Bitmap screenBmp = null;

                if (captureActiveWindow)
                {
                    screenBmp = GetActiveWindowBitmap();
                }
                else
                {
                    screenBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }

                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        screenBmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }

                bitmap = screenBmp;

                if (clipboard)
                {
                    Clipboard.SetImage(bitmapSource);
                }

                return bitmapSource;
            }
            catch
            {
                bitmap = null;

                return null;
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}
