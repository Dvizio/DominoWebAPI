namespace DominoGame;

public class DominoDeck
{
    List<DominoTile> Boneyard;
    int RemainingCount;

    public DominoDeck(List<DominoTile> boneyard, int remainingCount)
    {
        Boneyard = boneyard;
        RemainingCount = remainingCount;
    }
}