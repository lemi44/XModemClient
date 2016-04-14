using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            byte[] ret = new byte[2];
            int count = paczka.Count();
            int sumaKontrolnaCRC = 0;
            int n = 0;
            while (--count >= 0)
            {
                sumaKontrolnaCRC = sumaKontrolnaCRC ^ paczka[n++] << 8;                                  // weź znak i dopisz osiem zer
                for (int j = 0; j < 8; ++j)
                    if (Convert.ToBoolean(sumaKontrolnaCRC & 0x8000)) sumaKontrolnaCRC = sumaKontrolnaCRC << 1 ^ 0x1021; // jeśli lewy bit == 1 wykonuj XOR generatorm 1021
                    else sumaKontrolnaCRC = sumaKontrolnaCRC << 1;                                   // jeśli nie to XOR przez 0000, czyli przez to samo
            }
            sumaKontrolnaCRC = (sumaKontrolnaCRC & 0xFFFF);
            int[] binarnie = new int[16];
            int x;
            for (int i = 0; i < 16; i++)
            {
                x = sumaKontrolnaCRC % 2;
                if (x == 1) sumaKontrolnaCRC = (sumaKontrolnaCRC - 1) / 2;
                if (x == 0) sumaKontrolnaCRC = sumaKontrolnaCRC / 2;
                binarnie[15 - i] = x;
            }

            for(int i=0; i<2; i++)
            {
                for (int j = 0; j < 8; j++)
                    ret[i] = (byte)(ret[i] + (int)Math.Pow(2, j) * binarnie[((i+1)*8)-1 - j]);
            }
            return ret;
        }
    }
}
