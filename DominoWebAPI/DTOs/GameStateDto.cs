namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class GameStateDto
{
    public string GameId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Playing", "RoundOver", "GameOver"
    public int CurrentPlayerId { get; set; }
    public int RoundNumber { get; set; }

    // Board & Deck
    public List<DominoTile> PlayedBoard { get; set; } = new();
    public int RemainingDeckCount { get; set; }

    // Player-specific view (Security filtered)
    public List<DominoTile> YourHand { get; set; } = new();
    public Dictionary<int, int> OtherPlayerHandCounts { get; set; } = new();
    public Dictionary<int, int> Scores { get; set; } = new();

    // Round / Game End details
    public int? RoundWinnerId { get; set; }
    public int? GameWinnerId { get; set; }
}