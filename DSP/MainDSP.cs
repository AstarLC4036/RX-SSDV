using NAudio.Wave;
using NWaves.Filters.Base;
using NWaves.Transforms;
using NWaves.Utils;
using RX_SSDV.Base;
using RX_SSDV.CCSDS;
using RX_SSDV.Decoder;
using RX_SSDV.Graphic;
using RX_SSDV.IO;
using RX_SSDV.Utils;
using System;
using System.Drawing;
using MessageBox = System.Windows.MessageBox;

namespace RX_SSDV.DSP
{
    public class MainDSP
    {
        /*Base*/
        private static int sampleRate = 48000;
        public static int SampleRate => sampleRate;

        private static readonly object lockObj = new object();

        #region Drawing
        private CanvasGraphicDrawer spectrum;
        private CanvasGraphicDrawer constellationOrigin;
        private CanvasGraphicDrawer constellation;
        private Point[] points;
        private Point[] pointsOfFilterI;
        private Point[] pointsOfFilterQ;
        private RingBufferIQ constellationBuffer = new RingBufferIQ(2048);
        private Font font = new Font("Arial", 8);
        private SolidBrush brush = new SolidBrush(Color.Black);
        private SolidBrush bfBrush = new SolidBrush(Color.FromArgb(120, 0, 255, 255));

        private bool isDrawerOnline = false;
        private bool isDrawerPrepared = false;
        public float[] spectrumTempSamplesI;
        public float[] spectrumTempSamplesQ;
        private int currentDrawerTick = 0;
        #endregion

        #region FFT Spectrum
        public Fft fft;
        public const int FFT_SIZE = 2048;
        //public const int FFT_MAX = 120; //TODO: complete this work.
        //public const int FFT_MIN = 0;
        public const int FFT_POS = 100;
        public const int FFT_RANGE = -1;//2048
        public float spectrumScale = 0.01f;
        public int spectrumPeriod = 1;
        public int fftDatasetIndex = 0;
        public double[][] fftDataset;
        public Bitmap spectrumCacheBitmap;
        private float freqPerSample = 0;
        private float[] fftReal;
        private float[] fftImag;
        double[] magnitudeSpectrum;
        #endregion

        #region Filter
        public bool EnableFilter
        {
            get
            {
                return enableFilter;
            }
            set
            {
                enableFilter = value;
                if (value)
                {
                    UpdateFilter();
                }
            }
        }
        private bool enableFilter = false;
        public int bandwidth = 1;
        public int frequencyShift = 0;
        public ComplexFirFilter bpFilter;
        public const int BP_ORDER = 130;
        private float[] filteredSamplesI;
        private float[] filteredSamplesQ;
        #endregion

        #region SSDV Process
        //TODO: make it editable
        public static int symbolRate = 9600;
        public static int packetSize = 255;
        private static int samplesPerSymbol = 5;
        public static int SamplePerSymbol => samplesPerSymbol;

        public bool EnableProcess
        {
            get
            {
                return enableProcess;
            }
            set
            {
                enableProcess = value;
            }
        }
        private bool enableProcess = false;

        //BPSK Demod
        public BpskDemod bpskDemod;
        private float[] demodOutputI;
        private float[] demodOutputQ;
        private int demodOutputSize;

        //CCSDS Decode
        public CCSDSDecoder ccsds;
        private byte[] decodeOutput;
        private int decodeOutputSize;

        public float ConstellationMultiply
        {
            get
            {
                return constellationMultiply;
            }
            set
            {
                constellationMultiply = value;
            }
        }
        private int constellationStepsize = 1;
        private float constellationMultiply = 25;
        #endregion

        public MainDSP(CanvasGraphicDrawer spectrumArea, CanvasGraphicDrawer constellationAreaOrigin, CanvasGraphicDrawer clonstellationAreaProcessed)
        {
            spectrum = spectrumArea;
            constellationOrigin = constellationAreaOrigin;
            constellation = clonstellationAreaProcessed;

            //int spectrumLength = SampleSource.WAV_BUFFER_SIZE;

            Init();
        }

