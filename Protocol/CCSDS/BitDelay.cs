using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Protocol.CCSDS
{
    public class BitDelay : DigitalProcessingBlock
    {
        private int delayBits = 0;
        private byte[] delayBuffer;

        public BitDelay(int delayBits)
        {
            this.delayBits = delayBits;
            delayBuffer = new byte[delayBits];
        }

        public override int Process(int inputSize, byte[] inputArr, byte[] outputArr)
        {
            //base.Process(inputArr, outputArr, inputSize);

            for (int i = 0; i < delayBits; i++)
            {
                outputArr[i] = delayBuffer[i];
            }

            int delayIndex = inputSize - delayBits;
            for (int i = 0; i < delayIndex; i++)
            {
                outputArr[delayBits + i] = inputArr[i];
            }

            for(int i = 0; i < delayBits; i++)
            {
                delayBuffer[i] = inputArr[delayIndex + i];
            }

            //CompleteProcess(processedCount);
            return inputSize;
        }
    }
}
