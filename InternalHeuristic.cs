using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    double spreadValue = heuristic.Get(node).Value;
                    spreadValue = Math.Pow(spreadValue, .9); // magic number, seems solid though.  Could reasonably increase it to .92 or so

                    var spreadSites = node.Neighbors.Where(n => n.IsMine).Where(x => heuristic.Get(x).Value < spreadValue);
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

            heuristic.WriteCSV("internal");
            return heuristic;
        }

        //private static List<Site> GetPassiveNeutralNeighbors(Map m)
        //{
        //    return m.GetSites(s => s.Owner == 0
        //                        && s.Neighbors.Any(n => n.IsMine)
        //                        && s.Neighbors.All(n => !n.IsEnemy && (n.Strength > 0 || n.Neighbors.All(n2 => !n2.IsEnemy)))
        //                        ).ToList();
        //}
    }
}
