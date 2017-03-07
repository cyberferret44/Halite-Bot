using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Halite
{
    public class CombatLearning
    {
        //                    ___
        //                   | 1 |
        //                ___|___|___
        //               | 2 | 3 | 4 |
        //            ___|___|___|___|___
        //           | 5 | 6 | 7 | 8 | 9 |
        //        ___|___|___|___|___|___|___
        //       | 10| 11| 12| 13| 14| 15| 16|
        //       |___|___|___|___|___|___|___|
        //           | 17| 18| 19| 20| 21|
        //           |___|___|___|___|___|
        //               | 22| 23| 24|
        //               |___|___|___|
        //                   | 25|
        //                   |___|
        private static readonly int Depth = 3;
        private static string FilePath = Directory.GetCurrentDirectory() + "\\Learning.txt";
        public bool HasData => TrackedSites.Any();
        public List<Site> SitesInCombat => TrackedSites.Select(x => x.MainSite).ToList();
        private List<Grid> TrackedSites;
        public MapStats Stats;

        List<string> Lines;

        public CombatLearning(Map m)
        {
            Stats = new MapStats(m);
            TrackedSites = new List<Grid>();

            foreach(var site in m.MySites)
            {
                var grid = new Grid(site, m);
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
        
        private class Grid
        {
            List<Site> grid = new List<Site>();
            public Site MainSite;
            public Grid(Site site, Map map)
            {
                MainSite = site;
                int i = 0;
                for(int y = -Depth; y <= Depth; y++)
                {
                    for(int x = -(Depth - Math.Abs(y)); x <= Depth - Math.Abs(y); x++)
                    {
                        grid.Add(map[site.X + x, site.Y + y]);
                        i++;
                    }
                }
            }

            public bool HasEnemy => grid.Any(s => s.IsEnemy);

            public string ToString(List<Move> moves)
            {
                StringBuilder result = new StringBuilder();
                grid.ForEach(g => result.Append($"[{g.X},{g.Y},{moves.FirstOrDefault(x => x.Site == g)?.Direction ?? Direction.Still},{g.Strength},{g.Production},{g.Owner}],"));
                return result.ToString();
            }
        }

        public class MapStats
        {
            private int MyStrength;
            private int MyProduction;
            private int EnemyStrength;
            private int EnemyProduction;

            public MapStats(Map m)
            {
                foreach(var site in m.GetSites(s => !s.IsNeutral))
                {
                    if (site.IsMine)
                    {
                        MyStrength += site.Strength;
                        MyProduction += site.Production;
                    }
                    else if(site.IsEnemy)
                    {
                        EnemyStrength += site.Strength;
                        EnemyProduction += site.Production;
                    }
                }
            }

            public override string ToString()
            {
                return $"{MyStrength},{MyProduction},{EnemyStrength},{EnemyProduction}";
            }
        }
    }
}
