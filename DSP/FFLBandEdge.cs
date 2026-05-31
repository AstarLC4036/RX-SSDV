using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public class FFLBandEdge : DspBlock
    {
        /// <summary>
        /// Process signal.
        /// </summary>
        /// <param name="inputReal">Input real samples</param>
        /// <param name="inputImag">Input imag samples</param>
        /// <param name="outputReal">Output real samples</param>
        /// <param name="outputImag">Output imag samples</param>
        public override int Process(int inputSize, float[] inputReal, float[] inputImag, float[] outputReal, float[] outputImag)
        {
            return inputSize;
        }

        //public void DesignFilter()
        //{
        //    //gr::thread::scoped_lock lock (d_setlock) ;
        //    const int M = rintf(static_cast<float>(d_filter_size) / d_sps);
        //    float power = 0.0f;

        //    // Create the baseband filter by adding two sincs together
        //    std::vector<float> bb_taps;
        //    bb_taps.reserve(d_filter_size);
        //    const float half_sps_inv = 2.0f / d_sps;
        //    for (size_t i = 0; i < d_filter_size; i++)
        //    {
        //        const float k = -M + i * half_sps_inv;
        //        const float position = d_rolloff * k;
        //        const float tap = sinc(position - 0.5f) + sinc(position + 0.5f);
        //        power += tap * tap;

        //        bb_taps.push_back(tap);
        //    }

        //    d_taps_lower.resize(d_filter_size);
        //    d_taps_upper.resize(d_filter_size);

        //    // Create the band edge filters by spinning the baseband
        //    // filter up and down to the right places in frequency.
        //    // Also, normalize the power in the filters
        //    using signed_type = std::make_signed < decltype(d_filter_size) >::type;
        //    const signed_type N = (bb_taps.size() - 1) / 2;
        //    const float invpower = 1.0f / power;
        //    const float inv_twice_sps = 0.5f / d_sps;
        //    for (decltype(d_filter_size) i = 0; i < d_filter_size; i++)
        //    {
        //        const float tap = bb_taps[i] * invpower;
        //        const float k = (static_cast<signed_type>(i) - N) * inv_twice_sps;

        //        const size_t index = d_filter_size - i - 1;
        //        d_taps_lower[index] = tap * gr_expj(-M_TWOPI * (1 + d_rolloff) * k);
        //        d_taps_upper[index] = std::conj(d_taps_lower[d_filter_size - i - 1]);
        //    }

        //    d_filter_upper =
        //        std::make_unique<gr::filter::kernel::fir_filter_with_buffer_ccc>(d_taps_upper);
        //    d_filter_lower =
        //        std::make_unique<gr::filter::kernel::fir_filter_with_buffer_ccc>(d_taps_lower);
        //}
    }
}
