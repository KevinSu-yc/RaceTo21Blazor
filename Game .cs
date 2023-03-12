using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceTo21Blazor
{
    /// <summary>
    /// Represents the whole RaceTo21 game
    /// </summary>
    public class Game
    {
        // The CardTable and the cheating mode don't have to be reachable in other classes so I set it as totally private
        // private CardTable cardTable; // object in charge of displaying game information

        // These fields should be read-only outside of Game object so other classes can't affect the game process.
        public List<Player> Players { get; private set; } = new List<Player>(); // list of objects containing player data 
        public List<Player> InGamePlayers 
        { 
            get
            {
                return Players.FindAll(player => player.status != PlayerStatus.quit);
            } 
        }
        public List<Player> rankedPlayers
        {
            get
            {
                return Players.OrderByDescending(player => player.cash).ThenByDescending(player => player.wins).ThenBy(player => player.Name).ToList();
            }
        }

        public Deck Deck { get; private set; } = new Deck(); // deck of cards
        public int NumberOfPlayers { get; set; } // number of players in current game
        public int CurrentPlayer { get; private set; } = 0; // current player on list
        public int CurrentPot { get; set; } = 0; // amount of bet put in current pot
        public static readonly int defaultBet = 10;
        public int TimesToWin { get; set; } = 3;
        public string endGameReason;
        public GameTask NextTask { get; private set; } // keeps track of game state

        /// <summary>
        /// Sets up the card table and suffle the deck while creaing the game. Initiated the task to starts the game process.
        /// Called by Program.
        /// </summary>
        /// <param name="c">The card table set up for the game</param>
        public Game()
        {
            // cardTable = c;
            Deck.Shuffle();

            NumberOfPlayers = 2;
            Players = new List<Player>() { new Player(""), new Player("") };

            NextTask = GameTask.GetNumberOfPlayers;
        }

        // These 4 methods use total players
        public void SetUpGame()
        {
            Deck.Shuffle();
            NextTask = GameTask.GetNumberOfPlayers;
        }

        public void SetPlayers(int count)
        {
            if (count < NumberOfPlayers)
            {
                Players = Players.Take(count).ToList();
                NumberOfPlayers = count;
                return;
            }
            
            for (int i = NumberOfPlayers; i < count; i++)
            {
                AddPlayer("");
            }
            NumberOfPlayers = count;
        }

        public bool CheckRepeatName(int checkIndex)
        {
            string name = Players[checkIndex].Name;
            if (name == "")
            {
                return false;
            }

            if (Players.FindAll(p => p.Name == name).Count > 1) // If there's a player in the player list has the same name as the current input
            {
                return true;
            }

            return false;
        }

        public bool ValidatePlayers()
        {
            if (Players.Find(p => p.Name == "") != null)
            {
                return false;
            }

            foreach (Player player in Players)
            {
                if (Players.FindAll(p => p.Name == player.Name).Count > 1)
                {
                    return false;
                }
            }

            return true;
        }

        // Methods below use in game players

        public int[] AskBet()
        {
            int[] bets = new int[InGamePlayers.Count];
            NextTask = GameTask.AskBet;
            for (int i = 0; i < InGamePlayers.Count; i++)
            {
                int bet = (InGamePlayers[i].cash >= defaultBet) ? defaultBet : InGamePlayers[i].cash;
                CollectBet(bet, i);
                bets[i] = bet;
            }
            return bets;
        }

        public void CollectBet(int betChange, int playerIndex)
        {
            CurrentPot += 2 * betChange;
            InGamePlayers[playerIndex].Bet(betChange);
        }

        public void StartPlay()
        {
            NextTask = GameTask.OfferFirstCard;
            foreach (Player p in InGamePlayers) // Loop through the players who haven't quit 
            {
                Card card = Deck.DealTopCard();
                p.cards.Add(card);
                p.score = ScoreHand(p);
            }
            return;
        }

        public Player Deal(Player player)
        {
            Card card = Deck.DealTopCard();
            player.cards.Add(card);
            player.score = ScoreHand(player);

            if (player.score > 21) // If the total score is over 21, the player is busted
            {
                player.status = PlayerStatus.bust;
            }
            else if (player.score == 21) // If the total score is over 21, the player wins
            {
                player.status = PlayerStatus.win;
            }

            return NextTurn();
        }

        public Player StayPlayer(Player player)
        {
            player.status = PlayerStatus.stay;
            return NextTurn();
        }

        public Player NextTurn()
        {
            CurrentPlayer++; // add 1 to currentPlayer to get to the next player's position
            if (CurrentPlayer >  InGamePlayers.Count - 1)
            {
                CurrentPlayer = 0; // back to the first player who hasn't quit the game
            }

            // Check For End
            if (CheckWinner()) // If there is a winner
            {
                // Determine whose the winner from the players who hasn't quit
                Player winner = DoFinalScoring(InGamePlayers);
                winner.status = PlayerStatus.win;

                // The winner gets the cash from pot and gets a win
                winner.wins++;

                NextTask = GameTask.AnnounceCurrentWinner;
                return winner;
            }

            return null;
        }

        public void CheckNewRound(Player roundWinner)
        {
            Pay(roundWinner, CurrentPot);

            CurrentPot = 0; // Reset the pot
            CurrentPlayer = 0;
            NextTask = GameTask.CheckForNewGame;
            CheckNewGame();
        }

        public void CheckNewGame()
        {
            Player endGamePlayer = Players.Find(player => player.wins == TimesToWin); // Gets the player who wins enough times to end the game
            if (endGamePlayer != null)
            {
                // Card table announce the player who wins the most cash as the final winner
                // (case 1) Someone wins the amount of time set at the beginning
                GetEndGameReason(1, endGamePlayer);
                NextTask = GameTask.AnnounceFinalWinner;
            }
        }

        public void CheckNewGame(string[] keepPlaying)
        {
            // If there is only 1 player who havn't left the game still have cash
            if (InGamePlayers.FindAll(player => player.cash > 0).Count == 1)
            {
                // Card table announce the player who wins the most cash as the final winner
                // (case 2) Only 1 player who doesn't leave the game still has cash
                GetEndGameReason(2, InGamePlayers.Find(player => player.cash > 0));
                NextTask = GameTask.AnnounceFinalWinner;
            }
            else // There are still more than 1 players in current round and they all still have cash
            {
                Player endGamePlayer = Players.Find(player => player.wins == TimesToWin); // Gets the player who wins enough times to end the game
                if (endGamePlayer == null) // If there isn't a player who wins enough times
                {
                    // Ask each one of the players in the current round if they want to keep playing
                    List<Player> readyToQuit = new List<Player>();
                    for (int i = 0; i < InGamePlayers.Count; i++)
                    {
                        if (keepPlaying[i] == "No" || InGamePlayers[i].cash <= 0)
                        {
                            readyToQuit.Add(InGamePlayers[i]);
                        }
                    }
                    foreach (Player quitPlayer in readyToQuit)
                    {
                        quitPlayer.status = PlayerStatus.quit;
                    }

                    ResetPlayers(); // reset player's cards, card points, and status
                    if (InGamePlayers.Count <= 1) // if no more than 1 player wants to keep playing, end the game
                    {
                        // Card table announce the player who wins the most cash as the final winner
                        // (case 3) // No enough players
                        GetEndGameReason(3, null);
                        NextTask = GameTask.AnnounceFinalWinner;
                    }
                    else // more than 1 players wants to keep playing
                    {
                        // Reset the variable and the deck for next game
                        CurrentPlayer = 0;
                        RearrangePlayers();
                        Deck = new Deck();
                        Deck.Shuffle();

                        NextTask = GameTask.AskBet; // Return to Ask bet page
                    }
                }
                else
                {
                    // Card table announce the player who wins the most cash as the final winner
                    // (case 1) Someone wins the amount of time set at the beginning
                    GetEndGameReason(1, endGamePlayer);
                    NextTask = GameTask.AnnounceFinalWinner;
                }
            }
        }

        public string GetEndGameReason(int endReasonCode, Player endGamePlayer)
        {
            switch (endReasonCode)
            {
                case 1: // Someone wins the amount of time set at the beginning
                    endGameReason = $"{endGamePlayer.Name} ends the game by winning {endGamePlayer.wins} times";
                    break;
                case 2: // Only 1 player who doesn't leave the game still has cash
                    endGameReason = $"Game's over because {endGamePlayer.Name} is the only player who hasn't quit and still has cash";
                    break;
                default: // No enough players
                    endGameReason = $"Game's over because there is no more than 1 player wants to keep playing";
                    break;
            }
            return endGameReason;
        }

        /// <summary>
        /// Creates a player to add to the current game. Called by DoNextTask() method after getting the player names.
        /// </summary>
        /// <param name="n">Names for creating a Player object</param>
        public void AddPlayer(string n)
        {
            Players.Add(new Player(n));
        }

        /// <summary>
        /// Calculates the scores according to the cards in the player's hand.
        /// </summary>
        /// <param name="player">The player whose cards are ready to be calculated</param>
        /// <returns>Returns the total score in the player's hand</returns>
        public int ScoreHand(Player player)
        {
            int score = 0;
            // loop through all the cards and sum up their values
            foreach (Card card in player.cards)
            {
                string faceValue = card.Id.Remove(card.Id.Length - 1); // Gets the card value from the card's short name
                switch (faceValue)
                {
                    // K, Q, J, add 10 to score
                    case "K":
                    case "Q":
                    case "J":
                        score = score + 10;
                        break;
                    // A, add 1 to score
                    case "A":
                        score = score + 1;
                        break;
                    // If the card value is number, convert it to integer and add it to score
                    default:
                        score = score + int.Parse(faceValue);
                        break;
                }
            }
            return score;
        }

        /// <summary>
        /// Checks if there's a winner at the end of each turn.
        /// </summary>
        /// <returns>Return treu if there's a winner, otherwise, returns false</returns>
        public bool CheckWinner()
        {
            int bustedPlayers = 0; // Count the number of busted players for every round
            int activePlayers = 0; // Count the number of active players for every round

            // Loops throuh all the players who haven't quit
            foreach (Player player in InGamePlayers)
            {
                if (player.status == PlayerStatus.win) // If theres a player's status is win, there must be a winner
                {
                    return true;
                }
                else if (player.status == PlayerStatus.active) // If the player's status is active
                {
                    activePlayers++; // Add a count and keep looping
                }
                else if (player.status == PlayerStatus.bust) // If the player's status is bust
                {
                    bustedPlayers++; // Add a count and keep looping
                }
            }

            // If there's only 1 player isn't busted, there must be a winner
            if (bustedPlayers == (InGamePlayers.Count - 1))
            {
                return true;
            }

            // If there's at least 1 player still active, there's no winner
            if (activePlayers > 0)
            {
                return false;
            }

            // At this point, every player should be stayed, compare the score and announce the winner
            return true; 
        }

        /// <summary>
        /// Compare the scores and find out whose the winner
        /// </summary>
        /// <param name="currentPlayers">The players who haven't quit</param>
        /// <returns>Returns the winner</returns>
        public Player DoFinalScoring(List<Player> currentPlayers)
        {
            int highScore = 0; // Keep track of the highest score
            int bustedPlayers = 0; // Count the number of busted players for every round
            foreach (var player in currentPlayers)
            {
                // cardTable.ShowHand(player);
                if (player.status == PlayerStatus.win) // return the player whose status is win as the winner immediately
                {
                    return player;
                }
                if (player.status == PlayerStatus.stay) // if the player's status is stay
                {
                    // Replace the highest score if the player's score is higher
                    if (player.score > highScore)
                    {
                        highScore = player.score;
                    }
                }
                if (player.status == PlayerStatus.bust) // keep track of the number of busted players
                {
                    bustedPlayers++; 
                }
            }
            if (highScore > 0) // someone scored, anyway!
            {
                // find the FIRST player in list who meets win condition as the winner
                return currentPlayers.Find(player => player.score == highScore);
            }

            if (bustedPlayers == (InGamePlayers.Count - 1)) // If only 1 player isn't busted
            {
                // the only player left should have a score that is less than 21 and they should be the winner
                return currentPlayers.Find(player => player.score <= 21);
            }

            // Shouldn't get to this point since the only player who is not busted will be the winner
            return null; // everyone must have busted because nobody won!
        }

        /// <summary>
        /// Pays the player from the pot.
        /// </summary>
        /// <param name="player">The player who is getting the cash</param>
        /// <param name="cashAmount">The cash ready to be paid</param>
        public void Pay(Player player, int cashAmount)
        {
            player.cash += cashAmount;
        }

        /// <summary>
        /// Resets the cards, status, and scores of the players who haven't quit
        /// </summary>
        public void ResetPlayers()
        {
            foreach (Player player in InGamePlayers)
            {
                player.cards = new List<Card>();
                player.status = PlayerStatus.active;
                player.score = 0;
            }
        }

        /// <summary>
        /// Rearranges the order of the players
        /// </summary>
        public void RearrangePlayers()
        {
            // CardTable.WriteCardTableMessage("Rearranging players order...");
            Random rng = new Random();
            for (int i = 0; i < Players.Count; i++)
            {
                Player tmp = Players[i];
                int swapindex = rng.Next(Players.Count);
                Players[i] = Players[swapindex];
                Players[swapindex] = tmp;
            }
        }
    }
}
