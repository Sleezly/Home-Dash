using HashBoard;
using System;
using Windows.UI.Xaml.Controls;

namespace Hashboard
{
    public sealed partial class WebLinkControl : UserControl
    {
        public WebLinkControl(Entity entity, double width, double height)
        {
            this.InitializeComponent();

            this.RequestedTheme = ThemeControl.GetApplicationTheme();

            if (this.FindName("RootWebView") is WebView webView)
            {
                webView.Width = width - 30;
                webView.Height = height - 30;
                webView.Navigate(new Uri(entity.State));
            }
        }
    }
}
