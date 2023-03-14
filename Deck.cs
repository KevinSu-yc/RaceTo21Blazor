using System;
using System.Collections.Generic;

namespace RaceTo21Blazor
{
    public class Deck
    {
        // The cards and the image file names shouldn't be changed without using specific methods in Deck object so I set them to private.
        private List<Card> cards = new List<Card>(); // All the cards in the deck

        // A dictionary that helps get an image file name for a card according to it's short name
        public readonly Dictionary<string, string> cardImageName = new Dictionary<string, string>(); // Images are placed in wwwroot/sample-data/card-img

        /// <summary>
        /// Represents a Deck that inlcudes 52 Cards.
        /// </summary>
        public Deck()
        {
            Console.WriteLine("Building Deck...");
            string[] suits = { "Spades", "Hearts", "Clubs", "Diamonds" };

            for (int cardVal = 1; cardVal <= 13; cardVal++)
            {
                foreach (string cardSuit in suits)
                {
                    string cardValName; // a card value waited to be modified for forming card names
                    string cardImgNumber; // a card valeu waited to be modified for forming card image file names
                    switch (cardVal)
                    {
                        case 1:
                            cardValName = "Ace";
                            // Substring(x, y) returns a y letters long string starts from the x letter of the original string (the first letter's index is 0)
                            cardImgNumber = cardValName.Substring(0,1); // if the card value name is not a number, get it's first letter for image file name
                            break;
                        case 11:
                            cardValName = "Jack";
                            cardImgNumber = cardValName.Substring(0, 1);
                            break;
                        case 12:
                            cardValName = "Queen";
                            cardImgNumber = cardValName.Substring(0, 1);
                            break;
                        case 13:
                            cardValName = "King";
                            cardImgNumber = cardValName.Substring(0, 1);
                            break;
                        default:
                            cardValName = cardVal.ToString();
                            cardImgNumber = cardVal.ToString().PadLeft(2, '0'); // if the card value name is a number, place a 0 before the value if it's not 2-digit, ex: 7 -> 07
                            break;
                    }

                    // If the card value is 10, don't modify, otherwise, get the first digit/letter for a short name
                    string shortCardName = (cardValName == "10") ? "10" : cardValName.Substring(0,1);
                    cards.Add(new Card($"{shortCardName}{cardSuit.Substring(0, 1)}", $"{cardValName} of {cardSuit}")); // Also only use the first letter of the card suit for the short name

                    // dictionary for card image file names, the key uses the short name of the card
                    cardImageName[$"{shortCardName}{cardSuit.Substring(0, 1)}"] = $"card_{cardSuit.ToLower()}_{cardImgNumber}.png";
                }
            }
        }

        /// <summary>
        /// Shuffles the deck into a random order. Called by Game object.
        /// </summary>
        public void Shuffle()
        {
            Console.WriteLine("Shuffling Cards...");

            Random rng = new Random();

            // one-line method that uses Linq:
            // cards = cards.OrderBy(a => rng.Next()).ToList();

            // multi-line method that uses Array notation on a list!
            // (this should be easier to understand)
            for (int i=0; i<cards.Count; i++)
            {
                Card tmp = cards[i];
                int swapindex = rng.Next(cards.Count);
                cards[i] = cards[swapindex];
                cards[swapindex] = tmp;
            }
        }

        /// <summary>
        /// Prints the information of all the cards on the Console. Called for checking the deck is built successfully.
        /// </summary>
        public void ShowAllCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                // print the index, short name, and image file name of the cards
                Console.Write(i + ":" + cards[i].Id + $"({cardImageName[cards[i].Id]})"); // a list property can look like an Array!
                if (i < cards.Count -1)
                {
                    Console.Write(" ");
                } else
                {
                    Console.WriteLine("");
                }
            }
        }

        /// <summary>
        /// Deals the first card from the Deck's Card list. Called by Game object.
        /// </summary>
        /// <returns>Returns the Card object removed from the Deck</returns>
        public Card DealTopCard()
        {
            Card card = cards[cards.Count - 1];
            cards.RemoveAt(cards.Count - 1);
            return card;
        }
    }
}

