using RX_SSDV.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using static RX_SSDV.Utils.BinaryUtils;

namespace RX_SSDV.Protocol.CCSDS.Viterbi
{
    public class Trellis
    {
        private Viterbi viterbi;
        private int[] newPathDst;
        private int stateCount;

        public List<int> stateList;
        public int[] pathDst;
        public int StateCount => stateCount;

        public (int, int) MinPath
        {
            get
            {
                int index = -1;
                int min = int.MaxValue;
                for(int i = 0; i < pathDst.Length; i++)
                {
                    if(pathDst[i] < min)
                    {
                        min = pathDst[i];
                        index = i;
                    }
                }
                return (index, min);
            }
        }

        public Trellis(Viterbi viterbi) 
        {
            this.viterbi = viterbi;
            stateCount = viterbi.StateCount;
            Init();
        }

        /// <summary>
        /// Init <see cref="Trellis"/>
        /// </summary>
        public void Init()
        {
            stateList = new List<int>(stateCount * 4096);
            pathDst = new int[stateCount];
            newPathDst = new int[stateCount];

            for (int i = 1; i < pathDst.Length; i++)
                pathDst[i] = int.MaxValue / 2;
        }

        /// <summary>
        /// Clear trellis.
        /// </summary>
        public void ClearTrellis()
        {
            stateList.Clear();
            Array.Clear(pathDst, 0, pathDst.Length);
            Array.Clear(newPathDst, 0, pathDst.Length);
        }

        public void CleanUpTrellis()
        {
            stateList.Clear();
            Array.Clear(newPathDst, 0, pathDst.Length);

            int minPath = MinPath.Item2;
            for(int i = 0; i < pathDst.Length; i++)
            {
                pathDst[i] -= minPath;
            }
        }

        /// <summary>
        /// Update surviving path.
        /// </summary>
        /// <param name="bits">Input bits</param>
        public void UpdateSurvivingPath(byte bits)
        {
            for(int i = 0; i < pathDst.Length; i++)
            {
                (int, int) p = SurvivingPath(bits, i);
                newPathDst[i] = p.Item1;
                stateList.Add(p.Item2);
            }

            int[] temp = pathDst;
            pathDst = newPathDst;
            newPathDst = temp;
        }

        /// <summary>
        /// Find the surviving path between input bits and a state.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="state">state</param>
        /// <returns>The minimum Hamming distance and the state</returns>
        public ValueTuple<int, int> SurvivingPath(byte bits, int state)
        {
            (int, int) sourceStates = CalcSourceStates(state, viterbi.Constraint);
            int sourceState1 = sourceStates.Item1;
            int sourceState2 = sourceStates.Item2;
            
            int recoverInput = ReadInt(state, 6);

            int pd1 = pathDst[sourceState1] + HammingDst(bits, sourceState1, recoverInput);
            int pd2 = pathDst[sourceState2] + HammingDst(bits, sourceState2, recoverInput);

            return pd1 <= pd2 ? (pd1, sourceState1) : (pd2, sourceState2);
        }

        /// <summary>
        /// Calculate Hamming distance between input bits and current state.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="curState">Current state</param>
        /// <param name="input">New bit of state</param>
        /// <returns>The Hamming distance</returns>
        public int HammingDst(byte bits, int curState, int input)
        {
            int state = viterbi.BranchOutputs[curState, input];

            //Read code
            byte rx1 = ReadInt(bits, 1);
            byte rx2 = ReadInt(bits, 2);
            byte tx1 = ReadInt(state, 1);
            byte tx2 = ReadInt(state, 2);

            //Calcucate Hamming dst
            int dst = (rx1 ^ tx1) + (rx2 ^ tx2);

            return dst;
        }

        /// <summary>
        /// Calculate next statees by given state int.
        /// </summary>
        /// <param name="state">The state</param>
        /// <param name="constraint">Constraint of the state</param>
        /// <returns>Next states</returns>
        public static (int, int) CalcSourceStates(int state, int constraint)
        {
            int s = (state & ((1 << (constraint - 2)) - 1)) << 1;
            return (s | 0, s | 1);
        }

        /// <summary>
        /// Calculate source statees by given state int.
        /// </summary>
        /// <param name="state">The state</param>
        /// <param name="constraint">Constraint of the state</param>
        /// <returns>Next states</returns>
        public static (int, int) CalcNextStates(int state, int constraint)
        {
            int s = state >> 1;
            //return (s | (0 << (constraint - 2)), s | (1 << (constraint - 2));
            return (s | 0, s | (1 << (constraint - 2)));
        }
    }
}
