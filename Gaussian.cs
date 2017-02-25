using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Halite
{
    class Gaussian
    {
        //private static readonly double[] kernel25 = new double[] { 0.000331f, 0.000842f, 0.001972f, 0.00426f, 0.008486f, 0.015589f, 0.026406f, 0.041244f, 0.059405f, 0.078898f, 0.096627f, 0.109123f, 0.113637f, 0.109123f, 0.096627f, 0.078898f, 0.059405f, 0.041244f, 0.026406f, 0.015589f, 0.008486f, 0.00426f, 0.001972f, 0.000842f, 0.000331f };
        //private static readonly double[] kernel11 = new double[] { 0.001227f, 0.008468f, 0.037984f, 0.110892f, 0.210838f, 0.261182f, 0.210838f, 0.110892f, 0.037984f, 0.008468f, 0.001227f };
        private static readonly double[] kernel7 = new double[] { 0.06136,0.24477, 0.38774, 0.24477, 0.06136 };

        //public static void SetKernel(Map map, bool bob = true)
        //{
        //    //int mapSize = map.Height * map.Width;
        //    //int numPlayers = map.GetSites(x => x.Owner > 0).Distinct().Count();
        //    //int gaussSize = (int)Math.Round(Math.Sqrt((double)mapSize / (double)numPlayers));
        //    //gaussSize = gaussSize % 2 == 0 ? gaussSize - 1 : gaussSize;
        //    //Kernel = bob ? kernel25 : kernel11;
        //    Kernel = kernel7;
        //}

        private static double[] Kernel = kernel7;

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

            //foreach (var s in map.GetMySites())
            //{
            //    startingHeuristic.Update(s, 0f);
            //}
            //foreach(var s in map.GetAllSites())
            //{
            //    startingHeuristic.Update(s, Math.Pow(startingHeuristic.Get(s).Value, 1.5));
            //}

            //Heuristic bigShade = SimpleShade(startingHeuristic, map);
            //SetKernel(map, false);
            //Heuristic littleShade = SimpleShade(startingHeuristic, map);
            int mapSize = map.Height * map.Width;
            int numPlayers = map.GetSites(x => x.Owner > 0).Distinct().Count();
            int iterations = (int)Math.Round(Math.Sqrt((double)mapSize / (double)numPlayers)) / 4;
            //Heuristic littleHeuristic = SimpleShade(startingHeuristic, map);
            //littleHeuristic = SimpleShade(littleHeuristic, map);
            Heuristic shadedHeuristic = SimpleShade(startingHeuristic, map);
            for (int i=0; i <= iterations; i++)
            {
                shadedHeuristic = SimpleShade(shadedHeuristic, map);
                //foreach (var s in map.GetAllSites())
                //{
                //    startingHeuristic.Update(s, Math.Pow(shadedHeuristic.Get(s).Value, 1.2) * 10);
                //}
            }

            //Heuristic resultHeuristic = new Heuristic();
            //foreach (var kvp in startingHeuristic.GetDictionary())
            //{
            //    //var littleVal = littleHeuristic.Get(kvp.Key).Value;
            //    var biggerVal = shadedHeuristic.Get(kvp.Key).Value;
            //    //double newValue = kvp.Key.Production != 0 ? biggerVal * .9 + kvp.Value.Value * .1 : biggerVal * .1f;
            //    resultHeuristic.AddNew(kvp.Key, kvp.Key.Production, kvp.Key.Strength, newValue);
            //}
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
    }
}
