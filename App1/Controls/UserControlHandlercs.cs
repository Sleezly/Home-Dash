using HashBoard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Hashboard
{
    public class UserControlHandler
    {
        private Entity PanelEntity;

        public UserControlHandler(Entity entity)
        {
            PanelEntity = entity;
        }

        public void BitmapIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SendAction("media_play");
        }

        private void SendAction(string action)
        {
            string json = "{\"entity_id\":\"" + PanelEntity.EntityId + "\"}";
            //WebRequests.SendData(MainPage.hostname, PanelEntity.EntityId.Split('.')[0], action, MainPage.apiPassword, json);
        }
    }
}
