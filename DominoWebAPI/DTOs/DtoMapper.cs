namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;

public static class DtoMapper
{
    public static LobbyStateDto ToLobbyDto(LobbySession session)
    {
        return new LobbyStateDto
        {
            GameId = session.GameId,
            HostPlayerId = session.HostPlayerId,
            IsGameStarted = session.ActiveGame != null,
            Mode = session.Mode,
            DeckSize = session.DeckSize,
            TargetScore = session.TargetScore,
            HandSize = session.HandSize,
            StartingRule = session.StartingRule,
            Players = session.Players.Select(p => new LobbyPlayerDto
            {
                PlayerId = p.PlayerId,
                PlayerName = p.PlayerName,
                IsHost = p.IsHost
            }).ToList()
        };
    }

    public static GameStateDto ToGameDto(string gameId, GameLogic game, int requestingPlayerId)
    {
        return new GameStateDto
        {
            GameId = gameId,
            Status = game.Status.ToString(),
            CurrentPlayerId = game.CurrentPlayer.PlayerId,
            RoundNumber = game.RoundNumber,
            PlayedBoard = game.Board.PlayedTile,
            RemainingDeckCount = game.Deck.RemainingCount,
            YourHand = game.PlayerHands.TryGetValue(requestingPlayerId, out var hand)
                ? hand
                : new List<DominoTile>(),
            OtherPlayerHandCounts = game.PlayerHands
                .Where(kvp => kvp.Key != requestingPlayerId)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count),
            Scores = game.Scores,
            RoundWinnerId = game.RoundWinner?.PlayerId,
            GameWinnerId = game.Status == GameState.GameOver ? game.RoundWinner?.PlayerId : null
        };
    }
}