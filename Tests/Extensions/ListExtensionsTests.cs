using Xunit;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic; // Namespace donde está ListExtensions

namespace Tests.Extensions // O Tests.BusinessLogic
{
    public class ListExtensionsTests
    {
        [Fact]
        public void Shuffle_ShouldPreserveElements()
        {
            /* DOCUMENTACIÓN
             * ✔ Objetivo: Asegurar que Shuffle no agrega ni elimina elementos.
             */

            // Arrange
            var list = new List<int> { 1, 2, 3, 4, 5 };
            var originalCount = list.Count;
            var originalSum = list.Sum();

            // Act
            list.Shuffle();

            // Assert
            Assert.Equal(originalCount, list.Count);
            Assert.Equal(originalSum, list.Sum()); // La suma debe ser la misma

            // Verificamos que sigan estando los mismos números (aunque en otro orden)
            Assert.Contains(1, list);
            Assert.Contains(5, list);
        }

        [Fact]
        public void Shuffle_WhenListIsEmpty_ShouldNotThrow()
        {
            // Arrange
            var list = new List<string>();

            // Act
            list.Shuffle();

            // Assert
            Assert.Empty(list); // No debe fallar
        }

        [Fact]
        public void Shuffle_WhenListHasOneElement_ShouldRemainSame()
        {
            // Arrange
            var list = new List<int> { 42 };

            // Act
            list.Shuffle();

            // Assert
            Assert.Single(list);
            Assert.Equal(42, list[0]);
        }
    }
}