namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class PlayerActionRequest
{
    public string GameId { get; set; } = string.Empty;
    public int PlayerId { get; set; }
}