using Microsoft.Azure.CosmosRepository;

namespace Cucu.Engine
{
    public class GameState : Item
    {
        public GameEngineState? GameEngineState { get; set; }
    }
}