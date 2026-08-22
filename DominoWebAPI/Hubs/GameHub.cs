namespace DominoWebAPI.Hubs;

using Microsoft.AspNetCore.SignalR;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;

public class GameHub : Hub
{
    private readonly GameSessionManager _sessionManager;

    public GameHub(GameSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task<object> CreateLobby(string hostName)
    {
        var session = _sessionManager.CreateLobby(hostName, out int hostPlayerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId);

        return new { PlayerId = hostPlayerId, Lobby = DtoMapper.ToLobbyDto(session) };
    }

    public async Task JoinLobby(string gameId, string playerName)
    {
        var (session, newPlayerId, errorMessage) = _sessionManager.JoinLobby(gameId, playerName);

        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", errorMessage);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId.ToUpper());
        await Clients.Caller.SendAsync("JoinedSuccess", newPlayerId);
        await Clients.Group(session.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(session));
    }

    public async Task UpdateSettings(UpdateSettingsRequest settings)
    {
        bool success = _sessionManager.UpdateSettings(settings);
        if (success)
        {
            var session = _sessionManager.GetLobby(settings.GameId);
            if (session != null)
            {
                await Clients.Group(settings.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(session));
            }
        }
    }

    public async Task StartGame(string gameId, int playerId)
    {
        var game = _sessionManager.StartGame(gameId, playerId);
        if (game != null)
        {
            await Clients.Group(gameId.ToUpper()).SendAsync("GameStarted");
        }
    }

    public async Task PlayTile(PlayTileRequest request)
    {
        var game = _sessionManager.GetGame(request.GameId);
        if (game == null) return;

        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null) return;

        if (game.PlayTile(player, request.Tile, request.Side))
        {
            // Send personalized state update to each player
            foreach (var p in game.Players)
            {
                var dto = DtoMapper.ToGameDto(request.GameId, game, p.PlayerId);
                await Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated", dto);
            }
        }
    }
}