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
        // The player list should be read-only outside of Game object so other classes can only update the players by using public methods in this Game class.
        public List<Player> Players { get; private set; } = new List<Player>(); // list of objects containing player data 

        // Represents the players that are still in game (status is not quit)
        public List<Player> InGamePlayers 
        { 
            get
            {
                return Players.FindAll(player => player.Status != PlayerStatus.quit);
            } 
        }

        // Represents the all the players that are ranked (in the order of cash -> wins -> name)
        public List<Player> rankedPlayers
        {
            /*
             * I uses System.Linq for sorting the players
             * https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderby?view=net-7.0 - OrderByDescending()
             * https://learn.microsoft.com/en-us/dotnet/api/system.linq.queryable.thenby?view=net-7.0 - ThenBy()
             * Uses List.OrderByDescending(player => player.cash) to order the players first according to their cash from most to least, 
             * if the cash is same, uses ThenByDescending(player => player.wins) to keep ordering the players according to their times of win from most to least,
             * if both above are the same, uses ThenBy(player => player.name) to keep ordering the players according to their names alphebatically.
             */
            get
            {
                return Players.OrderByDescending(player => player.Cash).ThenByDescending(player => player.Wins).ThenBy(player => player.Name).ToList();
            }
        }

        // I use private set for these properties so other classes can only update the players by using public methods in this Game class.
        public Deck Deck { get; private set; } = new Deck(); // deck of cards
        public int NumberOfPlayers { get; private set; } // number of players in current game
        public int CurrentPlayer { get; private set; } = 0; // current player on list
        public int CurrentPot { get; private set; } = 0; // amount of bet put in current pot
        public static readonly int defaultBet = 10; // I set a default bet to put in the web page input at the beginning
        public GameTask NextTask { get; private set; } // keeps track of game state - I update the tasks to better fit game process of the Blazor version
        public string EndGameReason { get; private set; } // the reason of why the game ends - is used on the Game Over page

        // I make this public so the webpage can bind the value with the select element
        public int TimesToWin { get; set; } = 3; // number of rounds to win the game
        

        /// <summary>
        /// Shuffle the deck while creaing the game. Initiate the task to starts the game process.
        /// The default number of players is 2 as the web page is showing at the beginning.
        /// </summary>
        public Game()
        {
            Deck.Shuffle();

            NumberOfPlayers = 2;
            Players = new List<Player>() { new Player(""), new Player("") };

            NextTask = GameTask.GetGameInfo;
        }

        /* SetPlayers(), CheckRepeatName(), ValidatePlayers(), and GoAskBet() are methods called before the game starts
         * so they use the Players list that represents all the players that join the game
         */

        /// <summary>
        /// Updates the Players list with unnamed players according to the number of players selected on the web page.
        /// </summary>
        /// <param name="count">Number of players selected on the web page</param>
        public void SetPlayers(int count)
        {
            // If the user select less players
            if (count < NumberOfPlayers)
            {
                // Keep the first number of players
                Players = Players.Take(count).ToList();
                NumberOfPlayers = count;
                return;
            }
            
            // The user select more players, add more unnamed players to the list to name them later
            for (int i = NumberOfPlayers; i < count; i++)
            {
                AddPlayer("");
            }
            NumberOfPlayers = count;
        }

        /// <summary>
        /// Check if there are more than 1 players who have the same names in the Player List.
        /// </summary>
        /// <param name="checkPlayer">The player to be checked</param>
        /// <returns>Return true if there are more than 1 players who have the same names in the Player List. Otherwise, return false.</returns>
        public bool CheckRepeatName(Player checkPlayer)
        {
            string name = checkPlayer.Name;

            if (name == "") // If the player is unnamed, don't check
            {
                return false;
            }

            if (Players.FindAll(p => p.Name == name).Count > 1) // If there's a player in the player list has the same name as the current input
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if all players is Players List is named and unique.
        /// </summary>
        /// <returns>Return true if all players is Players List is named and unique. Otherwise, return false.</returns>
        public bool ValidatePlayers()
        {
            // Go through all players to check
            foreach (Player player in Players)
            {
                // First check if the player's name is an empty string, then check if the player name is unique
                if (player.Name == "" || Players.FindAll(p => p.Name == player.Name).Count > 1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Set up the default bets for all the players and update the GameTask to go to the Ask Bet page.
        /// </summary>
        /// <returns>Returns an int array for storing each player's bet</returns>
        public int[] GoAskBet()
        {
            // I keep track of how much each player bets on the web page with an int array
            int[] bets = new int[NumberOfPlayers];
            NextTask = GameTask.AskBet;

            // Go through all the players
            for (int i = 0; i < NumberOfPlayers; i++)
            {
                // Collect default bet when the game starts 
                CollectBet(defaultBet, i);
                bets[i] = defaultBet;
            }
            return bets;
        }

        /* The methods below are called after the game starts
         * so they use the InGamePlayers list that represents all the players who haven't quit
         */

        /// <summary>
        /// Collect bet from given player and add twice of it to the current pot.
        /// </summary>
        /// <param name="betChange">Bet to be collected</param>
        /// <param name="playerIndex">The index of the player to be collected from</param>
        public void CollectBet(int betChange, int playerIndex)
        {
            CurrentPot += 2 * betChange;
            InGamePlayers[playerIndex].Bet(betChange);
        }

        /// <summary>
        /// Update the game state to go to the Game Playing page, offer each player a card at the beginning.
        /// </summary>
        public void StartPlay()
        {
            NextTask = GameTask.PlayerTurn;
            foreach (Player p in InGamePlayers) // Loop through the players who haven't quit 
            {
                Card card = Deck.DealTopCard();
                p.Cards.Add(card);
                p.Score = ScoreHand(p);
            }
            return;
        }

        /// <summary>
        /// Deal a card to the given player then change to next turn and check if there's a winner.
        /// </summary>
        /// <param name="player">The player who wants a card</param>
        /// <returns>Returns the player who wins the current round if there is one after deal this card</returns>
        public Player Deal(Player player)
        {
            Card card = Deck.DealTopCard();
            player.Cards.Add(card);
            player.Score = ScoreHand(player);

            // Switch the player's status according to their score
            if (player.Score > 21) // If the total score is over 21, the player is busted
            {
                player.Status = PlayerStatus.bust;
            }
            else if (player.Score == 21) // If the total score is over 21, the player wins
            {
                player.Status = PlayerStatus.win;
            }

            return NextTurn(); // NextTurn() changes turn and return a winner if there is one
        }

        /// <summary>
        /// Let the given player stay then change to next turn and check if there's a winner.
        /// </summary>
        /// <param name="player">The player who wants to stay</param>
        /// <returns>Returns the player who wins the current round if there is one after the given player stays</returns>
        public Player StayPlayer(Player player)
        {
            // switch the player's status to stay
            player.Status = PlayerStatus.stay;
            return NextTurn();
        }

        /// <summary>
        /// Change to next turn and check if there's a winner of the current round appears.
        /// </summary>
        /// <returns>Return the winner if there's one, null if there's no winner</returns>
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
                winner.Status = PlayerStatus.win;

                // The winner gets a win
                winner.Wins++;

                // Switch the game state to go to the Announce Round Winner page
                NextTask = GameTask.AnnounceRoundWinner;
                return winner;
            }

            return null; // Returns null if there is no winner yet
        }

        /// <summary>
        /// Pay the player who wins a round and 
        /// switch the game state to go to the Check New Round page (or the Game Over page if the game ends).
        /// </summary>
        /// <param name="roundWinner">The player who wins a round</param>
        public void CheckNewRound(Player roundWinner)
        {
            // Pay and Reset the pot
            Pay(roundWinner, CurrentPot);
            CurrentPot = 0; 
            
            NextTask = GameTask.CheckForNewRound;
            CheckEndGame(); // Check if the game meets the condition to end
        }

        /// <summary>
        /// Overload method that doesn't take parameters.
        /// Is called when a round winner appears.
        /// Check if there's a player who wins enough times or only 1 player in game still has money.
        /// </summary>
        public void CheckEndGame()
        {
            Player endGamePlayer = Players.Find(player => player.Wins == TimesToWin); // Gets the player who wins enough times to end the game
            if (endGamePlayer != null) // If there is a player wins enough times
            {
                // (case 1) Someone wins the amount of time set at the beginning
                GetEndGameReason(1, endGamePlayer); // Update the string representing the reason that ends the game
                NextTask = GameTask.AnnounceFinalWinner; // Switch the game state to go to the Game Over page
            }

            // If there is only 1 player who havn't left the game still have cash
            if (InGamePlayers.FindAll(player => player.Cash > 0).Count == 1)
            {
                // (case 2) Only 1 player who doesn't leave the game still has cash
                GetEndGameReason(2, InGamePlayers.Find(player => player.Cash > 0));
                NextTask = GameTask.AnnounceFinalWinner;
            }
        }

        /// <summary>
        /// Overload method that takes a string array as parameter.
        /// Is called when the web page is asking players for a new round.
        /// Check if there's more than 1 players want to keep playing.
        /// If the game can keep going, remove the quit players in the next round.
        /// </summary>
        /// <param name="keepPlaying">A string array given be the web page representing if each player wants to keep playing</param>
        public void CheckEndGame(string[] keepPlaying)
        {
            // I still check for all end game conditions in case this method is called unexpectedly

            // If there is only 1 player who havn't left the game still have cash
            if (InGamePlayers.FindAll(player => player.Cash > 0).Count == 1)
            {
                // (case 2) Only 1 player who doesn't leave the game still has cash
                GetEndGameReason(2, InGamePlayers.Find(player => player.Cash > 0));
                NextTask = GameTask.AnnounceFinalWinner;
            }
            else // There are still more than 1 players in current round and they all still have cash
            {
                Player endGamePlayer = Players.Find(player => player.Wins == TimesToWin); // Gets the player who wins enough times to end the game
                if (endGamePlayer == null) // If there isn't a player who wins enough times
                {
                    List<Player> readyToQuit = new List<Player>(); // A temporary list that stores the players who wants to quit
                    for (int i = 0; i < InGamePlayers.Count; i++) // Go through all the players that have not quit yet
                    {
                        if (keepPlaying[i] == "No" || InGamePlayers[i].Cash <= 0) // If the player chooses no on the web page of don't have money
                        {
                            readyToQuit.Add(InGamePlayers[i]); // Add the player to the temporary list
                        }
                    }
                    foreach (Player quitPlayer in readyToQuit) // Go through all the players who wants to quit and switch their states to quit
                    {
                        quitPlayer.Status = PlayerStatus.quit;
                    }

                    ResetPlayers(); // reset player's cards, card points, and status
                    if (InGamePlayers.Count <= 1) // if no more than 1 player wants to keep playing, end the game
                    {
                        // (case 3) // No enough players
                        GetEndGameReason(3, null);
                        NextTask = GameTask.AnnounceFinalWinner;
                    }
                    else // more than 1 players wants to keep playing
                    {
                        // Reset the variable and the deck for next game
                        CurrentPlayer = 0;
                        RearrangePlayers(); // Rearrange the players order in the list
                        Deck = new Deck();
                        Deck.Shuffle();

                        NextTask = GameTask.AskBet; // Return to Ask bet page
                    }
                }
                else
                {
                    // (case 1) Someone wins the amount of time set at the beginning
                    GetEndGameReason(1, endGamePlayer);
                    NextTask = GameTask.AnnounceFinalWinner;
                }
            }
        }

        /// <summary>
        /// Craete and update the reason that ends the game according to the given reason code and the player who ends the game.
        /// </summary>
        /// <param name="endReasonCode">Code represents different condition that ends the game</param>
        /// <param name="endGamePlayer">The player who ends the game if there is one</param>
        public void GetEndGameReason(int endReasonCode, Player endGamePlayer)
        {
            switch (endReasonCode)
            {
                case 1: // Someone wins the amount of time set at the beginning
                    EndGameReason = $"{endGamePlayer.Name} ends the game by winning {endGamePlayer.Wins} times";
                    break;
                case 2: // Only 1 player who doesn't leave the game still has cash
                    EndGameReason = $"Game's over because {endGamePlayer.Name} is the only player who hasn't quit and still has cash";
                    break;
                default: // No enough players (so no player ends the game)
                    EndGameReason = $"Game's over because there is no more than 1 player wants to keep playing";
                    break;
            }
        }

        /// <summary>
        /// Creates a player to add to the current game.
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
            foreach (Card card in player.Cards)
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
        /// <returns>Return true if there's a winner, otherwise, returns false</returns>
        public bool CheckWinner()
        {
            int bustedPlayers = 0; // Count the number of busted players for every round
            int activePlayers = 0; // Count the number of active players for every round

            // Loops throuh all the players who haven't quit
            foreach (Player player in InGamePlayers)
            {
                if (player.Status == PlayerStatus.win) // If theres a player's status is win, there must be a winner
                {
                    return true;
                }
                else if (player.Status == PlayerStatus.active) // If the player's status is active
                {
                    activePlayers++; // Add a count and keep looping
                }
                else if (player.Status == PlayerStatus.bust) // If the player's status is bust
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
        /// Compare the scores and find out whose the winner.
        /// </summary>
        /// <param name="currentPlayers">The players who haven't quit</param>
        /// <returns>Returns the winner</returns>
        public Player DoFinalScoring(List<Player> currentPlayers)
        {
            int highScore = 0; // Keep track of the highest score
            int bustedPlayers = 0; // Count the number of busted players for every round
            foreach (Player player in currentPlayers)
            {
                // cardTable.ShowHand(player);
                if (player.Status == PlayerStatus.win) // return the player whose status is win as the winner immediately
                {
                    return player;
                }
                if (player.Status == PlayerStatus.stay) // if the player's status is stay
                {
                    // Replace the highest score if the player's score is higher
                    if (player.Score > highScore)
                    {
                        highScore = player.Score;
                    }
                }
                if (player.Status == PlayerStatus.bust) // keep track of the number of busted players
                {
                    bustedPlayers++; 
                }
            }

            if (bustedPlayers == (InGamePlayers.Count - 1)) // If only 1 player isn't busted
            {
                // the only player left should have a score that is less than 21 and they should be the winner
                return currentPlayers.Find(player => player.Score <= 21);
            }

            if (highScore > 0) // If there's a high score
            {
                // find the FIRST player in list who gets the highest score
                return currentPlayers.Find(player => player.Score == highScore);
            }

            // Shouldn't get to this point since the only player who is not busted will be the winner
            return null; 
        }

        /// <summary>
        /// Pays the player from the pot.
        /// </summary>
        /// <param name="player">The player who is getting the cash</param>
        /// <param name="cashAmount">The cash ready to be paid</param>
        public void Pay(Player player, int cashAmount)
        {
            player.Cash += cashAmount;
        }

        /// <summary>
        /// Resets the cards, status, and scores of the players who haven't quit.
        /// </summary>
        public void ResetPlayers()
        {
            foreach (Player player in InGamePlayers)
            {
                player.Cards = new List<Card>();
                player.Status = PlayerStatus.active;
                player.Score = 0;
            }
        }

        /// <summary>
        /// Rearranges the order of the players.
        /// </summary>
        public void RearrangePlayers()
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player tmp = Players[i];
                int swapindex = new Random().Next(Players.Count);
                Players[i] = Players[swapindex];
                Players[swapindex] = tmp;
            }
        }
    }
}
