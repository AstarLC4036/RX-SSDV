using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using NWaves.Utils;
using RX_SSDV.Utils;
using NWaves.Filters.Base;
using NAudio.Wave;

namespace RX_SSDV.DSP
{
    public class BpskDemod
    {
        public CostasLoop costasLoop;
        public LMS_DD_Equalizer equalizer;
        public ClockRecoveryBlock_MM clockRecovery;
        public FeedforwardAGC agc;
        public ComplexFirFilter rrcFilter;
        public FreqShift freqShift;
        public M2M4SNREstimator snr_estimator;

        private float[] outputBufferI;
        private float[] outputBufferQ;
        private float[] inputBufferI;
        private float[] inputBufferQ;

        private static float imaginaryPoint = 1;

        public BpskDemod()
        {
            InitModulesDefault();
        }

        //Well, actually I don't use these methods
        /*
        public BpskDemod(int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitModulesDefault();
        }

        public BpskDemod(float costasBw, float costasFreqLimit, 
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit,
            float agcGain, float agcRef)
        {
            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit);
            InitAGC(agcGain, agcRef);
            //InitRrcFilter();
        }

        public BpskDemod(float costasBw, float costasFreqLimit,
            float equalizerGain, int equalizerKernelSize, int equalizerSPS,
            float clockMu, float clockMuGain, float clockOmega, float clockOmegaGain, float clockOmegaLimit,
            float agcGain, float agcRef,
            int arrSize)
        {
            CheckProcessOutputArr(arrSize);

            InitCostas(costasBw, costasFreqLimit);
            InitEqualizer(equalizerGain, equalizerKernelSize, equalizerSPS);
            InitClockSync(clockMu, clockMuGain, clockOmega, clockOmegaGain, clockOmegaLimit);
            InitAGC(agcGain, agcRef);
            //InitRrcFilter();
        }
        */


        #region Module Define
        /*
        public void InitCostas(float bw, float freqLimit)
        {
            costasLoop = new CostasLoop(bw, freqLimit);
        }

        public void InitEqualizer(float gain, int kernelSize, int sps)
        {
            equalizer = LMS_DD_Equalizer.BuildEqualizer(gain, kernelSize, sps);
        }

        public void InitClockSync(float mu, float muGain, float omega, float omegaGain, float omegaLimit)
        {
            clockRecovery = new ClockRecoveryBlock_MM(mu, muGain, omega, omegaGain, omegaLimit);
        }

        public void InitAGC(float gain, float reference)
        {
            agc = new FeedforwardAGC(gain, reference);
        }

        public void InitRrcFilter(int sampleRate, int symbolRate, int sps)
        {
            float[] rrcTaps = FilterUtils.RootRaisedCosine(16, 1, 1 / sps, 0.35f, 11 * MainDSP.SamplePerSymbol);
            rrcFilter = new ComplexFirFilter(rrcTaps, new float[rrcTaps.Length]);
        }
        */
        
        //Make it looks more easier!
        public void InitModulesDefault()
        {
            freqShift = new FreqShift(48000, 0);
            costasLoop = new CostasLoop(0.1f, 10);
            equalizer = LMS_DD_Equalizer.BuildEqualizer(0.05f, 1, 1);
            clockRecovery = new ClockRecoveryBlock_MM(0.5f, 0.175f, MainDSP.SamplePerSymbol, 0.75f * 0.75f, 0.05f);
            agc = new FeedforwardAGC(1, 0.25f);
            float[] rrcTaps = FilterUtils.RootRaisedCosine(16, MainDSP.GetSPS(), 1 , 0.35f, 11 * MainDSP.SamplePerSymbol);
            rrcFilter = new ComplexFirFilter(rrcTaps, new float[rrcTaps.Length]);
            snr_estimator = new M2M4SNREstimator();
        }
        #endregion
        
        public void OnSampleSourceChange(WaveFormat waveFormat)
        {
            clockRecovery = new ClockRecoveryBlock_MM(0.5f, 0.175f, MainDSP.SamplePerSymbol, 0.75f * 0.75f, 0.05f);
        }

