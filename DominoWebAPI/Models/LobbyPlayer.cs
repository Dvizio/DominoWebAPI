namespace DominoWebAPI.Models;

public class LobbyPlayer
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
}