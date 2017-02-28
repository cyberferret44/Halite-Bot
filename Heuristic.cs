using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Halite
{
    public class Heuristic
    {
        Dictionary<Site, SiteValue> information = new Dictionary<Site, SiteValue>();

        public void AddNew(Site p, SiteValue newValue)
        {
            information.Add(p, newValue);
        }

        public void AddNew(Site t, double production, double strength, double value)
        {
            AddNew(t, new SiteValue { Production = production, Value = value, Strength = strength });
        }

        public void AddNew(Site t, double production, double strength)
        {
            AddNew(t, new SiteValue { Production = production, Value = production / strength, Strength = strength });
        }

        public void AddValue(Site t, double production, double strength, double value = 0.0)
        {
            var temp = information[t];
            information[t] = new SiteValue {
                Value = temp.Value + value,
                Production = temp.Production + production,
                Strength = temp.Strength + strength
            };
        }

        public SiteValue Get(Site t)
        {
            return information[t];
        }

        public void Update(Site t, double newValue)
        {
            var temp = information[t];
            information[t] = new SiteValue { Value = newValue, Production = temp.Production, Strength = temp.Strength };
        }

        public void Update(Site t, double newProduction, double newStrength, double newValue)
        {
            var temp = information[t];
            information[t] = new SiteValue { Value = newValue, Production = newProduction, Strength = newStrength };
        }

        public double GetReducedValue(Site target, double strengthLost)
        {
            var h = information[target];
            double value = h.Value; // * .9 + target.Production / (target.Strength == 0 ? 1 : target.Strength) * .1;
            return value * target.Production / (strengthLost + target.Strength); //TODO may need reevaluated...  Should only be used for conquering neutral territory
        }

        public Dictionary<Site, SiteValue> GetDictionary()
        {
            return information;
        }

        public static Heuristic GetStartingHeuristic(Map m)
        {
            var result = new Heuristic();
            foreach (var site in m.GetSites(x => true))
            {
                double strength = site.Strength;
                double production = site.Production;
                if (site.IsMine)
                {
                    production = 0;
                }
                else if (production == 0)
                {
                    production = .00001f;
                }
                if (site.Strength == 0)
                {
                    strength = 1;
                }

                result.AddNew(site, production, strength);
            }
            return result;
        }

        public Heuristic Clone()
        {
            Heuristic newHeuristic = new Heuristic();
            foreach(var kvp in information)
            {
                newHeuristic.AddNew(kvp.Key, kvp.Value);
            }

            return newHeuristic;
        }

        public void ZeroOutMySites()
        {
            List<Site> keys = new List<Site>();
            keys.AddRange(information.Keys.Where(x => x.IsMine));
            foreach(var key in keys)
            {
                Update(key, 0.0);
            }
        }

        public void WriteCSV(string name = "csv")
        {
            var csv = new StringBuilder();
            string filePath = Directory.GetCurrentDirectory();

            var list = information.Select(x => x.Key).OrderBy(x => x.X+ x.Y * 1000);
            foreach(var site in list)
            {
                if(site.X == 0 && site.Y != 0)
                {
                    csv.Length--;
                    csv.Append('\n');
                }
                csv.Append($"{(int)information[site].Value},");
            }
            csv.Length--;

            File.WriteAllText(Directory.GetCurrentDirectory() + $"\\{name}.csv", csv.ToString());
        }
    }

    public struct SiteValue
    {
        public double Value;
        public double Production;
        public double Strength;
    }
}
