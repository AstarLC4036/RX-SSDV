using RX_SSDV.Base;
using RX_SSDV.Protocol.CCSDS;
using RX_SSDV.Decoder;
using RX_SSDV.DSP;
using RX_SSDV.Graphic;
using RX_SSDV.IO;
using RX_SSDV.Test;
using RX_SSDV.UI;
using RX_SSDV.Utils;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Application = System.Windows.Forms.Application;

namespace RX_SSDV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow instance;

        public static MainWindow Instance
        {
            get
            {
                return instance;
            }
        }

        public CanvasGraphicDrawer spectrumDrawer;
        public CanvasGraphicDrawer constellationDrawer;
        public CanvasGraphicDrawer constellationDrawerProcessed;

        public Action onSizeChange;

        private MainDSP mainDSP;
        private SatDataUI satData;

        private bool isComponentsInited = false;

        private Dictionary<string, DecoderSet.Satellite> satellites = new Dictionary<string, DecoderSet.Satellite>()
        {
            { "Custom", DecoderSet.Satellite.None },
            { "AO-123(ASRTU-1)", DecoderSet.Satellite.AO123 },
            { "Geoscan", DecoderSet.Satellite.GEOSCAN }
        };

        private Dictionary<string, DecoderSet.Demodulator> modulations = new Dictionary<string, DecoderSet.Demodulator>()
        {
            { "BPSK", DecoderSet.Demodulator.BPSK },
            { "GMSK", DecoderSet.Demodulator.GMSK }
        };

        public MainWindow()
        {
            instance = this;
            SizeChanged += OnWindowsSizeChange;

            LogLogo();
            Logger.Log("[Info] Initializing components...");
            InitializeComponent();
            isComponentsInited = true;
            Logger.Log(" done\n");
            Logger.Instance.logDisplay = logText;
            Logger.Instance.lastLogDisplay = lastLogLabel;

            Init();

            SampleSource.ListDevices();

            //ViterbiTest test = new ViterbiTest();
            //test.Test();
            //DelayTest.Test();
            //MDecoderTest.Test();
            //DeframerTest.Test();
            //CCSDS_Test.Test();
        }

        private void Init()
        {
            Logger.Log("Starting up...\n");
            Settings.ApplySettings(); //init language

            InitDrawer();
            InitUI();
            InitDSP(); 
            Logger.Log("done\n");
        }

        private void InitDSP()
        {
            mainDSP = new MainDSP(spectrumDrawer, constellationDrawer, constellationDrawerProcessed);
            SetDSPArguments();
        }

        private void InitUI()
        {
            satData = new SatDataUI(this);

            processSatellite.ItemsSource = satellites.Keys;
            processSatellite.SelectedIndex = 0;

            processModulation.ItemsSource = modulations.Keys;
            processModulation.SelectedIndex = 0;
        }

        private void InitDrawer()
        {
            spectrumDrawer = new CanvasGraphicDrawer(590, 387, spectrum, spectrumDisplay);
            constellationDrawer = new CanvasGraphicDrawer(100, 100, oriCon, oriConDisplay);
            constellationDrawerProcessed = new CanvasGraphicDrawer(100, 100, constellation, constellationDisplay);
        }

        private void SetDSPArguments()
        {
            filterBandWidthInput.Text = mainDSP.bandwidth.ToString();
            filterFreqShiftInput.Text = mainDSP.frequencyShift.ToString();
            drawerPeriodInput.Text = mainDSP.spectrumPeriod.ToString();
            constellationScaleBox.Text = mainDSP.ConstellationMultiply.ToString();
            spectrumScaleBox.Text = mainDSP.spectrumScale.ToString();
            processSymbolRate.Text = MainDSP.decoderSet.symbolRate.ToString();
            processModulation.SelectedIndex = modulations.IndexOf(MainDSP.decoderSet.demodulator);
        }

        private void LogLogo()
        {
            Logger.Log(@"  ____   __  __          ____    ____    ____   __     __" + "\n" +
                       @" |  _ \  \ \/ /         / ___|  / ___|  |  _ \  \ \   / /" + "\n" +
                       @" | |_) |  \  /   _____  \___ \  \___ \  | | | |  \ \ / / " + "\n" +
                       @" |  _ <   /  \  |_____|  ___) |  ___) | | |_| |   \ V /  " + "\n" +
                       @" |_| \_\ /_/\_\         |____/  |____/  |____/     \_/   " + "\n" +
                       @"                                  " + "\n");
            Logger.Log($"RX-SSDV ver. {Application.ProductVersion}\n");
            Logger.Log($"By AstarLC(BI2QXZ), Polygone_(BG5JSB)\n\n");
        }

        private void OnWindowsSizeChange(object sender, SizeChangedEventArgs arg)
        {
            onSizeChange();
        }

        private void dataSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (dataSourceType.SelectedIndex)
            {
                case 0:
                    SampleSource.sourceType = SampleSource.DataSourceType.BasebandFile;
                    break;
                case 1:
                    SampleSource.sourceType = SampleSource.DataSourceType.SoundCard;
                    break;
            }

            if (!isComponentsInited)
                return;

            if(SampleSource.sourceType == SampleSource.DataSourceType.BasebandFile)
            {
                audioPathInput.Visibility = Visibility.Visible;
                browseAudioFileBtn.Visibility = Visibility.Visible;
                stopAudioBtn.Visibility = Visibility.Visible;
                readPeriodInputContainer.Visibility = Visibility.Visible;
            }
            else if(SampleSource.sourceType == SampleSource.DataSourceType.SoundCard)
            {
                audioPathInput.Visibility = Visibility.Collapsed;
                browseAudioFileBtn.Visibility = Visibility.Collapsed;
                stopAudioBtn.Visibility = Visibility.Collapsed;
                readPeriodInputContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void audioPathInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SampleSource.audioFilePath = audioPathInput.Text;
            if(!SampleSource.audioPathEdited)
                SampleSource.audioPathEdited = true;
        }

        private void browseAudioFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Wav文件|*.wav|任意文件|*.";
            if(openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SampleSource.audioFilePath = audioPathInput.Text;
                audioPathInput.Text = openFile.FileName;
                if (!SampleSource.audioPathEdited)
                    SampleSource.audioPathEdited = true;
            }
        }

        private void playAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Play();
        }

        private void pauseAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Pause();
        }

        private void stopAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Stop();
        }

        private void filterBandWidthInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int bandwidth = 1;
            if(int.TryParse(filterBandWidthInput.Text, out bandwidth))
            {
                if (bandwidth < 1)
                    bandwidth = 1;

                filterBandWidthInput.Text = $"{bandwidth}";
                if (mainDSP != null)
                {
                    mainDSP.bandwidth = bandwidth;
                    mainDSP.UpdateFilter();
                }
            }
            else
            {
                filterBandWidthInput.Text = "0";
            }
        }

        private void filterFreqShiftInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int freqShift = 0;
            if (int.TryParse(filterFreqShiftInput.Text, out freqShift))
            {
                filterFreqShiftInput.Text = $"{freqShift}";
                if (mainDSP != null)
                {
                    mainDSP.frequencyShift = freqShift;
                    mainDSP.UpdateFilter();
                }
            }
            else
            {
                filterFreqShiftInput.Text = "0";
            }
        }

        private void enableFilterBox_Click(object sender, RoutedEventArgs e)
        {
            mainDSP.EnableFilter = (bool)enableFilterBox.IsChecked;
        }

        private void enableProcessorBox_Click(object sender, RoutedEventArgs e)
        {
            mainDSP.EnableProcess = (bool)enableProcessorBox.IsChecked;
        }

        private void drawerPeriodInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int period = mainDSP.spectrumPeriod;
            if (int.TryParse(drawerPeriodInput.Text, out period))
            {
                drawerPeriodInput.Text = $"{period}";
                if (mainDSP != null)
                {
                    mainDSP.spectrumPeriod = period;
                }
            }
            else
            {
                drawerPeriodInput.Text = mainDSP.spectrumPeriod.ToString();
            }
        }

        private void constellationScaleBox_LostFocus(object sender, RoutedEventArgs e)
        {
            float consScale = mainDSP.ConstellationMultiply;
            if (float.TryParse(constellationScaleBox.Text, out consScale))
            {
                constellationScaleBox.Text = $"{consScale}";
                if (mainDSP != null)
                {
                    mainDSP.ConstellationMultiply = consScale;
                }
            }
            else
            {
                constellationScaleBox.Text = mainDSP.ConstellationMultiply.ToString();
            }
        }

        private void spectrumScaleBox_LostFocus(object sender, RoutedEventArgs e)
        {
            float spectrumScale = mainDSP.spectrumScale;
            if (float.TryParse(spectrumScaleBox.Text, out spectrumScale))
            {
                spectrumScaleBox.Text = $"{spectrumScale}";
                if (mainDSP != null)
                {
                    mainDSP.spectrumScale = spectrumScale;
                }
            }
            else
            {
                spectrumScaleBox.Text = mainDSP.spectrumScale.ToString();
            }
        }

        private void sampleReadPeriodInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int period = SampleSource.readPeriod;
            if (int.TryParse(sampleReadPeriodInput.Text, out period))
            {
                period = Math.Abs(period);
                sampleReadPeriodInput.Text = $"{period}";
                SampleSource.readPeriod = period;
            }
            else
            {
                constellationScaleBox.Text = SampleSource.readPeriod.ToString();
            }
        }

        private void freqShiftBox_LostFocus(object sender, RoutedEventArgs e)
        {
            float freqShift = mainDSP.demodulator.freqShift.Freq;
            if (float.TryParse(freqShiftBox.Text, out freqShift))
            {
                freqShiftBox.Text = $"{freqShift}";
                mainDSP.demodulator.freqShift.Freq = freqShift;
            }
            else
            {
                freqShiftBox.Text = mainDSP.demodulator.freqShift.Freq.ToString();
            }
        }

        private void resetProcessBtn_Click(object sender, RoutedEventArgs e)
        {
            mainDSP.ResetDecoder();
        }

        private void processSymbolRate_LostFocus(object sender, RoutedEventArgs e)
        {
            int symbolRate = MainDSP.decoderSet.symbolRate;
            if (int.TryParse(processSymbolRate.Text, out symbolRate))
            {
                processSymbolRate.Text = $"{symbolRate}";
                MainDSP.decoderSet.symbolRate = symbolRate;
            }
            else
            {
                processSymbolRate.Text = MainDSP.decoderSet.symbolRate.ToString();
            }
        }

        private void satDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PacketInfo? packet = satDataList.SelectedItem as PacketInfo;
            SatDataUI.SelectPacket(packet);
        }

        private void clearSatDataBtn_Click(object sender, RoutedEventArgs e)
        {
            SatDataUI.ClearPackets();
        }

        private void clearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.ClearLog();
        }

        private void processSatellite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DecoderSet decoderSet = DecoderSet.LoadPreset(satellites[(string)processSatellite.SelectedValue]);
            MainDSP.decoderSet = decoderSet;
            processModulation.SelectedIndex = modulations.IndexOf(decoderSet.demodulator);
            processSymbolRate.Text = decoderSet.symbolRate.ToString();
        }

        //private void applyFilterBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (mainDSP != null)
        //        mainDSP.UpdateFilter();
        //}
    }
}