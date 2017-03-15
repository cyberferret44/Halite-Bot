using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Halite
{
    public class CombatLearning
    {
        private static string FilePath = Directory.GetCurrentDirectory() + "\\Learning.txt";
        private static string OldPath = Directory.GetCurrentDirectory() + "\\OldLearning.txt";
        public bool HasData => TrackedSites.Any();
        public List<Site> SitesInCombat => TrackedSites.Select(x => x.MainSite).ToList();
        private List<LearningGrid> TrackedSites;
        public MapStats Stats;

        List<string> Lines;

        public CombatLearning(Map m)
        {
            Stats = new MapStats(m);
            TrackedSites = new List<LearningGrid>();

            foreach(var site in m.MySites)
            {
                var grid = new LearningGrid(site, m);
                if(grid.HasEnemy)
                {
                    TrackedSites.Add(grid);
                }
            }
        }

        public void Record(List<Move> moves)
        {
            Lines = new List<string>();
            TrackedSites.ForEach(ts => Lines.Add(ts.ToString(moves)));
        }

        public void WriteToFile(MapStats nextMapStats)
        {
            foreach(var l in Lines)
            {
                File.AppendAllText(FilePath, l + $"[{Stats.ToString()}],[{nextMapStats.ToString()}]\n");
            }
        }
    }

    public class MapStats
    {
        public int MyStrength;
        public int MyProduction;
        public int EnemyStrength;
        public int EnemyProduction;

        public MapStats(Map m)
        {
            foreach (var site in m.GetSites(s => !s.IsNeutral))
            {
                if (site.IsMine)
                {
                    MyStrength += site.Strength;
                    MyProduction += site.Production;
                }
                else if (site.IsEnemy)
                {
                    EnemyStrength += site.Strength;
                    EnemyProduction += site.Production;
                }
            }
        }

        public MapStats(string line)
        {
            var stats = line.Split(',').Select(x => int.Parse(x)).ToList();

            MyStrength = stats[0];
            MyProduction = stats[1];
            EnemyStrength = stats[2];
            EnemyProduction = stats[3];
        }

        public override string ToString()
        {
            return $"{MyStrength},{MyProduction},{EnemyStrength},{EnemyProduction}";
        }

        public bool Equals(MapStats stats)
        {
            return MyStrength == stats.MyStrength
                && MyProduction == stats.MyProduction
                && EnemyStrength == stats.EnemyStrength
                && EnemyProduction == stats.EnemyProduction;
        }
    }
}
