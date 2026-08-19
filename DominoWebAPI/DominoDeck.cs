using System;
namespace DominoGame;

public class DominoDeck : IDominoDeck
{
    public List<DominoTile> Boneyard {get;set;}
    public int RemainingCount {get;set;}

    public DominoDeck(List<DominoTile> boneyard, int remainingCount)
    {
        Boneyard = boneyard;
        RemainingCount = remainingCount;
    }
}