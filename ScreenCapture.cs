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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

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

        private string ParseMacroTags(string macro, DateTime dt, string format, string activeWindowTitle)
        {
            // Strip invalid Windows characters in the title.
            activeWindowTitle = activeWindowTitle.Replace(@"\", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("/", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace(":", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("*", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("?", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("\"", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("<", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace(">", string.Empty);
            activeWindowTitle = activeWindowTitle.Replace("|", string.Empty);

            macro = macro.Replace("%year%", dt.ToString("yyyy"));
            macro = macro.Replace("%month%", dt.ToString("MM"));
            macro = macro.Replace("%day%", dt.ToString("dd"));
            macro = macro.Replace("%hour%", dt.ToString("HH"));
            macro = macro.Replace("%minute%", dt.ToString("mm"));
            macro = macro.Replace("%second%", dt.ToString("ss"));
            macro = macro.Replace("%millisecond%", dt.ToString("fff"));
            macro = macro.Replace("%date%", dt.ToString("yyyy-MM-dd"));
            macro = macro.Replace("%time%", dt.ToString("HH-mm-ss-fff"));
            macro = macro.Replace("%format%", format);
            macro = macro.Replace("%title%", activeWindowTitle);

            return macro;
        }

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

        public BitmapSource TakeScreenshot(int x, int y, int width, int height, bool captureActiveWindow, out Bitmap bitmap)
        {
            if (!string.IsNullOrEmpty(ActiveWindowTitle) && !GetActiveWindowTitle().ToLower().Contains(ActiveWindowTitle.ToLower()))
            {
                bitmap = null;

                return null;
            }

            if (captureActiveWindow)
            {
                bitmap = GetActiveWindowBitmap();
            }
            else
            {
                bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }

            return GetBitmapSource(x, y, width, height, bitmap);
        }

        private BitmapSource GetBitmapSource(int x, int y, int width, int height, Bitmap bitmap)
        {
            BitmapSource bitmapSource = null;

            using (var bmpGraphics = Graphics.FromImage(bitmap))
            {
                bmpGraphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                var handle = bitmap.GetHbitmap();

                try
                {
                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        handle,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(handle);
                }
            }

            return bitmapSource;
        }

        public void SendToClipboard(BitmapSource bitmapSource)
        {
            Clipboard.SetImage(bitmapSource);
        }

        public void SaveScreenshot(Bitmap bitmap, string path, string format)
        {
            try
            {
                if (bitmap != null)
                {
                    path = ParseMacroTags(path, DateTime.Now, format, GetActiveWindowTitle());

                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        string dir = Path.GetDirectoryName(path);

                        if (!string.IsNullOrEmpty(dir))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                        }
                        else
                        {
                            path = AppDomain.CurrentDomain.BaseDirectory + path;
                        }
                    }

                    if (format.Equals("bmp"))
                    {
                        bitmap.Save(path, ImageFormat.Bmp);
                    }
                    else if (format.Equals("emf"))
                    {
                        bitmap.Save(path, ImageFormat.Emf);
                    }
                    else if (format.Equals("gif"))
                    {
                        bitmap.Save(path, ImageFormat.Gif);
                    }
                    else if (format.Equals("jpeg"))
                    {
                        int jpegQuality = 100;
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, jpegQuality);

                        var encoders = ImageCodecInfo.GetImageEncoders();
                        var encoderInfo = encoders.FirstOrDefault(t => t.MimeType == "image/jpeg");

                        bitmap.Save(path, encoderInfo, encoderParams);
                    }
                    else if (format.Equals("png"))
                    {
                        bitmap.Save(path, ImageFormat.Png);
                    }
                    else if (format.Equals("tiff"))
                    {
                        bitmap.Save(path, ImageFormat.Tiff);
                    }
                    else if (format.Equals("wmf"))
                    {
                        bitmap.Save(path, ImageFormat.Wmf);
                    }

                    bitmap.Dispose();
                }
            }
            catch
            {

            }
        }
    }
}
