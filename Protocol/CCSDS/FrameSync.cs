using RX_SSDV.Base;
using static RX_SSDV.Utils.BinaryUtils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Protocol.CCSDS
{
    public class FrameSync : DigitalProcessingBlock
    {
        public const uint syncHead = 0x1ACFFC1D; //0b_0001_1010_1100_1111_1111_1100_0001_1101
        public const uint ssdvSyncSymbol = 0x0322; //0b_0011_0010_0010
        public const int syncSymbolSize = 32;

        public FrameSync() { }

        public override int Process(int inputSize, byte[] inputArr, byte[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);

            int processedCount = 0;
            for (int i = 0; i + syncSymbolSize <= historyBuffer.Length; i++)
            {
                processedCount++;

                uint window = 0;
                for (int j = 0; j < syncSymbolSize; j++)
                {
                    window <<= 1;
                    window |= (byte)historyBuffer[i + j];
                }

                //Logger.CLogInfo("[FrameSync-Debug]" + Convert.ToString(window, 2));

                if (HammingDst(syncHead, window) <= 2 || HammingDst(syncHead, ~window) <= 2)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced at {i}");
                }
                else if (ssdvSyncSymbol == window || ssdvSyncSymbol == ~window)
                {
                    Logger.CLogInfo($"[FrameSync-Debug]Synced SSDV at {i}");
                }
            }

            CompleteProcess(processedCount);
            return 0;
        }
    }
}
