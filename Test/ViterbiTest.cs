using NWaves.Utils;
using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS.Viterbi;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace RX_SSDV.Test
{
    public class ViterbiTest
    {
        public Viterbi viterbi  = new Viterbi();
        public byte[] inputDataArray;
        public byte[] inputArray;
        public byte[] outputArray;
        public int outputSize;
        public int sampleCount = 128;

        public byte[] inputBytes;
        public int inputBytesLen = 0;

        private int generatorState = 0;

        public void Test()
        {
            Logger.LogInfo("Running Viterbi Test...");

            DecodeFixed();

            //Logger.LogInfo("GROUP FIXED ASM");
            //GenerateFixedInput();
            //Decode();
            //PrintResult();

            //Logger.LogInfo("GROUP 1");
            //GenerateInput(10);
            //Decode();
            //PrintResult();

            //Logger.LogInfo("GROUP 2");
            //GenerateInput(1);
            //Decode();
            //PrintResult();

            //Logger.LogInfo("GROUP RAND 1");
            //GenerateInputRand();
            //Decode();
            //PrintResult();

            //using(FileStream fs = new FileStream("C:\\Users\\AstarLC\\Desktop\\Documents\\misc\\test_out_viterbi.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            //{
            //    fs.Write(inputBytes, 0, inputBytesLen);
            //    fs.Flush();
            //    fs.Close();
            //}

            //Logger.LogInfo("GROUP RAND 2");
            //GenerateInputRand();
            //Decode();
            //PrintResult();
        }

        public void PrintResult()
        {
            //StringBuilder sb = new StringBuilder();
            //int count = 0;

            //Logger.LogInfo("Input array");
            //for (int i = 0; i < inputArray.Length; i++)
            //{
            //    sb.Append(inputArray[i]);
            //    count++;

            //    if (count > 32)
            //    {
            //        count = 0;
            //        Logger.LogInfo(sb.ToString());
            //        sb.Clear();
            //    }
            //}
            //count = 0;
            //sb.Clear();
            //Logger.LogInfo($"Input data array");
            //for (int i = 0; i < sampleCount; i++)
            //{
            //    sb.Append(inputDataArray[i]);
            //    count++;

            //    if (count > 32)
            //    {
            //        count = 0;
            //        Logger.LogInfo(sb.ToString());
            //        sb.Clear();
            //    }
            //}
            //count = 0;
            //sb.Clear();

            Logger.PrintArr(inputArray, inputArray.Length, "Input array");
            Logger.PrintArr(inputDataArray, sampleCount, "Input data array");
            Logger.PrintArr(outputArray, outputSize, "Output array");

            int err = 0;
            //Logger.LogInfo($"Output array");
            for (int i = 0; i < outputSize; i++)
            {
                if (inputDataArray[i] != outputArray[i])
                    err++;

                //sb.Append(outputArray[i]);
                //count++;

                //if (count > 32)
                //{
                //    count = 0;
                //    Logger.LogInfo(sb.ToString());
                //    sb.Clear();
                //}
            }

            Logger.LogInfo($"Errors: {err}/{inputDataArray.Length}, Error rate: { ((float)err / inputDataArray.Length) * 100}%");
        }

        public void GenerateInput(int modPtr = 10)
        {
            inputArray = new byte[sampleCount * 2];
            inputDataArray = new byte[sampleCount];
            inputBytes = new byte[sampleCount];
            //generatorState = 0;

            byte inputByte = 0;
            int byteInputIndex = 0;
            int inputTimes = 0;

            int decisionPtr = 0;
            for (int i = 0; i < sampleCount * 2; i+=2)
            {
                int mod = decisionPtr % 2;
                if(i % modPtr == 0)
                    decisionPtr++;

                //Logger.LogInfo(mod.ToString());

                int input = 0;
                if (mod == 0)
                {
                    input = viterbi.BranchOutputs[generatorState, 0];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item1;
                    inputDataArray[i / 2] = 0;

                    //Logger.LogInfo("input 0");
                }
                else
                {
                    input = viterbi.BranchOutputs[generatorState, 1];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item2;
                    inputDataArray[i / 2] = 1;
                }

                //Write
                int input1 = BinaryUtils.ReadInt(input, 2);
                int input2 = BinaryUtils.ReadInt(input, 1);

                inputArray[i] = (byte)input1;
                inputArray[i + 1] = (byte)input2;

                inputByte <<= 2;
                inputByte |= (byte)((input1 << 1) | input2);
                inputTimes += 1;
                if (inputTimes == 4)
                {
                    inputBytes[byteInputIndex++] = inputByte;
                    inputByte = 0;
                    inputTimes = 0;
                }
            }

            if (inputTimes < 3)
            {
                for (int i = inputTimes; i < 3; i++)
                {
                    inputByte <<= 2;
                    inputBytes[byteInputIndex++] = inputByte;
                }
            }

            inputBytesLen = byteInputIndex;
        }

        public void GenerateFixedInput()
        {
            int[] fixedInput = new int[] { 0, 0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1 };
            //int[] fixedInput = 
            //{ 
            //    0,0,0,0,0,0,1,1,
            //    0,1,0,1,1,1,0,1,
            //    0,1,0,0,1,0,0,1,
            //    1,1,0,0,0,0,1,0,
            //    0,1,0,0,1,1,1,1,
            //    1,1,1,1,0,0,1,0,
            //    0,1,1,0,1,0,0,0,
            //    0,1,1,0,1,0,1,1,
            //    0,0,0,1,0,1,0,0,
            //    0,0,1,0,1,1,0,1,
            //    0,1,0,0,1,0,0,1,
            //    1,1,0,0,0,0,1,0,
            //    0,1,0,0,1,1,1,1,
            //    1,1,1,1,0,0,1,0,
            //    0,1,1,0,1,0,0,0,
            //    0,1,1,0,1,0,1,1
            //};

            inputArray = new byte[fixedInput.Length * 2];
            inputDataArray = new byte[fixedInput.Length];

            int ouc = 0;

            for (int i = 0; i < fixedInput.Length; i++)
            {
                //Logger.LogInfo(mod.ToString());

                int input = 0;
                int nextCode = fixedInput[i];
                if (nextCode == 0)
                {
                    input = viterbi.BranchOutputs[generatorState, 0];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item1;
                    inputDataArray[i] = 0;

                    //Logger.LogInfo("input 0");
                }
                else
                {
                    input = viterbi.BranchOutputs[generatorState, 1];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item2;
                    inputDataArray[i] = 1;
                }

                //Write
                int input1 = BinaryUtils.ReadInt(input, 2);
                int input2 = BinaryUtils.ReadInt(input, 1);

                inputArray[ouc++] = (byte)input1;
                inputArray[ouc++] = (byte)input2;
            }
        }

        public void GenerateInputRand()
        {
            inputArray = new byte[sampleCount * 2];
            inputDataArray = new byte[sampleCount];
            inputBytes = new byte[sampleCount];
            //generatorState = 0;

            Random random = new Random();

            byte inputByte = 0;
            int byteInputIndex = 0;
            int inputTimes = 0;
            for (int i = 0, j = 0; i < sampleCount * 2; i += 2, j++)
            {
                int rand = random.Next(2);

                int input = 0;

                (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                //Logger.LogInfo($"{Trellis.CalcSourceStates(0b_101011, 7).Item1.ToString("B6")}");

                if (rand == 0)
                {
                    input = viterbi.BranchOutputs[generatorState, 0];
                    generatorState = nextState.Item1;
                    inputDataArray[j] = 0;
                }
                else
                {
                    input = viterbi.BranchOutputs[generatorState, 1];
                    generatorState = nextState.Item2;
                    inputDataArray[j] = 1;
                }

                //Write
                int input1 = BinaryUtils.ReadInt(input, 2);
                int input2 = BinaryUtils.ReadInt(input, 1);

                inputArray[i] = (byte)input1;
                inputArray[i + 1] = (byte)input2;

                inputByte <<= 2;
                inputByte |= (byte)((input1 << 1) | input2);
                inputTimes += 1;
                if(inputTimes == 4)
                {
                    inputBytes[byteInputIndex++] = inputByte;
                    inputByte = 0;
                    inputTimes = 0;
                }
            }

            if(inputTimes < 3)
            {
                for(int i = inputTimes; i < 3; i++)
                {
                    inputByte <<= 2;
                    inputBytes[byteInputIndex++] = inputByte;
                }
            }

            inputBytesLen = byteInputIndex;
        }

        public void Decode()
        {
            outputArray = new byte[sampleCount * 2];

            outputSize = viterbi.Process(inputArray.Length, inputArray, outputArray);
        }

        public void DecodeFixed()
        {
            outputArray = new byte[sampleCount * 2];
            byte[] inputArr =
            {
            0,1,0,1,0,1,1,0,
            0,0,0,0,1,0,0,0,
            0,0,0,1,1,1,0,0,
            1,0,0,1,0,1,1,1,
            0,0,0,1,1,0,1,0,
            1,0,1,0,0,1,1,1,
            0,0,1,1,1,1,0,1,
            0,0,1,1,1,1,1,0,
            0,1,0,0,0,0,1,0,
            0,0,1,0,0,1,
            };
            byte[] outputArr = new byte[inputArr.Length];

            int outSize = viterbi.Process(inputArr.Length, inputArr, outputArr);

            Logger.PrintArr(outputArr, outSize, "Test out");
        }
    }
}
