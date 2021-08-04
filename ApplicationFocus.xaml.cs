//-----------------------------------------------------------------------
// <copyright file="ApplicationFocus.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A window for controlling the forced focus on a selected application from a process list.</summary>
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
using System.Diagnostics;
using System.Windows;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for ApplicationFocus.xaml
    /// </summary>
    public partial class ApplicationFocus : Window
    {
        private ScreenCapture _screenCapture;

        public ApplicationFocus(ScreenCapture screenCapture)
        {
            InitializeComponent();

            _screenCapture = screenCapture;

            textBoxDelayBefore.Text = "0";
            textBoxDelayAfter.Text = "0";

            buttonRefreshProcessList_Click(null, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
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

        private void checkBoxApplicationFocus_Checked(object sender, RoutedEventArgs e)
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
