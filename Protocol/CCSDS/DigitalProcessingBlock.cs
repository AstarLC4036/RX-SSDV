using RX_SSDV.DSP;
using RX_SSDV.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Protocol.CCSDS
{
    //DspBlock but no IQ ^-^
    /// <summary>
    /// Single channel version of <see cref="DspBlock"/>
    /// </summary>
    public class DigitalProcessingBlock
    {
        public RingBufferBinary historyBuffer;

        public DigitalProcessingBlock()
        {
            SetHistory(SampleSource.WAV_BUFFER_SIZE * 2);
        }

        /// <summary>
        /// Clear the buffer and allocate new one.
        /// </summary>
        /// <param name="bufferSize">New buffer size</param>
        public virtual void SetHistory(int bufferSize)
        {
            historyBuffer = new RingBufferBinary(bufferSize);
        }

        public virtual void Process(byte[] inputArr, byte[] outArr, int inputSize)
        {
            if (inputSize > inputArr.Length)
                throw new ArgumentException("'inputSize' is too big!");

            historyBuffer.Write(inputArr, inputSize);
        }

        public virtual int Process(int inputSize, byte[] inputArr, byte[] outputArr)
        {
            Process(inputArr, outputArr, inputSize);
            CompleteProcess(inputSize);
            return outputArr.Length;
        }

        protected void CompleteProcess(int outputSize)
        {
            historyBuffer.MoveOutputIndex(outputSize);
        }
    }
}
