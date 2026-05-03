using NAudio.Wave;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public abstract class Demodulator
    {
        public M2M4SNREstimator snrEstimator;
        public FreqShift freqShift;
        public bool useFreqShift = false;

        protected float[] outputBufferI;
        protected float[] outputBufferQ;
        protected float[] inputBufferI;
        protected float[] inputBufferQ;

        public virtual void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount)
        {
            CheckProcessOutputArr(realSignal.Length);
            outputCount = realSignal.Length;
        }
        public virtual void OnSampleSourceChange(WaveFormat waveFormat)
        {

        }

        protected void ConfigureOutput()
        {
            float[] temp = outputBufferI;
            outputBufferI = inputBufferI;
            inputBufferI = temp;

            temp = outputBufferQ;
            outputBufferQ = inputBufferQ;
            inputBufferQ = temp;
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(inputBufferI, arrSize) || ArrayUtil.CheckNeedUpdate(inputBufferQ, arrSize))
            {
                inputBufferI = new float[arrSize];
                inputBufferQ = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(outputBufferI, arrSize) || ArrayUtil.CheckNeedUpdate(outputBufferQ, arrSize))
            {
                outputBufferI = new float[arrSize];
                outputBufferQ = new float[arrSize];
            }
        }
    }
}
