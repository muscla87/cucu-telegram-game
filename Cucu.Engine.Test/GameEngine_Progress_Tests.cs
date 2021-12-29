using Xunit;
using Cucu.Engine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Cucu.Test;

public class GameEngine_Progress_Tests
{
    private GameEngine SetupGameState(params int[] cardValues)
    {
        int i;
        var gameEngine = new GameEngine();
        for (i = 0; i < cardValues.Length; i++)
        {
            gameEngine.AddPlayer($"Player{i+1}");
        }
        gameEngine.StartGame();

        i = 0;
        foreach (var player in gameEngine.Players)
        {
            player.CardValue = cardValues[i];
            i++;
        }

        return gameEngine;
    }

    [Fact]
    public void SubmitActionWhenNotInProgress_ThrowsException()
    {
        var gameEngine = new GameEngine();
        Assert.Throws<InvalidOperationException>(() =>
        {
            var result = gameEngine.SubmitAction("player1", PlayerAction.Keep);
        });
    }

    [Fact]
    public void WrongPlayer_ThrowsException()
    {
        var gameEngine = SetupGameState(1, 1);
        Assert.Throws<InvalidOperationException>(() =>
        {
            var result = gameEngine.SubmitAction("player2", PlayerAction.Keep);
        });
    }

    [Fact]
    public void Keep_CardValuesUnchanged()
    {
        int p1Value = 1, p2Value = 2;
        var gameEngine = SetupGameState(p1Value, p2Value);
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var result = gameEngine.SubmitAction(firstPlayer.Username, PlayerAction.Keep);
        Assert.Equal(ActionResultType.Kept, result.Type);
        Assert.Equal(firstPlayer, result.ActionPlayer);
        Assert.Equal(secondPlayer, result.NextPlayer);
        Assert.Equal(p1Value, firstPlayer.CardValue);
        Assert.Equal(p2Value, secondPlayer.CardValue);
        Assert.Null(result.Losers);
    }

    [Theory]
    [MemberData(nameof(Swap_Successful_Data))]
    public void Swap_Successful(int p1Value, int p2Value)
    {
        var gameEngine = SetupGameState(p1Value, p2Value);
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var result = gameEngine.SubmitAction(firstPlayer.Username, PlayerAction.Swap);
        Assert.Equal(ActionResultType.Swapped, result.Type);
        Assert.Equal(firstPlayer, result.ActionPlayer);
        Assert.Equal(secondPlayer, result.NextPlayer);
        Assert.Equal(p2Value, firstPlayer.CardValue);
        Assert.Equal(p1Value, secondPlayer.CardValue);
        Assert.Null(result.Losers);
    }

    [Theory]
    [MemberData(nameof(Swap_Blocked_Data))]
    public void Swap_Blocked(params int[] pValues)
    {
        var gameEngine = SetupGameState(pValues);
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var result = gameEngine.SubmitAction(firstPlayer.Username, PlayerAction.Swap);
        Assert.Equal(pValues[0], firstPlayer.CardValue);
        Assert.Equal(pValues[1], secondPlayer.CardValue);
        Assert.Equal(10, secondPlayer.CardValue);
    }

    [Theory]
    [MemberData(nameof(Swap_Skipped_Data))]
    public void Swap_Skipped(int p1Value, int p2Value, int p3Value, bool isNextNull)
    {
        var gameEngine = SetupGameState(p1Value, p2Value, p3Value);
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var thirdPlayer = gameEngine.Players.Skip(2).First();
        var result = gameEngine.SubmitAction(firstPlayer.Username, PlayerAction.Swap);
        var expectedFirstValue = isNextNull ? gameEngine.DeckCardValue : p3Value;
        var expectedThirdValue = isNextNull ? p3Value : p1Value;
        var expectedResultType = isNextNull ? ActionResultType.Showdown : ActionResultType.Skipped;
        Assert.Equal(expectedFirstValue, firstPlayer.CardValue);
        Assert.Equal(p2Value, secondPlayer.CardValue);
        Assert.Equal(expectedThirdValue, thirdPlayer.CardValue);
        Assert.Equal(9, secondPlayer.CardValue);
    }

