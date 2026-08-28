namespace DominoWebAPI.Services;

using System.Collections.Concurrent;
using DominoWebAPI.Models;
using DominoWebAPI.DTOs;

public class GameSessionManager
{
    private readonly ConcurrentDictionary<string, LobbySession> _lobbies = new();

    private async void OnGameOver()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        CleanupExpiredSessions(TimeSpan.FromMinutes(10));
        Console.WriteLine("Cleanup Called");
    }
    public LobbySession CreateLobby(string hostName, out int hostPlayerId)
    {
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
        return session;
    }

    public (LobbySession? session, int? newPlayerId, string? errorMessage) JoinLobby(string gameId, string playerName)
    {
        if (!_lobbies.TryGetValue(gameId.ToUpper(), out var session))
            return (null, null, "Game room not found.");

        session.Touch();

        if (session.ActiveGame != null)
            return (null, null, "Game has already started.");

        if (session.Players.Count >= 4)
            return (null, null, "Lobby is full.");

        int newPlayerId = session.Players.Count + 1;
        var newPlayer = new LobbyPlayer
        {
            PlayerId = newPlayerId,
            PlayerName = playerName,
            IsHost = false
        };

        session.Players.Add(newPlayer);
        return (session, newPlayerId, null);
    }

    public bool IsHost(string gameId, int playerId)
    {
        var lobby = GetLobby(gameId);
        return lobby != null && lobby.HostPlayerId == playerId;
    }

    public bool UpdateSettings(UpdateSettingsRequest request)
    {
        if (!_lobbies.TryGetValue(request.GameId.ToUpper(), out var session))
            return false;

        session.Touch();

        if (session.ActiveGame != null) return false;

        session.Mode = request.Mode;
        session.DeckSize = request.DeckSize;
        session.TargetScore = request.TargetScore;
        session.HandSize = request.HandSize;
        session.StartingRule = request.StartingRule;

        return true;
    }

    public GameLogic? StartGame(string gameId, int requestingPlayerId)
    {
        if (!_lobbies.TryGetValue(gameId.ToUpper(), out var session))
            return null;

        session.Touch();

        if (session.HostPlayerId != requestingPlayerId)
            return null;

        if (session.Players.Count < 2)
            return null;

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

        return game;
    }

    public LobbySession? GetLobby(string gameId)
    {
        if (_lobbies.TryGetValue(gameId.ToUpper(), out var session))
        {
            session.Touch();
            return session;
        }
        return null;
    }

    public GameLogic? GetGame(string gameId)
    {
        var lobby = GetLobby(gameId);
        return lobby?.ActiveGame;
    }

    public bool RemoveLobby(string gameId)
    {
        return _lobbies.TryRemove(gameId.ToUpper(), out _);
    }

    public int CleanupExpiredSessions(TimeSpan inactivityTimeout)
    {
        var now = DateTime.UtcNow;
        int removedCount = 0;

        foreach (var kvp in _lobbies)
        {
            var gameId = kvp.Key;
            var session = kvp.Value;

            bool isGameOver = session.ActiveGame != null && session.ActiveGame.Status == GameState.GameOver;
            bool isIdleExpired = (now - session.LastActivityUtc) > inactivityTimeout;

            // Check if any disconnected player has exceeded the timeout (e.g. 1 hour)
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


//service result pattern.
// jadiin semua jadi different file