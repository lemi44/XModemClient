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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XModemClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel();
            
        }
        public MainWindow(object dc)
        {
            InitializeComponent();
            DataContext = dc;
            //wyborPortu.SelectedIndex = (dc as ViewModel).NrWybranegoPortu;
        }

        private void buttonWybierzPlik_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                (DataContext as ViewModel).Plik = filename;
            }
        }

        private void buttonWybierzFolder_Click(object sender, RoutedEventArgs e)
        {
            /*
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string foldername = dlg.SelectedPath;
                (DataContext as ViewModel).Folder = foldername;
            }
            */
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                (DataContext as ViewModel).PlikZapis = filename;
            }
        }

        private void buttonWyslij_Click(object sender, RoutedEventArgs e)
        {
            Wyslij wyslij = new Wyslij(DataContext);
            Hide();
            wyslij.ShowDialog();
            Show();
        }

        private void buttonOdbierz_Click(object sender, RoutedEventArgs e)
        {
            bool CRC;
            if (radioButton.IsChecked == true)
            {
                CRC = true;
            }
            else
                CRC = false;
            Odbierz odbierz = new Odbierz(DataContext,CRC);
            Hide();
            odbierz.ShowDialog();
            Show();

        }
    }
}
