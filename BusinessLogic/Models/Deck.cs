using BusinessLogic;
using System.Collections.Generic;

namespace BusinessLogic.Models
{
    public class Deck
    {
        private readonly Stack<Card> _cards;

        public Deck()
        {
            List<Card> allCards = new List<Card>
            {
                new Card { Id = 1 },
                new Card { Id = 2 },
                new Card { Id = 3 },
                new Card { Id = 4 },
                new Card { Id = 5 },
                new Card { Id = 6 },
                new Card { Id = 7 },
                new Card { Id = 8 },
                new Card { Id = 9 },
                new Card { Id = 10 },
                new Card { Id = 11 },
                new Card { Id = 12 },
                new Card { Id = 13 },
                new Card { Id = 14 },
                new Card { Id = 15 },
                new Card { Id = 16 },
                new Card { Id = 17 },
                new Card { Id = 18 },
                new Card { Id = 19 },
                new Card { Id = 20 },
                new Card { Id = 21 },
                new Card { Id = 22 },
                new Card { Id = 23 },
                new Card { Id = 24 },
                new Card { Id = 25 },
                new Card { Id = 26 },
                new Card { Id = 27 },
                new Card { Id = 28 },
                new Card { Id = 29 },
                new Card { Id = 30 },
                new Card { Id = 31 },
                new Card { Id = 32 },
                new Card { Id = 33 },
                new Card { Id = 34 },
                new Card { Id = 35 },
                new Card { Id = 36 },
                new Card { Id = 37 },
                new Card { Id = 38 },
                new Card { Id = 39 },
                new Card { Id = 40 },
                new Card { Id = 41 },
                new Card { Id = 42 },
                new Card { Id = 43 },
                new Card { Id = 44 },
                new Card { Id = 45 },
                new Card { Id = 46 },
                new Card { Id = 47 },
                new Card { Id = 48 },
                new Card { Id = 49 },
                new Card { Id = 50 },
                new Card { Id = 51 },
                new Card { Id = 52 },
                new Card { Id = 53 },
                new Card { Id = 54 }
            };

            allCards.Shuffle();

            _cards = new Stack<Card>(allCards);
        }

        public Card DrawCard()
        {
            if (_cards.Count > 0)
            {
                return _cards.Pop();
            }
            return null;
        }

        public int CardsRemaining => _cards.Count;
    }
}