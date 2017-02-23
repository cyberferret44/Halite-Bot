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
        private static readonly float[,] kernel7 = new float[,]
        {
            {0.000036f,    0.000363f,    0.001446f,    0.002291f,    0.001446f,    0.000363f,    0.000036f},
            {0.000363f,    0.003676f,    0.014662f,    0.023226f,    0.014662f,    0.003676f,    0.000363f},
            {0.001446f,    0.014662f,    0.058488f,    0.092651f,    0.058488f,    0.014662f,    0.001446f},
            {0.002291f,    0.023226f,    0.092651f,    0.146768f,    0.092651f,    0.023226f,    0.002291f},
            {0.001446f,    0.014662f,    0.058488f,    0.092651f,    0.058488f,    0.014662f,    0.001446f},
            {0.000363f,    0.003676f,    0.014662f,    0.023226f,    0.014662f,    0.003676f,    0.000363f},
            {0.000036f,    0.000363f,    0.001446f,    0.002291f,    0.001446f,    0.000363f,    0.000036f}
        };

        private static readonly float[,] kernel9 = new float[,]
        {
            { 0.000398f, 0.001142f, 0.002423f, 0.003805f, 0.004422f, 0.003805f, 0.002423f, 0.001142f, 0.000398f },
            { 0.001142f, 0.003274f, 0.006947f, 0.010908f, 0.012679f, 0.010908f, 0.006947f, 0.003274f, 0.001142f },
            { 0.002423f, 0.006947f, 0.014739f, 0.023143f, 0.026899f, 0.023143f, 0.014739f, 0.006947f, 0.002423f },
            { 0.003805f, 0.010908f, 0.023143f, 0.036340f, 0.042238f, 0.036340f, 0.023143f, 0.010908f, 0.003805f },
            { 0.004422f, 0.012679f, 0.026899f, 0.042238f, 0.049093f, 0.042238f, 0.026899f, 0.012679f, 0.004422f },
            { 0.003805f, 0.010908f, 0.023143f, 0.036340f, 0.042238f, 0.036340f, 0.023143f, 0.010908f, 0.003805f },
            { 0.002423f, 0.006947f, 0.014739f, 0.023143f, 0.026899f, 0.023143f, 0.014739f, 0.006947f, 0.002423f },
            { 0.001142f, 0.003274f, 0.006947f, 0.010908f, 0.012679f, 0.010908f, 0.006947f, 0.003274f, 0.001142f },
            { 0.000398f, 0.001142f, 0.002423f, 0.003805f, 0.004422f, 0.003805f, 0.002423f, 0.001142f, 0.000398f }
        };

        //private static readonly float[,] kernel11 = new float[,]
        //{
        //    { 0.000513f, 0.001045f, 0.001815f, 0.002694f, 0.003414f, 0.003694f, 0.003414f, 0.002694f, 0.001815f, 0.001045f, 0.000513f },
        //    { 0.001045f, 0.002126f, 0.003694f, 0.005482f, 0.006947f, 0.007518f, 0.006947f, 0.005482f, 0.003694f, 0.002126f, 0.001045f },
        //    { 0.001815f, 0.003694f, 0.006420f, 0.009527f, 0.012073f, 0.013065f, 0.012073f, 0.009527f, 0.006420f, 0.003694f, 0.001815f },
        //    { 0.002694f, 0.005482f, 0.009527f, 0.014138f, 0.017916f, 0.019388f, 0.017916f, 0.014138f, 0.009527f, 0.005482f, 0.002694f },
        //    { 0.003414f, 0.006947f, 0.012073f, 0.017916f, 0.022704f, 0.024568f, 0.022704f, 0.017916f, 0.012073f, 0.006947f, 0.003414f },
        //    { 0.003694f, 0.007518f, 0.013065f, 0.019388f, 0.024568f, 0.026586f, 0.024568f, 0.019388f, 0.013065f, 0.007518f, 0.003694f },
        //    { 0.003414f, 0.006947f, 0.012073f, 0.017916f, 0.022704f, 0.024568f, 0.022704f, 0.017916f, 0.012073f, 0.006947f, 0.003414f },
        //    { 0.002694f, 0.005482f, 0.009527f, 0.014138f, 0.017916f, 0.019388f, 0.017916f, 0.014138f, 0.009527f, 0.005482f, 0.002694f },
        //    { 0.001815f, 0.003694f, 0.006420f, 0.009527f, 0.012073f, 0.013065f, 0.012073f, 0.009527f, 0.006420f, 0.003694f, 0.001815f },
        //    { 0.001045f, 0.002126f, 0.003694f, 0.005482f, 0.006947f, 0.007518f, 0.006947f, 0.005482f, 0.003694f, 0.002126f, 0.001045f },
        //    { 0.000513f, 0.001045f, 0.001815f, 0.002694f, 0.003414f, 0.003694f, 0.003414f, 0.002694f, 0.001815f, 0.001045f, 0.000513f }
        //};

        public static void SetKernel(Map map)
        {
            int mapSize = map.Height * map.Width;
            int numPlayers = map.GetSites(x => x.Owner > 0).Count;
            int squaresPerPlayer = mapSize / numPlayers;

            if (squaresPerPlayer > 400 || mapSize > 1200)
            {
                Reduce = true;
                squaresPerPlayer /= 4;
            }

            if (squaresPerPlayer <= 100)
            {
                Kernel = kernel7;
            }
            else
            {
                Kernel = kernel9;
            }
        }

        private static float[,] Kernel;
        private static bool Reduce = false;

        public static Heuristic<Site> ShadeHeuristic(Heuristic<Site> startingHeuristic, Map map, bool noCombat)
        {
            var all = map.GetAllSites();
            
            // Average out enemy values so that our algorithm doesn't seek them out
            if(noCombat)
            {
                float averageProduction = (float)all.Sum(x => x.Production) / (float)all.Count;
                float averageStrength = (float)all.Sum(x => x.Strength) / (float)all.Count;
                float averageValue = averageProduction / averageStrength;
                foreach (var s in map.GetSites(x => x.IsEnemy))
                {
                    startingHeuristic.Update(s, averageValue);
                }
            }

            foreach(var s in map.GetMySites())
            {
                startingHeuristic.Update(s, 0f);
            }

            Heuristic<Point> resultHeuristic = null;
            if (Reduce)
            {
                resultHeuristic = ReduceShade(startingHeuristic, map.Width, map.Height);
            }
            else
            {
                var tempHeuristic = new Heuristic<Point>();
                foreach(var v in startingHeuristic.GetDictionary())
                {
                    tempHeuristic.AddNew(new Point(v.Key.X, v.Key.Y), v.Value);
                }
                resultHeuristic = SimpleShade(tempHeuristic, map.Width, map.Height, Kernel);
            }

            foreach (var kvp in startingHeuristic.GetDictionary())
            {
                var p = new Point(kvp.Key.X, kvp.Key.Y);
                var resultVal = resultHeuristic.Get(p);
                float newValue = resultVal.Production != 0 ? resultVal.Value * .9f + kvp.Value.Value * .1f : resultVal.Value * .1f;
                resultHeuristic.Update(p, newValue);
            }

            var finalResult = new Heuristic<Site>();
            foreach(var kvp in resultHeuristic.GetDictionary())
            {
                finalResult.AddNew(map[kvp.Key.X, kvp.Key.Y], kvp.Value);
            }
            return finalResult;
        }

        public static Heuristic<Point> ReduceShade(Heuristic<Site> startingHeuristic, int width, int height)
        {
            var reducedHeuristic = new Heuristic<Point>();
            // step 1 create new map
            int newWidth = (int)Math.Ceiling((float)width / 2f);
            int newHeight = (int)Math.Ceiling((float)height / 2f);
            for (int x=0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    reducedHeuristic.AddNew(new Point { X = x, Y = y }, 0f, 0f, 0f);
                }
            }

            foreach (var kvp in startingHeuristic.GetDictionary())
            {
                Point p = new Point { X = kvp.Key.X / 2, Y = kvp.Key.Y / 2 };
                float mult = .25f;
                mult *= p.X == width && p.X % 2 == 1 ? 2 : 1;
                mult *= p.Y == height && p.Y % 2 == 1 ? 2 : 1;
                reducedHeuristic.Increase(p, kvp.Value.Production * mult, kvp.Value.Strength * mult, kvp.Value.Value * mult);
            }

            // call shader
            reducedHeuristic = SimpleShade(reducedHeuristic, newWidth, newHeight, Kernel);

            var result = new Heuristic<Point>();

            // undo the map creation
            for (int x = 0; x <width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Point p = new Point { X = x / 2, Y = y / 2 };
                    var value = reducedHeuristic.Get(p);
                    result.AddNew(new Point { X = x, Y = y }, value);
                }
            }

            return SimpleShade(result, width, height, kernel7);
        }

        public static Heuristic<Point> SimpleShade(Heuristic<Point> baseHeuristic, int width, int height, float[,] kernelToUse)
        {
            int Size = kernelToUse.GetLength(1);
            int Center = (int)(Size / 2);

            Dictionary<Point, float> dictionary = new Dictionary<Point, float>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    dictionary.Add(new Point(x, y), 0f);
                }
            }

            foreach (var kvp in baseHeuristic.GetDictionary())
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        int deltaX = x - Center;
                        int deltaY = y - Center;
                        Point p = Truncate(kvp.Key.X + deltaX, kvp.Key.Y + deltaY, height, width);
                        dictionary[p] += kvp.Value.Value * kernelToUse[x, y];
                    }
                }
            }

            foreach (var kvp in dictionary)
            {
                baseHeuristic.Update(kvp.Key, kvp.Value);
            }
            return baseHeuristic;
        }

        public static Point Truncate(int x, int y, int height, int width)
        {
            return new Point()
            {
                X = x < 0 ? width + x : x >= width ? x - width : x,
                Y = y < 0 ? height + y : y >= height ? y - height : y
            };
        }
    }
}
