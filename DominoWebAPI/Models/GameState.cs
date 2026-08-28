using System.Data;
namespace DominoWebAPI.Models;


// public enum ScoringMethod { SumMinusWinner, SumOfOpponents }
public enum GameState { Playing, WaitingForNextPlayer, RoundOver, GameOver }
