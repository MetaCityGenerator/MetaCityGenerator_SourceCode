using System;
using System.Collections.Generic;

namespace UrbanX.Algorithms.Utility
{
    public static class Extension
    {
        /// <summary>
        /// Swaps two values in an IList<T> collection given their indexes.
        /// </summary>
        public static void Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
        {
            if (list.Count < 2 || firstIndex == secondIndex)
            {
                return;
            }
            var temp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = temp;
        }

        /// <summary>
        /// Populates a collection with a specific value.
        /// </summary>
        public static void Populate<T>(this IList<T> collection, T value)
        {
            if (collection == null)
            {
                return;
            }
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i] = value;
            }
        }

        /// <summary>
        /// Populates the specified two-dimensional with a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="row"></param>
        /// <param name="columns"></param>
        /// <param name="defaultValue"></param>
        public static void Populate<T>(this T[,] array, int row, int columns, T defaultValue = default)
        {
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    array[i, j] = defaultValue;
                }
            }
        }


        /// <summary>
        /// Shuffle the array.
        /// </summary>
        /// <typeparam name="T">Array element type.</typeparam>
        /// <param name="array">Array to shuffle.</param>
        public static void Shuffle<T>(this IList<T> collection)
        {
            Random random = new Random();
            int n = collection.Count;
            for (int i = 0; i < (n - 1); i++)
            {
                // Use Next on random instance with an argument.
                // ... The argument is an exclusive bound.
                //     So we will not go past the end of the array.
                int r = i + random.Next(n - i);
                T t = collection[r];
                collection[r] = collection[i];
                collection[i] = t;
            }
        }

        /// <summary>
        /// Tries to find a match for the predicate. Returns true if found; otherwise false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="match"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public static bool TryFindFirst<T>(this LinkedList<T> collection, Predicate<T> match, out T found)
        {
            // Initialize the output parameter
            found = default;

            if (collection.Count == 0)
            {
                return false;
            }

            var currentNode = collection.First;

            try
            {
                while (currentNode != null)
                {
                    if (match(currentNode.Value))
                    {
                        found = currentNode.Value;
                        return true;
                    }
                    currentNode = currentNode.Next;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Centralize a text.
        /// </summary>
        public static string PadCenter(this string text, int newWidth, char fillerCharacter = ' ')
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int length = text.Length;
            int charactersToPad = newWidth - length;
            if (charactersToPad < 0) throw new ArgumentException("New width must be greater than string length.", "newWidth");
            int padLeft = charactersToPad / 2 + charactersToPad % 2;
            //add a space to the left if the string is an odd number
            int padRight = charactersToPad / 2;

            return new String(fillerCharacter, padLeft) + text + new String(fillerCharacter, padRight);
        }


        /// <summary>
        /// Extension for creating circularly linked-list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="current"></param>
        /// <returns></returns>
        public static LinkedListNode<T> NextOrFirst<T>(this LinkedListNode<T> current)
        {
            return current.Next ?? current.List.First;
        }

        public static LinkedListNode<T> PreviousOrLast<T>(this LinkedListNode<T> current)
        {
            return current.Previous ?? current.List.Last;
        }

    }
}
