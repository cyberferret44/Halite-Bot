using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Halite
{
    public class Heuristic
    {
        private Dictionary<Site, SiteValue> information = new Dictionary<Site, SiteValue>();
        private Dictionary<Site, double> tempValues = new Dictionary<Site, double>();

        public SiteValue Get(Site t) => information[t];
        public Dictionary<Site, SiteValue> GetDictionary() => information;

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

        public void AddValue(Site t, double value)
        {
            information[t].Value += value;
        }

        public void AddValue(Site t, double production, double strength, double value = 0.0)
        {
            information[t].Strength += strength;
            information[t].Production += production;
            information[t].Value += value;
        }

        public void Update(Site t, double newValue)
        {
            information[t].Value = newValue;
        }

        public void Update(Site t, double newProduction, double newStrength, double newValue)
        {
            information[t].Strength = newStrength;
            information[t].Production = newProduction;
            information[t].Value = newValue;
        }

        public double GetReducedValue(Site target, double strengthLost)
        {
            var h = information[target];
            double value = h.Value; // * .9 + target.Production / (target.Strength == 0 ? 1 : target.Strength) * .1;
            return value * target.Production / (strengthLost + target.Strength); //TODO may need reevaluated...  Should only be used for conquering neutral territory
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

        public void ZeroOutMySitesValue()
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

    public class SiteValue
    {
        public double Value;
        public double Production;
        public double Strength;
    }
}
