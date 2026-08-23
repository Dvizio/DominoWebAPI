using DominoWebAPI.Models;
using Xunit;

namespace DominoWebAPI.Tests;

public class GameLogicTests
{
    [Fact]
    public void PlayTile_AllowsEquivalentTileOrientation_OnEmptyBoard()
    {
        var players = new List<IPlayer>
        {
            new Player(1, "Alice"),
            new Player(2, "Bob"),
            new Player(3, "Carol")
        };

        var game = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
        game.StartGame();

        var current = game.CurrentPlayer;
        game.PlayerHands[current.PlayerId] = new List<DominoTile>
        {
            new DominoTile(0, 4)
        };

        var reversed = new DominoTile(4, 0);
        var played = game.PlayTile(current, reversed, PlacementSide.Right);

        Assert.True(played);
    }
}
