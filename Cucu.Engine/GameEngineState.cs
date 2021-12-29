using Microsoft.Azure.CosmosRepository;

namespace Cucu.Engine
{
    public class GameEngineState
    {
        public GamePhase CurrentPhase { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int? DeckCardValue { get; set; }
        public IEnumerable<Player>? Players { get; set; } = new List<Player>();
    }
}