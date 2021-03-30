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
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    public class ScreenCapture
    {
        public BitmapSource TakeScreenshot(int x, int y, int width, int height, bool clipboard, out Bitmap bitmap)
        {
            BitmapSource bitmapSource = null;

            try
            {
                var screenBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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
