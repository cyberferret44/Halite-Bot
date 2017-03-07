using System.Collections.Generic;
using System.Linq;

namespace Halite
{
    public static class InternalHeuristic
    {
        public static Heuristic GetInternalHeuristic(Heuristic expansionHeuristic, List<Site> edgeTargets)
        {
            Heuristic heuristic = expansionHeuristic.Clone();
            heuristic.ZeroOutMySitesValue();

            List<Site> currentNodes = new List<Site>();
            List<Site> nextNodes = new List<Site>();
            currentNodes.AddRange(edgeTargets);

            while (currentNodes.Any())
            {
                foreach (var node in currentNodes)
                {
                    double spreadValue = heuristic.Get(node).Value * .92; // magic number, seems solid though.  Could reasonably increase it to .92 or so
                    var spreadSites = node.Neighbors.Where(n => n.IsMine && heuristic.Get(n).Value < spreadValue);
                    foreach(var s in spreadSites)
                    {
                        heuristic.Update(s, spreadValue);
                        nextNodes.Add(s);
                    }
                }
                currentNodes = new List<Site>();
                currentNodes.AddRange(nextNodes);
                nextNodes = new List<Site>();
            }

            //heuristic.WriteCSV("internal");
            return heuristic;
        }
    }
}
