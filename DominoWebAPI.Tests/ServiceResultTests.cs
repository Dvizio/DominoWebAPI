namespace DominoWebAPI.Tests;

using Microsoft.AspNetCore.Mvc;
using DominoWebAPI.Common;
using DominoWebAPI.Extensions;
using DominoWebAPI.Services;
using DominoWebAPI.DTOs;
using Xunit;

public class ServiceResultTests
{
    [Fact]
    public void ServiceResult_Success_CreatesSuccessfulResult()
    {
        var result = ServiceResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(ServiceErrorType.None, result.ErrorType);
    }

    [Fact]
    public void ServiceResultT_Success_ContainsData()
    {
        var result = ServiceResult<string>.Success("hello world");

        Assert.True(result.IsSuccess);
        Assert.Equal("hello world", result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(ServiceErrorType.None, result.ErrorType);
    }

    [Fact]
    public void ServiceResultT_BadRequest_ContainsErrorMessageAndErrorType()
    {
        var result = ServiceResult<string>.BadRequest("Invalid input");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal("Invalid input", result.ErrorMessage);
        Assert.Equal(ServiceErrorType.BadRequest, result.ErrorType);
    }

    [Fact]
    public void ServiceResultT_NotFound_ContainsNotFoundType()
    {
        var result = ServiceResult<int>.NotFound("Item not found");

        Assert.False(result.IsSuccess);
        Assert.Equal("Item not found", result.ErrorMessage);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public void ToActionResult_Success_ReturnsOkObjectResult()
    {
        var result = ServiceResult<string>.Success("Payload");
        var actionResult = result.ToActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Payload", okResult.Value);
    }

    [Fact]
    public void ToActionResult_WithMapper_ProjectsData()
    {
        var result = ServiceResult<int>.Success(42);
        var actionResult = result.ToActionResult(val => new { Mapped = val * 2 });

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void ToActionResult_NotFound_ReturnsNotFoundObjectResult()
    {
        var result = ServiceResult<string>.NotFound("Session not found");
        var actionResult = result.ToActionResult();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public void ToActionResult_BadRequest_ReturnsBadRequestObjectResult()
    {
        var result = ServiceResult<string>.BadRequest("Invalid move");
        var actionResult = result.ToActionResult();

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal("Invalid move", badRequestResult.Value);
    }

    [Fact]
    public void GameSessionManager_CreateLobby_ReturnsSuccessResult()
    {
        var manager = new GameSessionManager();
        var result = manager.CreateLobby("HostAlice", out int hostPlayerId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, hostPlayerId);
        Assert.Equal("HostAlice", result.Data.Players[0].PlayerName);
    }

    [Fact]
    public void GameSessionManager_CreateLobby_EmptyName_ReturnsBadRequest()
    {
        var manager = new GameSessionManager();
        var result = manager.CreateLobby("", out int hostPlayerId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorType.BadRequest, result.ErrorType);
        Assert.Equal(0, hostPlayerId);
    }

    [Fact]
    public void GameSessionManager_JoinLobby_Success_And_NotFound()
    {
        var manager = new GameSessionManager();
        var createResult = manager.CreateLobby("HostAlice", out _);
        var gameId = createResult.Data!.GameId;

        var joinResult = manager.JoinLobby(gameId, "Bob");
        Assert.True(joinResult.IsSuccess);
        Assert.Equal(2, joinResult.Data.NewPlayerId);
        Assert.Equal(2, joinResult.Data.Session.Players.Count);

        var notFoundJoin = manager.JoinLobby("NONEXISTENT", "Bob");
        Assert.False(notFoundJoin.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, notFoundJoin.ErrorType);
    }

    [Fact]
    public void GameSessionManager_StartGame_RequiresMinimum2Players()
    {
        var manager = new GameSessionManager();
        var createResult = manager.CreateLobby("HostAlice", out int hostPlayerId);
        var gameId = createResult.Data!.GameId;

        var startFail = manager.StartGame(gameId, hostPlayerId);
        Assert.False(startFail.IsSuccess);
        Assert.Equal(ServiceErrorType.BadRequest, startFail.ErrorType);

        manager.JoinLobby(gameId, "Bob");
        var startSuccess = manager.StartGame(gameId, hostPlayerId);
        Assert.True(startSuccess.IsSuccess);
        Assert.NotNull(startSuccess.Data);
    }

    [Fact]
    public void GameSessionManager_RemoveLobby_SuccessAndNotFound()
    {
        var manager = new GameSessionManager();
        var createResult = manager.CreateLobby("HostAlice", out _);
        var gameId = createResult.Data!.GameId;

        var removeResult = manager.RemoveLobby(gameId);
        Assert.True(removeResult.IsSuccess);

        var removeAgainResult = manager.RemoveLobby(gameId);
        Assert.False(removeAgainResult.IsSuccess);
        Assert.Equal(ServiceErrorType.NotFound, removeAgainResult.ErrorType);
    }
}

