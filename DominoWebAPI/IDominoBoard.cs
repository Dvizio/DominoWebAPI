namespace DominoGame;

public interface IDominoBoard
{
    public List<DominoTile> PlayedTile {get;set;}
    public List<int> OpenEnds {get;set;}
    bool IsEmpty { get; }
}