using Microsoft.Azure.CosmosRepository;

namespace Cucu.Engine
{
    public class Game
    {
        private readonly IRepository<GameState> _gameStateRepository;
        public GameEngine GameEngine { get; private set; }

        public Game(IRepository<GameState> gameStateRepository)
        {
            _gameStateRepository = gameStateRepository;
            GameEngine = new GameEngine();
        }

        public async Task LoadGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState?.GameEngineState != null)
            {
                GameEngine.InitializeGameState(gameState.GameEngineState);
            }
        }

        public async Task SaveGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            bool exists = true;
            if (gameState == null)
            {
                gameState = new GameState() { Id = chatId.ToString() };
                exists = false;
            }
            var gameEngineState = GameEngine.GetGameEngineState();
            gameState.GameEngineState = gameEngineState;
            if(exists)
                await _gameStateRepository.UpdateAsync(gameState);
            else
                await _gameStateRepository.CreateAsync(gameState);
        }
    }
}