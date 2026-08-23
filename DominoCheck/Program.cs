using DominoWebAPI.Models;

var players = new List<IPlayer>
{
    new Player(1, "A"),
    new Player(2, "B"),
    new Player(3, "C")
};
var game = new GameLogic(players, GameMode.Draw, 7, 100, 6, StartingPlayerRule.HighestDouble);
game.StartGame();
var target = new DominoTile(0,4);
var current = game.CurrentPlayer;
Console.WriteLine($"Current={current.PlayerId}");
var hand = game.PlayerHands[current.PlayerId];
Console.WriteLine(string.Join(" | ", hand.Select(t => $"{t.Left}-{t.Right}")));
Console.WriteLine(hand.Contains(target));
Console.WriteLine(game.CanPlay(target, game.Board.PlayedTile).Count);
Console.WriteLine(game.PlayTile(current, target, PlacementSide.Right));
Console.WriteLine(game.CurrentPlayer.PlayerId);
