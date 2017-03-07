using System;
using System.Collections.Generic;
using System.Linq;

namespace Halite
{
    public enum Direction
    {
        Still = 0,
        North = 1,
        East = 2,
        South = 3,
        West = 4
    }

    public class Site
    {
        public ushort Owner { get; internal set; }
        public ushort Strength { get; internal set; }
        public ushort Production { get; internal set; }
        public double BasicValue => (double)Production / (double)(Strength == 0 ? 1 : Strength);

        public int X { get; }
        public int Y { get; }

        public Site Top { get; set; }
        public Site Bottom { get; set; }
        public Site Left { get; set; }
        public Site Right { get; set; }

        public List<Site> Neighbors => new List<Site> { Top, Bottom, Left, Right };
        public List<Site> ThreatZone => new List<Site> { Top, Bottom, Left, Right, this };

        public List<Site> GetDangerousSites(List<Site> otherThreatZone)
        {
            var result = new List<Site>();
            foreach(var otherSite in otherThreatZone)
            {
                if(ThreatZone.Contains(otherSite))
                {
                    otherSite.Neighbors.ForEach(n => result.Add(n));
                }
            }
            return result;
        }

        public Site(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Site GetNeighborAtDirection(Direction d)
        {
            switch(d)
            {
                case Direction.North:
                    return Top;
                case Direction.South:
                    return Bottom;
                case Direction.West:
                    return Left;
                case Direction.East:
                    return Right;
                default:
                    return this;
            }
        }

        public Direction GetDirectionToNeighbour(Site neighbour)
        {
            if (neighbour == Top)
                return Direction.North;
            if (neighbour == Bottom)
                return Direction.South;
            if (neighbour == Left)
                return Direction.West;
            if (neighbour == Right)
                return Direction.East;
            if (neighbour == this)
                return Direction.Still;

            throw new ArgumentException("Specified site is not a neighbour");
        }

        public bool IsMine => Owner == Config.Get().PlayerTag;
        public bool IsEnemy => Owner != Config.Get().PlayerTag && Owner != 0;
        public bool IsNeutral => Owner == 0;
        public bool IsZeroNeutral => Owner == 0 && Strength == 0;

        public List<Site> GetDangerSites()
        {
            List<Site> result = new List<Site>();
            Neighbors.ForEach(x => result.Add(x));
            result.Add(Left.Bottom);
            result.Add(Left.Top);
            result.Add(Right.Bottom);
            result.Add(Right.Top);
            return result;
        }

        public void PopulateNeighbours(Site top, Site bottom, Site left, Site right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public void ClearNeighbors()
        {
            Top = null;
            Bottom = null;
            Right = null;
            Left = null;
        }

        public void AddStrength(ushort str)
        {
            Strength = (ushort)Math.Min(Strength + str, 255);
        }
    }

    public class Move
    {
        public Site Site;
        public Direction Direction;
        public Site Target => Site.GetNeighborAtDirection(Direction);

        public static string MovesToString(IEnumerable<Move> moves)
        {
            return string.Join(" ",
                moves.Select(m => $"{m.Site.X} {m.Site.Y} {(int)m.Direction}"));
        }
    }
}
