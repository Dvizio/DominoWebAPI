namespace DominoWebAPI.Models;

public class DominoBoard : IDominoBoard
{
    public List<DominoTile> PlayedTile { get; set; }
    public List<int> OpenEnds { get; set; }
    public bool IsEmpty => PlayedTile.Count == 0;

    public DominoBoard(List<DominoTile> playedTile, List<int> openEnds)
    {
        PlayedTile = playedTile; OpenEnds = openEnds;
    }
}