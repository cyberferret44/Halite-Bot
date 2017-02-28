using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Halite
{
    public static class Gaussian
    {
        private static readonly double[] Kernel = new double[] { 0.06136,0.24477, 0.38774, 0.24477, 0.06136 };

        public static Heuristic ShadeHeuristic(Heuristic startingHeuristic, Map map, bool noCombat)
        {
            var all = map.GetAllSites();
            if (noCombat)
            {
                double averageProduction = (double)all.Sum(x => x.Production) / (double)all.Count;
                double averageStrength = (double)all.Sum(x => x.Strength) / (double)all.Count;
                double averageValue = averageProduction / averageStrength;
                foreach (var s in map.GetSites(x => x.IsEnemy))
                {
                    startingHeuristic.Update(s, averageProduction, averageStrength, averageValue);
                }
            }
            int mapSize = map.Height * map.Width;
            int numPlayers = map.GetSites(x => x.Owner > 0).Distinct().Count();
            int iterations = (int)Math.Round(Math.Sqrt((double)mapSize / (double)numPlayers)) / 4;

            Heuristic shadedHeuristic = SimpleShade(startingHeuristic, map);
            for (int i=0; i <= iterations; i++)
            {
                shadedHeuristic = SimpleShade(shadedHeuristic, map);
            }

            Heuristic result = new Heuristic();
            foreach(var v in shadedHeuristic.GetDictionary())
            {
                result.AddNew(v.Key, v.Value.Production, v.Value.Strength, v.Value.Production/ v.Value.Strength);
            }
            return result;
        }

        public static Heuristic SimpleShade(Heuristic baseHeuristic, Map map)
        {
            Heuristic firstDictionary = new Heuristic();
            Heuristic lastDictionary = new Heuristic();
            int kernelSize = Kernel.Length;

            foreach (var kvp in baseHeuristic.GetDictionary())
            {
                firstDictionary.AddNew(kvp.Key, 0.0, 0.0, 0.0);
                lastDictionary.AddNew(kvp.Key, 0.0, 0.0, 0.0);
            }

            // Horizonotal Shade
            foreach (var kvp in baseHeuristic.GetDictionary())
            {
                for (int x = 0; x < Kernel.Length; x++)
                {
                    int deltaX = x - kernelSize;
                    Site target = map[kvp.Key.X + deltaX, kvp.Key.Y];
                    var seedSite = kvp.Key.IsMine ? baseHeuristic.Get(target) : kvp.Value;
                    firstDictionary.AddValue(target, seedSite.Production * Kernel[x], seedSite.Strength * Kernel[x]);// += kvp.Value.Value * Kernel[x];
                }
            }

            // Vertical Shade
            foreach (var kvp in firstDictionary.GetDictionary())
            {
                for (int y = 0; y < Kernel.Length; y++)
                {
                    int deltaY = y - kernelSize;
                    Site target = map[kvp.Key.X, kvp.Key.Y + deltaY];
                    var seedSite = kvp.Key.IsMine ? firstDictionary.Get(target) : kvp.Value;
                    lastDictionary.AddValue(target, seedSite.Production * Kernel[y], seedSite.Strength * Kernel[y]); // += kvp.Value * Kernel[y];
                }
            }

            Heuristic result = new Heuristic();
            foreach (var kvp in lastDictionary.GetDictionary())
            {
                result.AddNew(kvp.Key, kvp.Value.Production, kvp.Value.Strength, 0.0);
            }
            return result;
        }

        public static Heuristic ShadeValues(Heuristic baseHeuristic, Map map)
        {
            Heuristic firstDictionary = new Heuristic();
            Heuristic lastDictionary = new Heuristic();
            int kernelSize = Kernel.Length;

            foreach (var kvp in baseHeuristic.GetDictionary())
            {
                firstDictionary.AddNew(kvp.Key, kvp.Value.Production, kvp.Value.Strength, 0.0);
                lastDictionary.AddNew(kvp.Key, kvp.Value.Production, kvp.Value.Strength, 0.0);
            }

            // Horizonotal Shade
            foreach (var kvp in baseHeuristic.GetDictionary())
            {
                for (int x = 0; x < Kernel.Length; x++)
                {
                    int deltaX = x - kernelSize;
                    Site target = map[kvp.Key.X + deltaX, kvp.Key.Y];
                    firstDictionary.AddValue(target, 0.0, 0.0, kvp.Value.Value * Kernel[x]);
                }
            }

            // Vertical Shade
            foreach (var kvp in firstDictionary.GetDictionary())
            {
                for (int y = 0; y < Kernel.Length; y++)
                {
                    int deltaY = y - kernelSize;
                    Site target = map[kvp.Key.X, kvp.Key.Y + deltaY];
                    lastDictionary.AddValue(target, 0.0, 0.0, kvp.Value.Value * Kernel[y]);
                }
            }

            return lastDictionary;
        }
    }
}
