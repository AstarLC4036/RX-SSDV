using NWaves.Utils;
using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS.Viterbi;
using RX_SSDV.Decoder;
using RX_SSDV.IO;
using RX_SSDV.Utils;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Protocol.CCSDS
{
    public class CCSDSDecoder : IDecoder
    {
        //Processors
        private Viterbi.Viterbi viterbiDecoder0;
        private Viterbi.Viterbi viterbiDecoder1;
        private MDecoder mDecoder0;
        private MDecoder mDecoder1;
        private Deframer deframer0;
        private Deframer deframer1;
        private BitDelay delay;

        //Decoder
        private ITransportDecoder? decoder;

        //Decoder Config
        private bool useDiffDecode = false;
        private bool useDescrambling = false;
        private int packetSize = 255;

        //Buffers
        private byte[] inputBuffer;
        private byte[] outputBuffer;

        private byte[] hardDecisionBits;
        private byte[] packetByteBuffer;

        public const int DIGITAL_BUFFER_SIZE = 1024;

        public CCSDSDecoder()
        {
            Init(true, true, 255, null);
        }

        public CCSDSDecoder(bool useDiffDecode, bool useDescrambling, int frameSize, ITransportDecoder decoder)
        {
            Init(useDiffDecode, useDescrambling, frameSize, decoder);
        }

        public void Init(bool useDiffDecode, bool useDescrambling, int frameSize, ITransportDecoder? decoder)
        {
            this.useDiffDecode = useDiffDecode;
            this.useDescrambling = useDescrambling;
            this.decoder = decoder;
            packetSize = frameSize;

            InitProcessingFlow();
            CheckProcessOutputArr(DIGITAL_BUFFER_SIZE);
        }

        public void InitProcessingFlow()
        {
            viterbiDecoder0 = new Viterbi.Viterbi();
            viterbiDecoder1 = new Viterbi.Viterbi();
            mDecoder0 = new MDecoder();
            mDecoder1 = new MDecoder();
            deframer0 = new Deframer(packetSize * 8);
            deframer1 = new Deframer(packetSize * 8);

            packetByteBuffer = new byte[packetSize];
            deframer0.onPacketProcess += ProcessPacket;
            deframer1.onPacketProcess += ProcessPacket;

            delay = new BitDelay(1);
        }

        private void ConfigureOutput()
        {
            byte[] temp = outputBuffer;
            outputBuffer = inputBuffer;
            inputBuffer = temp;
        }

        public static void HardDecision(float[] inputSamplesI, float[] inputSamplesQ, byte[] outputBits, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }

            for (int i = 0; i < inputSize; i++)
            {
                outputBits[i] = (byte)(inputSamplesI[i] > 0 ? 1 : 0);
            }
        }

        public void Process(float[] inputSamplesI, float[] inputSamplesQ, byte[] outputBits, out int outputSize, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }
            
            HardDecision(inputSamplesI, inputSamplesQ, hardDecisionBits, inputSize);

            //Branch Normal
            int outputSize0 = viterbiDecoder0.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            if (useDiffDecode) { outputSize0 = mDecoder0.Process(outputSize0, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer0.Process(outputSize0, inputBuffer, outputBuffer); ConfigureOutput();

            //Branch Delay
            delay.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            int outputSize1 = viterbiDecoder1.Process(inputSize, inputBuffer, outputBuffer); ConfigureOutput();
            if (useDiffDecode) { outputSize1 = mDecoder1.Process(outputSize1, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer1.Process(outputSize1, inputBuffer, outputBuffer);

            //Output
            outputSize = outputSize1;
            outputBuffer.FastCopyTo(outputBits, outputSize, 0, 0);
        }

        public void ProcessPacket(byte[] bits)
        {
            int bufferInputIndex = 0;
            for (int i = 0; i < bits.Length; i += 8) {

                // Pack bytes
                byte inputByte = 0;
                for (int j = 0; j < 8; j++)
                {
                    byte inputBit = bits[i + j];
                    inputByte <<= 1;
                    inputByte |= (byte)(inputBit & 1);
                }

                // Descrambling
                if (useDescrambling) inputByte ^= DecodeTabs.descramblingSequence[bufferInputIndex%255];

                packetByteBuffer[bufferInputIndex++] = inputByte;

                //Debug log
                //if (bufferInputIndex - 1 != 0 && (bufferInputIndex - 1) % 16 == 0) Logger.CLog("\n");
                //Logger.CLog(inputByte.ToString("X2") + " ");
            }
            //Logger.CLog("\n");

            if(decoder != null)
                decoder.ProcessPacket(packetByteBuffer);
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outputBuffer, arrSize))
            {
                outputBuffer = new byte[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(inputBuffer, arrSize))
            {
                inputBuffer = new byte[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(hardDecisionBits, arrSize))
            {
                hardDecisionBits = new byte[arrSize];
            }
        }
    }
}
