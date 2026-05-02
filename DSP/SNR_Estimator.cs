using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public class M2M4SNREstimator : DspBlock
    {
        private float d_y1, d_y2;
        private float d_alpha, d_beta;
        private float d_signal, d_noise;

        private float snrCalculated;
        public float SNR => snrCalculated;

        public M2M4SNREstimator(float alpha=0.001f)
        {
            d_y1 = 0;
            d_y2 = 0;

            d_signal = 0;
            d_noise = 0;

            d_alpha = alpha;
            d_beta = 1.0f - alpha;
        }

        public void Update(int inputSize, float[] inputReal, float[] inputImag)
        {
            for (int i = 0; i < inputSize; i++)
            {
                float y1 = MathF.Pow(inputReal[i],2)+MathF.Pow(inputImag[i],2);
                d_y1 = d_alpha * y1 + d_beta * d_y1;

                float y2 = MathF.Pow(MathF.Pow(inputReal[i], 2) + MathF.Pow(inputImag[i], 2), 2);
                d_y2 = d_alpha * y2 + d_beta * d_y2;
            }

            if (d_y1 != d_y1)
                d_y1 = 0;
            if (d_y2 != d_y2)
                d_y2 = 0;

            snrCalculated = CalcSnr();
        }

        public float CalcSnr()
        {
            float y1_2 = d_y1 * d_y1;
            d_signal = MathF.Sqrt(2 * y1_2 - d_y2);
            d_noise = d_y1 - MathF.Sqrt(2 * y1_2 - d_y2);
            float snr = MathF.Max(0, 10.0f * MathF.Log10(d_signal / d_noise));
            return snr;
        }
    }
}
