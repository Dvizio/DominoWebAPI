using Microsoft.AspNetCore.SignalR;

namespace DominoWebAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;
using DominoWebAPI.Hubs;

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
        if (string.IsNullOrWhiteSpace(request.HostPlayerName))
            return BadRequest("Host player name is required.");

        var session = _sessionManager.CreateLobby(request.HostPlayerName, out int hostPlayerId);
        var lobbyDto = DtoMapper.ToLobbyDto(session);
        Console.WriteLine($"Create a game with id {lobbyDto.GameId}");
        // _hubContext.Clients.All.SendAsync("lobbyCreated", lobbyDto);
        return Ok(new { PlayerId = hostPlayerId, Lobby = lobbyDto });
    }

    //POST api/games/lobby/join - Others joins a lobby
    [HttpPost("lobby/join")]
    public IActionResult JoinLobby([FromBody] JoinLobbyRequest request)
    {
        Console.WriteLine($"a guy named {request.PlayerName} joined {request.GameId}");
        if (string.IsNullOrWhiteSpace(request.PlayerName))
            return BadRequest("Player name is required.");

        var (session, newPlayerId, errorMessage) = _sessionManager.JoinLobby(request.GameId, request.PlayerName);

        if (session == null)
            return BadRequest(errorMessage);

        var lobbyDto = DtoMapper.ToLobbyDto(session);
        _hubContext.Clients.Group(request.GameId.ToUpper())
        .SendAsync("LobbyUpdated", lobbyDto);
        return Ok(new { PlayerId = newPlayerId, Lobby = lobbyDto });
    }

    //PUT api/games/lobby/settings - Host updates settings
    [HttpPut("lobby/settings")]
    public IActionResult UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        bool updated = _sessionManager.UpdateSettings(request);
        if (!updated)
            return BadRequest("Could not update settings. Room might not exist or game is already active.");

        var session = _sessionManager.GetLobby(request.GameId);
        return Ok(DtoMapper.ToLobbyDto(session!));
    }

    // POST api/games/start - Host starts the match
    [HttpPost("start")]
    public async Task<IActionResult> StartGame([FromQuery] string gameId, [FromQuery] int playerId)
    {
        var game = _sessionManager.StartGame(gameId, playerId);
        if (game == null)
            return BadRequest("Failed to start game. Ensure you are the host and at least 2 players are present.");

        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("GameStarted");
        await _hubContext.Clients.Group(gameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(gameId, game, playerId));
    }

    // POST api/games/next-round - Host starts next round
    [HttpPost("next-round")]
    public async Task<IActionResult> StartNextRound([FromQuery] string gameId, [FromQuery] int playerId)
    {
        var lobby = _sessionManager.GetLobby(gameId);
        if (lobby == null || lobby.ActiveGame == null)
            return NotFound("Active game session not found.");

        if (!(lobby.Players.Count >= playerId))
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
        var lobby = _sessionManager.GetLobby(gameId);
        if (lobby == null)
            return NotFound("Game session not found.");


        if (lobby.ActiveGame == null)
        {
            return Ok(new { IsActive = false, Lobby = DtoMapper.ToLobbyDto(lobby) });
        }

        return Ok(new { IsActive = true, Game = DtoMapper.ToGameDto(gameId, lobby.ActiveGame, playerId) });
    }

    //POST api/games/play - Play a tile
    [HttpPost("play")]
    public async Task<IActionResult> PlayTile([FromBody] PlayTileRequest request)
    {
        var game = _sessionManager.GetGame(request.GameId);
        if (game == null)
            return NotFound("Active game session not found.");

        var player = game.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
        if (player == null)
            return BadRequest("Player is not part of this session.");

        bool success = game.PlayTile(player, request.Tile, request.Side);
        Console.WriteLine($"im from the controller {request.Tile.Left} {request.Tile.Right} {request.Side} ");
        if (!success)
            return BadRequest("Invalid move or it is not your turn.");

        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }

    //POST api/games/draw - Draw a tile from boneyard
    [HttpPost("draw")]
    public async Task<IActionResult> DrawTile([FromBody] PlayerActionRequest request)
    {
        var game = _sessionManager.GetGame(request.GameId);
        if (game == null)
            return NotFound("Active game session not found.");

        if (game.CurrentPlayer.PlayerId != request.PlayerId)
            return BadRequest("It is not your turn.");

        var player = game.Players.First(p => p.PlayerId == request.PlayerId);
        bool drew = game.AutoDrawToPlayerHand(player);

        if (!drew)
            return BadRequest("Cannot draw (boneyard is empty or mode is Block).");

        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }

    //POST api/games/pass - Pass turn
    [HttpPost("pass")]
    public async Task<IActionResult> PassTurn([FromBody] PlayerActionRequest request)
    {
        var game = _sessionManager.GetGame(request.GameId);
        if (game == null)
            return NotFound("Active game session not found.");

        if (game.CurrentPlayer.PlayerId != request.PlayerId)
            return BadRequest("It is not your turn.");

        var player = game.Players.First(p => p.PlayerId == request.PlayerId);
        game.PassTurn(player);

        await _hubContext.Clients.Group(request.GameId.ToUpper()).SendAsync("GameStateUpdated");

        return Ok(DtoMapper.ToGameDto(request.GameId, game, request.PlayerId));
    }
}