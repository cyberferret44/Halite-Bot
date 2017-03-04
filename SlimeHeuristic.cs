using System.Collections.Generic;
using System.Linq;

namespace Halite
{
    public class SlimeHeuristic
    {
        public Heuristic GetSlimeHeuristic(Map map)
        {
            var baseHeuristic = Heuristic.GetStartingHeuristic(map);
            foreach (var s in map.GetAllSites())
            {
                baseHeuristic.Update(s, (double)s.Production * (double)s.Production / (double)s.Strength);
            }
            var result = Heuristic.GetStartingHeuristic(map);
            foreach(var s in map.GetAllSites())
            {
                result.Update(s, 0);
            }

            int totalStrength = map.StrengthPerPlayer / (map.Height + map.Width);
            foreach (var site in map.GetAllSites())
            {
                ComputeSiteSpread(site, baseHeuristic, result, totalStrength);
            }

            return result;
        }

        public void ComputeSiteSpread(Site baseSite, Heuristic Base, Heuristic result, int totalStrength)
        {
            var workingOn = new HashSet<Site>();
            var done = new HashSet<Site>();
            var startingStrengthDictionary = new Dictionary<Site, int>();
            workingOn.Add(baseSite); // base case
            startingStrengthDictionary.Add(baseSite, totalStrength);

            double baseSiteValue = Base.Get(baseSite).Value;

            // Giant iterator...
            for (int i = totalStrength; i >= 0; i--)
            {
                double ratioA = (double)i / (double)totalStrength;
                var temp = workingOn.Select(x => x).ToList();
                foreach (var site in temp)
                {
                    double ratioB = 1 / (double)site.Strength;
                    double multiplier = ratioA * ratioB;
                    double spreadValue = baseSiteValue * multiplier;
                    result.AddValue(site, spreadValue);
                    if (startingStrengthDictionary[site] - i == site.Strength - 1)
                    {
                        workingOn.Remove(site);
                        done.Add(site);
                        foreach (var neighbor in site.Neighbors)
                        {
                            if (!workingOn.Contains(neighbor) && !done.Contains(neighbor))
                            {
                                workingOn.Add(neighbor);
                                startingStrengthDictionary.Add(neighbor, i - 1);
                            }
                        }
                    }
                }
            }
        }
    }
}


//// this should only be called once per game, to get the initial field
//public static Heuristic GetSlimeHeuristic(Heuristic startingHeuristic, Map map)
//{
//    Heuristic spreadHeuristic = startingHeuristic.Clone();
//    Heuristic slime = startingHeuristic.Clone();

//    foreach(var kvp in spreadHeuristic.GetDictionary())
//    {
//        spreadHeuristic.Update(kvp.Key, Math.Sqrt(Math.Pow(kvp.Value.Production, 2) / Math.Pow(kvp.Value.Strength, 2)));
//    }

//    for (int i = 0; i < Math.Sqrt(map.AreaPerPlayer) * 4; i++)
//    {
//        foreach (var site in map.GetAllSites())
//        {
//            SpreadToNeighbors(site, slime);
//        }
//    }

//    foreach (var site in map.GetAllSites())
//    {
//        var val = slime.Get(site);
//        slime.Update(site, val.Production, val.Strength, val.Production / val.Strength); // impossible to break
//    }

//    return slime;
//}

//public static void SpreadToNeighbors(Site s, Heuristic h)
//{
//    var siteProduction = h.Get(s).Production;
//    foreach (var neighbor in s.ThreatZone)
//    {
//        double spreadVal = siteProduction * (neighbor.Production * (255 - neighbor.Strength) / 255); // This won't work
//        h.AddValue(neighbor, spreadVal, 0.0);
//    }
//}