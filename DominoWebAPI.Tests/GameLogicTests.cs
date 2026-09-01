namespace DominoWebAPI.Tests;

using NUnit.Framework;
using DominoWebAPI.Models;

public class GameLogicTests
{
    private GameLogic _gameLogic;
    private List<IPlayer> players;

    [SetUp]
    public void Setup()
    {
        players = new List<IPlayer>
        {
            new Player(1, "Alice"),
            new Player(2, "Bob"),
            new Player(3, "Carol")
        };

        _gameLogic = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameLogic.StartGame();
    }

    [Test]
    public void Constructor_InitializesGameCorrectly()
    {
        Assert.That(_gameLogic.Players, Is.EqualTo(players));
        Assert.That(_gameLogic.Mode, Is.EqualTo(GameMode.Draw));
        Assert.That(_gameLogic.HandSize, Is.EqualTo(7));
        Assert.That(_gameLogic.TargetScore, Is.EqualTo(100));
        Assert.That(_gameLogic.DeckSize, Is.EqualTo(6));

    }
    [Test]
    public void Constructor_InitializesPlayerScoresToZero()
    {
        Assert.That(_gameLogic.Scores.Count, Is.EqualTo(3));

        foreach (var player in _gameLogic.Players)
        {
            Assert.That(_gameLogic.Scores[player.PlayerId], Is.EqualTo(0));
        }
    }

