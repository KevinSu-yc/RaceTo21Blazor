using System;
using System.Collections.Generic;

namespace RaceTo21Blazor
{
	public class Player
	{
		public string Name { get; set; } // Player name
		public List<Card> Cards { get; set; } = new List<Card>(); // The cards in the player's hands
		public PlayerStatus Status { get; set; } = PlayerStatus.active; // Whether the player wants to keep playing, or if the player is able to keep playing
		public int Score { get; set; } // The score calculated by the cards in the player's hands
		public int Cash { get; set; } = 100; // Set a default value for player to have at the beginning
		public int Wins { get; set; } = 0; // Amount of times that the player wins a round

		/// <summary>
		/// Represents a player in the game
		/// </summary>
		/// <param name="n">The player's name</param>
		public Player(string n)
		{
			Name = n;
        }

		/// <summary>
		/// Bets an amount from player's cash if they can afford it. Called by CardTable object
		/// </summary>
		/// <param name="betAmount">The amount of cash to bet</param>
		/// <returns>Returns the bet amount if the player can afford it, otherwise, return -1</returns>
		public int Bet(int betAmount)
        {
            if (betAmount > Cash) // if a player can't afford the betAmount, don't subtract the amount from player's cash
            {
				return -1;
            }

			Cash -= betAmount;
			return betAmount;
        }
	}
}

