using System;

namespace RaceTo21Blazor
{
	/// <summary>
	/// Represents different status of a player in the game
	/// </summary>
	public enum PlayerStatus
	{
		active, // is able to keep getting cards
		stay, // decided not to keep getting cards
		bust, // the score in hand is over 21
		win, // won a game
		quit, // left the game
	}
}

