namespace DominoWebAPI.Hubs;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;
using DominoWebAPI.Common;
using DominoWebAPI.Controllers;

public class GameHub : Hub
{
    private readonly GameSessionManager _sessionManager;
    private readonly ILogger<DominoGameController> _logger;

    private static readonly ConcurrentDictionary<string, (string GameId, int PlayerId)> _connectionMap = new();

    public GameHub(GameSessionManager sessionManager, ILogger<DominoGameController> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionMap.TryRemove(Context.ConnectionId, out var info))
        {
            var lobbyResult = _sessionManager.GetLobby(info.GameId);
            if (lobbyResult.IsSuccess)
            {
                var lobby = lobbyResult.Data!;
                if (lobby.ActiveGame == null)
                {
                    lobby.Players.RemoveAt(info.PlayerId - 1);
                    await Clients.Group(lobby.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(lobby));
                }
                lobby.MarkPlayerDisconnected(info.PlayerId);
                _logger.LogInformation("Player {PlayerId} in room {GameId} disconnected. Timeout timer started (10 minutes).", info.PlayerId, info.GameId);
                await Clients.Group(info.GameId).SendAsync("PlayerDisconnected", info.PlayerId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object?> CreateLobby(string hostName)
    {
        var result = _sessionManager.CreateLobby(hostName, out int hostPlayerId);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.ErrorMessage ?? "Failed to create room.");
            return null;
        }

        var session = result.Data!;
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId.ToUpper());
        _connectionMap[Context.ConnectionId] = (session.GameId.ToUpper(), hostPlayerId);
        _logger.LogInformation("Lobby created by {HostName} in room {GameId}", hostName, session.GameId);

        return new { PlayerId = hostPlayerId, Lobby = DtoMapper.ToLobbyDto(session) };
    }

    public async Task JoinLobby(string gameId, string playerName)
    {
        var result = _sessionManager.JoinLobby(gameId, playerName);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.ErrorMessage ?? "Failed to join room.");
            return;
        }

        var (session, newPlayerId) = result.Data;
        await Groups.AddToGroupAsync(Context.ConnectionId, session.GameId.ToUpper());
        _connectionMap[Context.ConnectionId] = (session.GameId.ToUpper(), newPlayerId);
        session.MarkPlayerReconnected(newPlayerId);

        _logger.LogInformation("Player {PlayerName} joined lobby {GameId}", playerName, gameId);

        await Clients.Caller.SendAsync("JoinedSuccess", newPlayerId);
        await Clients.Group(session.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(session));
    }

    public async Task JoinGame(string gameId, int? playerId = null)
    {
        if (string.IsNullOrWhiteSpace(gameId)) return;

        string upperGameId = gameId.ToUpper();
        await Groups.AddToGroupAsync(Context.ConnectionId, upperGameId);

        var lobbyResult = _sessionManager.GetLobby(upperGameId);

        if (playerId.HasValue)
        {
            _connectionMap[Context.ConnectionId] = (upperGameId, playerId.Value);
            if (lobbyResult.IsSuccess)
            {
                lobbyResult.Data!.MarkPlayerReconnected(playerId.Value);
                _logger.LogInformation("Player {PlayerId} joined/reconnected to group {GameId}", playerId.Value, upperGameId);
            }
        }
        else
        {
            _logger.LogInformation("Connection {ConnectionId} joined group {GameId}", Context.ConnectionId, upperGameId);
        }

        if (lobbyResult.IsSuccess)
        {
            await Clients.Group(upperGameId).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(lobbyResult.Data!));
        }
    }

    public async Task UpdateSettings(UpdateSettingsRequest settings)
    {
        var result = _sessionManager.UpdateSettings(settings);
        if (result.IsSuccess)
        {
            var session = result.Data!;
            await Clients.Group(settings.GameId.ToUpper()).SendAsync("LobbyUpdated", DtoMapper.ToLobbyDto(session));
            _logger.LogInformation("Settings updated for game {GameId}", settings.GameId);
            session.Touch();
        }
    }

    public async Task StartGame(string gameId, int playerId)
    {
        var gameResult = _sessionManager.GetGame(gameId);
        var game = gameResult.IsSuccess ? gameResult.Data : _sessionManager.StartGame(gameId, playerId).Data;
        _logger.LogInformation("Game with {GameId} started by player {PlayerId}", gameId, playerId);
        if (game != null)
        {
            await Clients.Group(gameId.ToUpper()).SendAsync("GameStarted");
            await Clients.Group(gameId.ToUpper()).SendAsync("GameStateUpdated");
        }
    }

    public async Task PlayTile(PlayTileRequest request)
    {
        var lobbyResult = _sessionManager.GetLobby(request.GameId);
        if (!lobbyResult.IsSuccess || lobbyResult.Data!.ActiveGame == null) return;

        var lobby = lobbyResult.Data!;
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
