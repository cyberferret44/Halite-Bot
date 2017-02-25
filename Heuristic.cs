using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

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
            double value = h.Value * .9 + target.Production / (target.Strength == 0 ? 1 : target.Strength) * .1;
            return value * h.Strength / (h.Strength + strengthLost); //TODO may need reevaluated...  Should only be used for conquering neutral territory
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
    }

    public struct SiteValue
    {
        public double Value;
        public double Production;
        public double Strength;
    }
}
