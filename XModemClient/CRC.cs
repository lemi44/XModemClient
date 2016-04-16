using System;
using System.Linq;

namespace XModemClient
{
    public static class CRC
    {
        /// <summary>
        /// Oblicza CRC
        /// </summary>
        /// <param name="paczka">Tablica z której jest obliczany CRC</param>
        /// <returns>Dwuelementowa tablica typu byte</returns>
        public static byte[] Policz(byte[] paczka)
        {
            int count = paczka.Count();
            ushort sumaKontrolnaCRC = 0;
            int n = 0;
            while (--count >= 0)
            {
                sumaKontrolnaCRC = (ushort)(sumaKontrolnaCRC ^ paczka[n++] << 8);                                  // weź znak i dopisz osiem zer
                for (int j = 0; j < 8; ++j)
                    if (Convert.ToBoolean(sumaKontrolnaCRC & 0x8000)) sumaKontrolnaCRC = (ushort)(sumaKontrolnaCRC << 1 ^ 0x1021); // jeśli lewy bit == 1 wykonuj XOR generatorm 1021
                    else sumaKontrolnaCRC = (ushort)(sumaKontrolnaCRC << 1);                                   // jeśli nie to XOR przez 0000, czyli przez to samo
            }
            sumaKontrolnaCRC = (ushort)(sumaKontrolnaCRC & 0xFFFF);
            return BitConverter.GetBytes(sumaKontrolnaCRC);
        }
    }
}
