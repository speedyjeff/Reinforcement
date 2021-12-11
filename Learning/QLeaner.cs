using System;
using System.Collections.Generic;
using System.IO;

namespace Learning
{
    public class QLearner
    {
        public QLearner(double reward, double learning, double discount)
        {
            Model = new Q<string, string>(reward, learning, discount);
        }

        public string ChooseAction(string context, List<string> actions)
        {
            if (actions.Count == 0) throw new Exception("must have at least 1 valid move");

            // stitch this to the previous context
            StitchToPrevious(context);

            // get next move (apply learning later once we have the other players move)
            var action = Model.ChooseAction(
                context,
                actions,
                applyActionFunc: null);

            // set context for next move
            SavePrevious(context, action);

            // return this as the choosen action
            return action;
        }

        public void Finish(string context, string lastAction, double value)
        {
            // apply terminal reward
            Model.ApplyTerminalCondition(
                context,
                lastAction,
                value);

            // stitch with previous
            StitchToPrevious(context);

            // clear out the previous
            ClearPrevious();
        }

        public void Save(string filename)
        {
            SaveModel(Model, filename);
        }

        public static QLearner Load(string filename)
        {
            var q = LoadModel(filename);
            if (q == null) return null;
            return new QLearner(reward: 0, learning: 0, discount: 0)
            {
                Model = q
            };
        }

        #region private
        private Q<string, string> Model;

        private string PreviousContext;
        private string PreviousAction;

        //
        // Previous state
        //

        private void StitchToPrevious(string context)
        {
            // stitch the previous move to the current context
            // this is necessary as the computer did not observe all the 
            // changes since we last decided on the board
            // eg. the other player may (except in the case of multiple jump) 
            //     played which changes the way the board looks
            if (!string.IsNullOrWhiteSpace(PreviousContext) && !string.IsNullOrWhiteSpace(PreviousAction))
            {
                // force to choose the same action
                var fakeaction = Model.ChooseAction(
                    PreviousContext,
                    new List<string>() { PreviousAction },
                    applyActionFunc: (lcontext, laction) =>
                    {
                        // return the current board to 'stitch' them together
                        return context;
                    });

                // reset
                ClearPrevious();
            }
        }

        private void SavePrevious(string context, string action)
        {
            // save context and action
            PreviousContext = context;
            PreviousAction = action;
        }

        private void ClearPrevious()
        {
            SavePrevious(context: "", action: "");
        }

        //
        // Save/Load
        //

        private void SaveModel(Q<string,string> model, string filename)
        {
            // stream the model to disk, as it is very large
            using (var writer = File.CreateText(filename))
            {
                // model attributes
                writer.WriteLine($"{model.Reward}\t{model.Discount}\t{model.Learning}");
                // write out all keys
                foreach (var kvp in model.Matrix)
                {
                    // write to disk
                    writer.Write($"{kvp.Key}");
                    foreach (var ikvp in kvp.Value)
                    {
                        writer.Write($"\t{ikvp.Key}:{ikvp.Value:f3}");
                    }
                    writer.WriteLine();
                }
            }
        }

        private static Q<string, string> LoadModel(string filename)
        {
            if (!File.Exists(filename)) return null;

            // read the model from disk a line at a time, as it is very large
            Q<string, string> q = null;
            using (var reader = File.OpenText(filename))
            {
                // first line are the attributes
                var attributes = reader.ReadLine().Split('\t');
                if (attributes.Length != 3) throw new Exception("failed to read attributes");
                q = new Q<string, string>(
                    reward: Convert.ToDouble(attributes[0]),
                    discount: Convert.ToDouble(attributes[1]),
                    learning: Convert.ToDouble(attributes[2])
                    );

                // now read in and replace the matrix
                while (!reader.EndOfStream)
                {
                    var sections = reader.ReadLine().Split('\t');
                    if (sections.Length <= 1) throw new Exception("invalid row");
                    q.Matrix.Add(sections[0], new Dictionary<string, double>());
                    for (int i = 1; i < sections.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(sections[i])) continue;
                        // split the section into 2 parts
                        var parts = sections[i].Split(':');
                        if (parts.Length != 2) throw new Exception($"invalid part {parts.Length} '{sections[i]}'");

                        q.Matrix[sections[0]].Add(parts[0], Convert.ToDouble(parts[1]));
                    }
                }
            }

            return q;
        }
        #endregion
    }
}
