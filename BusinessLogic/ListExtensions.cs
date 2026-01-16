using System;
using System.Collections.Generic;

namespace BusinessLogic
{
    public static class ListExtensions
    {
        private static Random _randomNumber = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _randomNumber.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}