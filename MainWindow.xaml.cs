//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>The main window for the application's interface.</summary>
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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Timers.Timer _timer;
        private Regex _rgxTime;
        private About _about;
        private ImageControls _imageControls;
        private ScreenCapture _screenCapture;
        private FormRegionSelectWithMouse _formRegionSelectWithMouse;
        private delegate void CheckTimedScreenshotDelegate();

        public MainWindow()
        {
            InitializeComponent();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += _timer_Elapsed;

            _about = new About();
            _rgxTime = new Regex(@"^\d{2}:\d{2}:\d{2}$");
            _screenCapture = new ScreenCapture();
            _imageControls = new ImageControls(new ScreenshotPreview());
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            _imageControls.comboBoxScreen.Items.Add("<Select Screen>");

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                _imageControls.comboBoxScreen.Items.Add(screen.DeviceName);
            }

            _imageControls.comboBoxScreen.SelectedIndex = 0;

            _imageControls.comboBoxFormat.Items.Add("BMP");
            _imageControls.comboBoxFormat.Items.Add("EMF");
            _imageControls.comboBoxFormat.Items.Add("GIF");
            _imageControls.comboBoxFormat.Items.Add("JPEG");
            _imageControls.comboBoxFormat.Items.Add("PNG");
            _imageControls.comboBoxFormat.Items.Add("TIFF");
            _imageControls.comboBoxFormat.Items.Add("WMF");

            _imageControls.comboBoxFormat.SelectedIndex = 3;

            _imageControls.Date.SelectedDate = DateTime.Now.Date;

            _imageControls.textBoxFile.Text = AppDomain.CurrentDomain.BaseDirectory + "screenshot.%format%";

            _timer.Start();
        }

        private void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Exit();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(new CheckTimedScreenshotDelegate(CheckTimedScreenshot));
        }

        private void CheckTimedScreenshot()
        {
            if (!string.IsNullOrEmpty(_imageControls.textBoxTime.Text) &&
                _imageControls.Date.SelectedDate.Value.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) &&
                _rgxTime.IsMatch(_imageControls.textBoxTime.Text) && _imageControls.textBoxTime.Text.Equals(DateTime.Now.ToString("HH:mm:ss")))
            {
                buttonTakeScreenshot_Click(null, null);
            }
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            _about.Show();
        }

        private void buttonImageControls_Click(object sender, RoutedEventArgs e)
        {
            _imageControls.Show();
        }

        private void buttonScreenshotPreview_Click(object sender, RoutedEventArgs e)
        {
            _imageControls.screenshotPreview.Show();
        }

        private void buttonRegionSelect_Click(object sender, RoutedEventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse(_imageControls.checkBoxClipboard.IsChecked);
            _formRegionSelectWithMouse.MouseSelectionCompleted += formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        private void formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted(object sender, EventArgs e)
        {
            int x = _formRegionSelectWithMouse.outputX + 1;
            int y = _formRegionSelectWithMouse.outputY + 1;
            int width = _formRegionSelectWithMouse.outputWidth - 2;
            int height = _formRegionSelectWithMouse.outputHeight - 2;

            _imageControls.textBoxX.Text = x.ToString();
            _imageControls.textBoxY.Text = y.ToString();
            _imageControls.textBoxWidth.Text = width.ToString();
            _imageControls.textBoxHeight.Text = height.ToString();
        }

        private void buttonTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_imageControls.textBoxFile.Text) &&
                int.TryParse(_imageControls.textBoxX.Text, out int x) &&
                int.TryParse(_imageControls.textBoxY.Text, out int y) &&
                int.TryParse(_imageControls.textBoxWidth.Text, out int width) &&
                int.TryParse(_imageControls.textBoxHeight.Text, out int height))
            {
                _screenCapture.TakeScreenshot(x, y, width, height, (bool)_imageControls.checkBoxClipboard.IsChecked, out Bitmap bitmap);

                if (bitmap != null)
                {
                    string format = _imageControls.comboBoxFormat.SelectedItem.ToString().ToLower();
                    string path = _imageControls.textBoxFile.Text.Replace("%format%", format);

                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
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
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);

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
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            DialogResult dialogResult = (DialogResult)System.Windows.MessageBox.Show("Are you sure you want to exit Xaimatzu?", "Exit Xaimatzu", (MessageBoxButton)MessageBoxButtons.YesNo, (MessageBoxImage)MessageBoxIcon.Question);

            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                Environment.Exit(0);
            }
        }
    }
}