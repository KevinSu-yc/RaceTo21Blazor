using System;

namespace RaceTo21Blazor
{
	/// <summary>
	/// Represents different phase of the game
	/// </summary>
	public enum GameTask
	{
		GetGameInfo,
		AskBet,
		PlayerTurn,
		AnnounceRoundWinner,
		CheckForNewRound,
		AnnounceFinalWinner
	}
}