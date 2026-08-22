namespace DominoWebAPI.DTOs;

using DominoWebAPI.Models;
public class PlayTileRequest
{
    public string GameId { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public DominoTile Tile { get; set; }
    public PlacementSide Side { get; set; }
}