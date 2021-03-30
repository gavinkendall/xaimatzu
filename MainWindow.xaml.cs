using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace xaimatzu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            buttonCaptureNow.IsEnabled = false;

            comboBoxScreen.Items.Add("<Select Screen>");

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                comboBoxScreen.Items.Add(screen.DeviceName);
            }

            comboBoxScreen.SelectedIndex = 0;

            comboBoxFormat.Items.Add("BMP");
            comboBoxFormat.Items.Add("EMF");
            comboBoxFormat.Items.Add("GIF");
            comboBoxFormat.Items.Add("JPEG");
            comboBoxFormat.Items.Add("PNG");
            comboBoxFormat.Items.Add("TIFF");
            comboBoxFormat.Items.Add("WMF");

            comboBoxFormat.SelectedIndex = 3;

            Date.SelectedDate = DateTime.Now.Date;

            textBoxFile.Text = AppDomain.CurrentDomain.BaseDirectory + "screenshot.%format%";
            textBoxFile.Focus();
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
            buttonCaptureNow.IsEnabled = false;
            imageScreenshotPreview.Source = null;

            if (!string.IsNullOrEmpty(textBoxFile.Text) &&
                int.TryParse(textBoxX.Text, out int x) &&
                int.TryParse(textBoxY.Text, out int y) &&
                int.TryParse(textBoxWidth.Text, out int width) &&
                int.TryParse(textBoxHeight.Text, out int height))
            {
                imageScreenshotPreview.Source = TakeScreenshot(x, y, width, height, out Bitmap bitmap);
                    
                if (imageScreenshotPreview.Source != null)
                {
                    buttonCaptureNow.IsEnabled = true;
                }
            }
        }

        private BitmapSource TakeScreenshot(int x, int y, int width, int height, out Bitmap bitmap)
        {
            BitmapSource bitmapSource = null;

            try
            {
                var screenBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        screenBmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }

                bitmap = screenBmp;

                return bitmapSource;
            }
            catch
            {
                bitmap = null;

                return null;
            }
            finally
            {
                GC.Collect();
            }
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var encoders = ImageCodecInfo.GetImageEncoders();
            return encoders.FirstOrDefault(t => t.MimeType == mimeType);
        }

        private void buttonCaptureNow_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxFile.Text) &&
                int.TryParse(textBoxX.Text, out int x) &&
                int.TryParse(textBoxY.Text, out int y) &&
                int.TryParse(textBoxWidth.Text, out int width) &&
                int.TryParse(textBoxHeight.Text, out int height))
            {
                Hide();

                TakeScreenshot(x, y, width, height, out Bitmap bitmap);

                if (bitmap != null)
                {
                    string format = comboBoxFormat.SelectedItem.ToString().ToLower();
                    string path = textBoxFile.Text.Replace("%format%", format);

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
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, jpegQuality);

                        var encoderInfo = GetEncoderInfo("image/jpeg");

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

                Show();
            }
        }
    }
}
