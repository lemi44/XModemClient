using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace XModemClient
{
    /// <summary>
    /// Interaction logic for Wyslij.xaml
    /// </summary>
    public partial class Wyslij : Window
    {
        private bool probowalWyslac;
        public Wyslij()
        {
            InitializeComponent();
            probowalWyslac = false;
        }
        public Wyslij(object dc) : this()
        {
            DataContext = dc;
        }

        private void buttonWroc_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).Komunikaty = "";
        }

        private void textBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!probowalWyslac)
                (DataContext as ViewModel).WyslijPlik(this, buttonWroc);
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBox.ScrollToEnd();
        }

        private void buttonAnuluj_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).AnulujTransmisje(buttonAnuluj);
        }
    }
}
