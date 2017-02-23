using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Halite
{
    public class MyBot
    {
        public const string RandomBotName = "PointlessBot";
        public static int MyId;
        public static int DEPTH = 4;
        public static bool InCombat = false;

        public static void Main(string[] args)
        {
            #region Shit to Start the Game
            //if (Directory.GetCurrentDirectory() == "C:\\HaliteCSharpStarter")
                Debugger.Launch();

            Console.SetIn(Console.In);
            Console.SetOut(Console.Out);
            var map = Networking.GetInit();
            MyId = Config.Get().PlayerTag;
            Gaussian.SetKernel(map);
            Networking.SendInit(RandomBotName);
            #endregion

            while (true)
            {
                #region Turn level variables
                Networking.GetFrame(map);
                InCombat = GetHostileNeutralSites(map).Any();
                var sitesToAvoid = new HashSet<Site>();
                var turn = new Turn(map.GetMySites());
                var heuristic = Gaussian.ShadeHeuristic(Heuristic<Site>.GetStartingHeuristic(map), map, InCombat);
                #endregion

                #region Combat!!!!!
                foreach (Site s in turn.RemainingSites.Where(m => m.Strength > 3 + m.Production))
                {
                    int maxDamage = -1;
                    Site target = null;
                    foreach (var neighbor in s.Neighbors.Where(n => n.Neighbors.Any(x => x.IsEnemy) && s.Strength == 0))
                    {
                        var enemies = neighbor.Neighbors.Where(x => x.IsEnemy);
                        int possibleDamage = enemies.Sum(e => Math.Min(e.Strength, s.Strength) + (e.Strength <= s.Strength ? e.Production : 0));
                        possibleDamage += Math.Max(0, s.Strength - enemies.Sum(x => x.Strength));
                        if (possibleDamage > maxDamage)
                        {
                            target = neighbor;
                            maxDamage = possibleDamage;
                        }
                    }

                    if (maxDamage > s.Strength + s.Production)
                    {
                        turn.AddMove(s, target);
                        foreach (var enemy in target.Neighbors.Where(x => x.IsEnemy && x.Strength > 1))
                        {
                            target.GetDangerousSites(enemy.ThreatZone).ForEach(x => sitesToAvoid.Add(x));
                        }
                    }
                }
                #endregion

                
                //var neutralSites = GetHostileNeutralSites(map);
                //List<Site> combatAssistanceSites = new List<Site>();
                //foreach (var site in turn.RemainingSites)
                //{
                //    if (neutralSites.Any(n => ManhattanDistance(n, site, map.Height, map.Width) < 8))
                //    {
                //        Helper.AssistCombatants(turn, heuristic, map);
                //        InCombat = true;
                //    }
                //}

                #region building out the neutral attack moves...
                var orderedList = GetOrderedNeutralConquerMoves(map, heuristic, turn.RemainingSites);
                //orderedList = orderedList.Take(Math.Min(5, orderedList.Count * 7 / 10)).ToList(); // Prune Bad Moves

                foreach (var nextBest in orderedList)
                {
                    if (nextBest.MovesToDo.All(m => turn.RemainingSites.Contains(m.Site)) && !nextBest.MovesToDo.Any(m => turn.Moves.Any(ick => ick.Target == m.Target)))
                    {
                        nextBest.MovesToDo.ForEach(x => turn.AddMove(x));
                    }
                }
                #endregion
                
                // mark and/or handle dangerous sites
                //foreach (var zero in map.GetSites(x => x.IsZeroNeutral && x.Neighbors.Any(n => n.IsEnemy && n.Strength > 0) && x.Neighbors.Any(n => n.IsMine)))
                //{
                //    var enemySites = zero.Neighbors.Where(x => x.IsEnemy && x.Strength > 0);
                //    var friendlySites = zero.Neighbors.Where(x => x.Strength > 0 && x.IsMine && (turn.Moves.All(m => m.Site != x) || turn.Moves.Any(m => m.Site == x && m.Direction == Direction.Still))).ToList();
                //    if (friendlySites.Count > 1)
                //    {
                //        if (enemySites.Sum(e => e.Strength) < friendlySites.Sum(f => f.Strength))
                //        {
                //            friendlySites.ForEach(f => turn.Moves.Add(new Move { Site = f, Direction = f.GetDirectionToNeighbour(zero) }));
                //        }
                //    }
                //}

                foreach (var site in turn.RemainingSites)
                {
                    // We want to move it to the area with the highest potential
                    Site bestTarget = Helper.GetBestNeighbor(site, heuristic, turn.Moves, map);
                    if (bestTarget != null)
                        turn.AddMove(site, bestTarget);
                }
                foreach (var site in turn.AllSites)
                {
                    var move = turn.Moves.FirstOrDefault(x => x.Site == site);
                    if ((move == null || move.Direction == Direction.Still) && sitesToAvoid.Contains(site) && site.Strength > 0)
                    {
                        turn.RemoveMove(move.Site);
                        RunAway(site, sitesToAvoid, heuristic, turn.Moves);
                    }
                }
                foreach (var site in turn.RemainingSites)
                {
                    if (WillMySiteOverflow(site, turn.Moves) || sitesToAvoid.Contains(site) && site.Strength > 0)
                    {
                        RunAway(site, sitesToAvoid, heuristic, turn.Moves);
                    }
                }

                Networking.SendMoves(turn.Moves);
            }
        }

        private static List<Site> GetHostileNeutralSites(Map m)
        {
            return m.GetSites(s => (s.IsZeroNeutral
                                && s.Neighbors.Any(n => n.Owner == MyId)
                                && s.Neighbors.Any(n => n.IsEnemy || n.Neighbors.Any(nn => nn.IsEnemy || nn.Neighbors.Any(nnn => nnn.IsEnemy))))
                                ||
                                (s.Owner == 0 && s.Neighbors.Any(x => x.IsMine) && s.Neighbors.Any(x => x.IsEnemy))).ToList();
        }

        private static void RunAway(Site site, HashSet<Site> sitesToAvoid, Heuristic<Site> heuristic, List<Move> moves)
        {
            var validNeighbors = site.Neighbors.Where(n => (!moves.Any(m => m.Site == n) ? n.Strength : 0)
                                    + moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == n).Sum(x => x.Site.Strength)
                                    + site.Strength <= 255);
            var bestNeighbor = validNeighbors.Where(n => !sitesToAvoid.Contains(n) && n.IsMine).OrderByDescending(x => heuristic.Get(x).Value).FirstOrDefault();
            if (bestNeighbor != null)
                moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(bestNeighbor) });
            else
            {
                bestNeighbor = validNeighbors.OrderByDescending(x => heuristic.Get(x).Value).FirstOrDefault();
                if (bestNeighbor != null)
                    moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(bestNeighbor) });
            }
        }

        private static Dictionary<Point, float> GetProductions(Map m)
        {
            var returnVal = new Dictionary<Point, float>();
            m.GetSites(x => true).ForEach(x => returnVal.Add(new Point(x.X, x.Y), (float)x.Production));
            return returnVal;
        }

        private static List<PotentialMove> GetOrderedNeutralConquerMoves(Map map, Heuristic<Site> heuristic, HashSet<Site> availableSites)
        {
            var neutralSites = GetPassiveNeutralNeighbors(map);
            float opportunityCost = (float)neutralSites.Sum(x => x.Production) / (float)neutralSites.Count();
            DEPTH =  Math.Min(20 - neutralSites.Count / 70, 4); //InCombat ? 1 :
            List<PotentialMove> potentialMoves = new List<PotentialMove>();
            foreach (var ns in neutralSites)
            {
                potentialMoves.AddRange(GetPotentialMoves(ns, heuristic, opportunityCost, availableSites));
            }
            return potentialMoves.OrderByDescending(x => x.Value).ToList();
        }

        private static List<PotentialMove> GetPotentialMoves(Site target, Heuristic<Site> h, float opportunityCost, HashSet<Site> availableSites)
        {
            return GetPotentialMoves(target, h, target.Neighbors.Where(n => n.Owner == MyId).ToList(), new List<Site>(), 1, 0, opportunityCost, availableSites);
        }

        private static List<PotentialMove> GetPotentialMoves(Site target, Heuristic<Site> h, List<Site> newPieces, List<Site> previousSites, int layer, int productionStrength, float opportunityCost, HashSet<Site> sitesICanUse)
        {
            // Add in the option to wait...
            List<Move> stillMoves = new List<Move>();
            var result = new List<PotentialMove>();
            foreach (var ps in previousSites)
                stillMoves.Add(new Move() { Site = ps, Direction = Direction.Still });

            // N moves and we can't get it?  throw that shit out
            newPieces = newPieces.Where(p => p.Strength > 0).ToList(); // prune this shit....
            if (previousSites.Count > DEPTH || !newPieces.Any())
                return new List<PotentialMove>();

            // base case 2
            // enumaerate all possible combinations
            var sitesAvailable = new List<List<Site>>();
            sitesAvailable.Add(new List<Site> { });
            foreach (var p in newPieces)
            {
                var moarSites = new List<List<Site>>();
                foreach (var sites in sitesAvailable)
                {
                    var newList = new List<Site>();
                    sites.ForEach(s => newList.Add(s));
                    newList.Add(p);
                    moarSites.Add(newList);
                }
                sitesAvailable.AddRange(moarSites);
            }
            sitesAvailable.Remove(sitesAvailable[0]);

            // prune our combinations of sites
            int previousSiteStrength = previousSites.Sum(x => x.Strength);
            var endSites = sitesAvailable.Where(s => s.Sum(x => x.Strength) + previousSiteStrength + productionStrength > target.Strength).ToList();

            if (endSites.Any())
            {
                // we've reached a terminable state
                foreach (var end in endSites)
                {
                    List<Move> moves = new List<Move>();
                    if (layer == 1)
                    {
                        foreach (var site in end)
                            moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(target) });
                    }
                    else
                    {
                        foreach (var site in end)
                            moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(site.Neighbors.First(n => previousSites.Contains(n))) });
                    }
                    moves.AddRange(stillMoves);
                    int strengthCost = moves.Sum(z => z.Site.Production) + Math.Max(0, moves.Sum(z => z.Site.Strength) + productionStrength - 255) + (int)Math.Ceiling(opportunityCost * layer); //TODO add average production to loss
                    result.Add(new PotentialMove { Target = target, MovesToDo = moves, Value = h.GetReducedValue(target, strengthCost) });
                }
            }
            else
            {
                // And the option to layer further
                foreach (var sa in sitesAvailable)
                {
                    List<Site> newPiecesParam = new List<Site>();
                    foreach (var np in sa)
                    {
                        newPiecesParam.AddRange(np.Neighbors.Where(n => n.Owner == MyId && newPieces.All(x => x != n) && previousSites.All(x => x != n) && newPiecesParam.All(x => x != n) && sitesICanUse.Contains(n)));
                    }
                    if (sa.Any() && sa.Sum(x => x.Production) + previousSites.Sum(x => x.Production) > 0)
                    {
                        var moreStillMoves = new List<Move>();
                        foreach (var ps in sa)
                            moreStillMoves.Add(new Move() { Site = ps, Direction = Direction.Still });
                        moreStillMoves.AddRange(stillMoves);
                        int strengthCost = moreStillMoves.Sum(sm => sm.Site.Production) + (int)Math.Ceiling(opportunityCost * (layer + (target.Strength - moreStillMoves.Sum(x => x.Site.Strength)) / moreStillMoves.Sum(x => x.Site.Production) + 1));
                        result.Add(new PotentialMove { MovesToDo = moreStillMoves, Target = target, Value = h.GetReducedValue(target, strengthCost) });
                    }
                    sa.AddRange(previousSites);
                    result.AddRange(GetPotentialMoves(target, h, newPiecesParam, sa, layer + 1, productionStrength + sa.Sum(s => s.Production), opportunityCost, sitesICanUse));
                }
            }
            return result;
        }

        private static List<Site> GetPassiveNeutralNeighbors(Map m)
        {
            return m.GetSites(s => s.Owner == 0
                                && s.Neighbors.Any(n => n.IsMine)
                                && s.Neighbors.All(n => !n.IsEnemy && (n.Strength > 0 || n.Neighbors.All(n2 => !n2.IsEnemy)))
                                ).ToList();
        }

        private static List<Site> HostileNeutralSites(Map m)
        {
            return m.GetSites(s => s.IsNeutral
                                && s.Neighbors.Any(n => n.Owner == MyId)
                                && (s.Neighbors.Any(n => n.IsEnemy)
                                || s.Neighbors.Any(n => n.IsZeroNeutral && n.Neighbors.Any(n2 => n2.IsEnemy)))
                                ).ToList();
        }

        private static bool WillMySiteOverflow(Site s, List<Move> moves)
        {
            var movesTo = moves.Where(x => x.Site.GetNeighborAtDirection(x.Direction) == s);
            return movesTo.Sum(x => x.Site.Strength) + s.Strength > 255;
        }

        private static int ManhattanDistance(Site p1, Site p2, int height, int width)
        {
            int deltaX = Math.Min(Math.Abs(p1.X - p2.X), width - Math.Abs(p1.X - p2.X));
            int deltaY = Math.Min(Math.Abs(p1.Y - p2.Y), height - Math.Abs(p1.Y - p2.Y));
            return deltaX + deltaY;
        }

        struct PotentialMove
        {
            public Site Target;
            public float Value;
            public List<Move> MovesToDo;
        }
    }
}