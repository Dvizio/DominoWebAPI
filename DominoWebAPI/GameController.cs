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
    public IPlayer CurrentPlayer;
    public GameMode Mode; //readonly
    public GameState Status;
    public int TargetScore = 100;
    public bool isMatchOver;
    public bool isRoundOver;
    IPlayer? RoundWinner;
    int RoundScore;
    public List<int> MatchWinners;
    public Dictionary<int, List<DominoTile>> PlayerHands;
    public Dictionary<int, int> Scores;
    public IDominoBoard Board;
    public IDominoDeck deck;
    public int CurrentPlayerIndex;
    public int ConsecutivePasses;
    public int RoundNumber;
    public int HandSize;
    public StartingPlayerRule FirstStarterRule;
    public DominoTile? DrawnTile;
    public ScoringMethod ScoringMethod;
    public int? NextStarter;

    public GameController(
        List<IPlayer> players,
        GameMode gameMode,
        ScoringMethod scoringMethod,
        int handSize,
        int targetScore,
        StartingPlayerRule rule) //first init will be this
    {
        Mode = gameMode;
        Players = players;
        ScoringMethod = scoringMethod;
        TargetScore = targetScore;
        HandSize = handSize;
        FirstStarterRule = rule;

        InitializeDeck();
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
        RoundNumber = 1;
        CurrentPlayer = DetermineStartingPlayer(PlayerHands, HandSize, FirstStarterRule, Players);
        CurrentPlayerIndex = CurrentPlayer.PlayerId;
        return;
    }

public bool AutoDraw(IPlayer player, List<DominoTile> dominoTiles)
    {
        
    }
    public bool CanDraw() //
    {
        //if(true)//
        // DrawFromDeck();
        return true;
    }
    public void DrawTile(Dictionary<int, List<DominoTile>> playerHands, int currentPlayerId) // implpemen
    {
        playerHands[currentPlayerId].Add(DrawRandomTile());  //langsung ke player hand
        return;
    }

    public bool HelperCheckCanPlayEachPlayer(int playerId){
        List<DominoTile> tempHand = PlayerHands[playerId];
        bool itCan =false;
        foreach (var tile in tempHand)
        {
            List<PlacementSide> check;
            check = CanPlay(tile, Board.PlayedTile);
            if (check == null)
            {
                itCan = true;
            }
        }
        return itCan;
    }

    public List<PlacementSide> CanPlay(DominoTile tile, List<DominoTile> playedTiles) // cek condition apakah bisa ditaro atau ngga
    {
        List<PlacementSide> answer = new List<PlacementSide>();
        int t = playedTiles.Count;
        if(tile.Left == playedTiles[0].Left || tile.Right == playedTiles[0].Left)
        {
            answer.Add(PlacementSide.Left);
        }
        if(tile.Left == playedTiles[t-1].Right || tile.Right == playedTiles[t-1].Right)
        {
            answer.Add(PlacementSide.Right);
        }
        return answer; //Todo
    }

    public void PlayTile(DominoTile tile, PlacementSide side) //ok
    {
        List<DominoTile> playedTile = Board.PlayedTile;
        if(Board.IsEmpty)
        {
            playedTile.Add(tile);
            Board.PlayedTile = playedTile;
            return;
        }
        if(side == PlacementSide.Left)
        {
            if(tile.Left == playedTile[0].Left)
            {
                DominoTile temp = new DominoTile(tile.Right,tile.Left);
                playedTile.Insert(0, temp);
            }
            else
            {
                playedTile.Insert(0, tile);
            }
        }
        if(side == PlacementSide.Right)
        {
             if(tile.Left == playedTile[^1].Right)
            {
                playedTile.Add(tile);
            }
            else
            {
                 DominoTile temp = new DominoTile(tile.Right,tile.Left);
                playedTile.Add(temp);
            }
        }
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

    public DominoTile DrawFromDeck()
    {
        int rand = RandomNumberGenerator.GetInt32(deck.RemainingCount);
        DominoTile tile = deck.Boneyard[rand];
        return tile;
    }

    public void DealHands(ref Dictionary<int, List<DominoTile>> playerHands, int handSize) // dealhands untuk nyebarin kartu setiap pemain
    {
        foreach (var Player in Players)
        {
            List<DominoTile> tempHands = new List<DominoTile>();
            for (int i = 0; i < handSize; i++)
            {
                tempHands.Add(DrawRandomTile());
            }

            playerHands.Add(Player.PlayerId, tempHands);
        }
        return;
    }
    public IPlayer NextPlayer()// might have to return currentplayer
    {
        CurrentPlayerIndex++;
        CurrentPlayerIndex %= Players.Count;
        CurrentPlayer = Players[CurrentPlayerIndex];
        return CurrentPlayer;
    }
    public void PassTurn() // if CanPlay return empty list 
    //kalo di  gamemode block dan semua ga bisa jalan, draw
    {
        NextPlayer();
        return;
    }
    public void ClearDrawnTile() //selfexplanatiory
    {
        InitializeDeck();
        return;
    }

    public int CalculatePipTotal(int playerId) //round end calculate winner score
    {
        List<DominoTile> tempTile = PlayerHands[playerId];
        int tempScore = 0;
        foreach (var tile  in tempTile)
        {
            tempScore += tile.Left;
            tempScore += tile.Right;
        }
        return tempScore;
    }
    public bool IsRoundBlocked() // draw condition in gamemode block
    {
        return true;
    }
    public int DetermineRoundWinner() //untuk ngecek siapa menang setiap ronde
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
    public IPlayer DetermineStartingPlayer(
        ref Dictionary<int, List<DominoTile>> playerHands,
        int handSize,
        StartingPlayerRule rule,
        List<IPlayer> players)
    {
        DealHands(playerHands, handSize);

        if (rule == StartingPlayerRule.PreviousWinner)
        {
            return RoundWinner;
        }

        if (rule == StartingPlayerRule.HighestDouble)
        {
            int winningPlayerId = -1;
            int highestDouble = -1;

            foreach (var hand in playerHands)
            {
                int playerId = hand.Key;
                List<DominoTile> tiles = hand.Value;

                foreach (DominoTile tile in tiles)
                {
                    if (tile.Left == tile.Right)
                    {
                        if (tile.Left > highestDouble)
                        {
                            highestDouble = tile.Left;
                            winningPlayerId = playerId;
                        }
                    }
                }
            }

            if (winningPlayerId != -1)
            {
                return players.First(p => p.PlayerId == winningPlayerId);
            }
        }
        int randomIndex = random.Next(players.Count);
        return players[randomIndex];
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