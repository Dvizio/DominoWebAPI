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
    public List<IPlayer> players;
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

    public void SetPLayerCount(int playerCount)
    {
        return;
    }
    public void SetGameMode(GameMode mode)
    {
        return;
    }
    public void StartGame()
    {
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
    public void InitializeDeck() //initialized randomized deck
    {
        return;
    }
    public void ShuffleDeck() // might be unused since it will be randomized during initialize
    {
        return;
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