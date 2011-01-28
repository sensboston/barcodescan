using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;

namespace BarcodeScanner
{
    public partial class Browser : PhoneApplicationPage
    {
        public Browser()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get url
            string url = "";
            try { url = this.NavigationContext.QueryString["url"]; } catch {}
            if (url != "")
            {
                Dispatcher.BeginInvoke(() =>
                {
                    httpAddress.Text = url;
                    webBrowser.Navigate(new Uri(url));
                });
            }
        }

        private void httpAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                webBrowser.Navigate(new Uri(httpAddress.Text));
            }
        }

        private void webBrowser_Navigated_1(object sender, NavigationEventArgs e)
        {
            httpAddress.Text = e.Uri.ToString();
        }

    }
}