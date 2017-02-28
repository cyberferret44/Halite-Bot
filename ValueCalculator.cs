using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class ValueCalculator
    {
        public static int Bound = 9;
        public List<List<Point>> North, South, East, West;

        public ValueCalculator()
        {
            North = CalculateAoE(new List<Point>() { new Point(0, 0) }, Direction.North);
            South = CalculateAoE(new List<Point>() { new Point(0, 0) }, Direction.South);
            East = CalculateAoE(new List<Point>() { new Point(0, 0) }, Direction.East);
            West = CalculateAoE(new List<Point>() { new Point(0, 0) }, Direction.West);
        }

        public static List<List<Point>> CalculateAoE(List<Point> includedPoints, Direction d)
        {
            var result = new List<List<Point>>();
            if (includedPoints.Count == Bound)
            {
                var newList = new List<Point>();
                newList.AddRange(includedPoints);
                result.Add(newList);
                return result;
            }

            var curNode = includedPoints.Last();
            List<Point> nextPoints;

            if (d == Direction.North)
                nextPoints = new List<Point>() {
                    new Point { X = curNode.X - 1, Y = curNode.Y },
                    new Point { X = curNode.X + 1, Y = curNode.Y },
                    new Point { X = curNode.X, Y = curNode.Y - 1 }
                };
            else if (d == Direction.South)
                nextPoints = new List<Point>() {
                    new Point { X = curNode.X - 1, Y = curNode.Y },
                    new Point { X = curNode.X + 1, Y = curNode.Y },
                    new Point { X = curNode.X, Y = curNode.Y + 1 }
                };
            else if (d == Direction.East)
                nextPoints = new List<Point>() {
                    new Point { X = curNode.X + 1, Y = curNode.Y },
                    new Point { X = curNode.X, Y = curNode.Y + 1 },
                    new Point { X = curNode.X, Y = curNode.Y - 1 }
                };
            else
                nextPoints = new List<Point>() {
                    new Point { X = curNode.X - 1, Y = curNode.Y },
                    new Point { X = curNode.X, Y = curNode.Y + 1 },
                    new Point { X = curNode.X, Y = curNode.Y - 1 }
                };

            foreach (var next in nextPoints.Where(p => !includedPoints.Any(i => i.X == p.X && i.Y == p.Y)))
            {
                var nextIncludedPoints = new List<Point>();
                nextIncludedPoints.AddRange(includedPoints);
                nextIncludedPoints.Add(next);
                result.AddRange(CalculateAoE(nextIncludedPoints, d));
            }
            return result;
        }

        public Heuristic CalculatePotentialValue(Heuristic h, List<Site> targets, Map map)
        {
            var resultHeuristic = h.Clone();
            foreach (var target in targets)
            {
                double bestValue = 0.0;
                if (target.Top.IsMine && target.Bottom.IsNeutral)
                    bestValue = Math.Max(bestValue, GetBestValue(h, target, Direction.South, map));
                if (target.Bottom.IsMine && target.Top.IsNeutral)
                    bestValue = Math.Max(bestValue, GetBestValue(h, target, Direction.North, map));
                if (target.Right.IsMine && target.Left.IsNeutral)
                    bestValue = Math.Max(bestValue, GetBestValue(h, target, Direction.West, map));
                if (target.Left.IsMine && target.Right.IsNeutral)
                    bestValue = Math.Max(bestValue, GetBestValue(h, target, Direction.East, map));

                resultHeuristic.Update(target, bestValue);
            }

            resultHeuristic.WriteCSV("valueH");
            return resultHeuristic;
        }

        private double GetBestValue(Heuristic h, Site startSite, Direction d, Map map)
        {
            var stencils = d == Direction.North ? North : d == Direction.South ? South : d == Direction.East ? East : West;

            double maxValue = 0.0;
            foreach (var combo in stencils.Where(s => s.All(x => map[x.X, x.Y].IsNeutral)))
            {
                double value = h.Get(startSite).Value;
                foreach (var point in combo)
                {
                    int px = startSite.X + point.X;
                    int py = startSite.Y + point.Y;
                    value += h.Get(map[px, py]).Value;
                }
                maxValue = Math.Max(maxValue, value);
            }

            return maxValue;
        }
    }
}
