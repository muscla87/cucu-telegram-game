using Xunit;
using Cucu.Engine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cucu.Test;

public class GameEngine_Setup_Tests
{
    [Fact]
    public void NewGameStateStartsWithSetup()
    {
        var gameEngine = new GameEngine();
        Assert.Equal(GamePhase.Setup, gameEngine.CurrentPhase);
    }

    [Fact]
    public void StartWithNoPlayersAdded_ThrowsException()
    {
        var gameEngine = new GameEngine();
        Assert.Throws<InvalidOperationException>(() => {
            gameEngine.StartGame();
        });
    }

    [Fact]
    public void StartWithOnePlayer_ThrowsException()
    {
        var gameEngine = new GameEngine();
        gameEngine.AddPlayer("player1");
        Assert.Throws<InvalidOperationException>(() => {
            gameEngine.StartGame();
        });
    }

    [Fact]
    public void AddPlayerNotInSetup_ThrowsException()
    {
        var gameEngine = new GameEngine();
        gameEngine.AddPlayer("player1");
        gameEngine.AddPlayer("player2");
        gameEngine.StartGame();
        Assert.Throws<InvalidOperationException>(() => {
            gameEngine.AddPlayer("player3");
        });
    }

    [Fact]
    public void AddTwiceSamePlayer_ThrowsException()
    {
        var gameEngine = new GameEngine();
        gameEngine.AddPlayer("player1");
        Assert.Throws<InvalidOperationException>(() => {
            gameEngine.AddPlayer("player1");
        });
    }

    [Fact]
    public void AddPlayer()
    {
        string playerName = "player1";
        var gameEngine = new GameEngine();
        gameEngine.AddPlayer(playerName);
        var player1 = gameEngine.Players.First(x => x.Username == playerName);

        Assert.NotNull(player1);
        Assert.Equal(playerName, player1.Username);
        Assert.Null(player1.CardValue);
    }

    [Fact]
    public void StartGame_ParametersConfigured() 
    {
        string player1Name = "player1";
        string player2Name = "player2";
        var gameEngine = new GameEngine();
        gameEngine.AddPlayer(player1Name);
        gameEngine.AddPlayer(player2Name);
        gameEngine.StartGame();

        Assert.Equal(0, gameEngine.CurrentPlayerIndex);
        Assert.Equal(GamePhase.InProgress, gameEngine.CurrentPhase);
        Assert.True(gameEngine.Players.All(x => x.CardValue.HasValue));
        Assert.True(gameEngine.Players.All(x => x.CardValue >= 1 && x.CardValue <= 10));
        Assert.NotNull(gameEngine.DeckCardValue);
        Assert.True(gameEngine.DeckCardValue >= 1 && gameEngine.DeckCardValue <= 8);
    }

    [Fact]
    public void InitializeGameState_ValuesCopied() 
    {
        var gameEngine = new GameEngine();
        var gameState = new GameEngineState()
        {
            CurrentPhase = GamePhase.InProgress,
            CurrentPlayerIndex = 1,
            DeckCardValue = 5,
            Players = new[] { new Player("p1") { CardValue = 2 }, new Player("p2") { CardValue = 4}, new Player("p3") { CardValue = 4} }
        };
        gameEngine.InitializeGameState(gameState);

        Assert.Equal(gameState.DeckCardValue, gameEngine.DeckCardValue);
        Assert.Equal(gameState.CurrentPlayerIndex, gameEngine.CurrentPlayerIndex);
        Assert.Equal(gameState.CurrentPhase, gameEngine.CurrentPhase);
        Assert.Equal(gameState.Players, gameEngine.Players, new PlayerComparer());
    }

    [Fact]
    public void GetGameState_ValuesCopied() 
    {
        var gameEngine = new GameEngine();
        var gameState = new GameEngineState()
        {
            CurrentPhase = GamePhase.InProgress,
            CurrentPlayerIndex = 1,
            DeckCardValue = 5,
            Players = new[] { new Player("p1") { CardValue = 2 }, new Player("p2") { CardValue = 4}, new Player("p3") { CardValue = 4} }
        };
        gameEngine.InitializeGameState(gameState);

        var outputGameState = gameEngine.GetGameEngineState();

        Assert.Equal(gameState.DeckCardValue, outputGameState.DeckCardValue);
        Assert.Equal(gameState.CurrentPlayerIndex, outputGameState.CurrentPlayerIndex);
        Assert.Equal(gameState.CurrentPhase, outputGameState.CurrentPhase);
        Assert.Equal(gameState.Players, outputGameState.Players, new PlayerComparer());
    }

    [Theory]
    [MemberData(nameof(InitializeGameStateWithInvalidGameState_ThrowsInvalidOpearationException_Data))]
    public void InitializeGameStateWithInvalidGameState_ThrowsInvalidOpearationException(GameEngineState gameState) 
    {
        var gameEngine = new GameEngine();

        Assert.Throws<InvalidOperationException>(() => gameEngine.InitializeGameState(gameState));
    }

    public static IEnumerable<object[]> InitializeGameStateWithInvalidGameState_ThrowsInvalidOpearationException_Data()
    {

        yield return new object[] {  new GameEngineState()
        {
            CurrentPhase = GamePhase.Setup,
            CurrentPlayerIndex = 2,
            DeckCardValue = 5,
            Players = new[] { new Player("p1") { CardValue = 2 }, new Player("p2") { CardValue = 4} }
         } };
        yield return new object[] {  new GameEngineState()
        {
            CurrentPhase = GamePhase.InProgress,
            CurrentPlayerIndex = 10,
            DeckCardValue = 5,
            Players = new[] { new Player("p1") { CardValue = 2 }, new Player("p2") { CardValue = 4} }
         } };
         
        yield return new object[] { new GameEngineState() { CurrentPhase = GamePhase.InProgress } };
        yield return new object[] { new GameEngineState() { CurrentPhase = GamePhase.End } };
    } 

    private class PlayerComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player? p1, Player? p2)
        {
            if(p1 == null || p2 == null)
                return false;
            return p1.CardValue == p2.CardValue && p1.Username == p2.Username && p1 != p2;
        }

        public int GetHashCode([DisallowNull] Player obj)
        {
            return obj.GetHashCode();
        }
    }
}