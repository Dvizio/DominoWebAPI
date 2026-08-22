namespace DominoWebAPI.Models;

public interface IDominoDeck
{
    public List<DominoTile> Boneyard { get; set; }
    public int RemainingCount { get; }
}