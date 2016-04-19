using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace XModemClient
{
    public class XModem
    {
        private int kod;
        byte[] paczka;
        private int nrPakietu;
        private int nrPakietuDo255;
        private bool CzyNawiazanoTransmisje;
        public event KomunikatXModemEventHandler KomunikatXModem;
        public delegate void KomunikatXModemEventHandler(object sender, KomunikatXModemEventArgs e);

        public XModem()
        {
            kod = 0;
            paczka = new byte[128];
            nrPakietu = 1;
            CzyNawiazanoTransmisje = false;
        }

        public bool WyslijPlik(Port port, string nazwaPliku, BackgroundWorker bgwrk)
        {
            kod = 0;
            paczka = new byte[128];
            nrPakietu = 1;
            if(port==null)
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Nie znaleziono portu"));
                return false;
            }
            if (nazwaPliku == null || nazwaPliku == "")
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Nie ustawiono pliku"));
                return false;
            }
            if(!File.Exists(nazwaPliku))
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Plik nie istnieje"));
                return false;
            }
            if(bgwrk.CancellationPending)
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                return false;
            }
            using (SerialPort comPort = new SerialPort(port.PortName))
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Ustawiam parametry (9600kb/s, 8-bitowe dane, jeden bit stopu)"));
                comPort.BaudRate = 9600;
                comPort.Parity = Parity.None;
                comPort.StopBits = StopBits.One;
                comPort.DataBits = 8;
                comPort.DtrEnable = false;
                comPort.RtsEnable = false;
                comPort.ReadTimeout = 10000;
                comPort.WriteTimeout = 100;
                KomunikatXModem(this, new KomunikatXModemEventArgs("Próbuję otworzyć port " + port?.InstanceName + "..."));
                comPort.Open();
                if (comPort.IsOpen)
                {
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Otworzono port"));
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Rozpoczynam wysyłanie pliku " + nazwaPliku));
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Oczekiwanie na rozpoczęcie transmisji..."));
                    char odczytanyZnak;
                    for (int i = 0; i < 6; i++)
                    {
                        try
                        {
                            if (bgwrk.CancellationPending)
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                try
                                {
                                    comPort.Write(new byte[] { 24 }, 0, 1);
                                }
                                catch (Exception e)
                                { }
                                return false;
                            }
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + "..."));
                            odczytanyZnak = Convert.ToChar(comPort.ReadChar());
                            string komunikat = "Otrzymano znak:[" + odczytanyZnak + "]";

                            if (odczytanyZnak == 'X')
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs(komunikat));
                                kod = 1;
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else if (odczytanyZnak == (char)21)
                            {
                                komunikat += "/[NAK]";
                                KomunikatXModem(this, new KomunikatXModemEventArgs(komunikat));
                                kod = 2;
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs(komunikat));
                            }
                        }
                        catch (TimeoutException e)
                        {
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + " zakończona niepowodzeniem"));
                            KomunikatXModem(this, new KomunikatXModemEventArgs(e.Message));
                            continue;
                        }
                    }
                    if (!CzyNawiazanoTransmisje)
                    {
                        KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia zakończona niepowodzeniem"));
                        return false;
                    }
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Połączenie zostało nawiązane, próba odczytania pliku..."));
                    using (FileStream plik = new FileStream(nazwaPliku, FileMode.Open))
                    {
                        using (BinaryReader czytnik = new BinaryReader(plik))
                        {
                            while (czytnik.BaseStream.Position!=czytnik.BaseStream.Length)
                            {
                                if (bgwrk.CancellationPending)
                                {
                                    KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                    comPort.Write(new byte[] { 24 }, 0, 1);
                                    return false;
                                }
                                for (int i = 0; i < 128; i++)
                                    paczka[i] = 26;
                                int w = 0;
                                while(w<128 && czytnik.BaseStream.Position != czytnik.BaseStream.Length)
                                {
                                    paczka[w] = (byte)plik.ReadByte();
                                    if (czytnik.BaseStream.Position == czytnik.BaseStream.Length)
                                        paczka[w] = 26;
                                    w++;
                                }
                                bool czyPoprawnyPakiet = false;
                                while(!czyPoprawnyPakiet)
                                {
                                    if (bgwrk.CancellationPending)
                                    {
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                        comPort.Write(new byte[] { 24 }, 0, 1);
                                        return false;
                                    }
                                    KomunikatXModem(this, new KomunikatXModemEventArgs("Trwa wysyłanie pakietu nr "+nrPakietu+", dopełnienie "+(255 - nrPakietu)+", proszę czekać..."));
                                    comPort.Write(new byte[] { 1 }, 0, 1); //SOH
                                    comPort.Write(new byte[] { (byte)nrPakietu }, 0, 1);
                                    comPort.Write(new byte[] { (byte)(255 - nrPakietu) }, 0, 1);
                                    for(int i=0; i<128; i++)
                                        comPort.Write(paczka, i, 1);
                                    if (kod == 2) //suma kontrolna
                                    {
                                        byte suma_kontrolna = 26;
                                        for (int i = 0; i < 128; i++)
                                            suma_kontrolna = (byte)((suma_kontrolna + paczka[i]) % 256);
                                        comPort.Write(new byte[] { suma_kontrolna }, 0, 1);
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("Suma kontrolna = " + suma_kontrolna));
                                    }
                                    else if (kod == 1) //obliczanie CRC i transfer
                                    {
                                        byte[] crc = CRC.Policz(paczka);
                                        comPort.Write(crc, 0, crc.Count());
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("CRC = " + BitConverter.ToUInt16(crc, 0).ToString("X4")));
                                    }
                                    while(true)
                                    {
                                        try
                                        {
                                            if (bgwrk.CancellationPending)
                                            {
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                                comPort.Write(new byte[] { 24 }, 0, 1);
                                                return false;
                                            }
                                            byte znak = (byte)comPort.ReadByte();
                                            if(znak==6) //ACK
                                            {
                                                czyPoprawnyPakiet = true;
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("Przesłano poprawnie pakiet danych"));
                                                break;
                                            }
                                            if(znak==21) //NAK
                                            {
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - otrzymano NAK"));
                                                break;
                                            }
                                            if(znak==24) //CAN
                                            {
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - połączenie przerwane"));
                                                return false;
                                            }

                                        }
                                        catch(System.TimeoutException e)
                                        {
                                            KomunikatXModem(this, new KomunikatXModemEventArgs(e.Message));
                                            return false;
                                        }
                                    }
                                }
                                if (nrPakietu == 255)
                                    nrPakietu = 1;
                                else
                                    nrPakietu++;
                            }
                        }
                    }
                    while (true)
                    {
                        if (bgwrk.CancellationPending)
                        {
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                            comPort.Write(new byte[] { 24 }, 0, 1);
                            return false;
                        }
                        comPort.Write(new byte[] { 4 }, 0, 1); // SEND EOT
                        try
                        {
                            byte znak = (byte)comPort.ReadByte();

                            if (znak == 6) //ACK
                                break;
                        }
                        catch(System.TimeoutException e)
                        {

                        }
                    }
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Wysłano plik"));
                    return true;
                }
                else
                {
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Otworzenie portu się niepowiodło"));

                }
            }
            return false;
        }
        public bool OdbierzPlik(Port port, string nazwaPliku, bool IsCRC, BackgroundWorker bgwrk)
        {
            paczka = new byte[128];
            if (port == null)
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Nie znaleziono portu"));
                return false;
            }
            if (nazwaPliku == null || nazwaPliku == "")
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Nie ustawiono pliku"));
                return false;
            }
            if (bgwrk.CancellationPending)
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                return false;
            }
            using (SerialPort comPort = new SerialPort(port.PortName))
            {
                KomunikatXModem(this, new KomunikatXModemEventArgs("Ustawiam parametry (9600kb/s, 8-bitowe dane, jeden bit stopu)"));
                comPort.BaudRate = 9600;
                comPort.Parity = Parity.None;
                comPort.StopBits = StopBits.One;
                comPort.DataBits = 8;
                comPort.DtrEnable = false;
                comPort.RtsEnable = false;
                comPort.ReadTimeout = 10000;
                comPort.WriteTimeout = 100;
                KomunikatXModem(this, new KomunikatXModemEventArgs("Próbuję otworzyć port " + port?.InstanceName + "..."));
                comPort.Open();
                if (comPort.IsOpen)
                {
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Otworzono port"));
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Rozpoczynam odbieranie pliku"));
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia..."));
                    char odczytanyZnak=(char)24;
                    for (int i = 0; i < 6; i++)
                    {
                        try
                        {
                            if (bgwrk.CancellationPending)
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                try
                                {
                                    comPort.Write(new byte[] { 24 }, 0, 1);
                                }
                                catch(Exception e)
                                { }
                                return false;
                            }
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + "..."));
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Wysyłanie"));
                            byte[] znak;
                            if (IsCRC)
                                znak = Encoding.ASCII.GetBytes("X");
                            else
                                znak = new byte[] { 21 }; //NAK
                            comPort.Write(znak, 0, znak.Count());
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Oczekiwanie na SOH..."));
                            odczytanyZnak = Convert.ToChar(comPort.ReadChar());
                            string komunikat = "Otrzymano znak:[" + odczytanyZnak + "]";

                            if (odczytanyZnak == (char)1)
                            {
                                komunikat += "/[SOH]";
                                KomunikatXModem(this, new KomunikatXModemEventArgs(komunikat));
                                KomunikatXModem(this, new KomunikatXModemEventArgs("Nawiązano połączenie"));
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs(komunikat));
                            }
                        }
                        catch (TimeoutException e)
                        {
                            KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + " zakończona niepowodzeniem"));
                            KomunikatXModem(this, new KomunikatXModemEventArgs(e.Message));
                            continue;
                        }
                    }
                    if (!CzyNawiazanoTransmisje)
                    {
                        KomunikatXModem(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia zakończona niepowodzeniem"));
                        return false;
                    }
                    KomunikatXModem(this, new KomunikatXModemEventArgs("Połączenie zostało nawiązane, próba odebrania pliku..."));
                    using (FileStream fs = new FileStream(nazwaPliku, FileMode.Create))
                    {
                        using (BinaryWriter plik = new BinaryWriter(fs))
                        {
                            try
                            {
                                while (true)
                                {
                                    if (bgwrk.CancellationPending)
                                    {
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                        comPort.Write(new byte[] { 24 }, 0, 1);
                                        return false;
                                    }
                                    byte[] sumcheck;
                                    nrPakietu = comPort.ReadByte();
                                    nrPakietuDo255 = comPort.ReadByte();
                                    for(int i=0; i<128; i++)
                                        paczka[i]=(byte)comPort.ReadByte();
                                    if (IsCRC)
                                    {
                                        sumcheck = new byte[2];
                                        comPort.Read(sumcheck, 0, 1);
                                        comPort.Read(sumcheck, 1, 1);
                                    }
                                    else
                                    {
                                        sumcheck = new byte[1];
                                        comPort.Read(sumcheck, 0, 1);
                                    }
                                    bool poprawnyPakiet = true;
                                    KomunikatXModem(this, new KomunikatXModemEventArgs("Odebrano pakiet nr: "+nrPakietu+", dopelnienie: "+nrPakietuDo255));
                                    if (bgwrk.CancellationPending)
                                    {
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                        comPort.Write(new byte[] { 24 }, 0, 1);
                                        return false;
                                    }

                                    if ((255 - nrPakietu) != nrPakietuDo255)
                                    {
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - otrzymano niepoprawny numer pakietu: "+nrPakietu+", dopelnienie "+nrPakietuDo255));
                                        comPort.Write(new byte[] { 21 }, 0, 1); //NAK
                                        poprawnyPakiet = false;
                                    }
                                    else
                                    {
                                        if (IsCRC)
                                        {
                                            KomunikatXModem(this, new KomunikatXModemEventArgs("CRC = " + BitConverter.ToUInt16(sumcheck,0).ToString("X4")));
                                            if (!CRC.Sprawdz(paczka,sumcheck))
                                            {
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - zła suma kontrolna"));
                                                comPort.Write(new byte[] { 21 }, 0, 1); //NAK
                                                poprawnyPakiet = false;
                                            }
                                        }
                                        else
                                        {
                                            byte suma_kontrolna = 26;
                                            for (int i = 0; i < 128; i++)
                                                suma_kontrolna = (byte)((suma_kontrolna + paczka[i]) % 256);
                                            KomunikatXModem(this, new KomunikatXModemEventArgs("Suma kontrolna = " + sumcheck[0]));
                                            if (suma_kontrolna != sumcheck[0])
                                            {
                                                KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - zła suma kontrolna"));
                                                comPort.Write(new byte[] { 21 }, 0, 1); //NAK
                                                poprawnyPakiet = false;
                                            }
                                        }
                                    }

                                    if (poprawnyPakiet)
                                    {
                                        for (int i = 0; i < 128; i++)
                                        {
                                            if (paczka[i] != 26)
                                                plik.Write(paczka[i]);
                                        }
                                        if (bgwrk.CancellationPending)
                                        {
                                            KomunikatXModem(this, new KomunikatXModemEventArgs("Anulowano transmisję"));
                                            comPort.Write(new byte[] { 24 }, 0, 1);
                                            return false;
                                        }
                                        KomunikatXModem(this, new KomunikatXModemEventArgs("Odebranie pakietu zakończone sukcesem"));
                                        comPort.Write(new byte[] { 6 }, 0, 1); //ACK
                                    }
                                    odczytanyZnak = (char)comPort.ReadChar();
                                    if (odczytanyZnak == (char)4 || odczytanyZnak == (char)24) //EOT or CAN
                                        break;
                                    KomunikatXModem(this, new KomunikatXModemEventArgs("Kontynuuję odbiór danych..."));
                                }
                            }
                            catch(System.TimeoutException e)
                            {
                                KomunikatXModem(this, new KomunikatXModemEventArgs(e.Message));
                                odczytanyZnak = (char)24;
                            }
                        }
                    }
                    comPort.Write(new byte[] { 6 }, 0, 1); //ACK
                    if (odczytanyZnak == (char)4)
                    {
                        KomunikatXModem(this, new KomunikatXModemEventArgs("Odebrano poprawnie plik"));
                        return true;
                    }
                    else
                    {
                        KomunikatXModem(this, new KomunikatXModemEventArgs("ERROR - połączenie zostało przerwane"));
                        return false;
                    }
                }
            }
            return false;
        }
    }
    
    public class KomunikatXModemEventArgs : EventArgs
    {
        private string komunikat;
        public string Komunikat
        {
            get { return komunikat; }
            set { komunikat = value; }
        }
        public KomunikatXModemEventArgs(string komunikat)
        {
            this.komunikat = komunikat;
        }
    }
}
