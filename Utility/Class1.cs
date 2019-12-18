using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class Tools
    {
        public static IComparable[] MergeSort(IComparable[] sortingArray)
        {
            Stack<int[]> Indicies = new Stack<int[]>();
            Indicies.Push(new int[] { 0 });
            
            bool dividing = true;
            while (dividing)
            {
                dividing = false;
                int[] currentDivides = Indicies.Peek();
                List<int> newDivides = new List<int>();
                for (int i = 0; i < currentDivides.Length; i++)
                {
                    int startIndex = currentDivides[i];
                    int endIndex;

                    if (currentDivides[i] == currentDivides.Length - 1)
                    {
                        endIndex = sortingArray.Length;
                    }
                    else
                    {
                        endIndex = currentDivides[i + 1];
                    }

                    if (endIndex - startIndex > 1)
                    {
                        dividing = true;
                        newDivides.Add(startIndex);
                        newDivides.Add((endIndex + startIndex) / 2);
                    }
                }

                if (dividing)
                {
                    Indicies.Push(newDivides.ToArray());
                }
            }

            while (Indicies.Peek().Length > 1)
            {
                int[] indicies = Indicies.Pop();
                for (int i = 0; i < indicies.Length; i += 2)
                {
                    Array.Copy(sortingArray)
                }
            }
        }
    }
}
