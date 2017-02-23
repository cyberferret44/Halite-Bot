using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public static class Helper
    {
        public static void AssistCombatants(Turn turn, Heuristic<Site> heuristic, Map map)
        {
            foreach (var site in turn.RemainingSites)
            {
                // We want to move it to the area with the highest potential
                Site bestTarget = GetBestNeighbor(site, heuristic, turn.Moves, map);
                if (bestTarget != null)
                    turn.AddMove(site, bestTarget);
            }
        }

        public static Site GetBestNeighbor(Site site, Heuristic<Site> heuristic, List<Move> moves, Map map)
        {
            if (site.Strength >= site.Production * 6)
            {
                var sortedNeighbors = site.Neighbors.Where(x => x.IsMine).OrderByDescending(x => heuristic.Get(x).Value);
                if (sortedNeighbors.All(s => heuristic.Get(site).Value < .00000001f))
                {
                    var farNeighbors = new List<Site>();
                    var p1 = Gaussian.Truncate(site.X - 10, site.Y, map.Height, map.Width);
                    var s1 = map[p1.X, p1.Y];
                    farNeighbors.Add(s1);
                    var p2 = Gaussian.Truncate(site.X + 10, site.Y, map.Height, map.Width);
                    var s2 = map[p2.X, p2.Y];
                    farNeighbors.Add(s2);
                    var p3 = Gaussian.Truncate(site.X, site.Y - 10, map.Height, map.Width);
                    var s3 = map[p3.X, p3.Y];
                    farNeighbors.Add(s3);
                    var p4 = Gaussian.Truncate(site.X, site.Y + 10, map.Height, map.Width);
                    var s4 = map[p4.X, p4.Y];
                    farNeighbors.Add(s4);
                    var best = farNeighbors.OrderByDescending(x => heuristic.Get(x).Value).First();
                    if (best == s1)
                        moves.Add(new Move { Site = site, Direction = Direction.West });
                    if (best == s2)
                        moves.Add(new Move { Site = site, Direction = Direction.East });
                    if (best == s3)
                        moves.Add(new Move { Site = site, Direction = Direction.North });
                    if (best == s4)
                        moves.Add(new Move { Site = site, Direction = Direction.South });
                }
                else
                {
                    foreach (var neighbor in sortedNeighbors)
                    {
                        var neighborsBestNeighbor = neighbor.Neighbors.Where(x => x.IsMine).OrderByDescending(x => heuristic.Get(x).Value).First();
                        if (neighborsBestNeighbor == site || moves.Any(x => x.Site == neighbor && x.Site.GetNeighborAtDirection(x.Direction) == site))
                        {
                            var movesToTarget = moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == site);
                            if (movesToTarget.Sum(m => m.Site.Strength) + site.Strength > 250)
                            {
                                var nextBestMove = site.Neighbors.Where(x => x.Owner == 0).OrderByDescending(x => heuristic.Get(x).Value).FirstOrDefault();
                                if (nextBestMove == null)
                                    continue;
                                return nextBestMove;
                            }
                            else
                            {
                                return site;
                            }
                        }
                        else
                        {
                            var movesToTarget = moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == neighbor);
                            if (movesToTarget.Sum(m => m.Site.Strength) + site.Strength <= (255 + 10 * neighbor.Production))
                            {
                                return neighbor;
                            }
                        }
                    }
                }
            }
            else if (site.Strength > 0)
            {
                foreach (var neighbor in site.Neighbors.Where(n => n.IsZeroNeutral))
                {
                    List<Site> twoLayerNeighbors = new List<Site>();
                    twoLayerNeighbors.AddRange(site.Neighbors);
                    HashSet<Site> moarNeighbors = new HashSet<Site>();
                    twoLayerNeighbors.ForEach(x => moarNeighbors.Add(x));
                    twoLayerNeighbors.ForEach(x => x.Neighbors.ForEach(n => moarNeighbors.Add(n)));
                    if (twoLayerNeighbors.All(n => n.Owner <= 1 || n.Strength + n.Production < site.Strength))
                    {
                        moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(neighbor) });
                    }
                }

            }
            return null;
        }
    }
}
