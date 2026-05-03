using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS;
using RX_SSDV.Protocol.CCSDS.Viterbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Test
{
    public class CCSDS_Test
    {
        public static void Test()
        {
            byte[] input = {
                0,1,0,0,0,1,0,1,
                0,0,0,0,0,0,0,0,
                0,0,0,0,1,1,0,1,
                0,0,0,0,0,0,0,0,
                0,0,1,0,0,0,1,0,
                0,0,0,0,0,0,0,0,
                1,1,1,1,1,0,0,1,
                0,0,0,0,0,0,0,0,
                0,0,1,0,0,0,1,0,
                0,0,1,0,0,0,0,1,
                1,0,1,1,1,1,1,0,
                0,0,0,0,0,0,0,0,
                0,0,1,0,0,1,1,1,
                0,0,0,0,0,0,0,0,
                0,0,0,1,0,1,0,0,
                0,0,0,0,0,0,0,0,
                1,0,1,1,0,1,1,0,
                0,0,0,0,0,0,0,0,
                0,0,1,0,1,0,1,0,
                0,0,0,0,0,0,0,0,
                0,1,1,1,1,0,0,0,
                0,0,0,0,0,0,0,0,
                0,1,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,
                1,1,0,1,0,1,1,0,
                0,0,0,0,0,0,0,0,
                0,1,0,1,1,0,0,0,
                0,0,0,0,0,0,0,0,
                0,1,0,0,1,1,1,1,
                0,0,0,0,0,0,0,0,
                0,1,0,0,0,1,1,0,
                0,0,0,0,0,0,0,0,
                0,0,0,1,0,0,0,1
            };

            byte[] inputM =
            {

            };
            byte[] outputViterbi = new byte[input.Length];
            byte[] outputM = new byte[input.Length];

            Viterbi viterbi = new Viterbi();
            MDecoder decoder = new MDecoder();

            int vLength = viterbi.Process(input.Length, input, outputViterbi);
            int mLength = decoder.Process(vLength, outputViterbi, outputM);

            Logger.LogInfo($"Input arr size: {input.Length}, Viterbi output size； {vLength}, M output size： {mLength}");
            Logger.PrintArr(outputViterbi, vLength, "Viterbi");
            Logger.PrintArr(outputM, mLength, "M Decoder");
        }
    }
}
