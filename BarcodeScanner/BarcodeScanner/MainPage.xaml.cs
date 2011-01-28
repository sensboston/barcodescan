/*
* Project name: BarcodeScanner
* 
* Copyright 2011 SeNSSoFT, www.senssoft.com
*
* Author: Sergey Svinolobov aka SeNS
* 
* Project code based on Kevin Marshall's code
* 
* Project uses open source zxing library, http://code.google.com/p/zxing/
* 
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;

using com.google.zxing;
using com.google.zxing.common;
using com.google.zxing.qrcode;

using Microsoft.Phone;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Xna.Framework.Media;

namespace BarcodeScanner
{
    public partial class MainPage : PhoneApplicationPage
    {
        private PhotoCamera _camera;
        private MultiFormatReader _mfr;
        private string _imageName = "";

        private int _skipFrames = 0;
        private bool _IsHTC = false;
        private int SkipedFramesMax { get { return _IsHTC ? 5 : 2;  } }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Detect HTC handsets: they have better built-in autofocus
            string result = string.Empty;
            object manufacturer;
            if (DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out manufacturer))
            {
                result = manufacturer.ToString();
                if (result.Contains("HTC")) _IsHTC = true;
            }
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Hide controls, show camera visualizer
            buttonsPanel.Visibility = Visibility.Collapsed;
            infoText.Visibility = Visibility.Collapsed;
            imageCanvas.Visibility = Visibility.Collapsed;

            txtStatus.Foreground = new SolidColorBrush(Colors.White);
            txtStatus.Text = "Scanning...";

            cameraVisualizer.Visibility = Visibility.Visible;


            InitializeCamera();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _camera.Dispose();
            _camera = null;
        }

        private void InitializeCamera()
        {
            _camera = new PhotoCamera();
            _camera.Initialized += OnCameraInitialized;
            cameraVisualizer.SetSource(_camera);
        }

        void OnCameraInitialized(object sender, EventArgs e)
        {
            _camera.Initialized -= OnCameraInitialized;
            _camera.SetResolution(_camera.GetAvailableResolutions().FirstOrDefault(r => r.Width == 640));
            _camera.FlashMode = FlashMode.Off;

            _mfr = new MultiFormatReader();

            GrabFrame();
        }

        private void GrabFrame()
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_camera != null && cameraVisualizer.Visibility == Visibility.Visible)
                {
                    txtStatus.Text = "Scanning...";

                    WriteableBitmap wb = new WriteableBitmap(640, 480);
                    
                    _camera.GetCurrentFrame(wb);
                    wb.Invalidate();

                    ScanBarcode(wb);

                    GrabFrame();
                }
            });
        }

        private void ScanBarcode(WriteableBitmap wb)
        {
            RGBLuminanceSource lum = new RGBLuminanceSource(wb, wb.PixelWidth, wb.PixelHeight);
            HybridBinarizer binarizer = new HybridBinarizer(lum);
            BinaryBitmap binBmp = new BinaryBitmap(binarizer);

            try
            {
                var result = _mfr.decode(binBmp);
                if (result != null)
                {
                    PointCollection linePoints = new PointCollection();
                    foreach (ResultPoint p in result.ResultPoints)
                        linePoints.Add(new Point(p.X, p.Y));
                    barcodeBounds.Points = linePoints;

                    txtStatus.Text = String.Format("Found barcode: {0} = {1}", result.BarcodeFormat.Name, result.Text);
                    txtStatus.Foreground = new SolidColorBrush(Colors.Yellow);
                    _imageName = "barcode_" + result.BarcodeFormat.Name + "_" + result.Text;
                    
                    snapshotImage.Source = new WriteableBitmap(wb);

                    // Hide camera visualizer, show controls
                    cameraVisualizer.Visibility = Visibility.Collapsed;
                    infoText.Visibility = Visibility.Visible;
                    imageCanvas.Visibility = Visibility.Visible;
                    buttonsPanel.Visibility = Visibility.Visible;
                }
                else AutoFocus();
            }
            catch 
            {
                AutoFocus();
            }
        }

        private void AutoFocus()
        {
            if (_skipFrames++ > SkipedFramesMax)
            {
                _camera.Focus();
                _skipFrames = 0;
            }
        }

        private void LayoutRoot_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (buttonsPanel.Visibility == Visibility.Visible)
            {
                // Hide controls, show camera visualizer
                buttonsPanel.Visibility = Visibility.Collapsed;
                infoText.Visibility = Visibility.Collapsed;
                imageCanvas.Visibility = Visibility.Collapsed;

                txtStatus.Foreground = new SolidColorBrush(Colors.White);
                txtStatus.Text = "Scanning...";

                cameraVisualizer.Visibility = Visibility.Visible;

                GrabFrame();
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            CompositeTransform cameraTrans = cameraVisualizer.RenderTransform as CompositeTransform;
            CompositeTransform canvasTrans = imageCanvas.RenderTransform as CompositeTransform;

            if (e.Orientation == PageOrientation.LandscapeRight)
            {
                cameraTrans.Rotation = canvasTrans.Rotation = 180;
            }
            else
            {
                cameraTrans.Rotation = canvasTrans.Rotation = 0;
            }
        }

        #region Services
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageName != "")
                using (MemoryStream stream = new MemoryStream())
                {
                    WriteableBitmap wb = snapshotImage.Source as WriteableBitmap;
                    wb.SaveJpeg(stream, wb.PixelWidth, wb.PixelHeight, 0, 80);
                    stream.Seek(0, SeekOrigin.Begin);
                    using (MediaLibrary library = new MediaLibrary()) library.SavePicture(_imageName, stream);
                }
        }

        private void bingButton_Click(object sender, RoutedEventArgs e)
        {
            string s = _imageName.Replace("_", " ").Replace("barcode", "").Trim();
            if (s.Contains("UPC") || s.Contains("EAN") || s.Contains("CODE"))
            {
                int pos = s.IndexOf(" ")+1;
                s = s.Remove(pos, s.IndexOf(" ", pos) - pos + 1);
            }
            SearchTask search = new SearchTask() { SearchQuery = s };
            search.Show();
        }

        private void googleButton_Click(object sender, RoutedEventArgs e)
        {
            string s = _imageName.Replace("_", "+").Replace("barcode", "").Trim().Remove(0, 1);
            if (s.Contains("UPC") || s.Contains("EAN") || s.Contains("CODE"))
            {
                int pos = s.IndexOf("+") + 1;
                s = s.Remove(pos, s.IndexOf("+", pos) - pos + 1);
            }
            NavigationService.Navigate(new Uri("/Browser.xaml?url=" + Uri.EscapeUriString("http://www.google.com/m/search?q="+s), UriKind.Relative));
        }

        private void mailButton_Click(object sender, RoutedEventArgs e)
        {
            string s = "Barcode " + _imageName.Replace("_", " ").Replace("barcode", "") + " found";
            EmailComposeTask mail = new EmailComposeTask() { Subject = s, Body = s };
            mail.Show();
        }

        private void smsButton_Click(object sender, RoutedEventArgs e)
        {
            string s = "Barcode " + _imageName.Replace("_", " ").Replace("barcode", "") + " found";
            SmsComposeTask sms = new SmsComposeTask() { Body = s };
            sms.Show();
        }
        #endregion
    }
}