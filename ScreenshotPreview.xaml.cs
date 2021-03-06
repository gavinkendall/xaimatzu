//-----------------------------------------------------------------------
// <copyright file="ScreenshotPreview.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021-2022 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A window that shows a preview of the screenshot.</summary>
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
using System.Windows;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for ScreenshotPreview.xaml
    /// </summary>
    public partial class ScreenshotPreview : Window
    {
        public ScreenshotPreview()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
        }
    }
}