    [Test]
    public void StartGame_SetsCurrentPlayerCorrectly()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        Assert.That(currentPlayer, Is.Not.Null);
        Assert.That(players.Contains(currentPlayer), Is.True);
        Assert.That(_gameLogic.Status == GameState.Playing, Is.True);
        foreach (var player in players)
        {
            Assert.That(_gameLogic.PlayerHands[player.PlayerId].Count, Is.EqualTo(7));
        }
    }

    [Test]
    public void StartNextRound_WhenRoundIsOver_StartsNextRound()
    {
        var initialRoundNumber = _gameLogic.RoundNumber;
        _gameLogic.EndRound();

        Assert.That(_gameLogic.Status, Is.EqualTo(GameState.RoundOver));
        _gameLogic.StartNextRound();

        Assert.That(
            _gameLogic.RoundNumber,
            Is.EqualTo(initialRoundNumber + 1));

        Assert.That(
            _gameLogic.Status,
            Is.EqualTo(GameState.Playing));
    }


    [Test]
    public void CanDraw_GameModeDraw_ReturnsTrue()
    {
        var canDraw = _gameLogic.CanDraw();
        Assert.That(canDraw, Is.True);
    }

    [Test]
    public void CanDraw_GameModeBlock_ReturnsFalse()
    {
        var _gameBlockLogic = new GameLogic(players, GameMode.Block, 7, 100, 6, StartingPlayerRule.HighestDouble);
        var canDraw = _gameBlockLogic.CanDraw();
        Assert.That(canDraw, Is.False);
    }

    [Test]
    public void AutoDrawToPlayerHand_PlayerIsCurrentPlayer_DrawsTile()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var initialHandCount = _gameLogic.PlayerHands[currentPlayer.PlayerId].Count;

        var result = _gameLogic.AutoDrawToPlayerHand(currentPlayer);

        Assert.That(result, Is.True);
        Assert.That(_gameLogic.PlayerHands[currentPlayer.PlayerId].Count, Is.EqualTo(initialHandCount + 1));
    }

    [Test]
    public void AutoDrawToPlayerHand_PlayerIsNotCurrentPlayer_DoesNotDrawTile()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var otherPlayer = players.First(p => p.PlayerId != currentPlayer.PlayerId);
        var initialHandCount = _gameLogic.PlayerHands[otherPlayer.PlayerId].Count;

        var result = _gameLogic.AutoDrawToPlayerHand(otherPlayer);

        Assert.That(result, Is.False);
        Assert.That(_gameLogic.PlayerHands[otherPlayer.PlayerId].Count, Is.EqualTo(initialHandCount));
    }

    [Test]
    public void AutoDrawToPlayerHand_CannotDraw_ReturnsFalse()
    {
        // Simulate a situation where drawing is not allowed
        var _gameBlockLogic = new GameLogic(players, GameMode.Block, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameBlockLogic.StartGame();
        var currentPlayer = _gameBlockLogic.CurrentPlayer;

        var result = _gameBlockLogic.AutoDrawToPlayerHand(currentPlayer);

        Assert.That(result, Is.False);
    }

    [Test]
    public void DrawRandomTile_ReturnsTileAndRemovesFromDeck()
    {
        var initialDeckCount = _gameLogic.Deck.RemainingCount;

        var tile = _gameLogic.DrawRandomTile();

        Assert.That(tile, Is.Not.Null);
        Assert.That(_gameLogic.Deck.RemainingCount, Is.EqualTo(initialDeckCount - 1));
    }

    [Test]
    public void DrawRandomTile_DeckIsEmpty_ReturnsNull()
    {
        var _gameDrawLogic = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameDrawLogic.StartGame();
        while (_gameDrawLogic.Deck.RemainingCount > 0)
        {
            _gameDrawLogic.DrawRandomTile();
        }

        var tile = _gameDrawLogic.DrawRandomTile();

        Assert.That(tile, Is.Null);
    }

    [Test]
    public void IsRoundBlock_ReturnsTrueWhenRoundIsBlock()
    {
        var _gameBlockLogic = new GameLogic(players, GameMode.Block, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameBlockLogic.StartGame();

        var isRoundBlock = _gameBlockLogic.IsRoundBlocked();

        Assert.That(isRoundBlock, Is.True);
    }

    [Test]
    public void IsRoundBlock_ReturnsFalseWhenRoundIsNotBlock()
    {
        var _gameDrawLogic = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameDrawLogic.StartGame();

        var isRoundBlock = _gameDrawLogic.IsRoundBlocked();

        Assert.That(isRoundBlock, Is.False);
    }

    [Test]
    public void DealHands_DealsCorrectNumberOfTilesToEachPlayer()
    {
        var _gameDealLogic = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameDealLogic.StartGame();

        foreach (var player in players)
        {
            Assert.That(_gameDealLogic.PlayerHands[player.PlayerId].Count, Is.EqualTo(7));
        }
    }

    [Test]
    public void DealHands_DeckHasCorrectNumberOfRemainingTiles()
    {
        var _gameDealLogic = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        _gameDealLogic.StartGame();

        var expectedRemainingCount = _gameDealLogic.Deck.RemainingCount;
        Assert.That(expectedRemainingCount, Is.EqualTo(28 - (players.Count * 7)));
    }

    [Test]
    public void NextPlayer_MovesToNextPlayer()
    {
        _gameLogic.StartGame();

        var originalIndex = _gameLogic.CurrentPlayerIndex;

        var nextPlayer = _gameLogic.NextPlayer();

        var expectedIndex =
            (originalIndex + 1) % players.Count;

        Assert.That(
            _gameLogic.CurrentPlayerIndex,
            Is.EqualTo(expectedIndex)
        );

        Assert.That(
            nextPlayer,
            Is.SameAs(players[expectedIndex])
        );
    }

    [Test]
    public void NextPlayer_WrapsAroundAtEnd()
    {
        _gameLogic.StartGame();
        var currentPlayerIndex = _gameLogic.CurrentPlayerIndex;
        for (int i = currentPlayerIndex; i < players.Count; i++)
        {
            _gameLogic.NextPlayer();
        }

        Assert.That(_gameLogic.CurrentPlayerIndex, Is.EqualTo(0));
        Assert.That(_gameLogic.CurrentPlayer, Is.SameAs(players[0]));
    }

    [Test]
    public void PassTurn_ChangesCurrentPlayerToNextPlayer()
    {
        var initialCurrentPlayer = _gameLogic.CurrentPlayer;

        _gameLogic.PassTurn(initialCurrentPlayer);

        var newCurrentPlayer = _gameLogic.CurrentPlayer;
        Assert.That(newCurrentPlayer, Is.Not.EqualTo(initialCurrentPlayer));
        Assert.That(_gameLogic.Players.Contains(newCurrentPlayer), Is.True);
    }

    [Test]
    public void PassTurn_DoesNotChangeCurrentPlayerIfPlayerIsNotCurrent()
    {
        var initialCurrentPlayer = _gameLogic.CurrentPlayer;
        var otherPlayer = players.First(p => p.PlayerId != initialCurrentPlayer.PlayerId);
        _gameLogic.PassTurn(otherPlayer);

        Assert.That(_gameLogic.CurrentPlayer, Is.SameAs(initialCurrentPlayer));
    }

    [Test]
    public void CanPlay_ReturnsTrueWhenPlayerCanPlay()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var canPlay = _gameLogic.CanPlay(_gameLogic.PlayerHands[currentPlayer.PlayerId].First(), _gameLogic.Board.PlayedTile);

        Assert.That(canPlay, Is.Not.Null);
    }

    [Test]
    public void PlayTile_UpdatesBoardAndRemovesTileFromPlayerHand()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var tileToPlay = _gameLogic.PlayerHands[currentPlayer.PlayerId].First();

        _gameLogic.PlayTile(currentPlayer, tileToPlay, PlacementSide.Left);

        Assert.That(_gameLogic.Board.PlayedTile, Does.Contain(tileToPlay));
        Assert.That(_gameLogic.PlayerHands[currentPlayer.PlayerId], Does.Not.Contain(tileToPlay));
    }

    [Test]
    public void PlayTile_NotCurrentPlayer_ReturnsFalse()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var otherPlayer = players.First(p => p.PlayerId != currentPlayer.PlayerId);
        var tileToPlay = _gameLogic.PlayerHands[currentPlayer.PlayerId].First();

        Assert.That(_gameLogic.PlayTile(otherPlayer, tileToPlay, PlacementSide.Left), Is.False);
    }

    [Test]
    public void PlayTile_WhenTileIsNotInPlayerHand_ReturnsFalse()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var tileNotInHand = new DominoTile(7, 7);

        Assert.That(_gameLogic.PlayTile(currentPlayer, tileNotInHand, PlacementSide.Left), Is.False);
    }

    [Test]
    public void PlayTile_WhenTileCannotBePlayed_ReturnsFalse()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var tileToPlay = _gameLogic.PlayerHands[currentPlayer.PlayerId].First();

        _gameLogic.Board.PlayedTile = new List<DominoTile> { new DominoTile(66, 66) };

        Assert.That(_gameLogic.PlayTile(currentPlayer, tileToPlay, PlacementSide.Left), Is.False);
    }

    [Test]
    public void PlayTile_LeftSideMatchingOrientation_PlacesTileWithoutRotation()
    {

        var currentPlayer = _gameLogic.CurrentPlayer;
        var boardTile = new DominoTile(6, 3);
        var tileToPlay = new DominoTile(2, 6);

        _gameLogic.Board.PlayedTile = new List<DominoTile>
    {
        boardTile
    };

        _gameLogic.PlayerHands[currentPlayer.PlayerId] = new List<DominoTile>
    {
        tileToPlay
    };

        var result = _gameLogic.PlayTile(
            currentPlayer,
            tileToPlay,
            PlacementSide.Left);

        Assert.That(result, Is.True);

        Assert.That(
            _gameLogic.Board.PlayedTile[0].Left,
            Is.EqualTo(2));

        Assert.That(
            _gameLogic.Board.PlayedTile[0].Right,
            Is.EqualTo(6));
    }

    [Test]
    public void PlayTile_LeftSideNeedsRotation_RotatesTile()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var boardTile = new DominoTile(6, 3);
        var tileToPlay = new DominoTile(6, 2);

        _gameLogic.Board.PlayedTile = new List<DominoTile>
    {
        boardTile
    };

        _gameLogic.PlayerHands[currentPlayer.PlayerId] = new List<DominoTile>
    {
        tileToPlay
    };


        var result = _gameLogic.PlayTile(
            currentPlayer,
            tileToPlay,
            PlacementSide.Left);

        Assert.That(result, Is.True);

        var placedTile = _gameLogic.Board.PlayedTile[0];

        Assert.That(placedTile.Left, Is.EqualTo(2));
        Assert.That(placedTile.Right, Is.EqualTo(6));
    }

    [Test]
    public void PlayTile_RightSide_NeedsRotation_RotatesTile()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var boardTile = new DominoTile(3, 6);
        var tileToPlay = new DominoTile(2, 6);

        _gameLogic.Board.PlayedTile = new List<DominoTile>
    {
        boardTile
    };

        _gameLogic.PlayerHands[currentPlayer.PlayerId] = new List<DominoTile>
    {
        tileToPlay
    };


        var result = _gameLogic.PlayTile(
            currentPlayer,
            tileToPlay,
            PlacementSide.Right);

        Assert.That(result, Is.True);

        var placedTile = _gameLogic.Board.PlayedTile[^1];

        Assert.That(placedTile.Left, Is.EqualTo(6));
        Assert.That(placedTile.Right, Is.EqualTo(2));
    }

    [Test]
    public void PlayTile_Success_ResetsConsecutivePasses()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        _gameLogic.PassTurn(currentPlayer);
        currentPlayer = _gameLogic.CurrentPlayer;
        var tile = _gameLogic.PlayerHands[currentPlayer.PlayerId].First();
        _gameLogic.Board.PlayedTile = new List<DominoTile>();

        var result = _gameLogic.PlayTile(
            currentPlayer,
            tile,
            PlacementSide.Left);

        Console.WriteLine(tile);
        Console.WriteLine(_gameLogic.Board.PlayedTile[0]);
        Assert.That(result, Is.True);
        Assert.That(_gameLogic.ConsecutivePasses, Is.EqualTo(0));
    }

    [Test]
    public void CanPlayerMakeAnyMove_WhenPlayerHasPlayableTile_ReturnsTrue()
    {
        var player = players[0];

        _gameLogic.Board.PlayedTile = new List<DominoTile>
    {
        new DominoTile(6, 3)
    };

        _gameLogic.PlayerHands[player.PlayerId] = new List<DominoTile>
    {
        new DominoTile(2, 6),
        new DominoTile(1, 1)
    };

        var result = _gameLogic.CanPlayerMakeAnyMove(player.PlayerId);
        Assert.That(result, Is.True);
    }



    [Test]
    public void CheckRoundEnd_ReturnsTrueWhenRoundIsOver()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;

        _gameLogic.PassTurn(currentPlayer);
        currentPlayer = _gameLogic.CurrentPlayer;
        _gameLogic.PassTurn(currentPlayer);
        currentPlayer = _gameLogic.CurrentPlayer;
        _gameLogic.PassTurn(currentPlayer);

        var isRoundOver = _gameLogic.Status == GameState.RoundOver;

        Assert.That(isRoundOver, Is.True);
    }

    [Test]
    public void CalculatePipTotal_ReturnsCorrectSumOfPips()
    {
        var currentPlayer = _gameLogic.CurrentPlayer;
        var hand = _gameLogic.PlayerHands[currentPlayer.PlayerId];

        int expectedPipTotal = hand.Sum(tile => tile.Left + tile.Right);
        int actualPipTotal = _gameLogic.CalculatePipTotal(currentPlayer.PlayerId);

        Assert.That(actualPipTotal, Is.EqualTo(expectedPipTotal));
    }

    [Test]
    public void DetermineRoundWinner_PlayerWithLowestPipTotalWins()
    {
        _gameLogic.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        _gameLogic.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(1, 1)
    };

        _gameLogic.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(3, 3)
    };

        var winnerId = _gameLogic.DetermineRoundWinner();

        Assert.That(winnerId, Is.EqualTo(2));
        Assert.That(_gameLogic.RoundWinner, Is.SameAs(players[1]));
    }

    [Test]
    public void DetermineRoundWinner_TieOnPips_PlayerWithFewerTilesWins()
    {
        _gameLogic.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(2, 2)
    };

        _gameLogic.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(1, 1),
        new DominoTile(0, 2)
    };

        _gameLogic.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        var winnerId = _gameLogic.DetermineRoundWinner();

        Assert.That(winnerId, Is.EqualTo(1));
    }

    [Test]
    public void EndRound_SetsStatusToRoundOver()
    {
        _gameLogic.StartGame();

        _gameLogic.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(1, 1)
    };

        _gameLogic.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        _gameLogic.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(5, 5)
    };

        _gameLogic.PlayerHands[4] = new List<DominoTile>
    {
        new DominoTile(4, 4)
    };

        _gameLogic.EndRound();

        Assert.That(
            _gameLogic.Status,
            Is.EqualTo(GameState.RoundOver)
        );
    }

    [Test]
    public void EndRound_SetsRoundWinner()
    {
        _gameLogic.StartGame();

        _gameLogic.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(1, 1)
    };

        _gameLogic.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        _gameLogic.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(5, 5)
    };

        _gameLogic.PlayerHands[4] = new List<DominoTile>
    {
        new DominoTile(4, 4)
    };

        _gameLogic.EndRound();

        Assert.That(
            _gameLogic.RoundWinner!.PlayerId,
            Is.EqualTo(1)
        );
    }

    [Test]
    public void EndRound_WhenTargetScoreReached_RaisesGameOverEvent()
    {
        var game = new GameLogic(
            players,
            GameMode.Draw,
            7,
            targetScore: 10,
            deckSize: 6,
            StartingPlayerRule.HighestDouble
        );

        game.StartGame();

        // Give player 1 the lowest hand.
        game.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(0, 0)
    };

        game.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        game.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        // Arrange
        game.Scores[1] = 9;

        bool eventRaised = false;

        game.GameStateGameOver += () =>
        {
            eventRaised = true;
        };

        game.EndRound();

        Assert.That(eventRaised, Is.True);
        Assert.That(game.Status, Is.EqualTo(GameState.GameOver));
    }

    [Test]
    public void DetermineStartingPlayer_PreviousWinner_PrioritizesPreviousWinnerOverHighestDouble()
    {
        // Arrange
        var previousWinner = players[1]; // Bob

        _gameLogic.PlayerHands[1] = new List<DominoTile>
    {
        new DominoTile(6, 6)
    };

        _gameLogic.PlayerHands[2] = new List<DominoTile>
    {
        new DominoTile(1, 1)
    };

        _gameLogic.PlayerHands[3] = new List<DominoTile>
    {
        new DominoTile(5, 5)
    };

        _gameLogic.EndRound();

        var startingPlayer = _gameLogic.DetermineStartingPlayer(
            StartingPlayerRule.PreviousWinner
        );

        Assert.That(_gameLogic.RoundWinner, Is.SameAs(previousWinner));
        Assert.That(startingPlayer, Is.SameAs(previousWinner));
    }


}