namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class LobbyPlayerDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
}