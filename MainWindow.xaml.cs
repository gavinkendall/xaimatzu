//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021-2022 Gavin Kendall
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
using System.Timers;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Timers.Timer _timerRepeatScreenshots;
        private System.Timers.Timer _timerScheduledScreenshots;

        private Regex _rgxTime;
        private Help _help;
        private Settings _settings;
        private ScreenCapture _screenCapture;
        private ScreenshotPreview _screenshotPreview;
        private FormRegionSelectWithMouse _formRegionSelectWithMouse;

        private delegate void CheckRepeatScreenshotDelegate();
        private delegate void CheckTimedScreenshotDelegate();

        private int _applicationFocusDelayBefore = 0;
        private int _applicationFocusDelayAfter = 0;

        /// <summary>
        /// The main window for the application's interface.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _timerRepeatScreenshots = new System.Timers.Timer(1000);
            _timerRepeatScreenshots.Elapsed += _timerRepeatScreenshots_Elapsed;

            // We check the given date and time every second.
            // If they match with the current date/time then we take a screenshot.
            // We also use this timer elapsed method for updating the screenshot preview window (if it's visible).
            _timerScheduledScreenshots = new System.Timers.Timer(1000);
            _timerScheduledScreenshots.Elapsed += _timerScheduledScreenshots_Elapsed;

            // The various forms to construct.
            _help = new Help();
            _screenCapture = new ScreenCapture();
            _screenshotPreview = new ScreenshotPreview();
            _settings = new Settings(_screenCapture, _screenshotPreview);

            // The regular expression to use for checking against the provided time in the Time text field.
            // (format must be HH:mm:ss)
            _rgxTime = new Regex(@"^\d{2}:\d{2}:\d{2}$");

            _settings.comboBoxFormat.Items.Add("BMP");
            _settings.comboBoxFormat.Items.Add("EMF");
            _settings.comboBoxFormat.Items.Add("GIF");
            _settings.comboBoxFormat.Items.Add("JPEG");
            _settings.comboBoxFormat.Items.Add("PNG");
            _settings.comboBoxFormat.Items.Add("TIFF");
            _settings.comboBoxFormat.Items.Add("WMF");

            _settings.comboBoxFormat.SelectedIndex = 3; // JPEG is the default image format

            _settings.Date.SelectedDate = DateTime.Now.Date;

            _settings.textBoxMacro.Text = AppDomain.CurrentDomain.BaseDirectory + "screenshot.%format%";

            _timerScheduledScreenshots.Start();

            Visibility = (Visibility)ParseCommandLineArguments();
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
                Regex rgxCommandLineOptionActiveWindowTitleMatch = new Regex(@"^-activeWindowTitleMatch=(?<ActiveWindowTitleMatch>.+)$"); // The command line option -activeWindowTitleMatch is the same as -activeWindowTitle (to maintain backwards compatibility with previous versions since -activeWindowTitle is the old command line option)
                Regex rgxCommandLineOptionActiveWindowTitleNoMatch = new Regex(@"^-activeWindowTitleNoMatch=(?<ActiveWindowTitleNoMatch>.+)$"); // The reverse logic. So we do not match on active window title.
                Regex rgxCommandLineOptionActiveWindowTitleMatchType = new Regex(@"^-activeWindowTitleMatchType=(?<ActiveWindowTitleMatchType>\d{1})$");
                Regex rgxCommandLineOptionApplicationFocus = new Regex(@"^-applicationFocus=(?<ApplicationFocus>.+)$");
                Regex rgxCommandLineOptionApplicationFocusDelayBefore = new Regex(@"^-applicationFocusDelayBefore=(?<ApplicationFocusDelayBefore>\d{1,5})$");
                Regex rgxCommandLineOptionApplicationFocusDelayAfter = new Regex(@"^-applicationFocusDelayAfter=(?<ApplicationFocusDelayAfter>\d{1,5})$");
                Regex rgxCommandLineOptionExecutablePath = new Regex(@"^-exePath=(?<ExecutablePath>.+)$");
                Regex rgxCommandLineOptionExecutableArguments = new Regex(@"^-exeArgs=(?<ExecutableArguments>.+)$");
                Regex rgxCommandLineOptionRepeat = new Regex(@"^-repeat=(?<Repeat>.+)$");

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
                        _settings.textBoxX.Text = rgxCommandLineOptionX.Match(arg).Groups["X"].Value;
                    }

                    if (rgxCommandLineOptionY.IsMatch(arg))
                    {
                        _settings.textBoxY.Text = rgxCommandLineOptionY.Match(arg).Groups["Y"].Value;
                    }

                    if (rgxCommandLineOptionWidth.IsMatch(arg))
                    {
                        _settings.textBoxWidth.Text = rgxCommandLineOptionWidth.Match(arg).Groups["Width"].Value;
                    }

                    if (rgxCommandLineOptionHeight.IsMatch(arg))
                    {
                        _settings.textBoxHeight.Text = rgxCommandLineOptionHeight.Match(arg).Groups["Height"].Value;
                    }

                    if (rgxCommandLineOptionDate.IsMatch(arg))
                    {
                        if (int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Year"].Value, out int year) &&
                            int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Month"].Value, out int month) &&
                            int.TryParse(rgxCommandLineOptionDate.Match(arg).Groups["Day"].Value, out int day))
                        {
                            DateTime dt = new DateTime(year, month, day);

                            _settings.Date.SelectedDate = dt;
                        }
                    }

                    if (rgxCommandLineOptionTime.IsMatch(arg))
                    {
                        _settings.textBoxTime.Text = rgxCommandLineOptionTime.Match(arg).Groups["Time"].Value;
                    }

                    if (rgxCommandLineOptionFormat.IsMatch(arg))
                    {
                        _settings.comboBoxFormat.SelectedItem = rgxCommandLineOptionFormat.Match(arg).Groups["Format"].Value.ToUpper();
                    }

                    if (rgxCommandLineOptionFile.IsMatch(arg))
                    {
                        _settings.textBoxMacro.Text = rgxCommandLineOptionFile.Match(arg).Groups["File"].Value;
                    }

                    if (rgxCommandLineOptionSave.IsMatch(arg))
                    {
                        checkBoxSave.IsChecked = true;
                    }

                    if (rgxCommandLineOptionClipboard.IsMatch(arg))
                    {
                        checkBoxClipboard.IsChecked = true;
                    }

                    if (rgxCommandLineOptionActiveWindow.IsMatch(arg))
                    {
                        _settings.radioButtonActiveWindow.IsChecked = true;
                    }

                    if (rgxCommandLineOptionActiveWindowTitle.IsMatch(arg))
                    {
                        string activeWindowTitle = rgxCommandLineOptionActiveWindowTitle.Match(arg).Groups["ActiveWindowTitle"].Value;

                        if (activeWindowTitle.Length > 0)
                        {
                            SetActiveWindowTitleMatch(activeWindowTitle);
                        }
                    }

                    if (rgxCommandLineOptionActiveWindowTitleMatch.IsMatch(arg))
                    {
                        string activeWindowTitle = rgxCommandLineOptionActiveWindowTitleMatch.Match(arg).Groups["ActiveWindowTitleMatch"].Value;

                        if (activeWindowTitle.Length > 0)
                        {
                            SetActiveWindowTitleMatch(activeWindowTitle);
                        }
                    }

                    if (rgxCommandLineOptionActiveWindowTitleNoMatch.IsMatch(arg))
                    {
                        string activeWindowTitle = rgxCommandLineOptionActiveWindowTitleNoMatch.Match(arg).Groups["ActiveWindowTitleNoMatch"].Value;

                        if (activeWindowTitle.Length > 0)
                        {
                            SetActiveWindowTitleNoMatch(activeWindowTitle);
                        }
                    }

                    if (rgxCommandLineOptionActiveWindowTitleMatchType.IsMatch(arg))
                    {
                        int.TryParse(rgxCommandLineOptionActiveWindowTitleMatchType.Match(arg).Groups["ActiveWindowTitleMatchType"].Value, out int activeWindowTitleMatchType);

                        switch (activeWindowTitleMatchType)
                        {
                            case 1:
                                _settings.radioButtonCaseSensitiveMatch.IsChecked = true;
                                break;

                            case 2:
                                _settings.radioButtonCaseInsensitiveMatch.IsChecked = true;
                                break;

                            case 3:
                                _settings.radioButtonRegularExpressionMatch.IsChecked = true;
                                break;
                        }
                    }

                    if (rgxCommandLineOptionApplicationFocusDelayBefore.IsMatch(arg))
                    {
                        int.TryParse(rgxCommandLineOptionApplicationFocusDelayBefore.Match(arg).Groups["ApplicationFocusDelayBefore"].Value, out int applicationFocusDelayBefore);

                        _applicationFocusDelayBefore = applicationFocusDelayBefore;
                        _settings.textBoxDelayBefore.Text = applicationFocusDelayBefore.ToString();
                    }

                    if (rgxCommandLineOptionApplicationFocusDelayAfter.IsMatch(arg))
                    {
                        int.TryParse(rgxCommandLineOptionApplicationFocusDelayAfter.Match(arg).Groups["ApplicationFocusDelayAfter"].Value, out int applicationFocusDelayAfter);

                        _applicationFocusDelayAfter = applicationFocusDelayAfter;
                        _settings.textBoxDelayAfter.Text = applicationFocusDelayAfter.ToString();
                    }

                    if (rgxCommandLineOptionExecutablePath.IsMatch(arg))
                    {
                        checkBoxRunExecutable.IsChecked = true;

                        _settings.textBoxExecutablePath.Text = rgxCommandLineOptionExecutablePath.Match(arg).Groups["ExecutablePath"].Value;
                    }

                    if (rgxCommandLineOptionExecutableArguments.IsMatch(arg))
                    {
                        _settings.textBoxExecutableArguments.Text = rgxCommandLineOptionExecutableArguments.Match(arg).Groups["ExecutableArguments"].Value;
                    }

                    if (rgxCommandLineOptionRepeat.IsMatch(arg))
                    {
                        _settings.checkBoxRepeat.IsChecked = true;
                        _settings.textBoxRepeatSecond.Text = rgxCommandLineOptionRepeat.Match(arg).Groups["Repeat"].Value;
                    }
                }

                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (rgxCommandLineOptionApplicationFocus.IsMatch(arg))
                    {
                        string applicationFocus = rgxCommandLineOptionApplicationFocus.Match(arg).Groups["ApplicationFocus"].Value;

                        if (applicationFocus.Length > 0)
                        {
                            _settings.RefreshProcessList();
                            _settings.DoApplicationFocus(applicationFocus, _applicationFocusDelayBefore, _applicationFocusDelayAfter);
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

        private void _timerRepeatScreenshots_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new CheckRepeatScreenshotDelegate(CheckRepeatScreenshots));
        }

        private void _timerScheduledScreenshots_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new CheckTimedScreenshotDelegate(CheckScheduledScreenshot));
        }

        private void CheckRepeatScreenshots()
        {
            buttonTakeScreenshot_Click(null, null);
        }

        private void CheckScheduledScreenshot()
        {
            if (!(bool)_settings.checkBoxRepeat.IsChecked)
            {
                _timerRepeatScreenshots.Stop();
            }

            DateTime dt = DateTime.Now;

            if (_settings.Date.SelectedDate.Value.ToString("yyyy-MM-dd").Equals(dt.ToString("yyyy-MM-dd")) &&
                _rgxTime.IsMatch(_settings.textBoxTime.Text) && _settings.textBoxTime.Text.Equals(dt.ToString("HH:mm:ss")))
            {
                buttonTakeScreenshot_Click(null, null);
            }

            _settings.UpdateScreenshotPreview();
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            _help.Show();
        }

        private void buttonImageControls_Click(object sender, RoutedEventArgs e)
        {
            _settings.Show();
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.Show();
        }

        private void buttonRegionSelect_Click(object sender, RoutedEventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse(checkBoxClipboard.IsChecked);
            _formRegionSelectWithMouse.MouseSelectionCompleted += formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        private void formRegionSelectWithMouse_RegionSelectMouseSelectionCompleted(object sender, EventArgs e)
        {
            int x = _formRegionSelectWithMouse.outputX + 1;
            int y = _formRegionSelectWithMouse.outputY + 1;
            int width = _formRegionSelectWithMouse.outputWidth - 2;
            int height = _formRegionSelectWithMouse.outputHeight - 2;

            _settings.textBoxX.Text = x.ToString();
            _settings.textBoxY.Text = y.ToString();
            _settings.textBoxWidth.Text = width.ToString();
            _settings.textBoxHeight.Text = height.ToString();

            _formRegionSelectWithMouse.Close();
        }

        private void buttonRegionSelectTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            _formRegionSelectWithMouse = new FormRegionSelectWithMouse(checkBoxClipboard.IsChecked);
            _formRegionSelectWithMouse.MouseSelectionCompleted += formRegionSelectTakeScreenshotWithMouse_RegionSelectMouseSelectionCompleted;
            _formRegionSelectWithMouse.LoadCanvas();
        }

        private void formRegionSelectTakeScreenshotWithMouse_RegionSelectMouseSelectionCompleted(object sender, EventArgs e)
        {
            int x = _formRegionSelectWithMouse.outputX + 1;
            int y = _formRegionSelectWithMouse.outputY + 1;
            int width = _formRegionSelectWithMouse.outputWidth - 2;
            int height = _formRegionSelectWithMouse.outputHeight - 2;

            _settings.textBoxX.Text = x.ToString();
            _settings.textBoxY.Text = y.ToString();
            _settings.textBoxWidth.Text = width.ToString();
            _settings.textBoxHeight.Text = height.ToString();

            buttonTakeScreenshot_Click(sender, null);

            _formRegionSelectWithMouse.Close();
        }

        private void buttonScreenshotPreview_Click(object sender, RoutedEventArgs e)
        {
            _settings.screenshotPreview.Show();
        }

        private void buttonTakeScreenshot_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)_settings.checkBoxRepeat.IsChecked)
            {
                double.TryParse(_settings.textBoxRepeatSecond.Text, out double seconds);

                if (seconds > 0)
                {
                    _timerRepeatScreenshots.Interval = 1000 * seconds;
                    _timerRepeatScreenshots.Start();
                }
            }

            _settings.TakeScreenshot((bool)checkBoxClipboard.IsChecked, (bool)checkBoxSave.IsChecked, (bool)checkBoxRunExecutable.IsChecked);
        }

        private void SetActiveWindowTitleMatch(string activeWindowTitleTextComparison)
        {
            activeWindowTitleTextComparison = activeWindowTitleTextComparison.Trim();

            _settings.checkBoxActiveWindowTitleComparisonCheck.IsChecked = true;

            _settings.textBoxActiveWindowTitleTextComparison.Text = activeWindowTitleTextComparison;
        }

        private void SetActiveWindowTitleNoMatch(string activeWindowTitleTextComparison)
        {
            activeWindowTitleTextComparison = activeWindowTitleTextComparison.Trim();

            _settings.checkBoxActiveWindowTitleComparisonCheckReverse.IsChecked = true;

            _settings.textBoxActiveWindowTitleTextComparison.Text = activeWindowTitleTextComparison;
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