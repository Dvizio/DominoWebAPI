using System;
using System.Data;
using System.Text.Json.Serialization;
namespace DominoWebAPI.Models;
public struct DominoTile : IEquatable<DominoTile>
{
    [JsonPropertyName("left")]
    public int Left { get; set; }
    [JsonPropertyName("right")]
    public int Right { get; set; }

    public DominoTile(int left, int right)
    {
        Left = left;
        Right = right;
    }

    public bool Equals(DominoTile other)
    {
        return (Left == other.Left && Right == other.Right)
            || (Left == other.Right && Right == other.Left);
    }

    public override bool Equals(object? obj)
    {
        return obj is DominoTile other && Equals(other);
    }

    public override int GetHashCode()
    {
        var first = Math.Min(Left, Right);
        var second = Math.Max(Left, Right);
        return HashCode.Combine(first, second);
    }

    public static bool operator ==(DominoTile left, DominoTile right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DominoTile left, DominoTile right)
    {
        return !left.Equals(right);
    }
}