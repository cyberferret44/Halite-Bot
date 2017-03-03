using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public static class SlimeHeuristic
    {
        // this should only be called once per game, to get the initial field
        public static Heuristic GetSlimeHeuristic(Heuristic startingHeuristic, Map map)
        {
            Heuristic spreadHeuristic = startingHeuristic.Clone();
            Heuristic slime = startingHeuristic.Clone();

            foreach(var kvp in spreadHeuristic.GetDictionary())
            {
                spreadHeuristic.Update(kvp.Key, Math.Sqrt(Math.Pow(kvp.Value.Production, 2) / Math.Pow(kvp.Value.Strength, 2)));
            }

            for (int i = 0; i < Math.Sqrt(map.AreaPerPlayer) * 4; i++)
            {
                foreach (var site in map.GetAllSites())
                {
                    SpreadToNeighbors(site, slime);
                }
            }

            foreach (var site in map.GetAllSites())
            {
                var val = slime.Get(site);
                slime.Update(site, val.Production, val.Strength, val.Production / val.Strength); // impossible to break
            }

            return slime;
        }

        public static void SpreadToNeighbors(Site s, Heuristic h)
        {
            var siteProduction = h.Get(s).Production;
            foreach (var neighbor in s.ThreatZone)
            {
                double spreadVal = siteProduction * (neighbor.Production * (255 - neighbor.Strength) / 255); // This won't work
                h.AddValue(neighbor, spreadVal, 0.0);
            }
        }
    }
}
