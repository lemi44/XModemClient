using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public bool WyslijPlik(Port port, string nazwaPliku)
        {
            kod = 0;
            paczka = new byte[128];
            nrPakietu = 1;
            if(port==null)
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Nie znaleziono portu"));
                return false;
            }
            if (nazwaPliku == null || nazwaPliku == "")
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Nie ustawiono pliku"));
                return false;
            }
            if(!File.Exists(nazwaPliku))
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Plik nie istnieje"));
                return false;
            }
            using (SerialPort comPort = new SerialPort(port.PortName))
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Ustawiam parametry (9600kb/s, 8-bitowe dane, jeden bit stopu)"));
                comPort.BaudRate = 9600;
                comPort.Parity = Parity.None;
                comPort.StopBits = StopBits.One;
                comPort.DataBits = 8;
                comPort.DtrEnable = false;
                comPort.RtsEnable = false;
                comPort.ReadTimeout = 10000;
                comPort.WriteTimeout = 100;
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próbuję otworzyć port " + port?.InstanceName + "..."));
                comPort.Open();
                if (comPort.IsOpen)
                {
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Otworzono port"));
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Rozpoczynam wysyłanie pliku " + nazwaPliku));
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Oczekiwanie na rozpoczęcie transmisji..."));
                    char odczytanyZnak;
                    for (int i = 0; i < 6; i++)
                    {
                        try
                        {
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + "..."));
                            odczytanyZnak = Convert.ToChar(comPort.ReadChar());
                            string komunikat = "Otrzymano znak:[" + odczytanyZnak + "]";

                            if (odczytanyZnak == 'X')
                            {
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(komunikat));
                                kod = 1;
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else if (odczytanyZnak == (char)21)
                            {
                                komunikat += "/[NAK]";
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(komunikat));
                                kod = 2;
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else
                            {
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(komunikat));
                            }
                        }
                        catch (TimeoutException e)
                        {
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + " zakończona niepowodzeniem"));
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(e.Message));
                            continue;
                        }
                    }
                    if (!CzyNawiazanoTransmisje)
                    {
                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia zakończona niepowodzeniem"));
                        return false;
                    }
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Połączenie zostało nawiązane, próba odczytania pliku..."));
                    using (FileStream plik = new FileStream(nazwaPliku, FileMode.Open))
                    {
                        using (BinaryReader czytnik = new BinaryReader(plik))
                        {
                            while (czytnik.BaseStream.Position!=czytnik.BaseStream.Length)
                            {
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
                                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Trwa wysyłanie pakietu nr "+nrPakietu+", dopełnienie "+(255 - nrPakietu)+", proszę czekać..."));
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
                                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Suma kontrolna = " + suma_kontrolna));
                                    }
                                    else if (kod == 1) //obliczanie CRC i transfer
                                    {
                                        byte[] crc = CRC.Policz(paczka);
                                        comPort.Write(crc, 0, crc.Count());
                                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("CRC = " + (crc[0]+crc[1]<<8)));
                                    }
                                    while(true)
                                    {
                                        try
                                        {
                                            byte znak = (byte)comPort.ReadByte();
                                            if(znak==6) //ACK
                                            {
                                                czyPoprawnyPakiet = true;
                                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Przesłano poprawnie pakiet danych"));
                                                break;
                                            }
                                            if(znak==21) //NAK
                                            {
                                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - otrzymano NAK"));
                                                break;
                                            }
                                            if(znak==24) //CAN
                                            {
                                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - połączenie przerwane"));
                                                return false;
                                            }

                                        }
                                        catch(System.TimeoutException e)
                                        {
                                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(e.Message));
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
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Wysłano plik"));
                    return true;
                }
                else
                {
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Otworzenie portu się niepowiodło"));

                }
            }
            return false;
        }
        public bool OdbierzPlik(Port port, string nazwaPliku, bool IsCRC)
        {
            paczka = new byte[128];
            if (port == null)
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Nie znaleziono portu"));
                return false;
            }
            if (nazwaPliku == null || nazwaPliku == "")
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Nie ustawiono pliku"));
                return false;
            }
            using (SerialPort comPort = new SerialPort(port.PortName))
            {
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Ustawiam parametry (9600kb/s, 8-bitowe dane, jeden bit stopu)"));
                comPort.BaudRate = 9600;
                comPort.Parity = Parity.None;
                comPort.StopBits = StopBits.One;
                comPort.DataBits = 8;
                comPort.DtrEnable = false;
                comPort.RtsEnable = false;
                comPort.ReadTimeout = 10000;
                comPort.WriteTimeout = 100;
                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próbuję otworzyć port " + port?.InstanceName + "..."));
                comPort.Open();
                if (comPort.IsOpen)
                {
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Otworzono port"));
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Rozpoczynam odbieranie pliku"));
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia..."));
                    char odczytanyZnak=(char)24;
                    for (int i = 0; i < 6; i++)
                    {
                        try
                        {
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + "..."));
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Wysyłanie"));
                            byte[] znak;
                            if (IsCRC)
                                znak = Encoding.ASCII.GetBytes("X");
                            else
                                znak = new byte[] { 21 }; //NAK
                            comPort.Write(znak, 0, znak.Count());
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Oczekiwanie na SOH..."));
                            odczytanyZnak = Convert.ToChar(comPort.ReadChar());
                            string komunikat = "Otrzymano znak:[" + odczytanyZnak + "]";

                            if (odczytanyZnak == (char)1)
                            {
                                komunikat += "/[SOH]";
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(komunikat));
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Nawiązano połączenie"));
                                CzyNawiazanoTransmisje = true;
                                break;
                            }
                            else
                            {
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(komunikat));
                            }
                        }
                        catch (TimeoutException e)
                        {
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nr " + (i + 1) + " zakończona niepowodzeniem"));
                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(e.Message));
                            continue;
                        }
                    }
                    if (!CzyNawiazanoTransmisje)
                    {
                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Próba nawiązania połączenia zakończona niepowodzeniem"));
                        return false;
                    }
                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Połączenie zostało nawiązane, próba odebrania pliku..."));
                    using (FileStream fs = new FileStream(nazwaPliku, FileMode.Create))
                    {
                        using (BinaryWriter plik = new BinaryWriter(fs))
                        {
                            try
                            {
                                while (true)
                                {
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

                                    if ((255 - nrPakietu) != nrPakietuDo255)
                                    {
                                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - otrzymano niepoprawny numer pakietu: "+nrPakietu+", dopelnienie "+nrPakietuDo255));
                                        comPort.Write(new byte[] { 21 }, 0, 1); //NAK
                                        poprawnyPakiet = false;
                                    }
                                    else
                                    {
                                        if (IsCRC)
                                        {
                                            byte[] tmpCRC = CRC.Policz(paczka);
                                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Suma kontrolna = " + (sumcheck[0]+sumcheck[1]<<8)));
                                            if (sumcheck[0] != tmpCRC[0] || sumcheck[1] != tmpCRC[1])
                                            {
                                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - zła suma kontrolna"));
                                                comPort.Write(new byte[] { 21 }, 0, 1); //NAK
                                                poprawnyPakiet = false;
                                            }
                                        }
                                        else
                                        {
                                            byte suma_kontrolna = 26;
                                            for (int i = 0; i < 128; i++)
                                                suma_kontrolna = (byte)((suma_kontrolna + paczka[i]) % 256);
                                            KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Suma kontrolna = " + sumcheck[0]));
                                            if (suma_kontrolna != sumcheck[0])
                                            {
                                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - zła suma kontrolna"));
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
                                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Odebranie pakietu zakończone sukcesem"));
                                        comPort.Write(new byte[] { 6 }, 0, 1); //ACK
                                    }
                                    odczytanyZnak = (char)comPort.ReadChar();
                                    if (odczytanyZnak == (char)4 || odczytanyZnak == (char)24) //EOT or CAN
                                        break;
                                    KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Kontynuuję odbiór danych..."));
                                }
                            }
                            catch(System.TimeoutException e)
                            {
                                KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs(e.Message));
                                odczytanyZnak = (char)24;
                            }
                        }
                    }
                    comPort.Write(new byte[] { 6 }, 0, 1); //ACK
                    if (odczytanyZnak == (char)4)
                    {
                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("Odebrano poprawnie plik"));
                        return true;
                    }
                    else
                    {
                        KomunikatXModem?.Invoke(this, new KomunikatXModemEventArgs("ERROR - połączenie zostało przerwane"));
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
