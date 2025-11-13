using BusinessLogic;
using System.Collections.Generic;

namespace BusinessLogic.Models
{
    public class Deck
    {
        private Stack<Card> _cards;

        public Deck()
        {
            List<Card> allCards = new List<Card>
            {
                new Card { Id = 1, Name = "El Gallo" },
                new Card { Id = 2, Name = "El Diablo" },
                new Card { Id = 3, Name = "La Dama" },
                new Card { Id = 4, Name = "El Catrín" },
                new Card { Id = 5, Name = "El Paraguas" },
                new Card { Id = 6, Name = "La Sirena" },
                new Card { Id = 7, Name = "La Escalera" },
                new Card { Id = 8, Name = "La Botella" },
                new Card { Id = 9, Name = "El Barril" },
                new Card { Id = 10, Name = "El Árbol" },
                new Card { Id = 11, Name = "El Melón" },
                new Card { Id = 12, Name = "El Valiente" },
                new Card { Id = 13, Name = "El Gorrito" },
                new Card { Id = 14, Name = "La Muerte" },
                new Card { Id = 15, Name = "La Pera" },
                new Card { Id = 16, Name = "La Bandera" },
                new Card { Id = 17, Name = "El Bandolón" },
                new Card { Id = 18, Name = "El Violoncello" },
                new Card { Id = 19, Name = "La Garza" },
                new Card { Id = 20, Name = "El Pájaro" },
                new Card { Id = 21, Name = "La Mano" },
                new Card { Id = 22, Name = "La Bota" },
                new Card { Id = 23, Name = "La Luna" },
                new Card { Id = 24, Name = "El Cotorro" },
                new Card { Id = 25, Name = "El Borracho" },
                new Card { Id = 26, Name = "El Negrito" },
                new Card { Id = 27, Name = "El Corazón" },
                new Card { Id = 28, Name = "La Sandía" },
                new Card { Id = 29, Name = "El Tambor" },
                new Card { Id = 30, Name = "El Camarón" },
                new Card { Id = 31, Name = "Las Jaras" },
                new Card { Id = 32, Name = "El Músico" },
                new Card { Id = 33, Name = "La Araña" },
                new Card { Id = 34, Name = "El Soldado" },
                new Card { Id = 35, Name = "La Estrella" },
                new Card { Id = 36, Name = "El Cazo" },
                new Card { Id = 37, Name = "El Mundo" },
                new Card { Id = 38, Name = "El Apache" },
                new Card { Id = 39, Name = "El Nopal" },
                new Card { Id = 40, Name = "El Alacrán" },
                new Card { Id = 41, Name = "La Rosa" },
                new Card { Id = 42, Name = "La Calavera" },
                new Card { Id = 43, Name = "La Campana" },
                new Card { Id = 44, Name = "El Cantarito" },
                new Card { Id = 45, Name = "El Venado" },
                new Card { Id = 46, Name = "El Sol" },
                new Card { Id = 47, Name = "La Corona" },
                new Card { Id = 48, Name = "La Chalupa" },
                new Card { Id = 49, Name = "El Pino" },
                new Card { Id = 50, Name = "El Pescado" },
                new Card { Id = 51, Name = "La Palma" },
                new Card { Id = 52, Name = "La Maceta" },
                new Card { Id = 53, Name = "El Arpa" },
                new Card { Id = 54, Name = "La Rana" }
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