using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class Turn
    {
        public readonly List<Move> Moves = new List<Move>();
        public readonly HashSet<Site> SitesUsed = new HashSet<Site>();
        public readonly List<Site> AllSites = new List<Site>();
        public readonly HashSet<Site> RemainingSites = new HashSet<Site>();

        // list enemies, strenght, pieces, id, etc

        public Turn(List<Site> availableSites)
        {
            availableSites.ForEach(x => AllSites.Add(x));
            availableSites.ForEach(x => RemainingSites.Add(x));
        }

        public void AddMove(Move m)
        {
            Moves.Add(m);
            SitesUsed.Add(m.Site);
            RemainingSites.Remove(m.Site);
        }

        public void AddMove(Site s, Direction d)
        {
            AddMove(new Move { Site = s, Direction = d });
        }

        public void AddMove(Site s, Site target)
        {
            Moves.Add(new Move { Site = s, Direction = s.GetDirectionToNeighbour(target) });
        }

        public void RemoveMove(Site s)
        {
            SitesUsed.Remove(s);
            RemainingSites.Add(s);
            Moves.RemoveAll(x => x.Site == s);
        }

        public int MovedToStrength(Site s) => Moves.Where(m => m.Site == s).Sum(m => m.Site.Strength);
    }
}
