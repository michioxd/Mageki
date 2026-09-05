using System;
using Mageki.Utils;
using Rg.Plugins.Popup.Extensions;
using Xamarin.Forms;

namespace Mageki
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            _ = Update.CheckUpdateAsync();
        }
    }
}