    [Theory]
    [MemberData(nameof(LosersCheck_Data))]
    public void LosersCheck(int p1Value, int p2Value, int p3Value, int[] expectedLosersIndex)
    {
        var gameEngine = SetupGameState(p1Value, p2Value, p3Value);
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var thirdPlayer = gameEngine.Players.Skip(2).First();

        var losers = gameEngine.GetLosers();
        var playersList = gameEngine.Players.ToList();

        Assert.Equal(expectedLosersIndex, losers.Select(x => playersList.IndexOf(x)));
    }

    [Theory]
    [MemberData(nameof(Get_ActionSequence_ShowdownResult_Data))]
    public void ActionSequence_ShowdownResult(int p1Value, int p2Value, int p3Value, int deckValue, PlayerAction[] actions)
    {
        var gameEngine = SetupGameState(p1Value, p2Value, p3Value);
        gameEngine.DeckCardValue = deckValue;
        var firstPlayer = gameEngine.Players.First();
        var secondPlayer = gameEngine.Players.Skip(1).First();
        var thirdPlayer = gameEngine.Players.Skip(2).First();

        var losers = gameEngine.GetLosers();
        var playersList = gameEngine.Players.ToList();

        ActionResult result = gameEngine.SubmitAction(firstPlayer.Username, actions.First());
        foreach (var action in actions.Skip(1))
        {
            if(result.NextPlayer != null)
                result = gameEngine.SubmitAction(result.NextPlayer.Username, action);
            else
                Assert.NotNull(result.NextPlayer);
        }

        Assert.Equal(GamePhase.End, gameEngine.CurrentPhase);
        Assert.Equal(ActionResultType.Showdown, result.Type);
        Assert.NotNull(result.Losers);
        Assert.Null(result.NextPlayer);
        Assert.Null(result.ActionPlayer);

    }

    public static IEnumerable<object[]> Swap_Successful_Data()
    {
        for (int i = 1; i < 11; i++)
        {
            for (int j = 1; j < 9; j++)
            {
                yield return new object[] { i, j };
            }
        }
    }

    public static IEnumerable<object[]> Swap_Blocked_Data()
    {
        for (int i = 1; i < 11; i++)
        {
            yield return new object[] { i, 10, };
        }
        for (int i = 1; i < 11; i++)
        {
            yield return new object[] { i, 10, 4 };
        }
    }

    public static IEnumerable<object[]> Swap_Skipped_Data()
    {
        for (int i = 1; i < 11; i++)
        { 
            yield return new object[] { i, 9, 5, false };
            yield return new object[] { i, 9, 9, true };
        }
    }

    public static IEnumerable<object[]> LosersCheck_Data()
    {
        yield return new object[] { 1, 2, 3, new [] { 0 } };
        yield return new object[] { 2, 1, 3, new [] { 1 } };
        yield return new object[] { 2, 1, 1, new [] { 1, 2 } };
        yield return new object[] { 1, 1, 1, new int[] {  } };
    } 

    public static IEnumerable<object[]> Get_ActionSequence_ShowdownResult_Data()
    {
        yield return new object[] { 1, 2, 3, 3, new [] { PlayerAction.Keep, PlayerAction.Keep, PlayerAction.Keep } };
        yield return new object[] { 2, 9, 3, 3, new [] { PlayerAction.Keep, PlayerAction.Keep, PlayerAction.Keep } };
        yield return new object[] { 2, 9, 3, 3, new [] { PlayerAction.Swap, PlayerAction.Keep } };
        yield return new object[] { 2, 10, 3, 3, new [] { PlayerAction.Swap, PlayerAction.Swap } };
        yield return new object[] { 2, 10, 10, 3, new [] { PlayerAction.Swap, PlayerAction.Keep } };
        yield return new object[] { 2, 10, 10, 3, new [] { PlayerAction.Swap, PlayerAction.Swap } };
        yield return new object[] { 2, 9, 9, 3, new [] { PlayerAction.Swap } };
        yield return new object[] { 2, 9, 9, 3, new [] { PlayerAction.Keep, PlayerAction.Swap } };
        yield return new object[] { 2, 9, 9, 3, new [] { PlayerAction.Keep, PlayerAction.Keep, PlayerAction.Keep } };
    } 
    

}