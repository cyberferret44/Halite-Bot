using System.Collections.Generic;
using System.Linq;

namespace Halite
{
    public class SlimeHeuristic
    {
        public Heuristic GetSlimeHeuristic(Map map)
        {
            var baseHeuristic = Heuristic.GetStartingHeuristic(map);
            var result = Heuristic.GetStartingHeuristic(map);
            var allSites = map.GetAllSites();
            int totalStrength = map.StrengthPerPlayer / (map.Height + map.Width);

            allSites.ForEach(s => baseHeuristic.Update(s, (double)s.Production * s.BasicValue));
            allSites.ForEach(s => result.Update(s, 0));
            allSites.ForEach(s => ComputeSiteSpread(s, baseHeuristic, result, totalStrength));

            double averageExpansionValue = result.GetDictionary().Average(x => x.Value.Value);
            double averageValue = map.GetAllSites().Average(x => x.BasicValue);
            double ValueMultiplier = averageExpansionValue / averageValue;
            foreach (var r in map.GetAllSites())
            {
                var val = result.Get(r);
                val.Value = val.Value * .9 + r.BasicValue * ValueMultiplier * .1;
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