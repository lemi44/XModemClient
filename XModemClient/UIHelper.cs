using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace XModemClient
{
    public static class UIHelper
    {
        public static void callAsync(UserControl el, Action<Object> action)
        {
            el.Cursor = Cursors.Wait;
            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(action, TaskCreationOptions.LongRunning).ContinueWith(_ => el.Cursor = Cursors.Arrow, uiScheduler);
        }

    }
}