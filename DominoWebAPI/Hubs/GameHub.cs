namespace DominoWebAPI.Hubs;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;

public class GameHub : Hub
{
    private readonly GameSessionManager _sessionManager;
    private static readonly ConcurrentDictionary<string, (string GameId, int PlayerId)> _connectionMap = new();

    public GameHub(GameSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionMap.TryRemove(Context.ConnectionId, out var info))
        {
            var lobby = _sessionManager.GetLobby(info.GameId);
            if (lobby != null)
            {
                lobby.MarkPlayerDisconnected(info.PlayerId);
                Console.WriteLine($"Player {info.PlayerId} in room {info.GameId} disconnected. Timeout timer started 10 minutes).");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object> CreateLobby(string hostName)
    {
        var session = _sessionManager.CreateLobby(hostName, out int hostPlayerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId.ToUpper());
        _connectionMap[Context.ConnectionId] = (session.GameId.ToUpper(), hostPlayerId);
        Console.WriteLine($"Lobby created by {hostName} in room {session.GameId}");

        return new { PlayerId = hostPlayerId, Lobby = DtoMapper.ToLobbyDto(session) };
    }

    public async Task JoinLobby(string gameId, string playerName)
    {
        var (session, newPlayerId, errorMessage) = _sessionManager.JoinLobby(gameId, playerName);

        if (session == null || !newPlayerId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", errorMessage ?? "Failed to join room.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId.ToUpper());
        _connectionMap[Context.ConnectionId] = (session.GameId.ToUpper(), newPlayerId.Value);
        session.MarkPlayerReconnected(newPlayerId.Value);

        Console.WriteLine($"Player {playerName} joined lobby {gameId}");

        await Clients.Caller.SendAsync("JoinedSuccess", newPlayerId.Value);
        await Clients.Group(session.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(session));
    }


    public async Task JoinGame(string gameId, int? playerId = null)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return;

        string upperGameId = gameId.ToUpper();
        await Groups.AddToGroupAsync(Context.ConnectionId, upperGameId);

        var lobby = _sessionManager.GetLobby(upperGameId);

        if (playerId.HasValue)
        {
            _connectionMap[Context.ConnectionId] = (upperGameId, playerId.Value);
            if (lobby != null)
            {
                lobby.MarkPlayerReconnected(playerId.Value);
                Console.WriteLine($"Player {playerId.Value} joined/reconnected to group {upperGameId}.");
            }
        }
        else
        {
            Console.WriteLine($"Connection {Context.ConnectionId} joined group {upperGameId}.");
        }

        // Broadcast updated lobby so the host sees the latest player list immediately
        if (lobby != null)
        {
            await Clients.Group(upperGameId).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(lobby));
        }
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
                session.Touch();
            }
        }
    }

    public async Task StartGame(string gameId, int playerId)
    {
        var game = _sessionManager.GetGame(gameId) ?? _sessionManager.StartGame(gameId, playerId);
        Console.WriteLine($"Game with {gameId} started by player {playerId}");
        if (game != null)
        {
            await Clients.Group(gameId.ToUpper()).SendAsync("GameStarted");
            await Clients.Group(gameId.ToUpper()).SendAsync("GameStateUpdated");
        }
    }

    public async Task PlayTile(PlayTileRequest request)
    {
        var lobby = _sessionManager.GetLobby(request.GameId);
        if (lobby == null || lobby.ActiveGame == null) return;

        var game = lobby.ActiveGame;
        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null) return;

        if (game.PlayTile(player, request.Tile, request.Side))
        {
            lobby.Touch();
            // Notify all clients in room to pull their fresh personalized game state
            await Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");
        }
    }
}
