namespace DominoWebAPI.Models;

public class Player : IPlayer
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;

    public Player(int playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
    }
}