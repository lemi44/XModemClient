using System;
using System.Collections.Generic;
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
    /// Interaction logic for Odbierz.xaml
    /// </summary>
    public partial class Odbierz : Window
    {
        private bool probowalOdebrac;
        private bool IsCRC;
        public Odbierz()
        {
            InitializeComponent();
            probowalOdebrac = false;
        }

        public Odbierz(object dc, bool CRC) : this()
        {
            DataContext = dc;
            IsCRC = CRC;
        }

        private void buttonWroc_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void textBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (!probowalOdebrac)
                (DataContext as ViewModel).OdbierzPlik(this, buttonWroc, IsCRC);
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBox.ScrollToEnd();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).Komunikaty = "";
        }
    }
}
