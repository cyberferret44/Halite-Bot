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
        public const string RandomBotName = "CaffineBotWithoutBugs";
        public float[,] importanceGrid;
        public static int MyId;
        public static int NEUTRAL = 0;
        public static float AverageProduction;
        public static Dictionary<Point, float> ProductionPotentialDictionary, BeginningStrengthDictionary;
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

            // Random shit  TODO maybe averages should be based on my territory???
            AverageProduction = (float)map.GetSites(x => true).Sum(x => x.Production) / (float)(map.Height * map.Width);
            ProductionPotentialDictionary = GetProductions(map);
            BeginningStrengthDictionary = Gaussian.CreatePSRatioPotentialGrid(map);

            if (map.Width * map.Height > 1000)
                Gaussian.DoSmallKernel();

            Networking.SendInit(RandomBotName);
            #endregion

            while (true)
            {
                Networking.GetFrame(map);

                #region building out the neutral attack moves first...
                var moves = new List<Move>();
                var piecesTouched = new HashSet<Site>();
                var orderedList = GetOrderedNeutralConquerMoves(map, ref piecesTouched);
                int maxPieces = piecesTouched.Count;
                var usedSites = new HashSet<Site>();

                foreach (var nextBest in orderedList)
                {
                    if (nextBest.MovesToDo.All(m => !usedSites.Contains(m.Site)))
                    {
                        moves.AddRange(nextBest.MovesToDo);
                        nextBest.MovesToDo.ForEach(x => usedSites.Add(x.Site));
                    }
                    if (usedSites.Count == maxPieces)
                    {
                        break;
                    }
                }
                List<Site> sitesAlreadyMoved = moves.Select(x => x.Site).ToList();
                #endregion

                //TODO thought.... use an enemy importance grid to mask on top of the productionimportancegrid
                var myRemainingSites = map.GetMySites().Where(x => !sitesAlreadyMoved.Contains(x)).ToList();

                // clone production ratios and zero out ones I own
                var clonedPotentialDictionary = ProductionPotentialDictionary.ToDictionary(x => x.Key, x => x.Value);
                foreach (var s in map.GetMySites())
                {
                    clonedPotentialDictionary[new Point(s.X, s.Y)] = 0f;
                }
                clonedPotentialDictionary = Gaussian.CreateRandomAssThing(clonedPotentialDictionary, map);
                clonedPotentialDictionary = Gaussian.CreateRandomAssThing(clonedPotentialDictionary, map); // do twice?....

                #region using the cloned potential dictionary, move any remaining pieces above a certain strength to the highest potential neighbor
                foreach (var site in myRemainingSites)
                {
                    // We want to move it to the area with the highest potential
                    if (site.Strength >= ProductionPotentialDictionary[new Point(site.X, site.Y)] * 7)
                    {
                        var sortedNeighbors = site.Neighbors.Where(x=> x.Owner != NEUTRAL || x.Strength == 0).OrderByDescending(x => clonedPotentialDictionary[new Point(x.X, x.Y)]);
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
                foreach (var site in myRemainingSites)
                {
                    if (WillMySiteOverflow(site, moves))
                    {
                        var validNeighbors = site.Neighbors.Where(n => (!moves.Any(m => m.Site == n) ? n.Strength : 0)
                                    + moves.Where(m => m.Site.GetNeighborAtDirection(m.Direction) == n).Sum(x => x.Site.Strength)
                                    + site.Strength <= 255);
                        var bestNeighbor = validNeighbors.OrderByDescending(x => clonedPotentialDictionary[new Point(x.X, x.Y)]).FirstOrDefault();
                        if (bestNeighbor != null)
                            moves.Add(new Move { Site = site, Direction = site.GetDirectionToNeighbour(bestNeighbor) });
                    }
                }
                #endregion

                Networking.SendMoves(moves);
            }
        }

        private static Dictionary<Point, float> GetProductions(Map m)
        {
            var returnVal = new Dictionary<Point, float>();
            m.GetSites(x => true).ForEach(x => returnVal.Add(new Point(x.X, x.Y), (float)x.Production));
            return returnVal;
        }

        private static List<PotentialMove> GetOrderedNeutralConquerMoves(Map map, ref HashSet<Site> myPiecesUsed)
        {
            //TODO replace this shit with a DFS algorithm
            var neutralSites = GetTrueNeutralNeighbors(map);
            List<PotentialMove> potentialMoves = new List<PotentialMove>();
            var piecesUsed = new HashSet<Site>();

            #region Potential First Layer Moves
            foreach (var site in neutralSites)
            {
                var gaussianProductionRatio = BeginningStrengthDictionary[new Point(site.X, site.Y)]; // site.production / site.strength

                List<Site> one = site.Neighbors.Where(x => x.Owner == MyId && x.Strength > 1).ToList();
                List<Site> singleKill = one.Where(x => x.Strength > site.Strength).ToList();
                if (singleKill.Any())
                {
                    foreach (var s in singleKill)
                    {
                        potentialMoves.Add(new PotentialMove()
                        {
                            Target = site,
                            Value = gaussianProductionRatio / (float)(site.Strength + s.Production),
                            MovesToDo = new List<Move> { new Move { Site = s, Direction = s.GetDirectionToNeighbour(site) } }
                        });
                        piecesUsed.Add(s);
                    }
                }
                else if (one.Sum(x => x.Strength) > site.Strength)
                {
                    potentialMoves.Add(new PotentialMove()
                    {
                        Target = site,
                        Value = gaussianProductionRatio / (float)(site.Strength + one.Sum(x => x.Production)),
                        MovesToDo = new List<Move>(one.Select(x => new Move { Site = x, Direction = x.GetDirectionToNeighbour(site) }))
                    });
                    one.ForEach(x => piecesUsed.Add(x));
                }
                else
                {
                    try
                    {
                        potentialMoves.Add(new PotentialMove()
                        {
                            Target = site,
                            Value = gaussianProductionRatio / (float)(site.Strength + one.Sum(x => x.Production) + AverageProduction * ((site.Strength - one.Sum(x => x.Strength)) / one.Sum(x => x.Production))),
                            MovesToDo = new List<Move>(one.Select(x => new Move { Site = x, Direction = Direction.Still }))
                        });
                        one.ForEach(x => piecesUsed.Add(x));
                    }
                    catch(DivideByZeroException) { }
                }
            }
            #endregion

            // TODO second layer moves
            var moarPotentialMoves = new List<PotentialMove>();
            foreach(var site in potentialMoves.Where(x => x.MovesToDo.Any(m => m.Direction == Direction.Still)).Select(x => x.Target))
            {
                int targetStrength = site.Strength;
                int totalStrength = 0;
                var layer2Moves = new List<Move>();
                List<Site> one = site.Neighbors.Where(x => x.Owner == MyId && x.Strength > 1).OrderBy(x => ProductionPotentialDictionary[new Point { X = x.X, Y = x.Y }]).ToList();
                var gaussianProductionRatio = BeginningStrengthDictionary[new Point(site.X, site.Y)];
                foreach (var s1 in one)
                {
                    layer2Moves.Add(new Move { Site = s1, Direction = Direction.Still });
                    totalStrength += s1.Strength;
                    foreach(var s2 in s1.Neighbors.Where(x => x.Owner == MyId && !one.Contains(x) && x.Strength > 1).OrderBy(x => ProductionPotentialDictionary[new Point { X = x.X, Y = x.Y }]))
                    {
                        layer2Moves.Add(new Move { Site = s2, Direction = s2.GetDirectionToNeighbour(s1) });
                        totalStrength += s2.Strength;
                        if (totalStrength > targetStrength)
                            break;
                    }
                    if (totalStrength > targetStrength)
                        break;
                }
                if (totalStrength > targetStrength)
                {
                    moarPotentialMoves.Add(new PotentialMove()
                    {
                        Target = site,
                        Value = (float)gaussianProductionRatio / (float)(site.Strength + layer2Moves.Sum(x => x.Site.Production)),
                        MovesToDo = layer2Moves
                    });
                }
            }
            potentialMoves.AddRange(moarPotentialMoves);

            
            return potentialMoves.OrderByDescending(x => x.Value).ToList(); ;
        }

        private static List<Site> GetTrueNeutralNeighbors(Map m)
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
    }

    struct PotentialMove
    {
        public Site Target;
        public float Value;
        public List<Move> MovesToDo;
    }

    enum WhatDo
    {
        KillOne,
        KillAll,
        Wait
    }
}

