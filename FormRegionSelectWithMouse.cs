//-----------------------------------------------------------------------
// <copyright file="FormRegionSelectWithMouse.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A form that covers all the available screens so we can do a mouse-driven region select. Imported from the Auto Screen Capture project.</summary>
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
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace xaimatzu
{
    /// <summary>
    /// A form that covers all the available screens so we can do a mouse-driven region select.
    /// </summary>
    public partial class FormRegionSelectWithMouse : Form
    {
        private bool? _saveToClipboard;

        private int _selectX;
        private int _selectY;
        private int _selectWidth;
        private int _selectHeight;
        private Pen _selectPen;

        /// <summary>
        /// X output
        /// </summary>
        public int outputX;

        /// <summary>
        /// Y output
        /// </summary>
        public int outputY;

        /// <summary>
        /// Width output
        /// </summary>
        public int outputWidth;

        /// <summary>
        /// Height output
        /// </summary>
        public int outputHeight;

        private void CompleteMouseSelection(object sender, EventArgs e)
        {
            MouseSelectionCompleted?.Invoke(sender, e);
        }

        private void pictureBoxMouseCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBoxMouseCanvas.Image == null || _selectPen == null)
            {
                return;
            }

            pictureBoxMouseCanvas.Refresh();

            _selectWidth = e.X - _selectX;
            _selectHeight = e.Y - _selectY;

            pictureBoxMouseCanvas.CreateGraphics().DrawRectangle(_selectPen, _selectX, _selectY, _selectWidth, _selectHeight);
        }

        private void pictureBoxMouseCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _selectX = e.X;
                _selectY = e.Y;

                _selectPen = new Pen(Color.Gold, 2)
                {
                    DashStyle = DashStyle.Dash
                };
            }

            pictureBoxMouseCanvas.Refresh();
        }

        private void pictureBoxMouseCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (pictureBoxMouseCanvas.Image == null || _selectPen == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                pictureBoxMouseCanvas.Refresh();

                _selectWidth = e.X - _selectX;
                _selectHeight = e.Y - _selectY;

                pictureBoxMouseCanvas.CreateGraphics().DrawRectangle(_selectPen, _selectX, _selectY, _selectWidth, _selectHeight);
            }

            Bitmap bitmapSource = null;

            if (_selectWidth > 0)
            {
                Rectangle rect = new Rectangle(_selectX, _selectY, _selectWidth, _selectHeight);

                Bitmap bitmapDestination = new Bitmap(pictureBoxMouseCanvas.Image, pictureBoxMouseCanvas.Width, pictureBoxMouseCanvas.Height);
                bitmapSource = new Bitmap(_selectWidth, _selectHeight);

                using (Graphics g = Graphics.FromImage(bitmapSource))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(bitmapDestination, 0, 0, rect, GraphicsUnit.Pixel);
                }

                bitmapDestination.Dispose();
            }

            if (bitmapSource != null)
            {
                if ((bool)_saveToClipboard)
                {
                    SaveToClipboard(bitmapSource);
                }

                outputX = _selectX;
                outputY = _selectY;
                outputWidth = _selectWidth;
                outputHeight = _selectHeight;

                CompleteMouseSelection(sender, e);

                bitmapSource.Dispose();
            }

            Cursor = Cursors.Arrow;

            Close();
        }

        /// <summary>
        /// Saves the bitmap to the clipboard when the mouse-drive selection has completeld.
        /// Do not confuse this method with the SendToClipboard method that takes a bitmap source when taking a screenshot.
        /// </summary>
        /// <param name="bitmap">The bitmap to save to the clipboard.</param>
        private void SaveToClipboard(Bitmap bitmap)
        {
            if (bitmap != null)
            {
                Clipboard.SetImage(bitmap);

                bitmap.Dispose();
            }
        }

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public FormRegionSelectWithMouse(bool? saveToClipboard)
        {
            InitializeComponent();

            outputX = 0;
            outputY = 0;
            outputWidth = 0;
            outputHeight = 0;

            _saveToClipboard = saveToClipboard;
        }

        /// <summary>
        /// An event handler for handling when the mouse selection has completed for the mouse-driven region capture.
        /// </summary>
        public event EventHandler MouseSelectionCompleted;

        /// <summary>
        /// Loads the canvas.
        /// </summary>
        public void LoadCanvas()
        {
            Top = 0;
            Left = 0;

            int width = 0;
            int height = 0;

            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.Bounds.X < Left)
                {
                    Left = screen.Bounds.X;
                }

                if (screen.Bounds.Y < Top)
                {
                    Top = screen.Bounds.Y;
                }

                width += screen.Bounds.Width;
                height += screen.Bounds.Height;
            }

            WindowState = FormWindowState.Normal;
            Width = width;
            Height = height;

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

                using (MemoryStream s = new MemoryStream())
                {
                    bitmap.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);

                    pictureBoxMouseCanvas.Size = new Size(Width, Height);
                    pictureBoxMouseCanvas.Image = Image.FromStream(s);
                }
            }

            bitmap.Dispose();

            Show();

            Cursor = Cursors.Cross;
        }
    }
}
