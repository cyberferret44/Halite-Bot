using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public static class SlimeHeuristic
    {
        public static double SpreadVal(double production, Site s) => (production) * s.Production * (255 - s.Strength) / 255; // This won't work


        // this should only be called once per game, to get the initial field
        public static Heuristic GetSlimeHeuristic(Heuristic startingHeuristic, Map map)
        {
            Heuristic slime = new Heuristic();

            foreach(var site in map.GetAllSites())
            {
                slime.AddNew(site, site.Production, site.Strength);
            }

            for(int i=0; i < map.AreaPerPlayer; i++)
            {
                foreach(var site in map.GetAllSites())
                {
                    SpreadToNeighbors(site, slime, startingHeuristic);
                }
            }

            foreach(var site in map.GetAllSites())
            {
                var val = slime.Get(site);
                slime.Update(site, val.Production, val.Strength, val.Production / val.Strength); // impossible to break
            }

            return slime;
        }

        public static void SpreadToNeighbors(Site s, Heuristic h, Heuristic starting)
        {
            var siteVal = starting.Get(s);
            foreach(var neighbor in s.ThreatZone)
            {
                h.AddValue(neighbor, SpreadVal(s.Production, neighbor), 0.0);
            }
        }
    }
}
