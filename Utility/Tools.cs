using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class Tools
    {
        //I wanted to try building a merge sort algorithm that didn't use recursion, which was an interesting concept as it required handling memory
        //and using stacks appropriately without using a call stack.
        public static T[] MergeSort<T>(T[] sortingArray, bool descendingOrder = false) where T : IComparable
        {
            //Stack of tuple arrays, tuple describes the index and the length of a split segment from the array
            //As the stack gets pushed onto, more and more divisions are made, so more and more segments are made
            //The stack is used to 'remember' the algorithm's splitting and segments to be able to correctly merge back on the way up
            Stack<(int, int)[]> divisions = new Stack<(int, int)[]>();
            divisions.Push(new [] { (0, sortingArray.Length) });                                    //Not actually necessary, just demonstrates the complete array as one segment
            
            bool dividing = true;
            while (dividing)
            {
                dividing = false;                                                                   //Remains false if no divisions are made and loop is exited
                (int, int)[] currentDivides = divisions.Peek();
                List<(int, int)> newDivides = new List<(int, int)>();                               //Using a list instead of an array as it is dynamic, and we
                for (int i = 0; i < currentDivides.Length; i++)                                     //don't know how many divisions and segments will be made
                {
                    int startIndex = currentDivides[i].Item1;
                    int length = currentDivides[i].Item2;

                    if (length > 1)
                    {
                        dividing = true;
                        newDivides.Add((startIndex, length / 2));                                   //Halves length, rounded down (C-style integer arithmetic)
                        newDivides.Add((startIndex + length / 2, length / 2 + length % 2));         //Deliberately rounds up with mod so as to make up for round down from above
                    }
                }

                if (dividing)
                {
                    divisions.Push(newDivides.ToArray());                                       //Once divided, pushes the new state to the stack to be used when merging later
                }
            }

            while (divisions.Count > 1)
            {
                (int, int)[] divides = divisions.Pop();
                for (int i = 0; i < divides.Length; i += 2)
                {
                    int index1 = divides[i].Item1;                  //Splitting up the values from the tuples indication segment index and length to make code easier on the eyes
                    int index2 = divides[i + 1].Item1;
                    int length1 = divides[i].Item2;
                    int length2 = divides[i + 1].Item2;

                    T[] tempArray = new T[length1 + length2];

                    int j, k;                                       //Effectively j + k is the indexer for tempArray, but only one of either j or k will get incremented in each interation
                    for (j = 0, k = 0; j < length1 && k < length2; /*Increments handled in loop*/)
                    {
                        if (sortingArray[index1 + j].CompareTo(sortingArray[index2 + k]) < 0 ^ descendingOrder)     //XOR (^) with descending order just flips the comparison
                        {
                            tempArray[j + k] = sortingArray[index1 + j];
                            j++;
                        }
                        else
                        {
                            tempArray[j + k] = sortingArray[index2 + k];
                            k++;
                        }
                    }

                    //There will often be items left in one array if the other array finished copying first, this just transfers the remainder
                    //j + k (tempArray indexer) is less than the capacity of tempArray if the copying isn't finished
                    if (j + k < length1 + length2)
                    {
                        if (j < k)
                        {
                            Array.Copy(sortingArray, index1 + j, tempArray, j + k, length1 - j);
                        }
                        else
                        {
                            Array.Copy(sortingArray, index2 + k, tempArray, j + k, length2 - k);
                        }
                    }

                    Array.Copy(tempArray, 0, sortingArray, index1, length1 + length2);
                }
            }

            return sortingArray;
        }

        //Merge sort as an extension method
        public static void Sort<T>(this T[] sortingArray, bool descendingOrder = false) where T : IComparable => sortingArray = MergeSort(sortingArray, descendingOrder);

        //This is a small tool to help with array manipulation, for if you want to populate a chunk of an array with just one value
        public static void Populate<T>(this T[] array, T value, int startIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                array[startIndex + i] = value;
            }
        }
    }
}
