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
        //    { 0.001283f, 0.002106f, 0.003096f, 0.004077f, 0.004809f, 0.005081f, 0.004809f, 0.004077f, 0.003096f, 0.002106f, 0.001283f },
        //    { 0.002106f, 0.003456f, 0.005081f, 0.006691f, 0.007892f, 0.008339f, 0.007892f, 0.006691f, 0.005081f, 0.003456f, 0.002106f },s
        //    { 0.003096f, 0.005081f, 0.007469f, 0.009836f, 0.011602f, 0.012258f, 0.011602f, 0.009836f, 0.007469f, 0.005081f, 0.003096f },
        //    { 0.004077f, 0.006691f, 0.009836f, 0.012952f, 0.015277f, 0.016142f, 0.015277f, 0.012952f, 0.009836f, 0.006691f, 0.004077f },
        //    { 0.004809f, 0.007892f, 0.011602f, 0.015277f, 0.018020f, 0.019040f, 0.018020f, 0.015277f, 0.011602f, 0.007892f, 0.004809f },
        //    { 0.005081f, 0.008339f, 0.012258f, 0.016142f, 0.019040f, 0.020117f, 0.019040f, 0.016142f, 0.012258f, 0.008339f, 0.005081f },
        //    { 0.004809f, 0.007892f, 0.011602f, 0.015277f, 0.018020f, 0.019040f, 0.018020f, 0.015277f, 0.011602f, 0.007892f, 0.004809f },
        //    { 0.004077f, 0.006691f, 0.009836f, 0.012952f, 0.015277f, 0.016142f, 0.015277f, 0.012952f, 0.009836f, 0.006691f, 0.004077f },
        //    { 0.003096f, 0.005081f, 0.007469f, 0.009836f, 0.011602f, 0.012258f, 0.011602f, 0.009836f, 0.007469f, 0.005081f, 0.003096f },
        //    { 0.002106f, 0.003456f, 0.005081f, 0.006691f, 0.007892f, 0.008339f, 0.007892f, 0.006691f, 0.005081f, 0.003456f, 0.002106f },
        //    { 0.001283f, 0.002106f, 0.003096f, 0.004077f, 0.004809f, 0.005081f, 0.004809f, 0.004077f, 0.003096f, 0.002106f, 0.001283f }
        //};

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
            else if (squaresPerPlayer <= 200)
            {
                Kernel = kernel9;
            }

            Size = Kernel.GetLength(1);
            Center = (int)(Size / 2);
        }

        private static float[,] Kernel;
        private static int Size = 0;
        private static int Center = 0;
        private static bool Reduce = false;

        public static Dictionary<Point, Heuristic> ShadeHeuristic(Dictionary<Point, Heuristic> dict, Map map)
        {
            foreach (var enemy in map.GetSites(x => x.IsEnemy))
            {
                var h = dict[new Point { X = enemy.X, Y = enemy.Y }];
                dict[new Point { X = enemy.X, Y = enemy.Y }] = new Heuristic { Production = h.Production, Strength = h.Strength, Value = h.Value * .7f }; //TODO target weak enemies
            }

            var resultHeuristic = new Dictionary<Point, Heuristic>();
            if (Reduce)
            {
                resultHeuristic = ReduceShade(dict, map);
            }
            else
            {
                resultHeuristic = SimpleShade(dict, map.Width, map.Height);
            }

            foreach (var kvp in dict)
            {
                var val = resultHeuristic[kvp.Key];
                float newValue = val.Production != 0 ? resultHeuristic[kvp.Key].Value * .9f + kvp.Value.Value * .1f : resultHeuristic[kvp.Key].Value * .1f;
                resultHeuristic[kvp.Key] = new Heuristic { Production = val.Production, Strength = val.Strength, Value = newValue };
            }

            return resultHeuristic;
        }

        public static Dictionary<Point, Heuristic> ReduceShade(Dictionary<Point, Heuristic> dict, Map map)
        {
            var newMap = new Dictionary<Point, Heuristic>();
            // step 1 create new map
            int newWidth = (int)Math.Ceiling((float)map.Width / 2f);
            int newHeight = (int)Math.Ceiling((float)map.Height / 2f);
            for (int x=0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    newMap.Add(new Point { X = x, Y = y }, new Heuristic { Production = 0, Strength = 0, Value = 0f});
                }
            }

            foreach (var kvp in dict)
            {
                Point p = new Point { X = kvp.Key.X / 2, Y = kvp.Key.Y / 2 };
                float mult = .25f;
                mult *= p.X == map.Width && p.X % 2 == 1 ? 2 : 1;
                mult *= p.Y == map.Height && p.Y % 2 == 1 ? 2 : 1;

                newMap[p] = new Heuristic
                {
                    Strength = newMap[p].Strength + dict[kvp.Key].Strength * mult,
                    Production = newMap[p].Production + dict[kvp.Key].Production * mult,
                    Value = newMap[p].Value + dict[kvp.Key].Value * mult,
                };
            }

            // call shader
            newMap = SimpleShade(newMap, newWidth, newHeight);

            var result = new Dictionary<Point, Heuristic>();

            // undo the map creation
            for (int x = 0; x <map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Point p = new Point { X = x / 2, Y = y / 2 };
                    float value = newMap[p].Value * .75f;
                    value += newMap[Truncate(p.X % 2 == 1 ? p.X - 1 : p.X + 1, p.Y % 2 == 1 ? p.Y - 1 : p.Y + 1, newHeight, newWidth)].Value * .05f;
                    value += newMap[Truncate(p.X, p.Y % 2 == 1 ? p.Y - 1 : p.Y + 1, newHeight, newWidth)].Value * .1f;
                    value += newMap[Truncate(p.X % 2 == 1 ? p.X - 1 : p.X + 1, p.Y, newHeight, newWidth)].Value * .1f;

                    float ratio = value / newMap[p].Value;
                    result.Add(new Point { X = x, Y = y }, new Heuristic { Production = newMap[p].Production * ratio, Strength = newMap[p].Strength * ratio, Value = value });
                }
            }

            return result;
        }

        public static Dictionary<Point, Heuristic> SimpleShade(Dictionary<Point, Heuristic> map, int width, int height)
        {
            Dictionary<Point, float> dictionary = new Dictionary<Point, float>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    dictionary.Add(new Point(x, y), 0f);
                }
            }

            foreach (var s in map)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        int deltaX = x - Center;
                        int deltaY = y - Center;
                        Point p = Truncate(s.Key.X + deltaX, s.Key.Y + deltaY, height, width);
                        dictionary[p] += s.Value.Value * Kernel[x, y];
                    }
                }
            }
            var result = new Dictionary<Point, Heuristic>();
            foreach (var kvp in dictionary)
            {
                var s = map[kvp.Key];
                result.Add(kvp.Key, new Heuristic { Production = s.Production, Strength = s.Strength, Value = kvp.Value });
            }
            return result;
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
