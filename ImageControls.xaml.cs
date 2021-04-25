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

        public ImageControls(ScreenshotPreview screenshotPreview)
        {
            InitializeComponent();

            _screenCapture = new ScreenCapture();
            this.screenshotPreview = screenshotPreview;
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
            if (!string.IsNullOrEmpty(textBoxFile.Text) &&
                int.TryParse(textBoxX.Text, out int x) &&
                int.TryParse(textBoxY.Text, out int y) &&
                int.TryParse(textBoxWidth.Text, out int width) &&
                int.TryParse(textBoxHeight.Text, out int height))
            {
                BitmapSource bitmapSource = _screenCapture.TakeScreenshot(x, y, width, height, (bool)checkBoxClipboard.IsChecked, false, out Bitmap bitmap);

                if (bitmapSource != null)
                {
                    screenshotPreview.Title = "Xaimatzu - Screenshot Preview (" + bitmapSource.Width + "x" + bitmapSource.Height + ")";
                    screenshotPreview.imageScreenshotPreview.Source = bitmapSource;
                }
            }
        }
    }
}