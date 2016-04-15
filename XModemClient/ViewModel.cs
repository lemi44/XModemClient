using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace XModemClient
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            WyszukiwaczPortow wp = new WyszukiwaczPortow();
            _XModem = new XModem();
            _XModem.KomunikatXModem += XModem_KomunikatXModem;
            _listaPortow = new CollectionView(wp.Porty);
            _backgroundworker1 = new BackgroundWorker();
        }

        private void XModem_KomunikatXModem(object sender, KomunikatXModemEventArgs e)
        {
            Komunikaty += e.Komunikat + "\n";
        }
        private BackgroundWorker _backgroundworker1;
        private object _backgroundworker1_sender;
        private Button _backgroundworker1_button;
        private bool _backgroundworker1_CRC;
        private XModem _XModem;
        private string _komunikaty;
        private readonly CollectionView _listaPortow;
        private Port _wybranyPort;
        private string _plik;
        private string _plikzapis;

        public CollectionView ListaPortow
        {
            get { return _listaPortow; }
        }

        public Port WybranyPort
        {
            get { return _wybranyPort; }
            set
            {
                if (_wybranyPort == value) return;
                _wybranyPort = value;
                OnPropertyChanged("WybranyPort");
            }
        }

        public string Komunikaty
        {
            get { return _komunikaty; }
            set
            {
                _komunikaty = value;
                OnPropertyChanged("Komunikaty");
            }
        }

        public void WyslijPlik(object sender, Button button)
        {
            if (!_backgroundworker1.IsBusy)
            {
                _backgroundworker1_sender = sender;
                _backgroundworker1_button = button;
                _backgroundworker1.DoWork += new DoWorkEventHandler(WyslijPlikAsync);
                _backgroundworker1.WorkerSupportsCancellation = true;
                _backgroundworker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WyslijPlikAsyncCompleted);
                _backgroundworker1.RunWorkerAsync();
                (sender as Wyslij).buttonAnuluj.IsEnabled = true;
            }
            else
                Komunikaty += "Program nie może wysłać pliku ponieważ obecnie pracuje";
        }

        private void WyslijPlikAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _backgroundworker1.DoWork -= WyslijPlikAsync;
            _backgroundworker1_button.IsEnabled = true;
            if (_backgroundworker1_sender as Wyslij != null)
                if ((_backgroundworker1_sender as Wyslij).buttonAnuluj != null)
                    (_backgroundworker1_sender as Wyslij).buttonAnuluj.IsEnabled = false;
            _backgroundworker1.RunWorkerCompleted -= WyslijPlikAsyncCompleted;
        }

        private void WyslijPlikAsync(object sender, DoWorkEventArgs e)
        {
            _XModem.WyslijPlik(_wybranyPort, _plik, _backgroundworker1);
        }

        public void OdbierzPlik(object sender, Button button, bool CRC)
        {
            if (!_backgroundworker1.IsBusy)
            {
                _backgroundworker1_sender = sender;
                _backgroundworker1_button = button;
                _backgroundworker1_CRC = CRC;
                _backgroundworker1.DoWork += new DoWorkEventHandler(OdbierzPlikAsync);
                _backgroundworker1.WorkerSupportsCancellation = true;
                _backgroundworker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OdbierzPlikAsyncCompleted);
                _backgroundworker1.RunWorkerAsync();
                (sender as Odbierz).buttonAnuluj.IsEnabled = true;
            }
            else
                Komunikaty += "Program nie może wysłać pliku ponieważ obecnie pracuje";
        }

        private void OdbierzPlikAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _backgroundworker1.DoWork -= OdbierzPlikAsync;
            _backgroundworker1_button.IsEnabled = true;
            if (_backgroundworker1_sender as Odbierz != null)
                if ((_backgroundworker1_sender as Odbierz).buttonAnuluj != null)
                    (_backgroundworker1_sender as Odbierz).buttonAnuluj.IsEnabled = false;
            _backgroundworker1.RunWorkerCompleted -= OdbierzPlikAsyncCompleted;
        }

        public void OdbierzPlikAsync(object sender, DoWorkEventArgs e)
        {
            _XModem.OdbierzPlik(_wybranyPort, _plikzapis, _backgroundworker1_CRC, _backgroundworker1);
        }
        /*
        public int NrWybranegoPortu
        {
            get { return _nrWybranegoPortu; }
            set
            {
                if (_nrWybranegoPortu == value) return;
                if (value != -1)
                    _nrWybranegoPortu = value;
                OnPropertyChanged("NrWybranegoPortu");
            }
        }
        */
        public void AnulujTransmisje(Button btnCancel)
        {
            if (_backgroundworker1.IsBusy && _backgroundworker1.WorkerSupportsCancellation)
            {
                _backgroundworker1.CancelAsync();
                btnCancel.IsEnabled = false;
            }
        }
        public string Plik
        {
            get { return _plik; }
            set
            {
                if (_plik == value) return;
                _plik = value;
                OnPropertyChanged("Plik");
            }
        }
        public string PlikZapis
        {
            get { return _plikzapis; }
            set
            {
                if (_plikzapis == value) return;
                _plikzapis = value;
                OnPropertyChanged("PlikZapis");
            }
        }
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}