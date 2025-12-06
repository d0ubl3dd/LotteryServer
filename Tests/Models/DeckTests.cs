using Xunit;
using BusinessLogic.Models;
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
        public void DrawCard_ShouldReduceCountAndReturnCard()
        {
            var deck = new Deck();
            int initialCount = deck.CardsRemaining;

            var card = deck.DrawCard();

            Assert.NotNull(card);
            Assert.Equal(initialCount - 1, deck.CardsRemaining);
        }

        [Fact]
        public void DrawCard_WhenEmpty_ShouldReturnNull()
        {
            var deck = new Deck();

            // Vaciamos el mazo
            for (int i = 0; i < 54; i++)
            {
                deck.DrawCard();
            }

            Assert.Equal(0, deck.CardsRemaining);

            // Intentamos sacar una más
            var card = deck.DrawCard();

            Assert.Null(card);
        }
    }
}