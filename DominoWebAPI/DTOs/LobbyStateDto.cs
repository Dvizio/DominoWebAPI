namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;

public class LobbyStateDto
{
    public string GameId { get; set; } = string.Empty;
    public int HostPlayerId { get; set; }
    public List<LobbyPlayerDto> Players { get; set; } = new();
    public bool IsGameStarted { get; set; }

    // Configured Settings
    public GameMode Mode { get; set; }
    public int DeckSize { get; set; }
    public int TargetScore { get; set; }
    public int HandSize { get; set; }
    public StartingPlayerRule StartingRule { get; set; }
}