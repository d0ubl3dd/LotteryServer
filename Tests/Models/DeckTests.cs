using Xunit;
using BusinessLogic.Models;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Models
{
    public class DeckTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWith54Cards()
        {
            var deck = new Deck();
            Assert.Equal(54, deck.CardsRemaining);
        }

        [Fact]
        public void DrawCard_WhenDeckIsFull_ShouldReturnCardAndDecreaseCount()
        {
            var deck = new Deck();
            int initialCount = deck.CardsRemaining;

            var card = deck.DrawCard();

            Assert.NotNull(card);
            Assert.Equal(initialCount - 1, deck.CardsRemaining);
        }

        [Fact]
        public void DrawCard_WhenDeckIsEmpty_ShouldReturnNull()
        {
            var deck = new Deck();

            for (int i = 0; i < 54; i++)
            {
                deck.DrawCard();
            }

            Assert.Equal(0, deck.CardsRemaining);

            var card = deck.DrawCard();
            Assert.Null(card);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]
        [InlineData(23)]
        [InlineData(24)]
        [InlineData(25)]
        [InlineData(26)]
        [InlineData(27)]
        [InlineData(28)]
        [InlineData(29)]
        [InlineData(30)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(34)]
        [InlineData(35)]
        [InlineData(36)]
        [InlineData(37)]
        [InlineData(38)]
        [InlineData(39)]
        [InlineData(40)]
        [InlineData(41)]
        [InlineData(42)]
        [InlineData(43)]
        [InlineData(44)]
        [InlineData(45)]
        [InlineData(46)]
        [InlineData(47)]
        [InlineData(48)]
        [InlineData(49)]
        [InlineData(50)]
        [InlineData(51)]
        [InlineData(52)]
        [InlineData(53)]
        [InlineData(54)]
        public void Constructor_ShouldContainSpecificCardId(int expectedId)
        {
            var deck = new Deck();
            var drawnCards = new List<Card>();

            while (deck.CardsRemaining > 0)
            {
                drawnCards.Add(deck.DrawCard());
            }

            Assert.Contains(drawnCards, c => c.Id == expectedId);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(27)]
        [InlineData(50)]
        [InlineData(53)]
        public void DrawCard_MultipleTimes_ShouldDecreaseCountAccurately(int cardsToDraw)
        {
            var deck = new Deck();
            int initialCount = 54;

            for (int i = 0; i < cardsToDraw; i++)
            {
                deck.DrawCard();
            }

            Assert.Equal(initialCount - cardsToDraw, deck.CardsRemaining);
        }

        [Fact]
        public void Deck_ShouldContainUniqueCardsOnly()
        {
            var deck = new Deck();
            var drawnIds = new HashSet<int>();

            while (deck.CardsRemaining > 0)
            {
                var card = deck.DrawCard();
                Assert.True(drawnIds.Add(card.Id));
            }

            Assert.Equal(54, drawnIds.Count);
        }

        [Fact]
        public void DrawCard_ShouldReturnDifferentCardsOnConsecutiveDraws()
        {
            var deck = new Deck();

            var card1 = deck.DrawCard();
            var card2 = deck.DrawCard();

            Assert.NotEqual(card1.Id, card2.Id);
        }
    }
}