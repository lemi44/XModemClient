using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XModemClient;

namespace XModemUnitTest
{
    [TestClass]
    public class CRCUnitTest
    {
        [TestMethod]
        public void PoliczTest()
        {
            byte[] p = new byte[] { 10 };
            byte[] crc = CRC.Policz(p);
            Assert.AreEqual(0xA14A, BitConverter.ToUInt16(crc, 0));
        }
        [TestMethod]
        public void SprawdzTest()
        {
            byte[] p = new byte[] { 10 };
            byte[] crc = BitConverter.GetBytes((ushort)0xA14A);
            byte[] badcrc = BitConverter.GetBytes((ushort)0);
            Assert.IsTrue(CRC.Sprawdz(p, crc));
            Assert.IsFalse(CRC.Sprawdz(p, badcrc));
        }
    }
}
