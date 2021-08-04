//-----------------------------------------------------------------------
// <copyright file="ImageControls.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>The window for image controls (including the filepath for where the screenshot is going to be written to on disk).</summary>
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
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for ImageControls.xaml
    /// </summary>
    public partial class ImageControls : Window
    {
        private ScreenCapture _screenCapture;

        public bool ActiveWindow;
        public ScreenshotPreview screenshotPreview;
        public ApplicationFocus applicationFocus;

        public ImageControls(ScreenCapture screenCapture, ScreenshotPreview screenshotPreview, ApplicationFocus applicationFocus)
        {
            InitializeComponent();

            _screenCapture = screenCapture;
            this.screenshotPreview = screenshotPreview;
            this.applicationFocus = applicationFocus;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboBoxScreen.Items.Add("<Select Screen>");

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                comboBoxScreen.Items.Add(screen.DeviceName);
            }

            comboBoxScreen.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
        }

        private void comboBoxScreen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (comboBoxScreen.SelectedItem.Equals(screen.DeviceName))
                {
                    textBoxX.Text = screen.Bounds.X.ToString();
                    textBoxY.Text = screen.Bounds.Y.ToString();
                    textBoxWidth.Text = screen.Bounds.Width.ToString();
                    textBoxHeight.Text = screen.Bounds.Height.ToString();
                }
            }

            comboBoxScreen.SelectedIndex = 0;
        }

        private void screenshotAttribute_TextChanged(object sender, TextChangedEventArgs e)
        {
            TakeScreenshot();
        }

        /// <summary>
        /// Takes a screenshot of either the active window or a region of the screen (that could also be sent to the clipboard and/or saved to a file).
        /// </summary>
        public void TakeScreenshot()
        {
            Bitmap bitmap = null;
            BitmapSource bitmapSource = null;

            try
            {
                applicationFocus.DoApplicationFocus();

                bool clipboard = (bool)checkBoxClipboard.IsChecked;

                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;

                if (ActiveWindow)
                {
                    if (!string.IsNullOrEmpty(textBoxFile.Text))
                    {
                        bitmapSource = _screenCapture.TakeScreenshot(0, 0, 0, 0, captureActiveWindow: true, out bitmap);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(textBoxFile.Text) &&
                        int.TryParse(textBoxX.Text, out x) &&
                        int.TryParse(textBoxY.Text, out y) &&
                        int.TryParse(textBoxWidth.Text, out width) &&
                        int.TryParse(textBoxHeight.Text, out height))
                    {
                        bitmapSource = _screenCapture.TakeScreenshot(x, y, width, height, captureActiveWindow: false, out bitmap);
                    }
                }

                if (clipboard)
                {
                    if (bitmapSource != null)
                    {
                        _screenCapture.SendToClipboard(bitmapSource);
                    }
                }

                if ((bool)checkBoxSave.IsChecked)
                {
                    _screenCapture.SaveScreenshot(bitmap, textBoxFile.Text, comboBoxFormat.SelectedItem.ToString().ToLower());
                }
            }
            catch (Exception)
            {
                
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        /// <summary>
        /// Updates the screenshot preview window with the selected area of the screen.
        /// </summary>
        public void UpdatePreview()
        {
            if (!screenshotPreview.IsVisible)
            {
                return;
            }

            if (int.TryParse(textBoxX.Text, out int x) &&
                int.TryParse(textBoxY.Text, out int y) &&
                int.TryParse(textBoxWidth.Text, out int width) &&
                int.TryParse(textBoxHeight.Text, out int height))
            {
                BitmapSource bitmapSource = _screenCapture.TakeScreenshot(x, y, width, height, captureActiveWindow: false, out Bitmap bitmap);
                bitmap.Dispose();

                if (bitmapSource != null)
                {
                    screenshotPreview.Title = "Xaimatzu - Screenshot Preview (" + bitmapSource.Width + "x" + bitmapSource.Height + ")";
                    screenshotPreview.imageScreenshotPreview.Source = bitmapSource;
                }
            }
        }
    }
}