using HashBoard;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Hashboard
{
    public abstract partial class BaseControl : UserControl
    {

        public abstract void EntityUpdated(Entity entity);

    }
}