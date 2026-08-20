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
    public GameMode Mode; //readonly
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

    public GameController(List<IPlayer> players, GameMode gameMode, ScoringMethod scoringMethod, int handSize, int targetScore, StartingPlayerRule rule) //first init will be this
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
    public void StartGame() //
    {
        InitializeDeck();
        RoundNumber = 1;
        return;
    }

    public bool CanDraw() //
    {
        //if(true)//
       // DrawFromDeck();
        return true;
    }
    public void DrawTile() // implpemen
    {
        DrawRandomTile(); //langsung ke player hand
        return;
    }

    public List<PlacementSide> CanPlay(DominoTile tile, List<DominoTile> drawnTile) // cek condition apakah bisa ditaro atau ngga
    {
        return ; //Todo
    }

    public void PlayTile(DominoTile tile, PlacementSide side) //ok
    {
        return;
    }
    public void ConfirmNextPlayer() //ok
    {
        return;
    }
    public IPlayer StartNextRound() //ok
    {
        return;
    }
    public void InitializeDeck(int deckSize)
    {
        List<DominoTile> boneyard = new List<DominoTile>();

        for (int left = 0; left <= deckSize; left++)
        {
            for (int right = left; right <= deckSize; right++)
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

    public void DealHands(ref Dictionary<int, List<DominoTile>> playerHands , int handSize) // dealhands untuk nyebarin kartu setiap pemain
    {
        return;
    }
    public void NextPlayer()// might have to return currentplayer
    {
        return;
    }
    public void PassTurn() // if CanPlay return empty list 
    //kalo di  gamemode block dan semua ga bisa jalan, draw
    {
        return;
    }
    public void ClearDrawnTile() //selfexplanatiory
    {
        return;
    }
    public DominoTile DrawFromDeck()
    {
        int rand = RandomNumberGenerator.GetInt32(deck.RemainingCount);
        DominoTile tile = deck.Boneyard[rand];
        return tile;
    }
    public int CalculatePipTotal(int playerID) //round end calculate winner score
    {
        return 1;
    }
    public bool IsRoundBlocked() // draw condition in gamemode block
    {
        return true;
    }
    public int DetermindRoundWinner() //untuk ngecek siapa menang setiap ronde
    {
        return 1;
    }
    public int ResolveBlockedTie() // TBD
    {
        return 1;
    }
    public int CalculateSumMinusWinner(int playerid) // sama kyk calculatepiptotal, exclusive for draw
    {
        return 1;
    }
    public Player DetermindStartingPlayer(StartingPlayerRule rule, List<IPlayer> Players) //tergantung starting player rule lanjut ke CanPLayTile
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