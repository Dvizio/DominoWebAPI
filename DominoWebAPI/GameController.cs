using System;
using System.Security.Cryptography;
namespace DominoGame;

public enum PlacementSide
{
    Left,
    Right
}

public enum GameMode
{
    Block,
    Draw
}

public enum StartingPlayerRule
{
    Random,
    HighestDouble, PreviousWinner
}
public enum ScoringMethod
{
    SumMinusWinner, SumOfOpponents
}

public enum GameState
{
    Playing, WaitingForNextPlayer
}

public class GameController
{
    private Random random = new Random();
    public List<IPlayer> Players;
    public IPlayer currentplayer;
    public GameMode Mode;
    public GameState Status;
    public int TargetScore = 100;
    public bool isMatchOver;
    public bool isRoundOver;
    int? RoundWinner;
    int RoundScore;
    public List<int> MatchWinners;
    public Dictionary<int, List<DominoTile>> playerHands;
    public Dictionary<int, int> Scores;
    public IDominoBoard Board;
    public IDominoDeck deck;
    public int CurrentPlayerIndex;
    public int ConsecutivePasses;
    public int RoundNumber;
    public int HandSize;
    public StartingPlayerRule firstStarter;
    public DominoTile? DrawnTile;
    public ScoringMethod ScoringMethod;
    public int? NextStarter;

    public GameController(List<IPlayer> players, GameMode gameMode, ScoringMethod scoringMethod)
    {
        Mode = gameMode; Players = players; ScoringMethod = scoringMethod;
    }

    public void SetPLayerCount(int playerCount) //i dont think this is needed
    {
        return;
    }
    public void SetGameMode(GameMode mode) // not needed as well? properties soalnya
    {
        return;
    }
    public void StartGame()
    {
        InitializeDeck();
        RoundNumber = 1;
        return;
    }

    public bool CanDraw()
    {
        return true;
    }
    public void DrawTile()
    {
        return;
    }

    public bool CanPlay(DominoTile tile, PlacementSide side)
    {
        return true;
    }

    public void PlayTile(DominoTile tile, PlacementSide side)
    {
        return;
    }
    public void ConfirmNextPlayer()
    {
        return;
    }
    public void StartNextRound()
    {
        return;
    }
    public void InitializeDeck()
    {
        List<DominoTile> boneyard = new List<DominoTile>();

        for (int left = 0; left <= 6; left++)
        {
            for (int right = left; right <= 6; right++)
            {
                boneyard.Add(new DominoTile(left, right));
            }
        }

        deck = new DominoDeck(boneyard);
    }
    public void ShuffleDeck() // might be unused since it will be randomized during initialize
    {
        return;
    }

    public DominoTile DrawRandomTile()
    {
        int index = random.Next(deck.Boneyard.Count);

        DominoTile drawnTile = deck.Boneyard[index];
        deck.Boneyard.RemoveAt(index);

        return drawnTile;
    }

    public void DealHands() // might have to return list<DominoTiles> to first initiate everyplayer hands
    {
        return;
    }
    public void NextPlayer()// might have to return currentplayer
    {
        return;
    }
    public void PassTurn() // might have to joined into NextPlayer instead
    {
        return;
    }
    public void ClearDrawnTile()
    {
        return;
    }
    public DominoTile DrawFromDeck()
    {
        int rand = RandomNumberGenerator.GetInt32(deck.RemainingCount);
        DominoTile tile = deck.Boneyard[rand];
        return tile;
    }
    public int CalculatePipTotal(int playerID)
    {
        return 1;
    }
    public bool IsRoundBlocked()
    {
        return true;
    }
    public int DetermindRoundWinner()
    {
        return 1;
    }
    public int ResolveBlockedTile()
    {
        return 1;
    }
    public int CalculateSumMinusWinner(int playerid)
    {
        return 1;
    }
    public int DetermindStartingPlayer()
    {
        return 1;
    }

    public void CompleteRound()
    {
        return;
    }
    public bool IsMatchFinished()
    {
        return false;
    }

    // +event TurnChanged

    // +event BoardChanged

    // +event PlayerHandChanged

    // +event ScoreChanged

    // +event GameStateChanged

    // +event RoundCompleted

    // +event MatchCompleted



}

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