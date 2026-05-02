using NAudio.Wave;
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
        public abstract void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount);
        public abstract void OnSampleSourceChange(WaveFormat waveFormat);
    }
}
