using NWaves.Filters.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public class CostasLoop : DspBlock
    {
        private float freqLimitMin;
        private float freqLimitMax;
        private float alpha;
        private float beta;

        //VCO arguments
        public float Phase => phase;
        public float Freq => freq;
        private float phase = 0;
        private float freq = 0;

        private float error = 0;

        private int order = 2;

        /// <summary>
        /// Construct a <see cref="CostasLoop">.
        /// </summary>
        /// <param name="loopBw">Loop bandwidth</param>
        /// <param name="freqLimit">Frequency limit</param>
        public CostasLoop(float loopBw, float freqLimit, int order)
        {
            freqLimitMin = -freqLimit;
            freqLimitMax = freqLimit;

            float damping = MathF.Sqrt(2.0f) / 2.0f;
            float denom = (1.0f + 2.0f * damping * loopBw + loopBw * loopBw);
            alpha = (4 * damping * loopBw) / denom;
            beta = (4 * loopBw * loopBw) / denom;
            this.order = order;
        }

        /// <summary>
        /// Process signal.
        /// </summary>
        /// <param name="inputReal">Input real samples</param>
        /// <param name="inputImag">Input imag samples</param>
        /// <param name="outputReal">Output real samples</param>
        /// <param name="outputImag">Output imag samples</param>
        public override int Process(int inputSize, float[] inputReal, float[] inputImag, float[] outputReal, float[] outputImag)
        {
            //Buffer is not necessary.
            //base.Process(inputReal, inputImag, outputReal, outputImag, inputSize);

            if (inputReal.Length != inputImag.Length)
                return 0;

            for(int i = 0; i < inputSize; i++)
            {
                Complex sample = new Complex(inputReal[i], inputImag[i]);
                //Complex sample = inputReal[i];
                Complex outSample = sample * CalcVCO(-phase);
                outputReal[i] = (float)outSample.Real;
                outputImag[i] = (float)outSample.Imaginary;

                if (order == 2) // BPSK
                {
                    error = BranchlessClip((float)(outSample.Real * outSample.Imaginary), 1.0f);
                }
                else if (order == 4) // QPSK/OQPSK/GMSK/GFSK
                {
                    error = BranchlessClip((float)((outSample.Real > 0.0f ? 1.0f : -1.0f) * outSample.Imaginary - (outSample.Imaginary > 0.0f ? 1.0f : -1.0f) * outSample.Real), 1);
                }
                else
                {
                    //oh hell no
                    throw new ArgumentOutOfRangeException("the 'order' must is 2 or 4");
                }

                //Calc new arguments
                freq += beta * error;
                phase += freq + alpha * error;

                //Wrap phase
                while (phase > (2 * MathF.PI))
                    phase -= 2 * MathF.PI;
                while (phase < (-2 * MathF.PI))
                    phase += 2 * MathF.PI;

                //Clamp freq
                freq = Math.Clamp(freq, freqLimitMin, freqLimitMax);
            }

            //CompleteProcess(inputSize);
            return inputSize;
        }

        /// <summary>
        /// Calculate complex VCO output by phase.
        /// </summary>
        /// <param name="phase">Phase of trigonometric functions</param>
        /// <returns></returns>
        public static Complex CalcVCO(float phase)
        {
            return new Complex(MathF.Cos(phase), MathF.Sin(phase));
        }

        /// <summary>
        /// Branchless clip.
        /// </summary>
        /// <param name="x">Input</param>
        /// <param name="clip">Clip</param>
        /// <returns></returns>
        public static float BranchlessClip(float x, float clip)
        {
            return 0.5f * (MathF.Abs(x + clip) - MathF.Abs(x - clip));
        }
    }
}
