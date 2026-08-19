namespace DominoGame;

public class DominoBoard : IDominoBoard
{
    public List<DominoTile> PlayedTile {get;set;}
    public List<int> OpenEnds {get;set;}
    public bool IsEmpty => PlayedTile.Count == 0;
}