//-----------------------------------------------------------------------
// <copyright file="Settings.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A window for determining various settings.</summary>
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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private ScreenCapture _screenCapture;
        private FormRegionSelectWithMouse _formRegionSelectWithMouse;

        SolidColorBrush _lightGreen;
        SolidColorBrush _lightYellow;
        SolidColorBrush _paleVioletRed;

        public ScreenshotPreview screenshotPreview;

        public Settings(ScreenCapture screenCapture, ScreenshotPreview screenshotPreview)
        {
            InitializeComponent();

            _screenCapture = screenCapture;
            this.screenshotPreview = screenshotPreview;

            _lightGreen = new SolidColorBrush(Colors.LightGreen);
            _lightYellow = new SolidColorBrush(Colors.LightYellow);
            _paleVioletRed = new SolidColorBrush(Colors.PaleVioletRed);

            CheckRegex();

            textBoxDelayBefore.Text = "0";
            textBoxDelayAfter.Text = "0";

            buttonRefreshProcessList_Click(null, null);
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

        private void radioButtonActiveWindow_Checked(object sender, RoutedEventArgs e)
        {
            radioButtonActiveWindow_Click(sender, e);
        }

        private void radioButtonActiveWindow_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)radioButtonActiveWindow.IsChecked)
            {
                labelScreen.IsEnabled = false;
                labelX.IsEnabled = false;
                labelY.IsEnabled = false;
                labelWidth.IsEnabled = false;
                labelHeight.IsEnabled = false;

                comboBoxScreen.IsEnabled = false;
                buttonRegionSelect.IsEnabled = false;

                textBoxX.IsEnabled = false;
                textBoxY.IsEnabled = false;
                textBoxWidth.IsEnabled = false;
                textBoxHeight.IsEnabled = false;
            }
            else
            {
                labelScreen.IsEnabled = true;
                labelX.IsEnabled = true;
                labelY.IsEnabled = true;
                labelWidth.IsEnabled = true;
                labelHeight.IsEnabled = true;

                comboBoxScreen.IsEnabled = true;
                buttonRegionSelect.IsEnabled = true;

                textBoxX.IsEnabled = true;
                textBoxY.IsEnabled = true;
                textBoxWidth.IsEnabled = true;
                textBoxHeight.IsEnabled = true;
            }
        }

        private void radioButtonRegion_Checked(object sender, RoutedEventArgs e)
        {
            radioButtonRegion_Click(sender, e);
        }

        private void radioButtonRegion_Click(object sender, RoutedEventArgs e)
        {
            // Avoid null reference exception due to event handler initializing on load.
            if (labelScreen == null)
            {
                return;
            }

            if ((bool)radioButtonRegion.IsChecked)
            {
                labelScreen.IsEnabled = true;
                labelX.IsEnabled = true;
                labelY.IsEnabled = true;
                labelWidth.IsEnabled = true;
                labelHeight.IsEnabled = true;

                comboBoxScreen.IsEnabled = true;
                buttonRegionSelect.IsEnabled = true;

                textBoxX.IsEnabled = true;
                textBoxY.IsEnabled = true;
                textBoxWidth.IsEnabled = true;
                textBoxHeight.IsEnabled = true;
            }
            else
            {
                labelScreen.IsEnabled = false;
                labelX.IsEnabled = false;
                labelY.IsEnabled = false;
                labelWidth.IsEnabled = false;
                labelHeight.IsEnabled = false;

                comboBoxScreen.IsEnabled = false;
                buttonRegionSelect.IsEnabled = false;

                textBoxX.IsEnabled = false;
                textBoxY.IsEnabled = false;
                textBoxWidth.IsEnabled = false;
                textBoxHeight.IsEnabled = false;
            }
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

        private void comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMacroPreview();
        }

        private void buttonRegionSelect_Click(object sender, RoutedEventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse(saveToClipboard: false);
            _formRegionSelectWithMouse.MouseSelectionCompleted += formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        private void formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted(object sender, EventArgs e)
        {
            int x = _formRegionSelectWithMouse.outputX + 1;
            int y = _formRegionSelectWithMouse.outputY + 1;
            int width = _formRegionSelectWithMouse.outputWidth - 2;
            int height = _formRegionSelectWithMouse.outputHeight - 2;

            textBoxX.Text = x.ToString();
            textBoxY.Text = y.ToString();
            textBoxWidth.Text = width.ToString();
            textBoxHeight.Text = height.ToString();

            _formRegionSelectWithMouse.Close();
        }

        private void textBoxMacro_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMacroPreview();
        }

        private void checkBoxActiveWindowTitleComparisonCheck_Checked(object sender, RoutedEventArgs e)
        {
            checkBoxActiveWindowTitleComparisonCheck_Click(sender, e);
        }

        private void checkBoxActiveWindowTitleComparisonCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxActiveWindowTitleComparisonCheck.IsChecked)
            {
                checkBoxActiveWindowTitleComparisonCheckReverse.IsChecked = false;

                labelComparisonText.IsEnabled = true;
                labelRegularExpression.IsEnabled = true;
                labelTestValue.IsEnabled = true;

                radioButtonCaseSensitiveMatch.IsEnabled = true;
                radioButtonCaseInsensitiveMatch.IsEnabled = true;
                radioButtonRegularExpressionMatch.IsEnabled = true;

                textBoxActiveWindowTitleTextComparison.IsEnabled = true;
                textBoxRegularExpression.IsEnabled = true;
                textBoxTestValue.IsEnabled = true;
            }
            else
            {
                labelComparisonText.IsEnabled = false;
                labelRegularExpression.IsEnabled = false;
                labelTestValue.IsEnabled = false;

                radioButtonCaseSensitiveMatch.IsEnabled = false;
                radioButtonCaseInsensitiveMatch.IsEnabled = false;
                radioButtonRegularExpressionMatch.IsEnabled = false;

                textBoxActiveWindowTitleTextComparison.IsEnabled = false;
                textBoxRegularExpression.IsEnabled = false;
                textBoxTestValue.IsEnabled = false;
            }
        }

        private void checkBoxActiveWindowTitleComparisonCheckReverse_Checked(object sender, RoutedEventArgs e)
        {
            checkBoxActiveWindowTitleComparisonCheckReverse_Click(sender, e);
        }

        private void checkBoxActiveWindowTitleComparisonCheckReverse_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxActiveWindowTitleComparisonCheckReverse.IsChecked)
            {
                checkBoxActiveWindowTitleComparisonCheck.IsChecked = false;
                labelComparisonText.IsEnabled = true;
                labelRegularExpression.IsEnabled = true;
                labelTestValue.IsEnabled = true;

                radioButtonCaseSensitiveMatch.IsEnabled = true;
                radioButtonCaseInsensitiveMatch.IsEnabled = true;
                radioButtonRegularExpressionMatch.IsEnabled = true;

                textBoxActiveWindowTitleTextComparison.IsEnabled = true;
                textBoxRegularExpression.IsEnabled = true;
                textBoxTestValue.IsEnabled = true;
            }
            else
            {
                labelComparisonText.IsEnabled = false;
                labelRegularExpression.IsEnabled = false;
                labelTestValue.IsEnabled = false;

                radioButtonCaseSensitiveMatch.IsEnabled = false;
                radioButtonCaseInsensitiveMatch.IsEnabled = false;
                radioButtonRegularExpressionMatch.IsEnabled = false;

                textBoxActiveWindowTitleTextComparison.IsEnabled = false;
                textBoxRegularExpression.IsEnabled = false;
                textBoxTestValue.IsEnabled = false;
            }
        }

        private void textBoxRegularExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckRegex();
        }

        private void textBoxTestValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckRegex();
        }

        private void CheckRegex()
        {
            try
            {
                if (string.IsNullOrEmpty(textBoxRegularExpression.Text) ||
                    string.IsNullOrEmpty(textBoxTestValue.Text))
                {
                    textBoxRegularExpression.Background = _lightYellow;
                    textBoxTestValue.Background = _lightYellow;
                }

                if (Regex.IsMatch(textBoxTestValue.Text, textBoxRegularExpression.Text))
                {
                    textBoxRegularExpression.Background = _lightGreen;
                    textBoxTestValue.Background = _lightGreen;
                }
                else
                {
                    textBoxRegularExpression.Background = _paleVioletRed;
                    textBoxTestValue.Background = _paleVioletRed;
                }
            }
            catch
            {
                textBoxRegularExpression.Background = _paleVioletRed;
                textBoxTestValue.Background = _paleVioletRed;
            }
        }

        private void checkBoxApplicationFocus_Checked(object sender, RoutedEventArgs e)
        {
            checkBoxApplicationFocus_Click(sender, e);
        }

        private void checkBoxApplicationFocus_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxApplicationFocus.IsChecked)
            {
                listBoxProcessName.IsEnabled = true;
                labelDelayBefore.IsEnabled = true;
                labelDelayAfter.IsEnabled = true;
                textBoxDelayBefore.IsEnabled = true;
                textBoxDelayAfter.IsEnabled = true;
                buttonRefreshProcessList.IsEnabled = true;
                buttonTestFocus.IsEnabled = true;
            }
            else
            {
                listBoxProcessName.IsEnabled = false;
                labelDelayBefore.IsEnabled = false;
                labelDelayAfter.IsEnabled = false;
                textBoxDelayBefore.IsEnabled = false;
                textBoxDelayAfter.IsEnabled = false;
                buttonRefreshProcessList.IsEnabled = false;
                buttonTestFocus.IsEnabled = false;
            }
        }

        private void buttonRefreshProcessList_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcessList();
        }

        private void buttonTestFocus_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxProcessName.SelectedItem != null &&
                !string.IsNullOrEmpty(listBoxProcessName.SelectedItem.ToString()) &&
                int.TryParse(textBoxDelayBefore.Text, out int delayBefore) &&
                int.TryParse(textBoxDelayAfter.Text, out int delayAfter))
            {
                DoApplicationFocus(listBoxProcessName.SelectedItem.ToString(), delayBefore, delayAfter);
            }
            else
            {
                MessageBox.Show("No application was selected from the process list.", "No Application Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateMacroPreview()
        {
            textBoxMacroPreview.Text = _screenCapture.ParseMacroTags(textBoxMacro.Text, comboBoxFormat.SelectedItem.ToString(), _screenCapture.GetActiveWindowTitle());
        }

        /// <summary>
        /// Takes a screenshot of either the active window or a region of the screen (that could also be sent to the clipboard and/or saved to a file).
        /// </summary>
        public void TakeScreenshot(bool clipboard, bool save)
        {
            Bitmap bitmap = null;
            BitmapSource bitmapSource = null;

            try
            {
                DoApplicationFocus();

                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;

                if ((bool)radioButtonActiveWindow.IsChecked)
                {
                    if (!string.IsNullOrEmpty(textBoxMacro.Text))
                    {
                        bitmapSource = _screenCapture.TakeScreenshot(0, 0, 0, 0, out bitmap, this);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(textBoxMacro.Text) &&
                        int.TryParse(textBoxX.Text, out x) &&
                        int.TryParse(textBoxY.Text, out y) &&
                        int.TryParse(textBoxWidth.Text, out width) &&
                        int.TryParse(textBoxHeight.Text, out height))
                    {
                        bitmapSource = _screenCapture.TakeScreenshot(x, y, width, height, out bitmap, this);
                    }
                }

                if (clipboard)
                {
                    if (bitmapSource != null)
                    {
                        _screenCapture.SendToClipboard(bitmapSource);
                    }
                }

                if (save)
                {
                    _screenCapture.SaveScreenshot(bitmap, textBoxMacro.Text, comboBoxFormat.SelectedItem.ToString());
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
        public void UpdateScreenshotPreview()
        {
            if (!screenshotPreview.IsVisible)
            {
                return;
            }
            
            int x = 0;
            int y = 0;
            int width = 0;
            int height = 0;

            string previewTitle = "Xaimatzu - Screenshot Preview";

            screenshotPreview.imageScreenshotPreview.Source = null;

            if ((bool)radioButtonActiveWindow.IsChecked)
            {
                previewTitle += " (Active Window)";
            }
            else
            {
                int.TryParse(textBoxX.Text, out x);
                int.TryParse(textBoxY.Text, out y);
                int.TryParse(textBoxWidth.Text, out width);
                int.TryParse(textBoxHeight.Text, out height);

                previewTitle += " (" + x + "," + y + " " + width + "x" + height + ")";
            }

            BitmapSource bitmapSource = _screenCapture.TakeScreenshot(x, y, width, height, out Bitmap bitmap, this);

            if (bitmap != null)
            {
                bitmap.Dispose();
            }

            if (bitmapSource != null)
            {
                screenshotPreview.imageScreenshotPreview.Source = bitmapSource;
            }

            screenshotPreview.Title = previewTitle;
        }

        /// <summary>
        /// Refreshes the process list.
        /// </summary>
        public void RefreshProcessList()
        {
            listBoxProcessName.Items.Clear();

            foreach (Process process in Process.GetProcesses())
            {
                if (!listBoxProcessName.Items.Contains(process.ProcessName))
                {
                    listBoxProcessName.Items.Add(process.ProcessName);
                }
            }

            listBoxProcessName.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(string.Empty, System.ComponentModel.ListSortDirection.Ascending));
        }

        /// <summary>
        /// Does application focus based on the values of the controls in the Application Focus window.
        /// This method should be called from a class outside of this class.
        /// </summary>
        public void DoApplicationFocus()
        {
            if (!(bool)checkBoxApplicationFocus.IsChecked)
            {
                return;
            }

            if (listBoxProcessName.SelectedItem != null &&
                !string.IsNullOrEmpty(listBoxProcessName.SelectedItem.ToString()) &&
                int.TryParse(textBoxDelayBefore.Text, out int delayBefore) &&
                int.TryParse(textBoxDelayAfter.Text, out int delayAfter))
            {
                DoApplicationFocus(listBoxProcessName.SelectedItem.ToString(), delayBefore, delayAfter);
            }
        }

        /// <summary>
        /// Does application focus. This method should be called when parsing command line arguments or called by internal methods within this class.
        /// </summary>
        /// <param name="processName">The name of the process.</param>
        /// <param name="delayBefore">The number of milliseconds before doing application focus.</param>
        /// <param name="delayAfter">The number of milliseconds after doing application focus.</param>
        public void DoApplicationFocus(string processName, int delayBefore, int delayAfter)
        {
            if (string.IsNullOrEmpty(processName))
            {
                return;
            }

            // If we've reached this point and the checkbox hasn't been set then set the checkbox.
            // This is likely to be used by the command line.
            if (!(bool)checkBoxApplicationFocus.IsChecked)
            {
                checkBoxApplicationFocus.IsChecked = true;

                listBoxProcessName.SelectedItem = processName;

                textBoxDelayBefore.Text = delayBefore.ToString();
                textBoxDelayAfter.Text = delayAfter.ToString();
            }

            processName = processName.Trim();

            if (delayBefore > 0)
            {
                System.Threading.Thread.Sleep(delayBefore);
            }

            _screenCapture.ForceFocusOnProcess(processName);

            if (delayAfter > 0)
            {
                System.Threading.Thread.Sleep(delayAfter);
            }
        }
    }
}
