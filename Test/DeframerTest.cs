using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Test
{
    public class DeframerTest
    {
        public static void Test()
        {
            Deframer deframer = new Deframer(255);

            byte[] inputArr =
            {
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1, //ASM
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1,
                1, 1, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 1,
                0, 1, 0, 1
            };

            byte[] outputArr = new byte[inputArr.Length];

            deframer.Process(inputArr.Length, inputArr, outputArr);

            //Logger.PrintArr(outputArr, outputArr.Length, "Test M out");
        }
    }
}
