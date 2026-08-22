using System.Data;
namespace DominoWebAPI.Models;

public enum PlacementSide { Left, Right }
public enum GameMode { Block, Draw }
public enum StartingPlayerRule { HighestDouble, PreviousWinner }
// public enum ScoringMethod { SumMinusWinner, SumOfOpponents }
public enum GameState { Playing, WaitingForNextPlayer, RoundOver, GameOver }
