using NAudio.SoundFont;
using RX_SSDV.DSP;
using RX_SSDV.Protocol;
using RX_SSDV.Protocol.CCSDS;
using RX_SSDV.Protocol.USP;
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
            AO123,
            GEOSCAN
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
            CCSDS,
            USP
        }

        public enum DataProcessor
        {
            SSDV,
            AO123Decoder
        }

        public Demodulator demodulator = Demodulator.BPSK;
        public Decoder decoder = Decoder.CCSDS;
        public DataProcessor processor = DataProcessor.SSDV;
        public int symbolRate = 1200;
        public int packetSize = 255;

        public static Dictionary<Satellite, DecoderSet> presetDecoders = new Dictionary<Satellite, DecoderSet>()
        {
            { Satellite.AO123, new DecoderSet(Demodulator.BPSK, Decoder.CCSDS, DataProcessor.AO123Decoder, 9600, 255) }, //AO-123(ASRTU-1) BPSK 9600bps r=1/2 CCSDS Concatenated SSDV & TLM
            { Satellite.GEOSCAN, new DecoderSet(Demodulator.GMSK, Decoder.USP, DataProcessor.SSDV, 9600, 255) } //Geoscan GMSK 9600bps USP SSDV & TLM
        };
        public static Dictionary<Demodulator, Type> demodulators = new Dictionary<Demodulator, Type>()
        {
            { Demodulator.BPSK, typeof(BpskDemod) },
            { Demodulator.GMSK, typeof(GmskDemod) }
        };
        public static Dictionary<Decoder, Type> decoders = new Dictionary<Decoder, Type>()
        {
            { Decoder.CCSDS, typeof(CCSDSDecoder) },
            { Decoder.USP, typeof(USPDecoder) }
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
                case Decoder.CCSDS:
                    ((CCSDSDecoder)dec).Init(true, true, packetSize, GetProcessor());
                    break;
                case Decoder.USP:
                    break; //TODO
            }

            return dec;
        }

        public ITransportDecoder GetProcessor()
        {
            return (ITransportDecoder)Activator.CreateInstance(processors[processor]);
        }
    }
}
