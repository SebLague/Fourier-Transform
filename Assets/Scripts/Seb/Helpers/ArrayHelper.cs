using System;

namespace Seb.Helpers
{
    // ---- Version 0.5 [26/Jan/2026] ----
    public static class ArrayHelper
    {
        /// <summary> Randomly shuffles the elements of the given array </summary>
        public static void ShuffleArrayTest<T>(T[] array, System.Random rng)
        {
            // wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
            for (int i = 0; i < array.Length - 1; i++)
            {
                int randomIndex = rng.Next(i, array.Length);
                (array[randomIndex], array[i]) = (array[i], array[randomIndex]); // Swap
            }
        }

        /// <summary> Randomly shuffles the elements of the given array </summary>
        public static void ShuffleArray<T>(Span<T> array, Random rng)
        {
            // wikipedia.org/wiki/Fisher–Yates_shuffle#The_modern_algorithm
            for (int i = 0; i < array.Length - 1; i++)
            {
                int randomIndex = rng.Next(i, array.Length);
                (array[randomIndex], array[i]) = (array[i], array[randomIndex]); // Swap
            }
        }

        public static void SortArray<ItemT>(ItemT[] items, System.Comparison<ItemT> compareFunction)
        {
            for (int i = 0; i < items.Length - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    int comparison = compareFunction(items[swapIndex], items[j]);
                    bool swap = comparison > 0;

                    if (swap)
                    {
                        (items[j], items[swapIndex]) = (items[swapIndex], items[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the given array based on their corresponding 'score' values.
        /// Note: the scores array will also be sorted in the process.
        /// </summary>
        public static void SortByScores<ItemT, ScoreT>(ItemT[] items, ScoreT[] scores, bool ascending) where ScoreT : System.IComparable
        {
            for (int i = 0; i < items.Length - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    int comparison = scores[swapIndex].CompareTo(scores[j]);
                    bool swap = ascending ? comparison > 0 : comparison < 0;

                    if (swap)
                    {
                        (items[j], items[swapIndex]) = (items[swapIndex], items[j]);
                        (scores[j], scores[swapIndex]) = (scores[swapIndex], scores[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an array of indices indicating where each element in given array would need to move in order to be sorted.
        /// The given array will not be altered.
        /// </summary>
        public static int[] CreateSortedIndices<T>(T[] items, System.Func<T, T, int> compare)
        {
            int[] sortedIndices = new int[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                sortedIndices[i] = i;
            }

            for (int i = 0; i < items.Length - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    bool swap = compare(GetItem(swapIndex), GetItem(j)) > 0;

                    if (swap)
                    {
                        (sortedIndices[j], sortedIndices[swapIndex]) = (sortedIndices[swapIndex], sortedIndices[j]);
                    }
                }
            }

            return sortedIndices;

            T GetItem(int i) => items[sortedIndices[i]];
        }

        public static bool ResizeAndCopy<T>(ref T[] array, T[] copySource)
        {
            bool hasResized = Resize(ref array, copySource.Length);

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = copySource[i];
            }

            return hasResized;
        }

        public static T[] CreateCopy<T>(T[] copySource)
        {
            T[] target = new T[copySource.Length];

            for (int i = 0; i < target.Length; i++)
            {
                target[i] = copySource[i];
            }

            return target;
        }


        public static bool Resize<T>(ref T[] array, int size)
        {
            if (array == null)
            {
                array = new T[size];
                return true;
            }

            if (array.Length != size)
            {
                Array.Resize(ref array, size);
                return true;
            }

            return false;
        }

        public static T[] Concatenate<T>(T[] a, T[] b)
        {
            T[] combined = new T[a.Length + b.Length];

            for (int i = 0; i < a.Length; i++)
            {
                combined[i] = a[i];
            }

            for (int i = 0; i < b.Length; i++)
            {
                combined[i + a.Length] = b[i];
            }

            return combined;
        }
    }
}