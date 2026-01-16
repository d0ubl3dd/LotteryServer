using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic;

namespace Tests.BusinessLogic
{
    public class ListExtensionsTests
    {
        [Fact]
        public void Shuffle_WhenListIsNull_ShouldThrowNullReferenceException()
        {
            IList<int> list = null;
            Assert.Throws<NullReferenceException>(() => ListExtensions.Shuffle(list));
        }

        [Fact]
        public void Shuffle_WhenListIsEmpty_ShouldNotThrow()
        {
            var list = new List<int>();
            ListExtensions.Shuffle(list);
            Assert.Empty(list);
        }

        [Theory]
        [InlineData(1)]
        [InlineData("test")]
        [InlineData(true)]
        public void Shuffle_WhenListHasOneItem_ShouldRemainUnchanged<T>(T item)
        {
            var list = new List<T> { item };
            ListExtensions.Shuffle(list);

            Assert.Single(list);
            Assert.Equal(item, list[0]);
        }

        [Fact]
        public void Shuffle_Integers_ShouldPreserveCount()
        {
            var list = Enumerable.Range(1, 100).ToList();
            int initialCount = list.Count;

            ListExtensions.Shuffle(list);

            Assert.Equal(initialCount, list.Count);
        }

        [Fact]
        public void Shuffle_Strings_ShouldPreserveCount()
        {
            var list = new List<string> { "A", "B", "C", "D", "E" };
            int initialCount = list.Count;

            ListExtensions.Shuffle(list);

            Assert.Equal(initialCount, list.Count);
        }

        [Fact]
        public void Shuffle_Integers_ShouldPreserveAllElements()
        {
            var list = Enumerable.Range(1, 50).ToList();
            var originalSet = new HashSet<int>(list);

            ListExtensions.Shuffle(list);

            foreach (var item in list)
            {
                Assert.Contains(item, originalSet);
            }
            Assert.Equal(originalSet.Count, list.Distinct().Count());
        }

        [Fact]
        public void Shuffle_LargeList_ShouldChangeOrder()
        {
            var list = Enumerable.Range(1, 100).ToList();
            var originalOrder = new List<int>(list);

            ListExtensions.Shuffle(list);

            Assert.NotEqual(originalOrder, list);
        }

        [Fact]
        public void Shuffle_MultipleExecutions_ShouldProduceDifferentResults()
        {
            var list1 = Enumerable.Range(1, 50).ToList();
            var list2 = Enumerable.Range(1, 50).ToList();

            ListExtensions.Shuffle(list1);
            ListExtensions.Shuffle(list2);

            Assert.NotEqual(list1, list2);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        [InlineData(50)]
        public void Shuffle_VerifyNoItemsLost(int count)
        {
            var list = Enumerable.Range(0, count).ToList();
            long sumBefore = list.Sum();

            ListExtensions.Shuffle(list);
            long sumAfter = list.Sum();

            Assert.Equal(sumBefore, sumAfter);
        }

        [Fact]
        public void Shuffle_Booleans_ShouldShuffle()
        {
            var list = new List<bool> { true, false, true, false, true };
            int trueCountBefore = list.Count(x => x);

            ListExtensions.Shuffle(list);
            int trueCountAfter = list.Count(x => x);

            Assert.Equal(trueCountBefore, trueCountAfter);
        }

        [Fact]
        public void Shuffle_CustomObjects_ShouldPreserveReferences()
        {
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();
            var list = new List<object> { obj1, obj2, obj3 };

            ListExtensions.Shuffle(list);

            Assert.Contains(obj1, list);
            Assert.Contains(obj2, list);
            Assert.Contains(obj3, list);
            Assert.Equal(3, list.Count);
        }
    }
}