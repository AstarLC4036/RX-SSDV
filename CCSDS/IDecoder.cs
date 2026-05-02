using RX_SSDV.Decoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.CCSDS
{
    public interface IDecoder
    {
        public void Init(bool useDiffDecode, bool useDescrambling, int frameSize, ITransportDecoder? decoder);
        public void Process(float[] inputSamplesI, float[] inputSamplesQ, byte[] outputBits, out int outputSize, int inputSize = -1);
    }
}
