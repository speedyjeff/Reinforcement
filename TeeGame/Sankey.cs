using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeeGame
{
    static class Sankey
    {
        // example input:
        // moves: d->a f->d l->e c->h n->l g->b a->d o->f f->m l->n d->m n->l k->m
        // moves: f->a m->f o->m j->c d->f g->i c->j l->n a->d j->h d->m n->l k->m
        public static void Parse(string filename, int maxIterations, Func<string, string, double, string> formater)
        {
            var nextId = 0L;
            var map = new Dictionary<int /*index*/, Dictionary<string /*transition*/, long /*id*/>>();
            var tree = new Dictionary<long /*parent id*/, Dictionary<long /*child id*/, long /*count*/>>();

            // read in all the lines and assign unique ids to each state change
            using (var reader = File.OpenText(filename))
            {
                while(!reader.EndOfStream)
                {
                    if (maxIterations-- < 0) break;

                    // read and clean up the input
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    line = line.Trim().Replace("moves: ", "");

                    // split into parts
                    var parts = line.Split(' ');
                    if (parts.Length != 13) throw new Exception("invalid input");

                    // iterate through each part and get the id
                    var parentId = -1L;
                    for(int i=0; i<parts.Length; i++)
                    {
                        // get the id for the part
                        if (!map.TryGetValue(i, out var idMap))
                        {
                            idMap = new Dictionary<string, long>();
                            map.Add(i, idMap);
                        }
                        if (!idMap.TryGetValue(parts[i], out var childId))
                        {
                            childId = nextId++;
                            idMap.Add(parts[i], childId);
                        }

                        if (parentId >= 0)
                        {
                            // add to the tree
                            if (!tree.TryGetValue(parentId, out var children))
                            {
                                children = new Dictionary<long, long>();
                                tree.Add(parentId, children);
                            }
                            if (!children.TryGetValue(childId, out var count))
                            {
                                children.Add(childId, 0);
                            }
                            children[childId]++;
                        }

                        // set the new parent
                        parentId = childId;
                    }
                }
            }

            // write out the data in a format consumable
            foreach(var okvp in tree)
            {
                foreach(var ikvp in okvp.Value)
                {
                    Console.WriteLine(formater($"{okvp.Key}", $"{ikvp.Key}", ikvp.Value));
                }
            }
        }

        #region private
        private static long NextId = 0;
        #endregion
    }
}
