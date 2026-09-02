namespace DominoWebAPI.Services;

using System.Collections.Concurrent;
using DominoWebAPI.Models;
using DominoWebAPI.DTOs;
using DominoWebAPI.Common;

public class GameSessionManager
{
    private readonly ConcurrentDictionary<string, LobbySession> _lobbies = new();

    private async void OnGameOver()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        CleanupExpiredSessions(TimeSpan.FromMinutes(10));
    }

    public virtual ServiceResult<LobbySession> CreateLobby(string hostName, out int hostPlayerId)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            hostPlayerId = 0;
            return ServiceResult<LobbySession>.BadRequest("Host player name is required.");
        }

        string gameId = GenerateRoomCode();
        hostPlayerId = 1;

        var hostPlayer = new LobbyPlayer
        {
            PlayerId = hostPlayerId,
            PlayerName = hostName,
            IsHost = true
        };

        var session = new LobbySession
        {
            GameId = gameId,
            HostPlayerId = hostPlayerId,
            Players = new List<LobbyPlayer> { hostPlayer }
        };

        _lobbies[gameId] = session;
        return ServiceResult<LobbySession>.Success(session);
    }

    public virtual ServiceResult<(LobbySession Session, int NewPlayerId)> JoinLobby(string gameId, string playerName)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return ServiceResult<(LobbySession Session, int NewPlayerId)>.BadRequest("Game ID is required.");

        if (string.IsNullOrWhiteSpace(playerName))
            return ServiceResult<(LobbySession Session, int NewPlayerId)>.BadRequest("Player name is required.");

        if (!_lobbies.TryGetValue(gameId.ToUpper(), out var session))
            return ServiceResult<(LobbySession Session, int NewPlayerId)>.NotFound("Game room not found.");

        session.Touch();

        if (session.ActiveGame != null)
            return ServiceResult<(LobbySession Session, int NewPlayerId)>.BadRequest("Game has already started.");

        if (session.Players.Count >= 4)
            return ServiceResult<(LobbySession Session, int NewPlayerId)>.BadRequest("Lobby is full.");

        int newPlayerId = session.Players.Count + 1;
        var newPlayer = new LobbyPlayer
        {
            PlayerId = newPlayerId,
            PlayerName = playerName,
            IsHost = false
        };

        session.Players.Add(newPlayer);
        return ServiceResult<(LobbySession Session, int NewPlayerId)>.Success((session, newPlayerId));
    }

    public virtual bool IsHost(string gameId, int playerId)
    {
        var lobbyResult = GetLobby(gameId);
        return lobbyResult.IsSuccess && lobbyResult.Data!.HostPlayerId == playerId;
    }

    public virtual ServiceResult<LobbySession> UpdateSettings(UpdateSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GameId))
            return ServiceResult<LobbySession>.BadRequest("Game ID is required.");

        if (!_lobbies.TryGetValue(request.GameId.ToUpper(), out var session))
            return ServiceResult<LobbySession>.NotFound("Could not update settings. Room might not exist.");

        session.Touch();

        if (session.ActiveGame != null)
            return ServiceResult<LobbySession>.BadRequest("Could not update settings. Game is already active.");

        session.Mode = request.Mode;
        session.DeckSize = request.DeckSize;
        session.TargetScore = request.TargetScore;
        session.HandSize = request.HandSize;
        session.StartingRule = request.StartingRule;

        return ServiceResult<LobbySession>.Success(session);
    }

    public virtual ServiceResult<GameLogic> StartGame(string gameId, int requestingPlayerId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return ServiceResult<GameLogic>.BadRequest("Game ID is required.");

        if (!_lobbies.TryGetValue(gameId.ToUpper(), out var session))
            return ServiceResult<GameLogic>.NotFound("Game room not found.");

        session.Touch();

        if (session.HostPlayerId != requestingPlayerId)
            return ServiceResult<GameLogic>.BadRequest("Failed to start game. Ensure you are the host.");

        if (session.Players.Count < 2)
            return ServiceResult<GameLogic>.BadRequest("Failed to start game. At least 2 players are required.");

        List<IPlayer> gamePlayers = session.Players
            .Select(p => new Player(p.PlayerId, p.PlayerName) as IPlayer)
            .ToList();

        var game = new GameLogic(
            gamePlayers,
            session.Mode,
            session.HandSize,
            session.TargetScore,
            session.DeckSize,
            session.StartingRule
        );

        game.GameStateGameOver += OnGameOver;
        session.ActiveGame = game;

        game.StartGame();

        return ServiceResult<GameLogic>.Success(game);
    }

    public virtual ServiceResult<LobbySession> GetLobby(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return ServiceResult<LobbySession>.BadRequest("Game ID is required.");

        if (_lobbies.TryGetValue(gameId.ToUpper(), out var session))
        {
            session.Touch();
            return ServiceResult<LobbySession>.Success(session);
        }

        return ServiceResult<LobbySession>.NotFound("Game session not found.");
    }

    public virtual ServiceResult<GameLogic> GetGame(string gameId)
    {
        var lobbyResult = GetLobby(gameId);
        if (!lobbyResult.IsSuccess)
            return ServiceResult<GameLogic>.Failure(lobbyResult.ErrorMessage!, lobbyResult.ErrorType);

        if (lobbyResult.Data!.ActiveGame == null)
            return ServiceResult<GameLogic>.NotFound("Active game session not found.");

        return ServiceResult<GameLogic>.Success(lobbyResult.Data.ActiveGame);
    }

    public virtual ServiceResult RemoveLobby(string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return ServiceResult.BadRequest("Game ID is required.");

        if (_lobbies.TryRemove(gameId.ToUpper(), out _))
        {
            return ServiceResult.Success();
        }

        return ServiceResult.NotFound("Game session not found.");
    }

    public virtual int CleanupExpiredSessions(TimeSpan inactivityTimeout)
    {
        var now = DateTime.UtcNow;
        int removedCount = 0;

        foreach (var kvp in _lobbies)
        {
            var gameId = kvp.Key;
            var session = kvp.Value;

            bool isGameOver = session.ActiveGame != null && session.ActiveGame.Status == GameState.GameOver;
            bool isIdleExpired = (now - session.LastActivityUtc) > inactivityTimeout;

            bool hasTimedOutDisconnectedPlayer = session.DisconnectedPlayersUtc.Values.Any(dt => (now - dt) > inactivityTimeout);

            if (isGameOver || isIdleExpired || hasTimedOutDisconnectedPlayer)
            {
                if (_lobbies.TryRemove(gameId, out _))
                {
                    removedCount++;
                    Console.WriteLine($"Removed game id:  {gameId}");
                }
            }
        }

        return removedCount;
    }

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}