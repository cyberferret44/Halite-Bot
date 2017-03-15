using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
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

    public class LearningGrid
    {
        List<Site> grid = new List<Site>();
        public Site MainSite;
        private static readonly int Depth = 3;
        public LearningGrid(Site site, Map map)
        {
            MainSite = site;
            int i = 0;
            for (int y = -Depth; y <= Depth; y++)
            {
                for (int x = -(Depth - Math.Abs(y)); x <= Depth - Math.Abs(y); x++)
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (var m in grid)
            {
                if (m.IsNeutral)
                {
                    string val = m.Strength > 0 ? "1" : "0";
                    sb.Append($"N{ val}");
                }
                else
                {
                    sb.Append(m.IsEnemy ? "E" : "A");
                    if (CombatHeuristic.IsFirstOrLast(i))
                    {
                        int val = m.Strength == 0 ? 0 : m.Strength < 5 * m.Production ? 1 : 2;
                        sb.Append(val);
                    }
                    else
                    {
                        sb.Append((int)Math.Ceiling((double)m.Strength / 10.0));
                    }
                }
                i++;
            }
            return sb.ToString();
        }
    }
}