        /// <summary>
        /// Init <see cref="MainDSP"/>
        /// </summary>
        private void Init()
        {
            fft = new Fft(FFT_SIZE);
            bpskDemod = new BpskDemod();
            ccsds = new CCSDSDecoder(true, true, packetSize, new AsrtuDecoder());

            UpdateBitmap(spectrum.Width);

            SampleSource.onDataAvalible += ProcessData;
            SampleSource.onSourceChange += OnSourceChange;

            spectrum.onSizeChange += (w, h) => { UpdateBitmap(w); };
            StartDrawing();
        }

        public void ResetDecoder()
        {
            bpskDemod = new BpskDemod();
        }

        /// <summary>
        /// Get samples per symbol
        /// </summary>
        /// <returns>Samples per symbol</returns>
        public static int GetSPS()
        {
            int sps = sampleRate / symbolRate;
            return sps;
        }

        //在采样源改变时被调用
        private void OnSourceChange(WaveFormat waveFormat)
        {
            freqPerSample = waveFormat.SampleRate / FFT_SIZE;
            sampleRate = waveFormat.SampleRate;
            samplesPerSymbol = GetSPS(); //Update sps
            bpskDemod.OnSampleSourceChange(waveFormat);
        }

        /// <summary>
        /// Update filter.
        /// </summary>
        public void UpdateFilter()
        {
            if (!enableFilter)
                return;

            if (SampleSource.IsSourceAvalible)
            {
                try
                {
                    //Calc kernel
                    double lowCutoff = (frequencyShift - bandwidth / 2.0) * 1000 / sampleRate;
                    double highCutoff = (frequencyShift + bandwidth / 2.0) * 1000 / sampleRate;
                    float[] bpKernelReal = ArrayUtil.Double2Float(DesignFilterUtil.FirWinBpReal(BP_ORDER, lowCutoff, highCutoff));
                    float[] bpKernelImag = ArrayUtil.Double2Float(DesignFilterUtil.FirWinBpImag(BP_ORDER, lowCutoff, highCutoff));

                    //Apply the kernel
                    if (bpFilter == null)
                    {
                        bpFilter = new ComplexFirFilter(bpKernelReal, bpKernelImag);
                    }
                    else
                    {
                        bpFilter.ChangeKernel(bpKernelReal, bpKernelImag);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unexpected filter arguments.", "Oh no!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        #region Drawing
        /// <summary>
        /// Start all graphic drawing.
        /// </summary>
        private void StartDrawing()
        {
            if(!isDrawerOnline)
            {
                isDrawerOnline = true;

                Task.Run(() =>
                {
                    //Origin samples buffer
                    float[] bufferI = new float[1], bufferQ = new float[1];

                    while(isDrawerOnline)
                    {
                        if(!SampleSource.IsPlaying)
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                        if (currentDrawerTick < spectrumPeriod)
                        {
                            currentDrawerTick++;
                            Thread.Sleep(10);
                        }
                        else if (currentDrawerTick >= spectrumPeriod && isDrawerPrepared)
                        {
                            lock (lockObj)
                            {
                                Draw(bufferI, bufferQ);
                            }
                        }
                    }
                });
            }
        }

        //进行主要的绘制
        private void Draw(float[] bufferI, float[] bufferQ)
        {
            //Before 'ProcessSpectrum', spectrumTempSamplesI/Q just are origin samples.
            if (ArrayUtil.CheckNeedUpdate(bufferI, spectrumTempSamplesI.Length) ||
                ArrayUtil.CheckNeedUpdate(bufferI, spectrumTempSamplesI.Length))
            {
                bufferI = new float[spectrumTempSamplesI.Length];
                bufferQ = new float[spectrumTempSamplesQ.Length];
            }
            spectrumTempSamplesI.FastCopyTo(bufferI, spectrumTempSamplesI.Length);
            spectrumTempSamplesQ.FastCopyTo(bufferQ, spectrumTempSamplesQ.Length);

            //Reset drawer ticker
            currentDrawerTick = 0;

            //DSP
            ProcessSpectrum(spectrumTempSamplesI, spectrumTempSamplesQ);

            if (enableProcess)
                UpdateConstellation(bufferI, bufferQ, demodOutputI, demodOutputQ);
        }

        //更新频谱每行的bitmap
        private void UpdateBitmap(int width)
        {
            spectrumCacheBitmap = new Bitmap(width, 1);
            fftDataset = new double[FFT_POS][];
            for (int i = 0; i < fftDataset.Length; i++)
            {
                fftDataset[i] = new double[SampleSource.WAV_BUFFER_SIZE];
            }
        }

        //更新星座图
        private void UpdateConstellation(float[] samplesReal, float[] samplesImag, float[] samplesRealDemod, float[] samplesImagDemod)
        {
            if (samplesReal == null || samplesImag == null || samplesReal.Length != samplesImag.Length)
                return;

            else if (samplesRealDemod == null || samplesImagDemod == null || samplesRealDemod.Length != samplesImagDemod.Length)
                return;

            //int sizeOfPointArr = (int)(1f * samplesReal.Length / constellationStepsize) + 1;
            //if (pointsOfconstellation == null || pointsOfconstellation.Length != sizeOfPointArr)
            //{
            //    pointsOfconstellation = new Point[sizeOfPointArr];
            //}
            //for(int i = 0, j = 0; i < samplesReal.Length; i+=constellationStepsize, j++)
            //{
            //    if (i >= samplesReal.Length)
            //        continue;
            //    pointsOfconstellation[j] = new Point((int)(samplesReal[i] * constellationMultiply), (int)(samplesImag[i] * constellationMultiply));
            //}

            constellationBuffer.Write(samplesRealDemod, samplesImagDemod, demodOutputSize);

            constellationOrigin.Draw((graphics) =>
            {
                for (int i = 0; i < samplesReal.Length; i += constellationStepsize)
                {
                    int x = (int)(samplesReal[i] * constellationMultiply) + 50;
                    int y = (int)(samplesImag[i] * constellationMultiply) + 50;
                    graphics.DrawRectangle(Pens.Green, x, y, 1, 1);
                }
            });

            constellation.Draw((graphics) =>
            {
                for (int i = 0; i < constellationBuffer.Length; i += constellationStepsize)
                {
                    int x = (int)(constellationBuffer.BufferI[i] * constellationMultiply) + 50;
                    int y = (int)(constellationBuffer.BufferQ[i] * constellationMultiply) + 50;
                    graphics.DrawRectangle(Pens.Green, x, y, 1, 1);
                }
            });
        }

        //更新频谱
        private void UpdateSpectrum(float maxFreq, int lengthReal, int lengthImag)
        {
            double[] actualProcess;

            //FFT_RANGE < 0 means no range select.(Debug)
            if (FFT_RANGE > 0)
            {
                actualProcess = magnitudeSpectrum[0..FFT_RANGE];
            }
            else
            {
                actualProcess = magnitudeSpectrum;
            }

            if (fftDatasetIndex >= FFT_POS)
            {
                fftDatasetIndex = 0;
            }

            //fftDataset.Add(actualProcess);
            actualProcess.FastCopyTo(fftDataset[fftDatasetIndex], actualProcess.Length);
            int readerIndex = fftDatasetIndex;
            fftDatasetIndex++;

            points = actualProcess
                .Select((v, i) => new Point((int)(i * ((float)spectrum.Width / actualProcess.Length)), spectrum.Height - (int)(v * spectrumScale) - FFT_POS))
                .ToArray();   // 将数据转换为一个个的坐标点

            spectrum.Draw((graphics) =>
            {
                float samplesPerKHz = (1000 / freqPerSample);
                float pixelsPerSample = ((float)spectrum.Width / actualProcess.Length);
                float scaleCoeff = samplesPerKHz * pixelsPerSample;
                int centerPos = spectrum.Width / 2;

                //Spectrum
                graphics.DrawLines(Pens.Black, points);   // 连接这些点, 画线

                //Band-pass filter
                if (enableFilter)
                {
                    int bpMin = frequencyShift - bandwidth / 2;
                    int bpMax = frequencyShift + bandwidth / 2;

                    graphics.DrawLine(Pens.LightCyan, new Point(centerPos + (int)(bpMin * scaleCoeff), 0), new Point(centerPos + (int)(bpMin * scaleCoeff), spectrum.Height - FFT_POS));
                    graphics.DrawLine(Pens.LightCyan, new Point(centerPos + (int)(bpMax * scaleCoeff), 0), new Point(centerPos + (int)(bpMax * scaleCoeff), spectrum.Height - FFT_POS));
                    graphics.FillRectangle(bfBrush, centerPos + (int)(bpMin * scaleCoeff), 0, (bpMax - bpMin) * scaleCoeff, spectrum.Height - FFT_POS);
                }

                if (bpskDemod != null)
                {
                    int freq = (int)bpskDemod.freqShift.Freq;
                    graphics.DrawLine(Pens.Blue, new Point(centerPos + (int)(freq * scaleCoeff), 0), new Point(centerPos + (int)(freq * scaleCoeff), spectrum.Height - FFT_POS));
                }

                //Analyze
                //不要担心这里编译器会帮你优化成StringBuilder的
                graphics.DrawString(
                    $"FFT[{FFT_SIZE}](Visualizing index range: {(FFT_RANGE < 0 ? "Unlimited" : " 0 - " + FFT_RANGE)} Freq: -{maxFreq / 2000}kHz - {maxFreq / 2000}kHz )" +
                    $"\nInput Signal[Real {lengthReal}, Imag {lengthImag}]" +
                    $"\nOutput FFT[{magnitudeSpectrum.Length}]" +
                    $"\nBandwidth: {bandwidth}kHz, Frequency Shift: {frequencyShift}kHz" +
                    $"\nTime {SampleSource.GetFormatedTimeString()}" +
                    $"\nCostas Loop [freq = {bpskDemod.costasLoop.Phase}, phase = {bpskDemod.costasLoop.Phase}]" +
                    $"\nClock Sync [mu = {bpskDemod.clockRecovery.Mu}, omega = {bpskDemod.clockRecovery.Omega}]",
                    font, brush, new Point(5, 5));

                //Separator
                graphics.DrawLine(Pens.Black, new Point(0, spectrum.Height - FFT_POS), new Point(spectrum.Width, spectrum.Height - FFT_POS));

                //Waterfall
                for (int i = 0, j = readerIndex; i < fftDataset.Length; i++, j--)
                {
                    if (j < 0)
                    { 
                        j = fftDataset.Length - 1;
                    }
                    //double[] data = fftDataset[^(i + 1)];
                    double[] data = fftDataset[j];
                    int lastX = -1;
                    for (int k = 0; k < data.Length; k++)
                    {
                        int x = (int)(k * pixelsPerSample);
                        if (x != lastX)
                        {
                            if (x > lastX + 1)
                            {
                                x = lastX + 1;
                            }
                            int value = (int)Math.Clamp(data[k] * spectrumScale, 0, 255);
                            //spectrumCacheBitmap.SetPixel(x, 0, Color.FromArgb(value, 0, 255 / 4));
                            spectrumCacheBitmap.SetPixel(x, 0, ColorFade.GetColorHSV(value, 0, 255));
                            lastX = x;
                        }
                    }
                    graphics.DrawImage(spectrumCacheBitmap, 0, spectrum.Height - (FFT_POS - 20) + i, spectrum.Width, 1);
                }

                //Scale
                for (int i = 0; i < actualProcess.Length / 2; i += 100)
                {
                    if (i < actualProcess.Length - 1)
                    {
                        int xPos = (int)(i * ((float)spectrum.Width / actualProcess.Length));

                        graphics.DrawLine(Pens.Black, new Point(centerPos + xPos, spectrum.Height - FFT_POS + 5), new Point(centerPos + xPos, spectrum.Height - FFT_POS));
                        graphics.DrawString($"{(i * freqPerSample) / 1000}kHz", font, brush, new Point(centerPos + xPos, spectrum.Height - FFT_POS + 5));

                        if (i != 0)
                        {
                            graphics.DrawLine(Pens.Black, new Point(centerPos - xPos, spectrum.Height - FFT_POS + 5), new Point(centerPos - xPos, spectrum.Height - FFT_POS));
                            graphics.DrawString($"-{(i * freqPerSample) / 1000}kHz", font, brush, new Point(centerPos - xPos, spectrum.Height - FFT_POS + 5));
                        }
                    }
                }

                //Debug
                if (pointsOfFilterI != null)
                {
                    graphics.DrawLines(Pens.Black, pointsOfFilterI);
                }
                if (pointsOfFilterQ != null)
                {
                    graphics.DrawLines(Pens.Black, pointsOfFilterQ);
                }
            });
        }
        #endregion

        #region Process
        /// <summary>
        /// Process input samples
        /// </summary>
        /// <param name="samplesReal">Input sample I(Real)</param>
        /// <param name="samplesImag">Input sample Q(Imag)</param>
        public void ProcessData(float[] samplesReal, float[] samplesImag)
        {
            //Filter
            filteredSamplesI = samplesReal;
            filteredSamplesQ = samplesImag;
            if(enableFilter)
                ProcessFilter(samplesReal, samplesImag);

            //Lock drawer to aviod unexpected read
            //drawerTempLock = true;

            //BPSK
            lock (lockObj)
            {
                if (enableProcess)
                {
                    ProcessBPSK(filteredSamplesI, filteredSamplesQ);
                    ProcessCCSDS(demodOutputI, demodOutputQ);
                }

                //Prepare data for drawer
                if (spectrumTempSamplesI == null || spectrumTempSamplesQ == null)
                {
                    spectrumTempSamplesI = new float[samplesReal.Length];
                    spectrumTempSamplesQ = new float[samplesImag.Length];
                }
                samplesReal.FastCopyTo(spectrumTempSamplesI, samplesReal.Length);
                samplesImag.FastCopyTo(spectrumTempSamplesQ, samplesImag.Length);
                //Unlock drawer and let it update
                //drawerTempLock = false;
                //drawerUpdateFlag = true;
            }

            if(!isDrawerPrepared)
                isDrawerPrepared = true;
        } 

        /// <summary>
        /// Process BPSK.
        /// </summary>
        /// <param name="realSignal">Input sample I(Real)</param>
        /// <param name="imagSignal">Input sample Q(Imag)</param>
        public void ProcessBPSK(float[] realSignal, float[] imagSignal)
        {
            CheckBPSKOutputAvalible();
            //ClearBPSKOutputArray();

            bpskDemod.Process(realSignal, imagSignal, demodOutputI, demodOutputQ, out demodOutputSize);
        }

        public void ProcessCCSDS(float[] realSignal, float[] imagSignal)
        {
            CheckCCSDSOutputAvalible();

            ccsds.Process(realSignal, imagSignal, decodeOutput, out decodeOutputSize, demodOutputSize);
        }

        /// <summary>
        /// Process filter.
        /// </summary>
        /// <param name="realSignal">Input sample I(Real)</param>
        /// <param name="imagSignal">Input sample Q(Imag)</param>
        public void ProcessFilter(float[] realSignal, float[] imagSignal)
        {
            //Check for output array
            if(ArrayUtil.CheckNeedUpdate(filteredSamplesI, realSignal.Length))
                filteredSamplesI = new float[realSignal.Length];
            if(ArrayUtil.CheckNeedUpdate(filteredSamplesQ, imagSignal.Length))
                filteredSamplesQ = new float[imagSignal.Length];

            if (bpFilter != null)
            {
                bpFilter.ProcessOnline(realSignal, imagSignal, filteredSamplesI, filteredSamplesQ);

                //Drawing
                pointsOfFilterI = filteredSamplesI
                    .Select((v, i) => new Point((int)(i * (spectrum.Width * 1f / filteredSamplesI.Length)), spectrum.Height - (int)(v * 100) - FFT_POS - 300))
                    .ToArray();

                pointsOfFilterQ = filteredSamplesQ
                    .Select((v, i) => new Point((int)(i * (spectrum.Width * 1f / filteredSamplesQ.Length)), spectrum.Height - (int)(v * 100) - FFT_POS - 400))
                    .ToArray();

                //graphics.DrawLines(Pens.Black, pointsOfFilter);
            }
        }

        /// <summary>
        /// Process spectrum.
        /// </summary>
        /// <param name="realSignal">Input sample I(Real)</param>
        /// <param name="imagSignal">Input sample Q(Imag)</param>
        public void ProcessSpectrum(float[] realSignal, float[] imagSignal)
        {
            //FFT
            if ((realSignal.Length > 0 && realSignal.Length >= FFT_SIZE) && (realSignal.Length == imagSignal.Length))
            {
                float maxFreq = realSignal.Length * freqPerSample;

                if (ArrayUtil.CheckNeedUpdate(fftReal, realSignal.Length))
                {
                    fftReal = new float[realSignal.Length];
                    fftImag = new float[imagSignal.Length];
                }
                fft.Direct(realSignal, imagSignal, fftReal, fftImag);

                double[] tempSpectrum = fftReal
                    .Select((v, i) => Math.Sqrt(v * v + fftImag[i] * fftImag[i]))
                    .ToArray();

                //must new object, or use object pool
                if(ArrayUtil.CheckNeedUpdate(magnitudeSpectrum, realSignal.Length))
                    magnitudeSpectrum = new double[realSignal.Length];

                //Positive/negative freq of magnitude spectrum
                for (int i = 0, j = tempSpectrum.Length / 2; i < magnitudeSpectrum.Length; i++, j++)
                {
                    if (j > tempSpectrum.Length - 1)
                    {
                        j = 0;
                    }

                    magnitudeSpectrum[i] = tempSpectrum[j];
                }

                UpdateSpectrum(maxFreq, realSignal.Length, imagSignal.Length);
            }
        }
        #endregion

        public void ClearBPSKOutputArray()
        {
            Array.Clear(demodOutputI, 0, demodOutputI.Length);
            Array.Clear(demodOutputQ, 0, demodOutputQ.Length);
        }

        public void CheckBPSKOutputAvalible()
        {
            //int demodOutputSize = bpskDemod.CalcOutputSize(inputSize);
            const int demodOutputSize = 2048;

            if(ArrayUtil.CheckNeedUpdate(demodOutputI, demodOutputSize) || ArrayUtil.CheckNeedUpdate(demodOutputQ, demodOutputSize))
            {
                demodOutputI = new float[demodOutputSize];
                demodOutputQ = new float[demodOutputSize];
            }
        }

        public void CheckCCSDSOutputAvalible()
        {
            //int demodOutputSize = bpskDemod.CalcOutputSize(inputSize);
            const int decodeOutputSize = 2048;

            if (ArrayUtil.CheckNeedUpdate(decodeOutput, decodeOutputSize))
            {
                decodeOutput = new byte[decodeOutputSize];
            }
        }
    }
}
