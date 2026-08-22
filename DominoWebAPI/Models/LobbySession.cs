namespace DominoWebAPI.Models;

public class LobbySession
{
    public string GameId { get; set; } = string.Empty;
    public int HostPlayerId { get; set; }
    public List<LobbyPlayer> Players { get; set; } = new();

    public GameMode Mode { get; set; } = GameMode.Draw;
    public int DeckSize { get; set; } = 6; // Double-6(default),9,12
    public int TargetScore { get; set; } = 100;
    public int HandSize { get; set; } = 7;
    public StartingPlayerRule StartingRule { get; set; } = StartingPlayerRule.HighestDouble;

    // Active Game Logic Instance
    public GameLogic? ActiveGame { get; set; }
}