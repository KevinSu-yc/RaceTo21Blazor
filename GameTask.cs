using System;

namespace RaceTo21Blazor
{
	/// <summary>
	/// Represents different phase of the game
	/// </summary>
	public enum GameTask
	{
		GetNumberOfPlayers,
		GetNames,
		IntroducePlayers,
		AskTimesToWin,
		AskBet,
		OfferFirstCard,
		PlayerTurn,
		CheckForEnd,
		GameOver,
		CheckForNewGame,
		AnnounceCurrentWinner,
		AnnounceFinalWinner
	}
}