        private void ConfigureOutput()
        {
            float[] temp = outputBufferI;
            outputBufferI = inputBufferI;
            inputBufferI = temp;

            temp = outputBufferQ;
            outputBufferQ = inputBufferQ;
            inputBufferQ = temp;
        }

        /// <summary>
        /// Process BPSK demodulation.
        /// </summary>
        /// <param name="realSignal">Input signal I(Real)</param>
        /// <param name="imagSignal">Input signal Q(Imag)</param>
        /// <param name="outReal">Output signal I(Real)</param>
        /// <param name="outImag">Output signal Q(Imag)</param>
        /// <param name="outputCount">Output array count</param>
        /// <param name="cutArray">Cut output array</param>
        public void Process(float[] realSignal, float[] imagSignal, float[] outReal, float[] outImag, out int outputCount)
        {
            CheckProcessOutputArr(realSignal.Length);
            CheckBlocks();

            freqShift.Process(realSignal.Length, realSignal, imagSignal, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int agcOutputSize = agc.Process(realSignal.Length, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int costasOutputSize = costasLoop.Process(realSignal.Length, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            ConfigureOutput();

            rrcFilter.ProcessOnline(inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int clockOutputSize = clockRecovery.Process(costasOutputSize, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);
            ConfigureOutput();

            int equalizerOutputSize = equalizer.Process(clockOutputSize, inputBufferI, inputBufferQ, outputBufferI, outputBufferQ);

            snr_estimator.update(equalizerOutputSize, outputBufferI, outputBufferQ);

            outputCount = equalizerOutputSize;

            outputBufferI.FastCopyTo(outReal, outputCount);
            outputBufferQ.FastCopyTo(outImag, outputCount);
        }

        public void CheckBlocks()
        {
            if (costasLoop == null)
                throw new NullReferenceException("Costas loop not initialized");
            if (clockRecovery == null)
                throw new NullReferenceException("Clock recovery block not initialized");
            if (equalizer == null)
                throw new NullReferenceException("LMS equalizer not initialized");
        }

        //use 'FeedforwardAGC' instead
        /*
        public float CalcAvgMagnitude(float[] samplesI, float[] samplesQ)
        {
            float positiveAvg = 0;
            float negativeAvg = 0;
            for (int i = 0; i < samplesI.Length; i++)
            {
                float sampleI = samplesI[i];
                float sampleQ = samplesQ[i];
                if (sampleI >= 0)
                {
                    positiveAvg += sampleI;
                }
                else
                {
                    negativeAvg += sampleQ;
                }
            }

            positiveAvg /= samplesI.Length;
            negativeAvg /= samplesQ.Length;

            return (positiveAvg - negativeAvg) / 2;
        }

        public void NormalizeSignal(float[] channelI, float[] channelQ)
        {
            if (channelI.Length != channelQ.Length)
                return;

            for (int i = 0; i < channelI.Length; i++)
            {
                float sampleI = channelI[i];
                float sampleQ = channelQ[i];
                float magnitude = MathF.Sqrt(sampleI * sampleI + sampleQ * sampleQ);
                if (magnitude == 0)
                    continue;
                channelI[i] = sampleI / magnitude;
                channelQ[i] = sampleQ / magnitude;
            }
        }
        */

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

        /// <summary>
        /// Decision maker of BPSK modulation.
        /// </summary>
        /// <param name="sample">The complex sample.</param>
        /// <returns>Decision</returns>
        public static float BpskDecisionMaker(Complex sample)
        {
            //return sample.Real > 0 ? 1: 0;
            return sample.Real > 0 ? 1 * imaginaryPoint : -1 * imaginaryPoint;
        }

        /// <summary>
        /// Decision maker of BPSK modulation.
        /// </summary>
        /// <param name="sample">The real part of complex sample, or I channel of the signal.</param>
        /// <returns>Decision</returns>
        public static float BpskDecisionMaker(float sample)
        {
            //return sample > 0 ? 1 : 0;
            return sample > 0 ? 1 * imaginaryPoint : -1 * imaginaryPoint;
        }
    }
}
