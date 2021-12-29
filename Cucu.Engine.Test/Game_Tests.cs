using Xunit;
using Cucu.Engine;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moq;
using Microsoft.Azure.CosmosRepository;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;
using System;
using System.Linq;

namespace Cucu.Test;

public class Game_Tests
{
    public Mock<IRepository<GameState>> gameStateRepoMock = new Mock<IRepository<GameState>>();
    private readonly Game game;

    public Game_Tests()
    {
        game = new Game(gameStateRepoMock.Object);
    }

    [Fact]
    public async Task LoadGameState_Calls_GetAsync()
    {
        long chatId = 0;
        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task LoadGameState_NoRecordInRepo_GetAsync()
    {
        long chatId = 0;
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(Enumerable.Empty<GameState>()));

        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task LoadGameState_RecordInRepoNoGameEngineState_GetAsync()
    {
        long chatId = 0;

        GameState gameState = new GameState();
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));

        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));

        Assert.Equal(GamePhase.Setup, game.GameEngine.CurrentPhase);
    }

    [Fact]
    public async Task LoadGameState_RecordInRepoWithGameEngineState_GetAsync()
    {
        long chatId = 0;

        GameState gameState = new GameState()
        {
            GameEngineState = new GameEngineState()
                {
                    CurrentPhase = GamePhase.InProgress,
                    CurrentPlayerIndex = 1,
                    DeckCardValue = 5,
                    Players = new[] { new Player("p1") { CardValue = 2 }, new Player("p2") { CardValue = 4}, new Player("p3") { CardValue = 4} }
                }
        };
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));

        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));

        Assert.Equal(gameState.GameEngineState.CurrentPhase, game.GameEngine.CurrentPhase);
        Assert.Equal(gameState.GameEngineState.DeckCardValue, game.GameEngine.DeckCardValue);
        Assert.Equal(gameState.GameEngineState.CurrentPlayerIndex, game.GameEngine.CurrentPlayerIndex);
        Assert.Equal(gameState.GameEngineState.CurrentPhase, game.GameEngine.CurrentPhase);
    }

    [Fact]
    public async Task SaveGameState_ShouldTryToLoadExistingRecordAndCheckExistance()
    {
        long chatId = 0;
        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task SaveGameState_ShouldCallUpdateIfStateExists()
    {
        long chatId = 0;
        GameState gameState = new GameState() { Id = chatId.ToString() };
        gameStateRepoMock.Setup(x => x.ExistsAsync(chatId.ToString(), null, default(CancellationToken)))
                            .Returns(ValueTask.FromResult<bool>(true));
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));

        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
        gameStateRepoMock.Verify(x => x.UpdateAsync(gameState, default(CancellationToken)));
    }

    [Fact]
    public async Task SaveGameState_ShouldCallCreateIfStateDoesNotExist()
    {
        long chatId = 0;
        gameStateRepoMock.Setup(x => x.ExistsAsync(chatId.ToString(), null, default(CancellationToken)))
                            .Returns(ValueTask.FromResult<bool>(true));
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(Enumerable.Empty<GameState>()));

        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
        gameStateRepoMock.Verify(x => x.CreateAsync(It.IsAny<GameState>(), default(CancellationToken)));
    }

}