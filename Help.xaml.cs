//-----------------------------------------------------------------------
// <copyright file="Help.xaml.cs" company="Gavin Kendall">
//     Copyright (c) 2021 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A window to show useful help information.</summary>
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
using System.IO;
using System.Text;
using System.Windows;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        public Help()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxHelp.Items.Add("About");
            listBoxHelp.Items.Add("Welcome");

            listBoxHelp.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
        }

        private void listBoxHelp_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            richTextBoxHelpText.Document.Blocks.Clear();

            MemoryStream stream;

            switch (listBoxHelp.SelectedIndex)
            {
                case 0:
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.help_0_About));
                    richTextBoxHelpText.Selection.Load(stream, DataFormats.Rtf);
                    break;

                case 1:
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Resources.help_1_Welcome));
                    richTextBoxHelpText.Selection.Load(stream, DataFormats.Rtf);
                    break;
            }

            richTextBoxHelpText.CaretPosition = richTextBoxHelpText.CaretPosition.DocumentStart;
            richTextBoxHelpText.ScrollToHome();
        }
    }
}
