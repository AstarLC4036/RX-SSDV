using NWaves.Utils;
using RX_SSDV.DSP;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Protocol.CCSDS
{
    /// <summary>
    /// Single array version of <see cref="RingBufferIQ"/>
    /// </summary>
    public class RingBufferBinary
    {
        private byte[] buffer;
        public byte[] Buffer => buffer;

        private int inputIndex = 0;
        private int outputIndex = 0;
        private int availableDataCount = 0;
        public int Length => availableDataCount;

        private int bufferSize = 0;
        public int Size => bufferSize;

        public byte this[int index]
        {
            get
            {
                int indexOfArr = index + outputIndex;
                if (indexOfArr >= bufferSize)
                    indexOfArr -= bufferSize;

                if (index < availableDataCount)
                    return buffer[indexOfArr];
                else
                    throw new IndexOutOfRangeException();
            }
            set
            {
                int indexOfArr = index + outputIndex;
                if (indexOfArr >= bufferSize)
                    indexOfArr -= bufferSize;

                if (index < availableDataCount)
                {
                    buffer[indexOfArr] = value;
                }
                else
                    throw new IndexOutOfRangeException();
            }
        }

        public RingBufferBinary(int size)
        {
            bufferSize = size;
            buffer = new byte[bufferSize];
        }

        public void MoveOutputIndex(int delta)
        {
            if (delta < 0)
                throw new ArgumentException("'delta' must greater than zero");
            if (delta < 0)
                throw new InvalidOperationException("'delta' must smaller than 'availableDataCount'");

            outputIndex += delta;

            if (outputIndex >= bufferSize)
            {
                outputIndex -= bufferSize;
            }

            if (availableDataCount > 0)
            {
                availableDataCount -= delta;
            }

            if (availableDataCount < 0)
            {
                availableDataCount = 0;
            }

            //UpdateDataCount();
        }

        public void UpdateDataCount()
        {
            if (inputIndex > outputIndex)
            {
                availableDataCount = inputIndex - outputIndex;
            }
            else if (inputIndex < outputIndex)
            {
                availableDataCount = bufferSize - (outputIndex - inputIndex);
            }
            else
            {
                availableDataCount = 0;
            }
        }

        public void Write(byte[] inputArr, int length)
        {
            if (length > inputArr.Length)
                throw new ArgumentException("The 'length' is to big.");
            if (length >bufferSize)
                throw new ArgumentException("Write length cannot exceed buffer size.");

            int remainedSpace = bufferSize - inputIndex;
            int inputLength = length;
            if(remainedSpace >= inputLength)
            {
                inputArr.FastCopyTo(buffer, inputLength, 0, inputIndex);
                inputIndex += inputLength;

                if (inputIndex == bufferSize)
                    inputIndex = 0;
            }
            else
            {
                int copyLength = inputLength - remainedSpace;
                inputArr.FastCopyTo(buffer, remainedSpace, 0, inputIndex);
                inputIndex = 0;
                inputArr.FastCopyTo(buffer, copyLength, remainedSpace, inputIndex);
                inputIndex += copyLength;
            }

            //UpdateDataCount();

            if (availableDataCount < bufferSize)
            {
                availableDataCount += inputLength;
            }

            if (availableDataCount > bufferSize)
            {
                availableDataCount = bufferSize;
            }
        }

        [Obsolete("Please use RingBufferBinary[index] instead")]
        public void Read(byte[] output, int startIndex = 0, int length = -1)
        {
            if (length == -1)
                length = output.Length;

            int relativeIndex = outputIndex + startIndex;

            if(relativeIndex + length <= bufferSize - 1)
            {
                buffer.FastCopyTo(output, length, relativeIndex, 0);
                outputIndex += length;
            }
            else
            {
                int copyLength = bufferSize - outputIndex;
                int remainLength = length - copyLength;
                buffer.FastCopyTo(output, copyLength, outputIndex, 0);
                outputIndex = 0;
                buffer.FastCopyTo(output, remainLength, outputIndex, copyLength);
                outputIndex += remainLength;
            }
            availableDataCount -= length;
        }
    }
}
