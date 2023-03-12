using System;
using System.Collections.Generic;

namespace RaceTo21Blazor
{
	public class Player
	{
		public string Name { get; set; }
		public List<Card> cards = new List<Card>(); // The cards in the player's hands
		public PlayerStatus status = PlayerStatus.active; // Whether the player wants to keep playing, or if the player is able to keep playing
		public int score; // The score calculated by the cards in the player's hands
		public int cash = 100; // Set a default value for player to have at the beginning
		public int wins = 0; // Amount of times that the player wins a round

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
            if (betAmount > cash) // if a player can't afford the betAmount, don't subtract the amount from player's cash
            {
				return -1;
            }

			cash -= betAmount;
			return betAmount;
        }
	}
}

