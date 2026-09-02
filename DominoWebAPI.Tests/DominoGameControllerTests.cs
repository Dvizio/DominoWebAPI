namespace DominoWebAPI.Tests;

using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using DominoWebAPI.Controllers;
using DominoWebAPI.Services;
using DominoWebAPI.Hubs;
using DominoWebAPI.DTOs;
using DominoWebAPI.Models;
using DominoWebAPI.Common;

[TestFixture]
public class DominoGameControllerTests
{
    private Mock<GameSessionManager> _sessionManagerMock;
    private Mock<IHubContext<GameHub>> _hubContextMock;
    private Mock<IHubClients> _clientsMock;
    private Mock<IClientProxy> _clientProxyMock;
    private Mock<ILogger<DominoGameController>> _loggerMock;
    
    private DominoGameController _controller;

    [SetUp]
    public void Setup()
    {
        _sessionManagerMock = new Mock<GameSessionManager>();
        _hubContextMock = new Mock<IHubContext<GameHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _loggerMock = new Mock<ILogger<DominoGameController>>();

        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

        _controller = new DominoGameController(
            _sessionManagerMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object
        );
    }

    #region Helper Methods

    private (LobbySession Lobby, GameLogic Game, List<IPlayer> Players) CreateTestLobbyWithActiveGame(
        string gameId = "GAME123",
        int hostId = 1,
        GameMode mode = GameMode.Draw)
    {
        var p1 = new Player(1, "Alice");
        var p2 = new Player(2, "Bob");
        var players = new List<IPlayer> { p1, p2 };
        var game = new GameLogic(players, mode, 7, 100, 6, StartingPlayerRule.HighestDouble);
        game.StartGame();

        var lobby = new LobbySession
        {
            GameId = gameId,
            HostPlayerId = hostId,
            Players = new List<LobbyPlayer>
            {
                new LobbyPlayer { PlayerId = 1, PlayerName = "Alice", IsHost = true },
                new LobbyPlayer { PlayerId = 2, PlayerName = "Bob", IsHost = false }
            },
            ActiveGame = game
        };

        return (lobby, game, players);
    }

    private LobbySession CreateTestLobbyWithoutActiveGame(string gameId = "GAME123", int hostId = 1)
    {
        return new LobbySession
        {
            GameId = gameId,
            HostPlayerId = hostId,
            Players = new List<LobbyPlayer>
            {
                new LobbyPlayer { PlayerId = 1, PlayerName = "Alice", IsHost = true }
            },
            ActiveGame = null
        };
    }

    #endregion

    #region 1. CreateLobby Tests

