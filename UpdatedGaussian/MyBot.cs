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
        #region Global Variables.... *yuck*
        public const string RandomBotName = "UpdatedGaussian-CombatBot";
        public float[,] importanceGrid;
        public static int MyId;
        public static int NEUTRAL = 0;
        public static float AverageProduction;
        public static Dictionary<Point, float> ProductionPotentialDictionary;
        public static int DEPTH = 4;
        #endregion

        public static void Main(string[] args)
        {
            #region Shit to Start the Game
            if (Directory.GetCurrentDirectory() == "C:\\HaliteCSharpStarter")
                Debugger.Launch();

            Console.SetIn(Console.In);
            Console.SetOut(Console.Out);

            var map = Networking.GetInit();
            MyId = Config.Get().PlayerTag;

            AverageProduction = (float)map.GetSites(x => true).Sum(x => x.Production) / (float)map.SiteCount;

            // Random shit  TODO maybe averages should be based on my territory???
            ProductionPotentialDictionary = GetProductions(map);
            Gaussian.SetKernel(map);
            Networking.SendInit(RandomBotName);
            #endregion

            while (true)
            {
                #region Turn level variables
                Networking.GetFrame(map);
                var mySites = map.GetMySites();
                AverageProduction = (float)mySites.Sum(x => x.Production) / mySites.Count; //todo
                var usedSites = new HashSet<Site>();
                var sitesToAvoid = new HashSet<Site>();
                #endregion

                #region Populate our Heuristic
                //TODO thought.... use an enemy importance grid to mask on top of the productionimportancegrid
                var siteValues = GetSiteValues(map);
                var heuristic = Gaussian.ShadeHeuristic(siteValues, map);

                
                #endregion

                var moves = new List<Move>();

                #region Combat!!!!!
                foreach (Site s in mySites.Where(m => m.Strength > 3 + m.Production))
                {
                    int maxDamage = -1;
                    Site target = null;
                    foreach (var neighbor in s.Neighbors.Where(n => n.Strength == 0))
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

                    if (maxDamage > s.Strength)
                    {
                        moves.Add(new Move { Site = s, Direction = s.GetDirectionToNeighbour(target) });
                        usedSites.Add(s);
                        target.Neighbors.Where(x => x.IsEnemy && x.Strength > 1).ToList().ForEach(x => x.Neighbors.ForEach(d => sitesToAvoid.Add(d))); // todo except target
                    }
                }
                #endregion

                #region building out the neutral attack moves first...
                var orderedList = GetOrderedNeutralConquerMoves(map, heuristic);

                foreach (var nextBest in orderedList)
                {
                    if (nextBest.MovesToDo.All(m => !usedSites.Contains(m.Site)) && !nextBest.MovesToDo.Any(m => moves.Any(ick => ick.Target == m.Target)))
                    {
                        moves.AddRange(nextBest.MovesToDo);
                        nextBest.MovesToDo.ForEach(x => usedSites.Add(x.Site));
                    }
                }
                List<Site> sitesAlreadyMoved = moves.Select(x => x.Site).ToList();
                #endregion

                var myRemainingSites = map.GetMySites().Where(x => !sitesAlreadyMoved.Contains(x)).ToList();

                #region using the cloned potential dictionary, move any remaining pieces above a certain strength to the highest potential neighbor
                foreach (var site in myRemainingSites)
                {
                    // We want to move it to the area with the highest potential
                    if (site.Strength >= ProductionPotentialDictionary[new Point(site.X, site.Y)] * 7)
                    {
                        var sortedNeighbors = site.Neighbors.Where(x=> x.Owner != NEUTRAL || x.Strength == 0).OrderByDescending(x => heuristic[new Point(x.X, x.Y)].Value);
                        if (sortedNeighbors.Any(x => heuristic[new Point { X = x.X, Y = x.Y }].Value < .00001f || heuristic[new Point { X = x.X, Y = x.Y }].Value > 2f))
                            Console.Write("");
                        foreach(var neighbor in sortedNeighbors)
                        {
                            var movesToTarget = moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == neighbor);
                            if(movesToTarget.Sum(m => m.Site.Strength) + site.Strength <= (255 + 10*neighbor.Production))
                            {
                                moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(neighbor) });
                                sitesAlreadyMoved.Add(site);
                                break;
                            }
                        }
                    }
                }

                // Remove pieces moved in this region from the remaining pieces
                myRemainingSites.RemoveAll(x => sitesAlreadyMoved.Contains(x));
                #endregion

                #region Move all pieces sitting in a sqaure which will overflow
                // mark and/or handle dangerous sites
                foreach(var zero in map.GetSites(x=> x.IsZeroNeutral && x.Neighbors.Any(n => n.IsEnemy && n.Strength > 0) && x.Neighbors.Any(n => n.IsMine)))
                {
                    var enemySites = zero.Neighbors.Where(x => x.IsEnemy && x.Strength > 0);
                    var friendlySites = zero.Neighbors.Where(x => x.Strength > 0 && x.IsMine && (!moves.Any(m => m.Site == x) || moves.Any(m => m.Site == x && m.Direction == Direction.Still))).ToList();
                    if(friendlySites.Count > 1)
                    {
                        if(enemySites.Sum(e => e.Strength) < friendlySites.Sum(f => f.Strength))
                        {
                            friendlySites.ForEach(f => moves.Add(new Move { Site = f, Direction = f.GetDirectionToNeighbour(zero) }));
                        }
                    }
                }

                foreach(var site in map.GetMySites())
                {
                    var move = moves.FirstOrDefault(x => x.Site == site);
                    if ((move == null || move.Direction == Direction.Still) && sitesToAvoid.Contains(site))
                    {
                        moves.Remove(move);
                        RunAway(site, sitesToAvoid, heuristic, moves);
                        myRemainingSites.Remove(site);
                    }
                }
                foreach (var site in myRemainingSites)
                {
                    if (WillMySiteOverflow(site, moves) || sitesToAvoid.Contains(site))
                    {
                        RunAway(site, sitesToAvoid, heuristic, moves);
                    }
                }
                #endregion

                Networking.SendMoves(moves);
            }
        }

        private static void RunAway(Site site, HashSet<Site> sitesToAvoid, Dictionary<Point, Heuristic> heuristic, List<Move> moves)
        {
            var validNeighbors = site.Neighbors.Where(n => (!moves.Any(m => m.Site == n) ? n.Strength : 0)
                                    + moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == n).Sum(x => x.Site.Strength)
                                    + site.Strength <= 255);
            var bestNeighbor = validNeighbors.Where(n => !sitesToAvoid.Contains(n)).OrderByDescending(x => heuristic[new Point(x.X, x.Y)].Value).FirstOrDefault();
            if (bestNeighbor != null)
                moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(bestNeighbor) });
            else
            {
                bestNeighbor = validNeighbors.OrderByDescending(x => heuristic[new Point(x.X, x.Y)].Value).FirstOrDefault();
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

        private static List<PotentialMove> GetOrderedNeutralConquerMoves(Map map, Dictionary<Point, Heuristic> heuristic)
        {
            var neutralSites = GetPassiveNeutralNeighbors(map);
            DEPTH = Math.Min(6 - neutralSites.Count / 100, 4);
            List<PotentialMove> potentialMoves = new List<PotentialMove>();
            foreach(var ns in neutralSites)
            {
                potentialMoves.AddRange(GetPotentialMoves(ns, heuristic[new Point { X = ns.X, Y = ns.Y }]));
            }
            return potentialMoves.OrderByDescending(x => x.Value).ToList();
        }

        private static List<PotentialMove> GetPotentialMoves(Site target, Heuristic h)
        {
            return GetPotentialMoves(target, h, target.Neighbors.Where(n => n.Owner == MyId).ToList(), new List<Site>(), 1, 0);
        }

        private static List<PotentialMove> GetPotentialMoves(Site target, Heuristic h, List<Site> newPieces, List<Site> previousSites, int layer, int productionStrength)
        {
            // Add in the option to wait...
            List<Move> stillMoves = new List<Move>();
            var result = new List<PotentialMove>();
            foreach (var ps in previousSites)
                stillMoves.Add(new Move() { Site = ps, Direction = Direction.Still });

            // 6 moves and we can't get it?  throw that shit out
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
                foreach(var sites in sitesAvailable)
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
            var endSites = sitesAvailable.Where(s => s.Sum(x => x.Strength) + previousSites.Sum(m => m.Strength) + productionStrength > target.Strength).ToList();

            if (endSites.Any())
            {
                // we've reached a terminable state
                foreach(var end in endSites)
                {
                    var pm = new PotentialMove();
                    pm.Target = target;
                    List<Move> zzz = new List<Move>();
                    if(layer == 1)
                    {
                        foreach (var site in end)
                            zzz.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(target) });
                    }
                    else
                    {
                        foreach (var site in end)
                            zzz.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(site.Neighbors.First(n => previousSites.Contains(n))) });
                    }
                    zzz.AddRange(stillMoves);
                    int strengthCost = zzz.Sum(z => z.Site.Production) + Math.Max(0, zzz.Sum(z => z.Site.Strength) + productionStrength - 255) + (int)Math.Ceiling(AverageProduction * layer); //TODO add average production to loss
                    result.Add(new PotentialMove { Target = target, MovesToDo = zzz, Value = GetMoveValue(h, strengthCost) });
                }
            }
            else
            {
                // And the option to layer further
                foreach(var sa in sitesAvailable)
                {
                    List<Site> newPiecesParam = new List<Site>();
                    foreach(var np in sa)
                    {
                        newPiecesParam.AddRange(np.Neighbors.Where(n => n.Owner == MyId && newPieces.All(x => x != n) && previousSites.All(x => x != n) && newPiecesParam.All(x => x != n)));
                    }
                    if (sa.Any() && sa.Sum(x => x.Production) + previousSites.Sum(x => x.Production) > 0)
                    {
                        var moreStillMoves = new List<Move>();
                        foreach (var ps in sa)
                            moreStillMoves.Add(new Move() { Site = ps, Direction = Direction.Still });
                        moreStillMoves.AddRange(stillMoves);
                        int strengthCost = moreStillMoves.Sum(sm => sm.Site.Production) + (int)Math.Ceiling(AverageProduction * (layer + (target.Strength - moreStillMoves.Sum(x => x.Site.Strength)) / moreStillMoves.Sum(x => x.Site.Production) + 1));
                        result.Add(new PotentialMove { MovesToDo = moreStillMoves, Target = target, Value = GetMoveValue(h, strengthCost) });
                    }
                    sa.AddRange(previousSites);
                    result.AddRange(GetPotentialMoves(target, h, newPiecesParam, sa, layer + 1, productionStrength + sa.Sum(s => s.Production)));
                }
            }
            return result;
        }

        private static List<Site> GetPassiveNeutralNeighbors(Map m)
        {
            return m.GetSites(s => s.Owner == NEUTRAL
                                && s.Neighbors.Any(n => n.Owner == MyId)
                                && s.Neighbors.All(n => n.Owner <= MyId)
                                ).ToList();
        }

        private static List<Site> HostileNeutralSites(Map m)
        {
            return m.GetSites(s => s.Owner == NEUTRAL
                                && s.Neighbors.Any(n => n.Owner == MyId)
                                && s.Neighbors.Any(n => n.Owner > MyId)
                                ).ToList();
        }

        private static bool WillMySiteOverflow(Site s, List<Move> moves)
        {
            var movesTo = moves.Where(x => x.Site.GetNeighborAtDirection(x.Direction) == s);
            return movesTo.Sum(x => x.Site.Strength) + s.Strength > 255;
        }

        private static Dictionary<Point, Heuristic> GetSiteValues(Map m)
        {
            var result = new Dictionary<Point, Heuristic>();
            foreach (var site in m.GetSites(x => true))
            {
                float strength = site.Strength;
                float production = site.Production;
                if(site.Owner == MyId)
                {
                    production = 0;
                }
                if(site.Strength == 0)
                {
                    strength = 1;
                }
                result.Add(new Point { X = site.X, Y = site.Y }, new Heuristic { Production = production, Strength = strength, Value = production / strength });
            }
            return result;
        }

        private static float GetMoveValue(Heuristic h, int strengthLost)
        {
            return h.Value * h.Strength/(h.Strength + strengthLost); //TODO may need reevaluated...  Should only be used for conquering neutral territory
        }
    }

    struct PotentialMove
    {
        public Site Target;
        public float Value;
        public List<Move> MovesToDo;
    }

    struct Heuristic
    {
        public float Value;
        public float Production;
        public float Strength;
    }
}