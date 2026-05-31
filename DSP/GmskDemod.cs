using NAudio.Wave;
using NWaves.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public class GmskDemod : Demodulator
    {
        public CostasLoop costasLoop;
        public FeedforwardAGC agc;

        public GmskDemod()
        {
            InitModulesDefault();
        }

        public void InitModulesDefault()
        {
            freqShift = new FreqShift(MainDSP.SampleRate, 0);
            costasLoop = new CostasLoop(0.01f, 10, 4);
            agc = new FeedforwardAGC(1, 0.25f);
            snrEstimator = new M2M4SNREstimator();

            useFreqShift = true;
        }

        public override void OnSampleSourceChange(WaveFormat waveFormat)
        {
            freqShift = new FreqShift(waveFormat.SampleRate, 0);
        }

        public override void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount)
        {
            CheckProcessOutputArr(realSignal.Length);

            freqShift.Process(realSignal.Length, realSignal, imagSignal, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int agcOutputSize = agc.Process(realSignal.Length, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int costasOutputSize = costasLoop.Process(realSignal.Length, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            //ConfigureOutput();

            snrEstimator.Update(costasOutputSize, outputBufferI, outputBufferQ);

            outputCount = costasOutputSize;

            outputBufferI.FastCopyTo(outReal, outputCount);
            outputBufferQ.FastCopyTo(outImag, outputCount);
        }
    }
}
