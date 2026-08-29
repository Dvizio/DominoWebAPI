using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;
using DominoWebAPI.Hubs;
using DominoWebAPI.Common;
using DominoWebAPI.Extensions;

namespace DominoWebAPI.Controllers;

[ApiController]
[Route("api/games")]
public class DominoGameController : ControllerBase
{
    private readonly GameSessionManager _sessionManager;
    private readonly IHubContext<GameHub> _hubContext;

    public DominoGameController(
        GameSessionManager sessionManager,
        IHubContext<GameHub> hubContext)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
    }

    //POST api/games/lobby - Host creates a lobby
    [HttpPost("lobby")]
    public IActionResult CreateLobby([FromBody] CreateLobbyRequest request)
    {
        Console.WriteLine($"host spawned {request.HostPlayerName}");
        var result = _sessionManager.CreateLobby(request.HostPlayerName, out int hostPlayerId);
        if (!result.IsSuccess)
            return result.ToActionResult();

        var lobbyDto = DtoMapper.ToLobbyDto(result.Data!);
        Console.WriteLine($"Create a game with id {lobbyDto.GameId}");
        return Ok(new { PlayerId = hostPlayerId, Lobby = lobbyDto });
    }

    //POST api/games/lobby/join - Others joins a lobby
    [HttpPost("lobby/join")]
    public async Task<IActionResult> JoinLobby([FromBody] JoinLobbyRequest request)
    {
        Console.WriteLine($"a guy named {request.PlayerName} joined {request.GameId}");
        var result = _sessionManager.JoinLobby(request.GameId, request.PlayerName);
        if (!result.IsSuccess)
            return result.ToActionResult();

        var (session, newPlayerId) = result.Data;
        var lobbyDto = DtoMapper.ToLobbyDto(session);
        await _hubContext.Clients.Group(request.GameId.ToUpper())
            .SendAsync("LobbyUpdated", lobbyDto);

        return Ok(new { PlayerId = newPlayerId, Lobby = lobbyDto });
    }

    //PUT api/games/lobby/settings - Host updates settings
    [HttpPut("lobby/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var result = _sessionManager.UpdateSettings(request);
        if (!result.IsSuccess)
            return result.ToActionResult();

        var lobbyDto = DtoMapper.ToLobbyDto(result.Data!);
        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("LobbyUpdated", lobbyDto);
        return Ok(lobbyDto);
    }

    // POST api/games/start - Host starts the match
    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromQuery] string gameId, [FromQuery] int playerId)
    {
        var result = _sessionManager.StartGame(gameId, playerId);
        if (!result.IsSuccess)
            return result.ToActionResult();

        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("GameStarted");
        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(gameId, result.Data!, playerId));
    }

    // POST api/games/next-round - Host starts next round
    [HttpPost("next-round")]
    public async Task<IActionResult> StartNextRound([FromQuery] string gameId, [FromQuery] int playerId)
    {
        var lobbyResult = _sessionManager.GetLobby(gameId);
        if (!lobbyResult.IsSuccess)
            return lobbyResult.ToActionResult();

        var lobby = lobbyResult.Data!;
        if (lobby.ActiveGame == null)
            return NotFound("Active game session not found.");

        if (lobby.HostPlayerId != playerId)
            return BadRequest("Only the host can start the next round.");

        var game = lobby.ActiveGame;
        if (game.Status != GameState.RoundOver)
            return BadRequest("Game is not in RoundOver state.");

        game.StartNextRound();

        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(gameId, game, playerId));
    }

    //GET api/games/{gameId}?playerId=X - Get current lobby/game state
    [HttpGet("{gameId}")]
    public IActionResult GetState(string gameId, [FromQuery] int playerId)
    {
        var lobbyResult = _sessionManager.GetLobby(gameId);
        if (!lobbyResult.IsSuccess)
            return lobbyResult.ToActionResult();

        var lobby = lobbyResult.Data!;
        lobby.Touch();

        if (lobby.ActiveGame == null)
        {
            return Ok(new { IsActive = false, Lobby = DtoMapper.ToLobbyDto(lobby) });
        }

        return Ok(new
        {
            IsActive = true,
            Game = DtoMapper.ToGameDto(gameId, lobby.ActiveGame, playerId),
            Lobby = DtoMapper.ToLobbyDto(lobby)
        });
    }

    //DELETE api/games/{gameId} - Delete/clean up lobby when game over or host exits
    [HttpDelete("{gameId}")]
    public async Task<IActionResult> DeleteLobby(string gameId)
    {
        var result = _sessionManager.RemoveLobby(gameId);
        if (!result.IsSuccess)
            return result.ToActionResult();

        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("LobbyClosed", "Game session has ended.");
        return Ok(new { Message = "Game session removed successfully." });
    }

    //POST api/games/play - Play a tile
    [HttpPost("play")]
    public async Task<IActionResult> PlayTile([FromBody] PlayTileRequest request)
    {
        var lobbyResult = _sessionManager.GetLobby(request.GameId);
        if (!lobbyResult.IsSuccess)
            return lobbyResult.ToActionResult();

        var lobby = lobbyResult.Data!;
        if (lobby.ActiveGame == null)
            return NotFound("Active game session not found.");

        var game = lobby.ActiveGame;
        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null)
            return BadRequest("Player is not part of this session.");

        bool success = game.PlayTile(player, request.Tile, request.Side);
        Console.WriteLine($"PlayTile request {request.Tile.Left}|{request.Tile.Right} side={request.Side} success={success}");
        if (!success)
            return BadRequest("Invalid move or it is not your turn.");

        lobby.Touch();
        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }

    //POST api/games/draw - Draw a tile from boneyard
    [HttpPost("draw")]
    public async Task<IActionResult> DrawTile([FromBody] PlayerActionRequest request)
    {
        var lobbyResult = _sessionManager.GetLobby(request.GameId);
        if (!lobbyResult.IsSuccess)
            return lobbyResult.ToActionResult();

        var lobby = lobbyResult.Data!;
        if (lobby.ActiveGame == null)
            return NotFound("Active game session not found.");

        var game = lobby.ActiveGame;
        if (game.CurrentPlayer.PlayerId != request.PlayerId)
            return BadRequest("It is not your turn.");

        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null)
            return BadRequest("Player is not part of this session.");

        bool drew = game.AutoDrawToPlayerHand(player);
        if (!drew)
            return BadRequest("Cannot draw (boneyard is empty or mode is Block).");

        lobby.Touch();
        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }

    //POST api/games/pass - Pass turn
    [HttpPost("pass")]
    public async Task<IActionResult> PassTurn([FromBody] PlayerActionRequest request)
    {
        var lobbyResult = _sessionManager.GetLobby(request.GameId);
        if (!lobbyResult.IsSuccess)
            return lobbyResult.ToActionResult();

        var lobby = lobbyResult.Data!;
        if (lobby.ActiveGame == null)
            return NotFound("Active game session not found.");

        var game = lobby.ActiveGame;
        if (game.CurrentPlayer.PlayerId != request.PlayerId)
            return BadRequest("It is not your turn.");

        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null)
            return BadRequest("Player is not part of this session.");

        game.PassTurn(player);

        lobby.Touch();
        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }
}