using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Halite
{
    public class Heuristic<T>
    {
        Dictionary<T, SiteValue> information = new Dictionary<T, SiteValue>();

        public void AddNew(T p, SiteValue newValue)
        {
            information.Add(p, newValue);
        }

        public void AddNew(T t, float production, float strength, float value)
        {
            AddNew(t, new SiteValue { Production = production, Value = value, Strength = strength });
        }

        public void AddNew(T t, float production, float strength)
        {
            AddNew(t, new SiteValue { Production = production, Value = production / strength, Strength = strength });
        }

        public void Increase(T t, float production, float strength, float value)
        {
            var temp = information[t];
            information[t] = new SiteValue {
                Value = temp.Value + value,
                Production = temp.Production + production,
                Strength = temp.Strength + strength
            };
        }

        public SiteValue Get(T t)
        {
            return information[t];
        }

        public void Update(T t, float newValue)
        {
            var temp = information[t];
            information[t] = new SiteValue { Value = newValue, Production = temp.Production, Strength = temp.Strength };
        }

        public float GetReducedValue(T target, int strengthLost)
        {
            var h = information[target];
            return h.Value * h.Strength / (h.Strength + strengthLost); //TODO may need reevaluated...  Should only be used for conquering neutral territory
        }

        public Dictionary<T, SiteValue> GetDictionary()
        {
            return information;
        }

        public static Heuristic<Site> GetStartingHeuristic(Map m)
        {
            var result = new Heuristic<Site>();
            foreach (var site in m.GetSites(x => true))
            {
                float strength = site.Strength;
                float production = site.Production;
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
        public float Value;
        public float Production;
        public float Strength;
    }
}
