using System;

namespace Cucu.Engine
{
    public class GameEngine
    {
        private List<Player> players = new List<Player>(); 

        public GameEngine()
        {
            CurrentPhase = GamePhase.Setup;
            random = new Random();
        }

        public GamePhase CurrentPhase { get; private set; }
        public int? DeckCardValue { get; set; }

        private readonly Random random;

        public IEnumerable<Player> Players { get {return players; }}

        public int CurrentPlayerIndex { get; set; }

        public void AddPlayer(string username)
        {
            if(CurrentPhase != GamePhase.Setup)
                throw new InvalidOperationException("Cannot add a player when not in Startup phase");
            
            if(players.Any(x => x.Username == username))
                throw new InvalidOperationException("Cannot add the same player twice");

            players.Add(new Player(username));
        }

        public void StartGame() {
            if(CurrentPhase != GamePhase.Setup)
                throw new InvalidOperationException("Cannot start a game  when not in Startup phase");
            if(Players.Count() < 2)
                throw new InvalidOperationException("Cannot start a game with less than two players");
            
            //distribute cards 
            foreach(var player in players) 
            {
                player.CardValue = random.Next(1, 11);
            }
            CurrentPhase = GamePhase.InProgress;

            DeckCardValue = random.Next(1, 9);
        }


        public Player? GetCurrentPlayer() {
            //returns null if not started or completed
            return Players.Skip(CurrentPlayerIndex).FirstOrDefault();
        }

        public Player? GetNextPlayer() {
            //returns null if not started or completed
            return Players.Skip(CurrentPlayerIndex+1).FirstOrDefault();
        }

        public int? GetNextCardValue() 
        {
            var nextPlayer = Players.Skip(CurrentPlayerIndex + 1).FirstOrDefault();
            var nextCardValue = nextPlayer != null ? nextPlayer.CardValue : DeckCardValue;
            return nextCardValue;
        }

        public ActionResult SubmitAction(string playerUsername, PlayerAction action) 
        {
            if(CurrentPhase != GamePhase.InProgress)
                throw new InvalidOperationException("Cannot submit an action when not in InProgress phase");
            var currentPlayer = GetCurrentPlayer();
            if(currentPlayer == null || currentPlayer?.Username != playerUsername) 
                throw new InvalidOperationException("Only CurrentPlayer can submit an action");

            var nextPlayer = GetNextPlayer();
            var nextCardValue = GetNextCardValue();
            ActionResultType? resultType = null;

            if(action == PlayerAction.Keep)
            {
                resultType = ActionResultType.Kept;
            }
            else
            {
                if (nextCardValue == 10)
                {
                    resultType = ActionResultType.Blocked;
                    MoveToNextPlayer();
                    nextPlayer = GetNextPlayer();
                }
                else if (nextCardValue == 9)
                {
                    resultType = ActionResultType.Skipped;
                    while (nextCardValue == 9)
                    {
                        MoveToNextPlayer();
                        nextPlayer = GetNextPlayer();
                        nextCardValue = GetNextCardValue();
                    }
                    if (nextPlayer != null)
                        nextPlayer.CardValue = currentPlayer.CardValue;
                    currentPlayer.CardValue = nextCardValue;
                }
                else
                {
                    if (nextPlayer != null)
                        nextPlayer.CardValue = currentPlayer.CardValue;
                    currentPlayer.CardValue = nextCardValue;
                    resultType = ActionResultType.Swapped;
                }
            }

            MoveToNextPlayer();

            if(nextPlayer == null) 
            {
                CurrentPhase = GamePhase.End;
                return new ActionResult(ActionResultType.Showdown, GetLosers());
            }
            else 
            {
                return new ActionResult(resultType.Value, currentPlayer, nextPlayer);
            }
        }

