namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class JoinLobbyRequest
{
    public string GameId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
}