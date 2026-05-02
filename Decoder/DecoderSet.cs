using NAudio.SoundFont;
using RX_SSDV.CCSDS;
using RX_SSDV.DSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Decoder
{
    public struct DecoderSet
    {
        public enum Satellite
        {
            None,
            AO123
        }

        public enum Demodulator
        {
            BPSK,
            QPSK, //todo
            GMSK,
            GFSK
        }

        public enum Decoder
        {
            CCSDSConcatenated
        }

        public enum DataProcessor
        {
            SSDV,
            AO123Decoder
        }

        public Demodulator demodulator = Demodulator.BPSK;
        public Decoder decoder = Decoder.CCSDSConcatenated;
        public DataProcessor processor = DataProcessor.SSDV;
        public int symbolRate = 1200;
        public int packetSize = 255;

        public static Dictionary<Satellite, DecoderSet> presetDecoders = new Dictionary<Satellite, DecoderSet>()
        {
            { Satellite.AO123, new DecoderSet(Demodulator.BPSK, Decoder.CCSDSConcatenated, DataProcessor.AO123Decoder, 9600, 255) } //AO-123(ASRTU-1) BPSK 9600bps r=1/2 CCSDS Concatenated SSDV & TLM
        };
        public static Dictionary<Demodulator, Type> demodulators = new Dictionary<Demodulator, Type>()
        {
            { Demodulator.BPSK, typeof(BpskDemod)}
        };
        public static Dictionary<Decoder, Type> decoders = new Dictionary<Decoder, Type>()
        {
            { Decoder.CCSDSConcatenated, typeof(CCSDSDecoder)}
        };
        public static Dictionary<DataProcessor, Type> processors = new Dictionary<DataProcessor, Type>()
        {
            { DataProcessor.AO123Decoder, typeof(AsrtuDecoder)}
        };

        public DecoderSet()
        {

        }

        public DecoderSet(Demodulator demodulator, Decoder decoder, DataProcessor processor, int symbolRate, int packetSize)
        {
            this.demodulator = demodulator; 
            this.decoder = decoder; 
            this.processor = processor;
            this.symbolRate = symbolRate;
            this.packetSize = packetSize;
        }

        public static DecoderSet LoadPreset(Satellite satellite)
        {
            DecoderSet set;
            if(presetDecoders.TryGetValue(satellite, out set))
            {
                return set;
            }
            else
            {
                return new DecoderSet();
            }
        }

        public DSP.Demodulator GetDemodulator()
        {
            return (DSP.Demodulator)Activator.CreateInstance(demodulators[demodulator]);
        }

        public IDecoder GetDecoder()
        {
            IDecoder dec = (IDecoder)Activator.CreateInstance(decoders[decoder]);

            switch (decoder)
            {
                case Decoder.CCSDSConcatenated:
                    ((CCSDSDecoder)dec).Init(true, true, packetSize, GetProcessor());
                    break;
            }

            return dec;
        }

        public ITransportDecoder GetProcessor()
        {
            return (ITransportDecoder)Activator.CreateInstance(processors[processor]);
        }
    }
}
