using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Test
{
    public class MDecoderTest
    {
        public static void Test()
        {
            MDecoder decoder = new MDecoder();

            //float[] inputArr = 
            //{ 
            //    0,0,0,1,0,0,1,1,
            //    0,1,1,1,0,1,0,1,
            //    0,1,0,1,0,1,1,1,
            //    1,1,1,0,1,0,0,1,
            //    1,1,1,0,1,1,0,0,
            //    1,0,0,0,1,0,1,0,
            //    1,0,1,0,1,0,0,0,
            //    0,0,0,1,0,1,1,0
            //};

            byte[] inputArr =
            {
                0,1,0,1,1,0,1,0,0,1,0,1,0,0,1,0,1,0,1,1,1,0,1,0,1,1,1,1,0,1,1,0,1,0
            };

            byte[] outputArr = new byte[inputArr.Length];

            decoder.Process(inputArr.Length, inputArr, outputArr);

            Logger.PrintArr(outputArr, outputArr.Length, "Test M out");
        }
    }
}
