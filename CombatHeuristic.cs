using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    class CombatHeuristic
    {
        private static Dictionary<string, DirectionValues> combatHeuristic = new Dictionary<string, DirectionValues>();
        private static string FilePath = Directory.GetCurrentDirectory() + "\\Learning.txt";
        static Random r = new Random();
        //private static string OldPath = Directory.GetCurrentDirectory() + "\\OldLearning.txt";

        public static Direction getBestMove(string state)
        {
            if (!combatHeuristic.ContainsKey(state))
                return (Direction)r.Next(0, 5);
            else
                return combatHeuristic[state].BestMove;
        }

        private class DirectionValues
        {
            Dictionary<Direction, double> moveValues;
            public DirectionValues()
            {
                moveValues = new Dictionary<Direction, double>();
                foreach (var d in Enum.GetValues(typeof(Direction)))
                {
                    moveValues.Add((Direction)d, 1.0);
                }
            }

            public void AddValue(Direction d, double newValue)
            {
                moveValues[d] = GetNewValue(moveValues[d], newValue);
            }

            private static double GetNewValue(double current, double newValue)
            {
                return ((current * .98) + (newValue * .02)) / current;
            }

            public Direction BestMove => moveValues.OrderByDescending(x => x.Value).First().Key;
        }
        /// <summary>
        /// open the combat heuristic file.
        /// read the values into the dictionary
        /// 
        /// open the Learning.txt file
        /// read the values in and add them to the combatHeuristic
        /// </summary>
        public static void PopulateCombarHeuristic()
        {
            combatHeuristic.Clear();
            string line;
            while((line = reader.ReadLine()) != null)//foreach (var model in GetModels(FilePath))
            {
                try
                {
                    var model = GetModel(line);
                    string state = GetState(model);
                    if (!combatHeuristic.ContainsKey(state))
                        combatHeuristic.Add(state, new DirectionValues());

                    double value = 0.0;
                    for (int i = 0; i < model.Results.Count - 1; i++)
                    {
                        var startResults = model.Results[i];
                        var endResults = model.Results[i + 1];
                        int myDeltaStrength = endResults.MyStrength - startResults.MyStrength;
                        int myDeltaProduction = endResults.MyProduction - startResults.MyProduction;
                        int enDeltaStrength = endResults.EnemyStrength - startResults.EnemyStrength;
                        int enDeltaProduction = endResults.EnemyProduction - startResults.EnemyProduction;

                        int deltaStrength = myDeltaStrength - enDeltaStrength;
                        int deltaProduction = myDeltaProduction - enDeltaProduction;

                        value += deltaStrength + (4 * deltaProduction);
                    }
                    value /= model.Results.Count;

                    combatHeuristic[state].AddValue(model.BaseSite.Direction, value);
                }
                catch (Exception) { }
            }
        }

        private static StreamReader reader = new StreamReader(FilePath);
        //static List<Model> GetModels(string FilePath)
        //{
        //    ////List<Model> activeModels = new List<Model>();
            
        //    //string line;
        //    //while ((line = reader.ReadLine()) != null)
        //    //{
        //    //    Model model;
        //    //    try
        //    //    {
        //    //        model = GetModel(line);
        //    //        // Next turn
        //    //        //if (!models.Any() || model.Results[0] == models.Last().Results[1])
        //    //        //{
        //    //        //    activeModels.ForEach(a => a.Results.Add(model.Results.Last()));
        //    //        //    activeModels = activeModels.Where(x => x.Results.Count < 4).ToList();
        //    //        //}
        //    //        // Different game
        //    //        //if (models.Any() && model.Results[0] == models.Last().Results[0])
        //    //        //{
        //    //        //    activeModels.Clear();
        //    //        //}
        //    //        models.Add(model);
        //    //        if (model.Results.Count < 4)
        //    //            activeModels.Add(model);
        //    //    }
        //    //    catch (Exception) { }  // couldn't parse or couldn't read line
        //    //}
        //    //return models;
        //}


        private static Model GetModel(string line)
        {
            Model model = new Model() { SitesAndMoves = new List<Move>(), Results = new List<MapStats>() };

            line = line.Substring(1, line.Length - 2); // cut off the start and final bracket
            var list = line.Split(new string[] { "],[" }, StringSplitOptions.None).ToList();
            foreach (var point in list.GetRange(0, 25))
            {
                var pointValues = point.Split(',').ToList();
                if (pointValues.Count == 6)
                    pointValues = pointValues.GetRange(2, pointValues.Count - 2);
                Move move = new Move() { Direction = (Direction)Enum.Parse(typeof(Direction), pointValues[0]), Site = new Site(0, 0) { Strength = ushort.Parse(pointValues[1]), Production = ushort.Parse(pointValues[2]), Owner = ushort.Parse(pointValues[3]) } };
                model.SitesAndMoves.Add(move);
            }

            list.RemoveRange(0, 25);
            list.ForEach(l => model.Results.Add(new MapStats(l)));

            return model;
        }

        private struct Model
        {
            public List<Move> SitesAndMoves;
            public List<MapStats> Results;
            public Move BaseSite => SitesAndMoves[12];
        }

        private static string GetState(Model model)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (var m in model.SitesAndMoves)
            {
                if (m.Site.IsNeutral)
                {
                    string val = m.Site.Strength > 0 ? "1" : "0";
                    sb.Append($"N{ val}");
                }
                else
                {
                    sb.Append(m.Site.IsEnemy ? "E" : "A");
                    if (IsFirstOrLast(i))
                    {
                        int val = m.Site.Strength == 0 ? 0 : m.Site.Strength < 5 * m.Site.Production ? 1 : 2;
                        sb.Append(val);
                    }
                    else
                    {
                        sb.Append((int)Math.Ceiling((double)m.Site.Strength / 10.0));
                    }
                }
                i++;
            }
            return sb.ToString();
        }

        public static bool IsFirstOrLast(int pos) => pos == 0 ||
                                                  pos == 1 ||
                                                  pos == 3 ||
                                                  pos == 4 ||
                                                  pos == 8 ||
                                                  pos == 9 ||
                                                  pos == 15 ||
                                                  pos == 16 ||
                                                  pos == 20 ||
                                                  pos == 21 ||
                                                  pos == 23 ||
                                                  pos == 24;
    }
}
