using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Utils
{
    public class ArrayUtil
    {
        public static float[] Double2Float(double[] array)
        {
            float[] output = new float[array.Length];

            for(int i = 0; i < array.Length; i++)
            {
                output[i] = (float)array[i];
            }

            return output;
        }

        public static bool CheckNeedUpdate(Array array, int size)
        {
            return array == null || array.Length != size;
        }
    }

    public static class ArrayMemoryExtensions
    {
        private static int _8Bits = sizeof(byte);
        private static int _32Bits = sizeof(int);
        /// <summary>
        /// Makes fast copy of array (or its part) to existing <paramref name="destination"/> array (or its part).
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="destination">Destination array</param>
        /// <param name="size">Number of elements to copy</param>
        /// <param name="sourceOffset">Offset in source array</param>
        /// <param name="destinationOffset">Offset in destination array</param>
        public static void FastCopyTo(this byte[] source, byte[] destination, int size, int sourceOffset = 0, int destinationOffset = 0)
        {
            Buffer.BlockCopy(source, sourceOffset * _8Bits, destination, destinationOffset * _8Bits, size * _8Bits);
        }

        /// <summary>
        /// Makes fast copy of array (or its part) to existing <paramref name="destination"/> array (or its part).
        /// </summary>
        /// <param name="source">Source array</param>
        /// <param name="destination">Destination array</param>
        /// <param name="size">Number of elements to copy</param>
        /// <param name="sourceOffset">Offset in source array</param>
        /// <param name="destinationOffset">Offset in destination array</param>
        public static void FastCopyTo(this int[] source, int[] destination, int size, int sourceOffset = 0, int destinationOffset = 0)
        {
            Buffer.BlockCopy(source, sourceOffset * _32Bits, destination, destinationOffset * _32Bits, size * _32Bits);
        }

        public static int IndexOf<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value)
        {
            int i = 0;
            foreach (var pair in dictionary)
            {
                if (pair.Value.Equals(value))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
    }
}
