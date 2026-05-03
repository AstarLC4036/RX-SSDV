using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Test
{
    public class DelayTest
    {
        public static void Test()
        {
            BitDelay delay = new BitDelay(1);
            TestFunc(delay);
            TestFunc(delay);
        }

        public static void TestFunc(BitDelay delay)
        {
            byte[] input = new byte[32];
            byte[] output = new byte[32];
            Random random = new Random();
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)random.Next(2);
            }

            delay.Process(32, input, output);
            PrintResult(input, output);
        }

        public static void PrintResult(byte[] inputArr, byte[] outputArr)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;

            Logger.LogInfo("Input array");
            for (int i = 0; i < inputArr.Length; i++)
            {
                sb.Append(inputArr[i]);
                count++;

                if (count >= 32)
                {
                    count = 0;
                    Logger.LogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            count = 0;
            sb.Clear();
            Logger.LogInfo($"Output array");
            for (int i = 0; i < outputArr.Length; i++)
            {
                sb.Append(outputArr[i]);
                count++;

                if (count >= 32)
                {
                    count = 0;
                    Logger.LogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            count = 0;
            sb.Clear();
        }
    }
}
