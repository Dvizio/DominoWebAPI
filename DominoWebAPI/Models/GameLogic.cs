using System;
using System.Data;
namespace DominoWebAPI.Models;


public class GameLogic
{
    private Random _random = new Random();
    public List<IPlayer> Players { get; private set; }
    public IPlayer CurrentPlayer { get; private set; }
    public GameMode Mode { get; }
    public GameState Status { get; private set; }
    public int TargetScore { get; }
    public IPlayer? RoundWinner { get; private set; }
    public Dictionary<int, List<DominoTile>> PlayerHands { get; private set; } = new();
    public Dictionary<int, int> Scores { get; private set; } = new();
    public IDominoBoard Board { get; private set; }
    public IDominoDeck Deck { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public int ConsecutivePasses { get; private set; }
    public int RoundNumber { get; private set; }
    public int HandSize { get; }
    public int DeckSize { get; }
    public StartingPlayerRule FirstStarterRule { get; private set; }
    // public ScoringMethod ScoringMethod { get; }
    public int? NextStarter;

    public GameLogic(
        List<IPlayer> players,
        GameMode gameMode,
        // ScoringMethod scoringMethod,
        int handSize,
        int targetScore,
        int deckSize,
        StartingPlayerRule rule) //first init will be this
    {
        Mode = gameMode;
        Players = players;
        // ScoringMethod = scoringMethod;
        TargetScore = targetScore;
        HandSize = handSize;
        FirstStarterRule = rule;
        DeckSize = deckSize;

        Scores = new Dictionary<int, int>();
        PlayerHands = new Dictionary<int, List<DominoTile>>();
        foreach (var player in Players)
        {
            Scores[player.PlayerId] = 0;
            PlayerHands[player.PlayerId] = new List<DominoTile>();
        }
        // Board = new DominoBoard(new List<DominoTile>(), new List<int>());
    }
    public void StartGame() //
    {
        RoundNumber = 1;
        StartRound();
    }

    public void StartRound()
    {
        InitializeDeck(DeckSize);
        Board = new DominoBoard(new List<DominoTile>(), new List<int>());
        ConsecutivePasses = 0;
        Console.WriteLine("Dealing Hands!");
        DealHands(HandSize);
        CurrentPlayer = DetermineStartingPlayer(FirstStarterRule);
        CurrentPlayerIndex = Players.IndexOf(CurrentPlayer);
        Status = GameState.Playing;
    }
    public void StartNextRound()
    {
        if (Status == GameState.GameOver)
            return;
        if (Status != GameState.RoundOver)
            return;

        RoundNumber++;
        StartRound();
    }

    public bool AutoDrawToPlayerHand(IPlayer player)
    {
        if (CanDraw())
        {
            if (DrawRandomTile() is DominoTile tile)
            {
                PlayerHands[player.PlayerId].Add(tile);
                return true;
            }
        }
        return false;
    }
    public bool CanDraw()
    {
        return Deck != null && Deck.Boneyard != null && Deck.Boneyard.Count > 0;
        // if (Mode == GameMode.Block)
        // {
        //     return false;
        // }
        // else
        // {
        //     if (Deck.RemainingCount == 0)
        //     {
        //         return false;
        //     }
        //     else
        //     {
        //         return true;
        //     }
        // }

    }
    public bool CanPlayerMakeAnyMove(int playerId)
    {
        foreach (var tile in PlayerHands[playerId])
        {
            var validSides = CanPlay(tile, Board.PlayedTile);

            if (validSides.Count > 0)
                return true;
        }

        return false;
    }

    public List<PlacementSide> CanPlay(DominoTile tile, List<DominoTile> playedTiles) // cek condition apakah bisa ditaro atau ngga
    {
        List<PlacementSide> answer = new List<PlacementSide>();
        int t = playedTiles.Count;
        if (t == 0)
        {
            answer.Add(PlacementSide.Left);
            answer.Add(PlacementSide.Right);
            return answer;
        }
        if (tile.Left == playedTiles[0].Left || tile.Right == playedTiles[0].Left)
        {
            answer.Add(PlacementSide.Left);
        }
        if (tile.Left == playedTiles[t - 1].Right || tile.Right == playedTiles[t - 1].Right)
        {
            answer.Add(PlacementSide.Right);
        }
        return answer; //Todo
    }

    public bool PlayTile(IPlayer player, DominoTile tile, PlacementSide side) //ok
    {
        List<DominoTile> playedTile = Board.PlayedTile;
        if (player.PlayerId != CurrentPlayer.PlayerId)
            return false;

        var hand = PlayerHands[player.PlayerId];
        if (!hand.Contains(tile))
            return false;

        var validSides = CanPlay(tile, playedTile);
        if (!validSides.Contains(side))
            return false;

        hand.Remove(tile);
        if (Board.IsEmpty)
        {
            playedTile.Add(tile);
            Board.PlayedTile = playedTile;
        }
        else if (side == PlacementSide.Left)
        {
            if (tile.Left == playedTile[0].Left)
            {
                DominoTile temp = new DominoTile(tile.Right, tile.Left);
                playedTile.Insert(0, temp);
            }
            else
            {
                playedTile.Insert(0, tile);
            }
        }
        else if (side == PlacementSide.Right)
        {
            if (tile.Left == playedTile[^1].Right)
            {
                playedTile.Add(tile);
            }
            else
            {
                DominoTile temp = new DominoTile(tile.Right, tile.Left);
                playedTile.Add(temp);
            }
        }
        ConsecutivePasses = 0;

        if (hand.Count == 0 || CheckRoundEndCondition())
        {
            EndRound();
        }
        else
        {
            NextPlayer();
        }

        return true;
    }

    private bool CheckRoundEndCondition()
    {
        return ConsecutivePasses >= Players.Count;
    }

    public void EndRound()
    {
        Status = GameState.RoundOver;
        int winnerId = DetermineRoundWinner();

        if (winnerId != -1)
        {
            RoundWinner = Players.First(p => p.PlayerId == winnerId);
            int roundScore = CalculateSumOfOpponents(winnerId);
            Scores[winnerId] += roundScore;
            FirstStarterRule = StartingPlayerRule.PreviousWinner;
            if (Scores[winnerId] >= TargetScore)
            {
                Status = GameState.GameOver;
            }
        }
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

        Deck = new DominoDeck(boneyard);
    }

    public DominoTile? DrawRandomTile()
    {
        if (CanDraw())
        {
            int index = _random.Next(Deck.Boneyard.Count);

            DominoTile drawnTile = Deck.Boneyard[index];
            Deck.Boneyard.RemoveAt(index);

            return drawnTile;
        }
        return null;
    }

    private void DealHands(int handSize)
    {
        foreach (var player in Players)
        {
            PlayerHands[player.PlayerId] = new List<DominoTile>();
            for (int i = 0; i < handSize; i++)
            {
                if (DrawRandomTile() is DominoTile tile)
                {
                    PlayerHands[player.PlayerId].Add(tile);
                }
            }
        }
    }
    public IPlayer NextPlayer()// might have to return currentplayer
    {
        CurrentPlayerIndex++;
        CurrentPlayerIndex %= Players.Count;
        CurrentPlayer = Players[CurrentPlayerIndex];
        return CurrentPlayer;
    }
    public void PassTurn(IPlayer player)
    {
        if (player.PlayerId != CurrentPlayer.PlayerId) return;

        ConsecutivePasses++;
        if (CheckRoundEndCondition())
        {
            EndRound();
        }
        else
        {
            NextPlayer();
        }
    }

    public int CalculatePipTotal(int playerId) //round end calculate winner score
    {
        List<DominoTile> tempTile = PlayerHands[playerId];
        int tempScore = 0;
        foreach (var tile in tempTile)
        {
            tempScore += tile.Left;
            tempScore += tile.Right;
        }
        return tempScore;
    }
    public bool IsRoundBlocked() // draw condition in gamemode block
    {
        if (Mode == GameMode.Block)
        {
            return true;
        }
        return false;
    }
    public int DetermineRoundWinner()
    {
        int WinnerId = -1;
        int lowestTileLength = int.MaxValue;
        int lowestPip = int.MaxValue;
        foreach (var player in Players)
        {
            int tempPip = CalculatePipTotal(player.PlayerId);
            int tempTileLength = PlayerHands[player.PlayerId].Count;
            if (tempPip < lowestPip || (tempPip == lowestPip && tempTileLength < lowestTileLength))
            {
                lowestPip = tempPip;
                lowestTileLength = tempTileLength;
                WinnerId = player.PlayerId;
                RoundWinner = player;

            }
        }
        return WinnerId;
    }
    public int CalculateSumMinusWinner(int player1, int player2) // sama kyk calculatepiptotal, exclusive for draw, might be not needed
    {
        int a = CalculatePipTotal(player1);
        int b = CalculatePipTotal(player2);
        return Math.Abs(a - b);
    }
    public int CalculateSumOfOpponents(int winnerId)
    {
        int calculateScore = 0;
        foreach (var player in Players)
        {
            if (player.PlayerId != winnerId)
            {
                calculateScore += CalculatePipTotal(player.PlayerId);
            }
        }
        calculateScore -= CalculatePipTotal(winnerId);
        return calculateScore;
    }
    public IPlayer DetermineStartingPlayer(StartingPlayerRule rule)
    {
        if (rule == StartingPlayerRule.PreviousWinner && RoundWinner != null)
        {
            return RoundWinner;
        }

        if (rule == StartingPlayerRule.HighestDouble)
        {
            IPlayer? highestDoubleOwner = null;
            int highestDouble = -1;

            foreach (var (playerId, hand) in PlayerHands)
            {
                foreach (var tile in hand.Where(t => t.Left == t.Right))
                {
                    if (tile.Left > highestDouble)
                    {
                        highestDouble = tile.Left;
                        highestDoubleOwner = Players.First(p => p.PlayerId == playerId);
                    }
                }
            }

            if (highestDoubleOwner != null) return highestDoubleOwner;
        }

        return Players[_random.Next(Players.Count)];
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