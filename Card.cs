using System;
using System.Collections.Generic;

namespace RaceTo21Blazor
{
    /// <summary>
    /// Represents a poker card.
    /// </summary>
    public class Card
    {
        // The card names should never change so I used private setters for them to make them read-only in other classed
        public string Id { get; private set; } // Short name of the card, ex: 7D.
        public string DisplayName { get; private set; } // Full name of the card, ex: 7 of Diamonds

        /// <summary>
        /// Create a poker card.
        /// </summary>
        /// <param name="shordCardName">Short name of the card, ex: 7D</param>
        /// <param name="longCardName">Full name of the card, ex: 7 of Diamonds</param>
        public Card(string shordCardName, string longCardName)
        {
            Id = shordCardName;
            DisplayName = longCardName;
        }

    }
}
