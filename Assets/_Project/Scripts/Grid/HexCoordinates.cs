using System;
using UnityEngine;

namespace GothicTactics.Grid
{
    [Serializable]
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        private static readonly HexCoordinates[] Directions =
        {
            new(1, 0), new(1, -1), new(0, -1),
            new(-1, 0), new(-1, 1), new(0, 1)
        };

        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        public HexCoordinates Neighbour(int direction) => this + Directions[direction];

        public int DistanceTo(HexCoordinates other)
        {
            return (Mathf.Abs(Q - other.Q) + Mathf.Abs(R - other.R) + Mathf.Abs(S - other.S)) / 2;
        }

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b) => new(a.Q + b.Q, a.R + b.R);
        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public override string ToString() => $"({Q}, {R})";
    }
}
