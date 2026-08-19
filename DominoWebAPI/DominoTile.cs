namespace DominoGame;

public readonly struct DominoTile
{
    public int Left { get; }
    public int Right { get; }

    public DominoTile(int left, int right)
    {
        Left = left;
        Right = right;
    }
}