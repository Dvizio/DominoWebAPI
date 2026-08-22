namespace DominoWebAPI.Models;

public interface IDominoBoard
{
    public List<DominoTile> PlayedTile { get; set; }
    public List<int> OpenEnds { get; set; }
    bool IsEmpty { get; }
}