using System;
namespace DominoGame;

public class DominoDeck : IDominoDeck
{
    public List<DominoTile> Boneyard { get; set; }

    public int RemainingCount => Boneyard.Count;

    public DominoDeck(List<DominoTile> boneyard)
    {
        Boneyard = boneyard;
    }
}