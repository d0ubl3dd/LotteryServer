using System.Collections.Generic;

namespace Contracts.GameData
{
    public static class BoardConfigurations
    {
        public static readonly Dictionary<int, List<int>> FixedBoards = new Dictionary<int, List<int>>
        {
            { 1, new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 } },
            
            { 2, new List<int> { 10, 20, 30, 40, 50, 1, 11, 21, 31, 41, 2, 12, 22, 32, 42, 3 } },
            
            { 3, new List<int> { 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39 } },

            { 4, new List<int> { 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 1, 5, 9 } },

            { 5, new List<int> { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 } },

            { 6, new List<int> { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 } },

            { 7, new List<int> { 22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52 } },

            { 8, new List<int> { 2, 5, 9, 14, 18, 21, 27, 33, 35, 39, 41, 44, 48, 51, 53, 6 } },

            { 9, new List<int> { 3, 7, 13, 19, 25, 31, 37, 43, 49, 2, 8, 14, 20, 26, 32, 38 } },

            { 10, new List<int> { 5, 15, 25, 35, 45, 6, 16, 26, 36, 46, 7, 17, 27, 37, 47, 8 } }
        };

        public static List<int> GetBoardById(int boardId)
        {
            if (FixedBoards.ContainsKey(boardId))
            {
                return FixedBoards[boardId];
            }
            return new List<int>();
        }
    }
}