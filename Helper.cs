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
        public static void AssistCombatants(Turn turn, Heuristic heuristic, Map map)
        {
            foreach (var site in turn.RemainingSites)
            {
                // We want to move it to the area with the highest potential
                Site bestTarget = GetBestNeighbor(site, heuristic, turn.Moves, map);
                if (bestTarget != null)
                    turn.AddMove(site, bestTarget);
            }
        }

        public static Site GetBestNeighbor(Site site, Heuristic heuristic, List<Move> moves, Map map)
        {
            if (site.Strength >= site.Production * 6)
            {
                var sortedNeighbors = site.Neighbors.Where(x => x.IsMine).OrderByDescending(x => heuristic.Get(x).Value);
                //return sortedNeighbors.First();
                foreach (var neighbor in sortedNeighbors)
                {
                    //var neighborsBestNeighbor = neighbor.Neighbors.Where(x => x.IsMine).OrderByDescending(x => heuristic.Get(x).Value).First();
                    //if (neighborsBestNeighbor == site || moves.Any(x => x.Site == neighbor && x.Site.GetNeighborAtDirection(x.Direction) == site))
                    //{
                    //    var movesToTarget = moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == site);
                    //    if (movesToTarget.Sum(m => m.Site.Strength) + site.Strength > 250)
                    //    {
                    //        var nextBestMove = site.Neighbors.Where(x => x.Owner == 0).OrderByDescending(x => heuristic.Get(x).Value).FirstOrDefault();
                    //        if (nextBestMove == null)
                    //            continue;
                    //        return nextBestMove;
                    //    }
                    //    else
                    //    {
                    //        return site;
                    //    }
                    //}
                    //else
                    //{
                        var movesToTarget = moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == neighbor);
                        if (movesToTarget.Sum(m => m.Site.Strength) + site.Strength <= (260))
                        {
                            return neighbor;
                        }
                    //}
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