        public void InitializeGameState(GameEngineState gameState)
        {
            if(!ValidateGameState(gameState))
                throw new InvalidOperationException();
            CurrentPlayerIndex = gameState.CurrentPlayerIndex;
            DeckCardValue = gameState.DeckCardValue;
            CurrentPhase = gameState.CurrentPhase;
            players.Clear();
            if (gameState.Players != null)
            {
                foreach (Player player in gameState.Players)
                {
                    players.Add(new Player(player.Username) { CardValue = player.CardValue });
                }
            }
        }

        public bool ValidateGameState(GameEngineState gameState)
        {
            //Rewrite condition by checking properties by properties
            if(gameState.CurrentPhase == GamePhase.Setup)
            {
                return gameState.CurrentPlayerIndex == 0 &&
                       gameState.DeckCardValue == null &&
                       gameState.Players != null &&
                       (gameState.Players.Count() == 0 ||
                        gameState.Players.All(p => p.CardValue == null));
            }
            else if(gameState.CurrentPhase == GamePhase.InProgress)
            {
                return gameState.CurrentPlayerIndex >= 0 &&
                       gameState.DeckCardValue != null && 
                       gameState.DeckCardValue >= 1 && gameState.DeckCardValue < 9 &&
                       gameState.Players != null &&
                       gameState.CurrentPlayerIndex < gameState.Players.Count() - 1 &&
                       (gameState.Players.Count() > 1 ||
                        gameState.Players.All(p => p.CardValue != null && p.CardValue >= 1 && p.CardValue <= 10));
            }
            else if(gameState.CurrentPhase == GamePhase.End)
            {
                return gameState.DeckCardValue != null &&
                       gameState.Players != null &&
                       (gameState.Players.Count() > 1 ||
                        gameState.Players.All(p => p.CardValue != null && p.CardValue >= 1 && p.CardValue <= 10)) &&
                       gameState.CurrentPlayerIndex != gameState.Players.Count() - 1;
            }
            else 
            {
                throw  new NotSupportedException($"GameState phase {gameState.CurrentPhase} value is not supported for GamePhase");
            }
        }

        public GameEngineState GetGameEngineState()
        {
            return new GameEngineState()
            {
                CurrentPhase = this.CurrentPhase,
                CurrentPlayerIndex = this.CurrentPlayerIndex,
                DeckCardValue = this.DeckCardValue,
                Players = this.Players.Select(x => new Player(x.Username) { CardValue = x.CardValue }).ToList()
            };
        } 
        
        public IEnumerable<Player> GetLosers()
        {
            List<Player> losers = new List<Player>();
            int minValue = int.MaxValue;
            foreach (var item in players)
            {
                if(item.CardValue < minValue)
                {
                    minValue = item.CardValue.Value;
                    losers.Clear();
                    losers.Add(item);
                }
                else if(item.CardValue == minValue)
                {
                    losers.Add(item);
                }
            }

            if(losers.Count == players.Count)
            {
                losers.Clear();
            }
            return losers;
        }

        private void MoveToNextPlayer() 
        {
            CurrentPlayerIndex++;
        }
    }

    public class ActionResult
    {
        public ActionResult(ActionResultType type, Player actionPlayer, Player? nextPlayer)
        {
            Type = type;
            ActionPlayer = actionPlayer;
            NextPlayer = nextPlayer;
        }

        public ActionResult(ActionResultType type, IEnumerable<Player> losers)
        {
            Type = type;
            Losers = losers;
        }

        public ActionResultType Type { get; private set; }

        public Player? ActionPlayer { get; private set; }
        public Player? NextPlayer { get; private set; }

        public IEnumerable<Player>? Losers {get; private set;} 
    }

    public enum ActionResultType
    {
        Kept,
        Swapped,
        Blocked,
        Skipped,
        Showdown
    }

    public enum PlayerAction
    {
        Keep,
        Swap
    }

    public enum GamePhase
    {
        Setup,
        InProgress,
        End
    }

    public class Player 
    {
        public Player(string username)
        {
            Username = username;
        }
        public string Username { get; set; }
        public int? CardValue { get; set; }
    }
}