    [Test]
    public void CreateLobby_Success_ReturnsOkObjectResult()
    {
        // Arrange
        var request = new CreateLobbyRequest { HostPlayerName = "Alice" };
        int outPlayerId = 1;
        var fakeLobby = new LobbySession
        {
            GameId = "GAME123",
            HostPlayerId = outPlayerId,
            Players = new List<LobbyPlayer> { new LobbyPlayer { PlayerId = 1, PlayerName = "Alice", IsHost = true } }
        };

        _sessionManagerMock
            .Setup(m => m.CreateLobby(request.HostPlayerName, out outPlayerId))
            .Returns(ServiceResult<LobbySession>.Success(fakeLobby));

        // Act
        var actionResult = _controller.CreateLobby(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public void CreateLobby_Failure_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateLobbyRequest { HostPlayerName = "" };
        int outPlayerId = 0;

        _sessionManagerMock
            .Setup(m => m.CreateLobby(request.HostPlayerName, out outPlayerId))
            .Returns(ServiceResult<LobbySession>.BadRequest("Host player name is required."));

        // Act
        var actionResult = _controller.CreateLobby(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badResult = actionResult as BadRequestObjectResult;
        Assert.That(badResult!.Value, Is.EqualTo("Host player name is required."));
    }

    #endregion

    #region 2. JoinLobby Tests

    [Test]
    public async Task JoinLobby_Success_NotifiesSignalRGroupAndReturnsOk()
    {
        // Arrange
        var request = new JoinLobbyRequest { GameId = "GAME123", PlayerName = "Bob" };
        var fakeLobby = new LobbySession { GameId = "GAME123", HostPlayerId = 1 };
        int newPlayerId = 2;

        _sessionManagerMock
            .Setup(m => m.JoinLobby(request.GameId, request.PlayerName))
            .Returns(ServiceResult<(LobbySession, int)>.Success((fakeLobby, newPlayerId)));

        // Act
        var actionResult = await _controller.JoinLobby(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("LobbyUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Test]
    public async Task JoinLobby_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new JoinLobbyRequest { GameId = "INVALID", PlayerName = "Bob" };

        _sessionManagerMock
            .Setup(m => m.JoinLobby(request.GameId, request.PlayerName))
            .Returns(ServiceResult<(LobbySession, int)>.NotFound("Game room not found."));

        // Act
        var actionResult = await _controller.JoinLobby(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Game room not found."));
    }

    [Test]
    public async Task JoinLobby_LobbyFull_ReturnsBadRequest()
    {
        // Arrange
        var request = new JoinLobbyRequest { GameId = "GAME123", PlayerName = "Bob" };

        _sessionManagerMock
            .Setup(m => m.JoinLobby(request.GameId, request.PlayerName))
            .Returns(ServiceResult<(LobbySession, int)>.BadRequest("Lobby is full."));

        // Act
        var actionResult = await _controller.JoinLobby(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Lobby is full."));
    }

    #endregion

    #region 3. UpdateSettings Tests

    [Test]
    public async Task UpdateSettings_Success_NotifiesSignalRGroupAndReturnsOk()
    {
        // Arrange
        var request = new UpdateSettingsRequest
        {
            GameId = "GAME123",
            Mode = GameMode.Block,
            HandSize = 5,
            DeckSize = 6,
            TargetScore = 150,
            StartingRule = StartingPlayerRule.HighestDouble
        };

        var fakeLobby = new LobbySession
        {
            GameId = "GAME123",
            HostPlayerId = 1,
            Mode = GameMode.Block,
            HandSize = 5,
            DeckSize = 6,
            TargetScore = 150,
            StartingRule = StartingPlayerRule.HighestDouble,
            Players = new List<LobbyPlayer> { new LobbyPlayer { PlayerId = 1, PlayerName = "Alice", IsHost = true } }
        };

        _sessionManagerMock
            .Setup(m => m.UpdateSettings(request))
            .Returns(ServiceResult<LobbySession>.Success(fakeLobby));

        // Act
        var actionResult = await _controller.UpdateSettings(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<LobbyStateDto>());
        var dto = okResult.Value as LobbyStateDto;
        Assert.That(dto!.Mode, Is.EqualTo(GameMode.Block));
        Assert.That(dto.HandSize, Is.EqualTo(5));

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("LobbyUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Test]
    public async Task UpdateSettings_RoomNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateSettingsRequest { GameId = "NOTFOUND" };

        _sessionManagerMock
            .Setup(m => m.UpdateSettings(request))
            .Returns(ServiceResult<LobbySession>.NotFound("Could not update settings. Room might not exist."));

        // Act
        var actionResult = await _controller.UpdateSettings(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Could not update settings. Room might not exist."));
    }

    [Test]
    public async Task UpdateSettings_GameAlreadyActive_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateSettingsRequest { GameId = "GAME123" };

        _sessionManagerMock
            .Setup(m => m.UpdateSettings(request))
            .Returns(ServiceResult<LobbySession>.BadRequest("Could not update settings. Game is already active."));

        // Act
        var actionResult = await _controller.UpdateSettings(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Could not update settings. Game is already active."));
    }

    #endregion

    #region 4. StartGame Tests

    [Test]
    public async Task StartGame_Success_SendsSignalRNotificationsAndReturnsOk()
    {
        // Arrange
        string gameId = "GAME123";
        int playerId = 1;
        var p1 = new Player(1, "Alice");
        var p2 = new Player(2, "Bob");
        var game = new GameLogic(new List<IPlayer> { p1, p2 }, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        game.StartGame();

        _sessionManagerMock
            .Setup(m => m.StartGame(gameId, playerId))
            .Returns(ServiceResult<GameLogic>.Success(game));

        // Act
        var actionResult = await _controller.StartGame(gameId, playerId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<GameStateDto>());

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Exactly(2));
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStarted", It.IsAny<object[]>(), default),
            Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStateUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Test]
    public async Task StartGame_NotHost_ReturnsBadRequest()
    {
        // Arrange
        string gameId = "GAME123";
        int nonHostPlayerId = 2;

        _sessionManagerMock
            .Setup(m => m.StartGame(gameId, nonHostPlayerId))
            .Returns(ServiceResult<GameLogic>.BadRequest("Failed to start game. Ensure you are the host."));

        // Act
        var actionResult = await _controller.StartGame(gameId, nonHostPlayerId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Failed to start game. Ensure you are the host."));
    }

    [Test]
    public async Task StartGame_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        string gameId = "NOTFOUND";
        int playerId = 1;

        _sessionManagerMock
            .Setup(m => m.StartGame(gameId, playerId))
            .Returns(ServiceResult<GameLogic>.NotFound("Game room not found."));

        // Act
        var actionResult = await _controller.StartGame(gameId, playerId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    #endregion

    #region 5. StartNextRound Tests

    [Test]
    public async Task StartNextRound_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        string gameId = "NOTFOUND";
        int hostId = 1;

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.NotFound("Game session not found."));

        // Act
        var actionResult = await _controller.StartNextRound(gameId, hostId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task StartNextRound_NoActiveGame_ReturnsNotFound()
    {
        // Arrange
        string gameId = "GAME123";
        int hostId = 1;
        var fakeLobby = CreateTestLobbyWithoutActiveGame(gameId, hostId);

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(fakeLobby));

        // Act
        var actionResult = await _controller.StartNextRound(gameId, hostId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Active game session not found."));
    }

    [Test]
    public async Task StartNextRound_PlayerNotHost_ReturnsBadRequest()
    {
        // Arrange
        string gameId = "GAME123";
        var (lobby, _, _) = CreateTestLobbyWithActiveGame(gameId, hostId: 1);
        int nonHostId = 2;

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.StartNextRound(gameId, nonHostId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Only the host can start the next round."));
    }

    [Test]
    public async Task StartNextRound_GameNotRoundOver_ReturnsBadRequest()
    {
        // Arrange
        string gameId = "GAME123";
        var (lobby, game, _) = CreateTestLobbyWithActiveGame(gameId, hostId: 1);
        // game status is GameState.Playing

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.StartNextRound(gameId, 1);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Game is not in RoundOver state."));
    }

    [Test]
    public async Task StartNextRound_Success_AdvancesRoundSendsSignalRAndReturnsOk()
    {
        // Arrange
        string gameId = "GAME123";
        var (lobby, game, _) = CreateTestLobbyWithActiveGame(gameId, hostId: 1);
        game.EndRound(); // Sets game status to GameState.RoundOver
        int initialRound = game.RoundNumber;

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.StartNextRound(gameId, 1);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<GameStateDto>());
        Assert.That(game.Status, Is.EqualTo(GameState.Playing));
        Assert.That(game.RoundNumber, Is.EqualTo(initialRound + 1));

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStateUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    #endregion

    #region 6. GetState Tests

    [Test]
    public void GetState_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        string gameId = "NOTFOUND";

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.NotFound("Game session not found."));

        // Act
        var actionResult = _controller.GetState(gameId, 1);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetState_NoActiveGame_ReturnsOkWithIsActiveFalseAndLobbyDto()
    {
        // Arrange
        string gameId = "GAME123";
        var lobby = CreateTestLobbyWithoutActiveGame(gameId, 1);

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = _controller.GetState(gameId, 1);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public void GetState_ActiveGameExists_ReturnsOkWithIsActiveTrueAndGameDto()
    {
        // Arrange
        string gameId = "GAME123";
        var (lobby, _, _) = CreateTestLobbyWithActiveGame(gameId, 1);

        _sessionManagerMock
            .Setup(m => m.GetLobby(gameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = _controller.GetState(gameId, 1);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    #endregion

    #region 7. DeleteLobby Tests

    [Test]
    public async Task DeleteLobby_Success_SendsLobbyClosedSignalRAndReturnsOk()
    {
        // Arrange
        string gameId = "GAME123";

        _sessionManagerMock
            .Setup(m => m.RemoveLobby(gameId))
            .Returns(ServiceResult.Success());

        // Act
        var actionResult = await _controller.DeleteLobby(gameId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("LobbyClosed", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Test]
    public async Task DeleteLobby_NotFound_ReturnsNotFound()
    {
        // Arrange
        string gameId = "NOTFOUND";

        _sessionManagerMock
            .Setup(m => m.RemoveLobby(gameId))
            .Returns(ServiceResult.NotFound("Game session not found."));

        // Act
        var actionResult = await _controller.DeleteLobby(gameId);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    #endregion

    #region 8. PlayTile Tests

    [Test]
    public async Task PlayTile_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayTileRequest { GameId = "NOTFOUND", PlayerId = 1 };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.NotFound("Game session not found."));

        // Act
        var actionResult = await _controller.PlayTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task PlayTile_NoActiveGame_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayTileRequest { GameId = "GAME123", PlayerId = 1 };
        var lobby = CreateTestLobbyWithoutActiveGame(request.GameId, 1);

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PlayTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Active game session not found."));
    }

    [Test]
    public async Task PlayTile_PlayerNotPartOfGame_ReturnsBadRequest()
    {
        // Arrange
        var (lobby, _, _) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var request = new PlayTileRequest { GameId = "GAME123", PlayerId = 999 };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PlayTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Player is not part of this session."));
    }

    [Test]
    public async Task PlayTile_InvalidMoveOrNotTurn_ReturnsBadRequest()
    {
        // Arrange
        var (lobby, game, _) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var currentPlayer = game.CurrentPlayer;
        // Tile not in hand
        var request = new PlayTileRequest
        {
            GameId = "GAME123",
            PlayerId = currentPlayer.PlayerId,
            Tile = new DominoTile(99, 99),
            Side = PlacementSide.Left
        };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PlayTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Invalid move or it is not your turn."));
    }

    [Test]
    public async Task PlayTile_Success_SendsSignalRAndReturnsOk()
    {
        // Arrange
        var (lobby, game, _) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var currentPlayer = game.CurrentPlayer;
        game.Board.PlayedTile = new List<DominoTile>(); // Empty board accepts any tile in hand
        var tileToPlay = game.PlayerHands[currentPlayer.PlayerId].First();

        var request = new PlayTileRequest
        {
            GameId = "GAME123",
            PlayerId = currentPlayer.PlayerId,
            Tile = tileToPlay,
            Side = PlacementSide.Left
        };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PlayTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<GameStateDto>());

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStateUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    #endregion

    #region 9. DrawTile Tests

    [Test]
    public async Task DrawTile_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayerActionRequest { GameId = "NOTFOUND", PlayerId = 1 };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.NotFound("Game session not found."));

        // Act
        var actionResult = await _controller.DrawTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DrawTile_NoActiveGame_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = 1 };
        var lobby = CreateTestLobbyWithoutActiveGame(request.GameId, 1);

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.DrawTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Active game session not found."));
    }

    [Test]
    public async Task DrawTile_NotPlayerTurn_ReturnsBadRequest()
    {
        // Arrange
        var (lobby, game, players) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var nonCurrentPlayer = players.First(p => p.PlayerId != game.CurrentPlayer.PlayerId);
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = nonCurrentPlayer.PlayerId };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.DrawTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("It is not your turn."));
    }

    [Test]
    public async Task DrawTile_CannotDrawInBlockMode_ReturnsBadRequest()
    {
        // Arrange
        var (lobby, game, _) = CreateTestLobbyWithActiveGame("GAME123", 1, mode: GameMode.Block);
        var currentPlayer = game.CurrentPlayer;
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = currentPlayer.PlayerId };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.DrawTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("Cannot draw (boneyard is empty or mode is Block)."));
    }

    [Test]
    public async Task DrawTile_Success_SendsSignalRAndReturnsOk()
    {
        // Arrange
        var (lobby, game, _) = CreateTestLobbyWithActiveGame("GAME123", 1, mode: GameMode.Draw);
        var currentPlayer = game.CurrentPlayer;
        int initialHandCount = game.PlayerHands[currentPlayer.PlayerId].Count;
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = currentPlayer.PlayerId };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.DrawTile(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<GameStateDto>());
        Assert.That(game.PlayerHands[currentPlayer.PlayerId].Count, Is.EqualTo(initialHandCount + 1));

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStateUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    #endregion

    #region 10. PassTurn Tests

    [Test]
    public async Task PassTurn_LobbyNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayerActionRequest { GameId = "NOTFOUND", PlayerId = 1 };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.NotFound("Game session not found."));

        // Act
        var actionResult = await _controller.PassTurn(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task PassTurn_NoActiveGame_ReturnsNotFound()
    {
        // Arrange
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = 1 };
        var lobby = CreateTestLobbyWithoutActiveGame(request.GameId, 1);

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PassTurn(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult!.Value, Is.EqualTo("Active game session not found."));
    }

    [Test]
    public async Task PassTurn_NotPlayerTurn_ReturnsBadRequest()
    {
        // Arrange
        var (lobby, game, players) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var nonCurrentPlayer = players.First(p => p.PlayerId != game.CurrentPlayer.PlayerId);
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = nonCurrentPlayer.PlayerId };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PassTurn(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult!.Value, Is.EqualTo("It is not your turn."));
    }

    [Test]
    public async Task PassTurn_Success_SendsSignalRAndReturnsOk()
    {
        // Arrange
        var (lobby, game, _) = CreateTestLobbyWithActiveGame("GAME123", 1);
        var initialCurrentPlayer = game.CurrentPlayer;
        var request = new PlayerActionRequest { GameId = "GAME123", PlayerId = initialCurrentPlayer.PlayerId };

        _sessionManagerMock
            .Setup(m => m.GetLobby(request.GameId))
            .Returns(ServiceResult<LobbySession>.Success(lobby));

        // Act
        var actionResult = await _controller.PassTurn(request);

        // Assert
        Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<GameStateDto>());
        Assert.That(game.CurrentPlayer.PlayerId, Is.Not.EqualTo(initialCurrentPlayer.PlayerId));

        _clientsMock.Verify(c => c.Group("GAME123"), Times.Once);
        _clientProxyMock.Verify(
            p => p.SendCoreAsync("GameStateUpdated", It.IsAny<object[]>(), default),
            Times.Once);
    }

    #endregion
}