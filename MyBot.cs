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
        public const string BotName = "RandomSlime";
        public static int DEPTH = 4;
        public static bool InCombat = false;
        public static double OpportunityCost;
        public static Heuristic ExpansionHeuristic;
        private static CombatLearning CurrentLearning;
        private static CombatLearning PreviousLearning;

        public static void Main(string[] args)
        {
            #region Variables to Start the Game
            //Debugger.Launch();
            Console.SetIn(Console.In);
            Console.SetOut(Console.Out);
            var map = Networking.GetInit();
            OpportunityCost = map.GetSites(x => x.IsNeutral).OrderByDescending(x => x.Production).Take(map.GetSites(x => x.IsNeutral).Count / 3).Average(x => (double)x.Production);
            Networking.SendInit(BotName);
            ExpansionHeuristic = new SlimeHeuristic().GetSlimeHeuristic(map);
            Random random = new Random();
            //ExpansionHeuristic.WriteCSV("csv2");
            #endregion

            while (true)
            {
                // Initialize the new map state
                try
                {
                    Networking.GetFrame(map);
                }
                catch (Exception)
                {
                    break;
                }
                

                // Record data for Machine Learning before doing anything else.....
                CurrentLearning = new CombatLearning(map);
                if(PreviousLearning != null && PreviousLearning.HasData)
                {
                    PreviousLearning.WriteToFile(CurrentLearning.Stats);
                }
                

                // Turn level variables
                InCombat = GetHostileNeutralSites(map).Any();
                var sitesToAvoid = new HashSet<Site>();
                var turn = new Turn(map.MySites);
                var edgeTargets = GetPassiveNeutralNeighbors(map).OrderByDescending(x => ExpansionHeuristic.Get(x).Value).ToList();
                var internalHeuristic = InternalHeuristic.GetInternalHeuristic(ExpansionHeuristic, edgeTargets);
                
                
                // Combat Logic...  Random for now, will train on data later
                foreach(var combatSite in CurrentLearning.SitesInCombat.Where(s => s.Strength > 0))
                {
                    Direction randomDirection = (Direction)random.Next(Enum.GetValues(typeof(Direction)).Length);
                    turn.AddMove(combatSite, randomDirection);
                }
                

                // Expansion logic...
                var orderedList = GetOrderedNeutralConquerMoves(map, turn.RemainingSites, edgeTargets);
                foreach (var nextBest in orderedList)
                {
                    if (nextBest.MovesToDo.All(m => turn.RemainingSites.Contains(m.Site)) && !nextBest.MovesToDo.Any(m => turn.Moves.Any(ick => ick.Target == m.Target)))
                    {
                        nextBest.MovesToDo.ForEach(x => turn.AddMove(x));
                    }
                }

                // Internal Logic: move to the area with the highest potential
                foreach (var site in turn.RemainingSites)
                {
                    Site bestTarget = Helper.GetBestNeighbor(site, internalHeuristic, turn.Moves, map);
                    if (bestTarget != null)
                        turn.AddMove(site, bestTarget);
                }
                foreach (var site in turn.RemainingSites)
                {
                    if (WillMySiteOverflow(site, turn.Moves) || sitesToAvoid.Contains(site) && site.Strength > 0)
                    {
                        RunAway(site, sitesToAvoid, internalHeuristic, turn.Moves);
                    }
                }

                if (CurrentLearning.HasData)
                {
                    CurrentLearning.Record(turn.Moves);
                }
                PreviousLearning = CurrentLearning;
                Networking.SendMoves(turn.Moves);
            }
        }

        private static List<Site> GetHostileNeutralSites(Map m)
        {
            return m.GetSites(s => (s.IsZeroNeutral
                                && s.Neighbors.Any(n => n.IsMine)
                                && s.Neighbors.Any(n => n.IsEnemy || n.Neighbors.Any(nn => nn.IsEnemy || nn.Neighbors.Any(nnn => nnn.IsEnemy))))
                                ||
                                (s.Owner == 0 && s.Neighbors.Any(x => x.IsMine) && s.Neighbors.Any(x => x.IsEnemy))).ToList();
        }

        private static void RunAway(Site site, HashSet<Site> sitesToAvoid, Heuristic heuristic, List<Move> moves)
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

        private static Dictionary<Point, double> GetProductions(Map m)
        {
            var returnVal = new Dictionary<Point, double>();
            m.GetSites(x => true).ForEach(x => returnVal.Add(new Point(x.X, x.Y), (double)x.Production));
            return returnVal;
        }

        private static List<PotentialMove> GetOrderedNeutralConquerMoves(Map map, HashSet<Site> availableSites, List<Site> bestTargets)
        {
            DEPTH =  Math.Max(18 - bestTargets.Count / 3, 4); //InCombat ? 1 :
            List<PotentialMove> potentialMoves = new List<PotentialMove>();
            foreach (var target in bestTargets)
            {
                potentialMoves.AddRange(GetPotentialMoves(target, target.Neighbors.Where(n => n.IsMine).ToList(), new List<Site>(), 1, 0, availableSites));
            }
            return potentialMoves.OrderByDescending(x => x.Value).ToList();
        }

        private static List<PotentialMove> GetPotentialMoves(Site target, List<Site> newPieces, List<Site> previousSites, int layer, int productionStrength, HashSet<Site> sitesICanUse)
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
                    int strengthCost = moves.Sum(z => z.Site.Production) + Math.Max(0, moves.Sum(z => z.Site.Strength) + productionStrength - 255) + (int)Math.Ceiling(OpportunityCost * end.Count * layer); //TODO add average production to loss
                    result.Add(new PotentialMove { Target = target, MovesToDo = moves, Value = ExpansionHeuristic.GetReducedValue(target, strengthCost) });
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
                        newPiecesParam.AddRange(np.Neighbors.Where(n => n.IsMine && newPieces.All(x => x != n) && previousSites.All(x => x != n) && newPiecesParam.All(x => x != n) && sitesICanUse.Contains(n)));
                    }
                    if (sa.Any() && sa.Sum(x => x.Production) + previousSites.Sum(x => x.Production) > 0)
                    {
                        var moreStillMoves = new List<Move>();
                        foreach (var ps in sa)
                            moreStillMoves.Add(new Move() { Site = ps, Direction = Direction.Still });
                        moreStillMoves.AddRange(stillMoves);
                        int strengthCost = moreStillMoves.Sum(sm => sm.Site.Production) + (int)Math.Ceiling(OpportunityCost * sa.Count * (layer + (target.Strength - moreStillMoves.Sum(x => x.Site.Strength)) / moreStillMoves.Sum(x => x.Site.Production) + 1));
                        result.Add(new PotentialMove { MovesToDo = moreStillMoves, Target = target, Value = ExpansionHeuristic.GetReducedValue(target, strengthCost) });
                    }
                    sa.AddRange(previousSites);
                    result.AddRange(GetPotentialMoves(target, newPiecesParam, sa, layer + 1, productionStrength + sa.Sum(s => s.Production), sitesICanUse));
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
                                && s.Neighbors.Any(n => n.IsMine)
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
            public double Value;
            public List<Move> MovesToDo;
        }
    }
}