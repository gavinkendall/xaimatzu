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
using System.Drawing;
using System.Timers;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Media.Imaging;

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
        private HowToUse _howToUse;
        private ImageControls _imageControls;
        private ScreenCapture _screenCapture;
        private ScreenshotPreview _screenshotPreview;
        private FormRegionSelectWithMouse _formRegionSelectWithMouse;
        private delegate void CheckTimedScreenshotDelegate();

        private int _applicationFocusDelayBefore = 0;
        private int _applicationFocusDelayAfter = 0;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += _timer_Elapsed;

            _about = new About();
            _howToUse = new HowToUse();
            _rgxTime = new Regex(@"^\d{2}:\d{2}:\d{2}$");
            _screenCapture = new ScreenCapture();
            _screenshotPreview = new ScreenshotPreview();
            _imageControls = new ImageControls(_screenshotPreview);

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

            int visibility = ParseCommandLineArguments();

            Visibility = (Visibility)visibility;
        }

        private void Main_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Exit(prompt: true);
        }

        private int ParseCommandLineArguments()
        {
            int visibility = 0;

            try
            {
                Regex rgxCommandLineOptionX = new Regex(@"^-x=(?<X>\d+)$");
                Regex rgxCommandLineOptionY = new Regex(@"^-y=(?<Y>\d+)$");
                Regex rgxCommandLineOptionWidth = new Regex(@"^-width=(?<Width>\d+)$");
                Regex rgxCommandLineOptionHeight = new Regex(@"^-height=(?<Height>\d+)$");
                Regex rgxCommandLineOptionDate = new Regex(@"^-date=(?<Year>\d{4})-(?<Month>\d{2})-(?<Day>\d{2})$");
                Regex rgxCommandLineOptionTime = new Regex(@"^-time=(?<Time>\d{2}:\d{2}:\d{2})$");
                Regex rgxCommandLineOptionFormat = new Regex(@"^-format=(?<Format>.+)$");
                Regex rgxCommandLineOptionFile = new Regex(@"^-file=(?<File>.+)$");
                Regex rgxCommandLineOptionSave = new Regex(@"^-save$");
                Regex rgxCommandLineOptionClipboard = new Regex(@"^-clipboard$");
                Regex rgxCommandLineOptionActiveWindow = new Regex(@"^-activeWindow$");
                Regex rgxCommandLineOptionActiveWindowTitle = new Regex(@"^-activeWindowTitle=(?<ActiveWindowTitle>.+)$");
                Regex rgxCommandLineOptionApplicationFocus = new Regex(@"^-applicationFocus=(?<ApplicationFocus>.+)$");
                Regex rgxCommandLineOptionApplicationFocusDelayBefore = new Regex(@"^-applicationFocusDelayBefore=(?<ApplicationFocusDelayBefore>\d{1,5})$");
                Regex rgxCommandLineOptionApplicationFocusDelayAfter = new Regex(@"^-applicationFocusDelayAfter=(?<ApplicationFocusDelayAfter>\d{1,5})$");

                // Prepares all values ready for screen capture.
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.Equals("-kill"))
                    {
                        int thisProcessId = Process.GetCurrentProcess().Id;

                        foreach (var process in Process.GetProcessesByName("xaimatzu"))
                        {
                            if (!process.Id.Equals(thisProcessId))
                            {
                                process.Kill();
                            }
                        }
                    }

                    if (rgxCommandLineOptionX.IsMatch(arg))
                    {
                        _imageControls.textBoxX.Text = rgxCommandLineOptionX.Match(arg).Groups["X"].Value;
                    }

                    if (rgxCommandLineOptionY.IsMatch(arg))
                    {
                        _imageControls.textBoxY.Text = rgxCommandLineOptionY.Match(arg).Groups["Y"].Value;
                    }

                    if (rgxCommandLineOptionWidth.IsMatch(arg))
                    {
                        _imageControls.textBoxWidth.Text = rgxCommandLineOptionWidth.Match(arg).Groups["Width"].Value;
                    }

                    if (rgxCommandLineOptionHeight.IsMatch(arg))
                    {
                        _imageControls.textBoxHeight.Text = rgxCommandLineOptionHeight.Match(arg).Groups["Height"].Value;
                    }

                    if (rgxCommandLineOptionDate.IsMatch(arg))
                    {
                        if (int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Year"].Value, out int year) &&
                            int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Month"].Value, out int month) &&
                            int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Day"].Value, out int day))
                        {
                            DateTime dt = new DateTime(year, month, day);

                            _imageControls.Date.SelectedDate = dt;
                        }
                    }

                    if (rgxCommandLineOptionTime.IsMatch(arg))
                    {
                        _imageControls.textBoxTime.Text = rgxCommandLineOptionTime.Match(arg).Groups["Time"].Value;
                    }

                    if (rgxCommandLineOptionFormat.IsMatch(arg))
                    {
                        _imageControls.comboBoxFormat.SelectedItem = rgxCommandLineOptionFormat.Match(arg).Groups["Format"].Value.ToUpper();
                    }

                    if (rgxCommandLineOptionFile.IsMatch(arg))
                    {
                        _imageControls.textBoxFile.Text = rgxCommandLineOptionFile.Match(arg).Groups["File"].Value;
                    }

                    if (rgxCommandLineOptionSave.IsMatch(arg))
                    {
                        _imageControls.checkBoxSave.IsChecked = true;
                    }

                    if (rgxCommandLineOptionClipboard.IsMatch(arg))
                    {
                        _imageControls.checkBoxClipboard.IsChecked = true;
                    }

                    if (rgxCommandLineOptionActiveWindow.IsMatch(arg))
                    {
                        _imageControls.ActiveWindow = true;
                    }

                    if (rgxCommandLineOptionActiveWindowTitle.IsMatch(arg))
                    {
                        string activeWindowTitle = rgxCommandLineOptionActiveWindowTitle.Match(arg).Groups["ActiveWindowTitle"].Value;

                        if (activeWindowTitle.Length > 0)
                        {
                            SetActiveWindowTitle(activeWindowTitle);
                        }
                    }

                    if (rgxCommandLineOptionApplicationFocusDelayBefore.IsMatch(arg))
                    {
                        int.TryParse(rgxCommandLineOptionApplicationFocusDelayBefore.Match(arg).Groups["ApplicationFocusDelayBefore"].Value, out int applicationFocusDelayBefore);

                        _applicationFocusDelayBefore = applicationFocusDelayBefore;
                    }

                    if (rgxCommandLineOptionApplicationFocusDelayAfter.IsMatch(arg))
                    {
                        int.TryParse(rgxCommandLineOptionApplicationFocusDelayAfter.Match(arg).Groups["ApplicationFocusDelayAfter"].Value, out int applicationFocusDelayAfter);

                        _applicationFocusDelayAfter = applicationFocusDelayAfter;
                    }
                }

                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (rgxCommandLineOptionApplicationFocus.IsMatch(arg))
                    {
                        string applicationFocus = rgxCommandLineOptionApplicationFocus.Match(arg).Groups["ApplicationFocus"].Value;

                        if (applicationFocus.Length > 0)
                        {
                            SetApplicationFocus(applicationFocus);
                        }
                    }
                }

                // Takes screenshot when found -capture command line option (now that values are prepared).
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.Equals("-capture"))
                    {
                        buttonTakeScreenshot_Click(null, null);
                    }

                    if (arg.Equals("-exit"))
                    {
                        Exit(prompt: false);
                    }

                    if (arg.Equals("-hide"))
                    {
                        visibility = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }

            return visibility;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new CheckTimedScreenshotDelegate(CheckTimedScreenshot));
        }

        private void CheckTimedScreenshot()
        {
            if (_imageControls.Date.SelectedDate.Value.ToString("yyyy-MM-dd").Equals(DateTime.Now.ToString("yyyy-MM-dd")) &&
                _rgxTime.IsMatch(_imageControls.textBoxTime.Text) && _imageControls.textBoxTime.Text.Equals(DateTime.Now.ToString("HH:mm:ss")))
            {
                buttonTakeScreenshot_Click(null, null);
            }

            _imageControls.UpdatePreview();
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            _about.Show();
        }

        private void buttonHowToUse_Click(object sender, RoutedEventArgs e)
        {
            _howToUse.Show();
        }

        private void buttonImageControls_Click(object sender, RoutedEventArgs e)
        {
            _imageControls.Show();
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

            if ((bool)_imageControls.checkBoxSave.IsChecked)
            {
                buttonTakeScreenshot_Click(sender, null);
            }

            _formRegionSelectWithMouse.Close();
        }

        private void buttonScreenshotPreview_Click(object sender, RoutedEventArgs e)
        {
            _imageControls.screenshotPreview.Show();
        }

        private void buttonTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            Bitmap bitmap = null;
            BitmapSource bitmapSource = null;

            try
            {
                bool clipboard = (bool)_imageControls.checkBoxClipboard.IsChecked;

                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;

                if (_imageControls.ActiveWindow)
                {
                    if (!string.IsNullOrEmpty(_imageControls.textBoxFile.Text))
                    {
                        bitmapSource = _screenCapture.TakeScreenshot(0, 0, 0, 0, captureActiveWindow: true, out bitmap);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(_imageControls.textBoxFile.Text) &&
                        int.TryParse(_imageControls.textBoxX.Text, out x) &&
                        int.TryParse(_imageControls.textBoxY.Text, out y) &&
                        int.TryParse(_imageControls.textBoxWidth.Text, out width) &&
                        int.TryParse(_imageControls.textBoxHeight.Text, out height))
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

                _screenCapture.SaveScreenshot(bitmap, _imageControls.textBoxFile.Text, _imageControls.comboBoxFormat.SelectedItem.ToString().ToLower());
            }
            catch (Exception ex)
            {
                Error(ex);
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        private void SetActiveWindowTitle(string activeWindowTitle)
        {
            activeWindowTitle = activeWindowTitle.Trim();

            _imageControls.ActiveWindow = true;

            _screenCapture.ActiveWindowTitle = activeWindowTitle;
        }

        private void SetApplicationFocus(string applicationFocus)
        {
            applicationFocus = applicationFocus.Trim();

            if (_applicationFocusDelayBefore > 0)
            {
                System.Threading.Thread.Sleep(_applicationFocusDelayBefore);
            }

            _screenCapture.SetApplicationFocus(applicationFocus);

            if (_applicationFocusDelayAfter > 0)
            {
                System.Threading.Thread.Sleep(_applicationFocusDelayAfter);
            }
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Exit(prompt: true);
        }

        private void Exit(bool prompt)
        {
            if (!prompt)
            {
                Environment.Exit(0);
            }

            DialogResult dialogResult = (DialogResult)System.Windows.MessageBox.Show("Are you sure you want to exit Xaimatzu?", "Exit Xaimatzu", (MessageBoxButton)MessageBoxButtons.YesNo, (MessageBoxImage)MessageBoxIcon.Question);

            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                Environment.Exit(0);
            }
        }

        private void Error(Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(ex.Message + " " + ex.StackTrace, "Xaimatzu - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}