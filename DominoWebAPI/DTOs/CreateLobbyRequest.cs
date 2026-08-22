namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class CreateLobbyRequest
{
    public string HostPlayerName { get; set; } = string.Empty;
}