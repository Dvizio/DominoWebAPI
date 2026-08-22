namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class UpdateSettingsRequest
{
    public string GameId { get; set; } = string.Empty;
    public GameMode Mode { get; set; } = GameMode.Draw;
    public int DeckSize { get; set; } = 6; // Double-6, Double-9, Double-12
    public int TargetScore { get; set; } = 100;
    public int HandSize { get; set; } = 7;
    public StartingPlayerRule StartingRule { get; set; } = StartingPlayerRule.HighestDouble;
}