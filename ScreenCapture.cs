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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    /// <summary>
    /// A class for handling screen capture functionality. Imported from the Auto Screen Capture project.
    /// </summary>
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
        private static extern bool DeleteObject([In] IntPtr hObject);

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

        /// <summary>
        /// Gets the bitmap source based on X, Y, Width, Height, and a bitmap image.
        /// </summary>
        /// <param name="x">The X coordinate of the image location.</param>
        /// <param name="y">The Y coordinate of the image location.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="bitmap">The image to use for creating the bitmap source.</param>
        /// <returns>The bitmap source used for the clipboard and screenshot preview.</returns>
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

        private bool ActiveWindowTitleMatchText(string activeWindowTitle, Settings settings)
        {
            try
            {
                if (!string.IsNullOrEmpty(activeWindowTitle) && settings != null &&
                    !string.IsNullOrEmpty(settings.textBoxActiveWindowTitleTextComparison.Text))
                {
                    settings.textBoxActiveWindowTitleTextComparison.Text = settings.textBoxActiveWindowTitleTextComparison.Text.Trim();

                    if ((bool)settings.radioButtonCaseSensitiveMatch.IsChecked)
                    {
                        return activeWindowTitle.Contains(settings.textBoxActiveWindowTitleTextComparison.Text);
                    }
                    else if ((bool)settings.radioButtonCaseInsensitiveMatch.IsChecked)
                    {
                        return activeWindowTitle.ToLower().Contains(settings.textBoxActiveWindowTitleTextComparison.Text.ToLower());
                    }
                    else if ((bool)settings.radioButtonRegularExpressionMatch.IsChecked)
                    {
                        return Regex.IsMatch(activeWindowTitle, settings.textBoxActiveWindowTitleTextComparison.Text);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool ActiveWindowTitleDoesNotMatchText(string activeWindowTitle, Settings settings)
        {
            try
            {
                if (!string.IsNullOrEmpty(activeWindowTitle) && settings != null &&
                        !string.IsNullOrEmpty(settings.textBoxActiveWindowTitleTextComparison.Text))
                {
                    settings.textBoxActiveWindowTitleTextComparison.Text = settings.textBoxActiveWindowTitleTextComparison.Text.Trim();

                    if ((bool)settings.radioButtonCaseSensitiveMatch.IsChecked)
                    {
                        return !activeWindowTitle.Contains(settings.textBoxActiveWindowTitleTextComparison.Text);
                    }
                    else if ((bool)settings.radioButtonCaseInsensitiveMatch.IsChecked)
                    {
                        return !activeWindowTitle.ToLower().Contains(settings.textBoxActiveWindowTitleTextComparison.Text.ToLower());
                    }
                    else if ((bool)settings.radioButtonRegularExpressionMatch.IsChecked)
                    {
                        return !Regex.IsMatch(activeWindowTitle, settings.textBoxActiveWindowTitleTextComparison.Text);
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the provided macro for macro tags and returns the resulting value based on those macro tags.
        /// </summary>
        /// <param name="macro">The macro to provide that contains macro tags (such as %date% and %time%).</param>
        /// <param name="format">The image format represented as a string (such as "jpeg").</param>
        /// <param name="activeWindowTitle">The title of the active window.</param>
        /// <returns>The parsed macro (so %date% in the macro will return the current date in the format yyyy-MM-dd).</returns>
        public string ParseMacroTags(string macro, string format, string activeWindowTitle)
        {
            DateTime dt = DateTime.Now;

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
            macro = macro.Replace("%format%", format.ToLower());
            macro = macro.Replace("%title%", activeWindowTitle);

            return macro;
        }

        /// <summary>
        /// Gets the title of the active window.
        /// </summary>
        /// <returns>The title of the active window.</returns>
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

        /// <summary>
        /// Gets the bitmap image of the active window.
        /// </summary>
        /// <returns>The bitmap image of the active window.</returns>
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

        /// <summary>
        /// Forces the focus on the foreground window of the given application process name.
        /// </summary>
        /// <param name="processName">The name of the process to focus on.</param>
        public void ForceFocusOnProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
            {
                return;
            }

            processName = processName.Trim();

            Process[] process = Process.GetProcessesByName(processName);

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

        /// <summary>
        /// Takes a screenshot based on the given parameters for X, Y, Width, and Height. Also determines if we should capture
        /// the active window. The captured image is stored in the proided Bitmap object and a BitmapSource is returned.
        /// </summary>
        /// <param name="x">The X coordinate of the image location.</param>
        /// <param name="y">The Y coordinate of the image location.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="bitmap">The bitmap where the image will be stored after screen capture.</param>
        /// <param name="settings">Access to settings.</param>
        /// <returns>The bitmap source to be used for the clipboard and screenshot preview.</returns>
        public BitmapSource TakeScreenshot(int x, int y, int width, int height, out Bitmap bitmap, Settings settings)
        {
            try
            {
                if ((bool)settings.checkBoxActiveWindowTitleComparisonCheck.IsChecked &&
                    !ActiveWindowTitleMatchText(GetActiveWindowTitle(), settings))
                {
                    bitmap = null;

                    return null;
                }

                if ((bool)settings.checkBoxActiveWindowTitleComparisonCheckReverse.IsChecked &&
                    !ActiveWindowTitleDoesNotMatchText(GetActiveWindowTitle(), settings))
                {
                    bitmap = null;

                    return null;
                }

                if ((bool)settings.radioButtonActiveWindow.IsChecked)
                {
                    bitmap = GetActiveWindowBitmap();
                }
                else
                {
                    bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                }

                return GetBitmapSource(x, y, width, height, bitmap);
            }
            catch
            {
                bitmap = null;

                return null;
            }
        }

        /// <summary>
        /// Gives the provided image to the clipboard.
        /// </summary>
        /// <param name="bitmapSource">The bitmap source containing the image for the clipboard.</param>
        public void SendToClipboard(BitmapSource bitmapSource)
        {
            Clipboard.SetImage(bitmapSource);
        }

        /// <summary>
        /// Saves a screenshot of the provided bitmap to a file using the provided path.
        /// </summary>
        /// <param name="bitmap">The image to save.</param>
        /// <param name="path">The filepath to save the image to.</param>
        /// <param name="format">The image format.</param>
        public void SaveScreenshot(Bitmap bitmap, string path, string format)
        {
            try
            {
                if (bitmap != null)
                {
                    path = ParseMacroTags(path, format, GetActiveWindowTitle());

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

                    format = format.ToLower();

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
