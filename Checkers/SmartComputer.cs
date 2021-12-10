using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    internal class SmartComputer : IPlayer
    {
        public SmartComputer(Side side, int dimension, double reward, double learning, double discount)
        {
            Side = side;
            Dimension = dimension;
            IsDebug = false;

            // attempt to load the model from disk
            Model = Load(Side, Dimension);

            if (Model == null)
            {
                Model = new Q<string, string>(reward, learning, discount);
            }

            // check if we need to adjust the reward/learning/discount
            if (Model.Reward != reward || 
                Model.Learning != learning ||
                Model.Discount != discount)
            {
                Console.WriteLine($"Warning adjusting model parameters: reward({Model.Reward}->{reward}) learning({Model.Learning}->{learning} discount({Model.Discount}->{discount})");
                Model.Reward = reward;
                Model.Learning = learning;
                Model.Discount = discount;
            }
        }

        public Move ChooseAction(CheckersBoard board)
        {
            if (board.Turn != Side) throw new Exception("computer trying to run on not its turn");

            // convert board to context
            var context = BoardToContext(board, Side);

            if (IsDebug) Console.WriteLine($"** {context}");

            // encode all the possible moves
            var actions = new List<string>();
            foreach (var move in board.GetAvailableMoves())
            {
                // translate into action
                var laction = MoveToAction(move);
                actions.Add(laction);

                if (IsDebug)
                {
                    var qfound = Model.TryGetQValue(context, laction, out double qvalue);
                    Console.WriteLine($"* {move.Coordinate.Row},{move.Coordinate.Column}:{(int)move.Direction} {laction} {qvalue} [{qfound}]");
                }
            }

            if (actions.Count == 0) throw new Exception("must have at least 1 valid move");

            // stitch this to the previous context
            StitchToPrevious(context);

            // get next move (apply learning later once we have the other players move)
            var action = Model.ChooseAction(
                context,
                actions,
                applyActionFunc: null);

            if (IsDebug) Console.WriteLine($"*> {action}");

            // set context for next move
            SavePrevious(context, action);

            // return this as the choosen action
            return ActionToMove(action, Side);
        }

        public void Finish(CheckersBoard board, Side winner, Move lastMove)
        {
            // get the last action that lead to the end
            var action = MoveToAction(lastMove);

            // get context
            var context = BoardToContext(board, Side);

            // apply terminal reward
            Model.ApplyTerminalCondition(
                context,
                action,
                value: (winner == Side.None ? -1d : (winner == Side ? 1d : -2d)));

            // stitch with previous
            StitchToPrevious(context);

            // clear out the previous
            ClearPrevious();
        }

        public void Save()
        {
            Save(Model, Side, Dimension);
        }

        public bool IsDebug { get; set; }

        #region private
        private Q<string, string> Model;
        private Side Side;
        private int Dimension;

        private string PreviousContext = "";
        private string PreviousAction = "";

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
        // Conversions
        //

        private Move ActionToMove(string action, Side side)
        {
            // encoded as: RowColumnDirection (eg. 000)
            if (string.IsNullOrWhiteSpace(action) || action.Length != 3) throw new Exception("failed to get a valid action");

            var parts = action.ToCharArray();
            var move = new Move()
            {
                Coordinate = new Coordinate()
                {
                    Row = (int)Char.GetNumericValue(parts[0]),
                    Column = (int)Char.GetNumericValue(parts[1])
                },
                Direction = (Direction)(int)Char.GetNumericValue(parts[2])
            };

            return move;
        }

        private string MoveToAction(Move move)
        {
            // encode as: RowColumnDirection (eg. 000)
            return $"{move.Coordinate.Row}{move.Coordinate.Column}{(int)move.Direction}";
        }

        private string BoardToContext(CheckersBoard board, Side side)
        {
            return MinimalBoard.Create(board).AsString();
        }

        //
        // Save/Load
        //

        private static void Save(Q<string,string> model, Side side, int dimension)
        {
            var modelFilename = $"model.{side}.{dimension}.tsv";
            var prune = false;

            if (modelFilename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(model, typeof(Q<string, string>));
                File.WriteAllText(modelFilename, json);
            }
            else
            {
                // stream the model to disk, as it is very large
                using (var writer = File.CreateText(modelFilename))
                {
                    // model attributes
                    writer.WriteLine($"{model.Reward}\t{model.Discount}\t{model.Learning}");
                    // write out all keys
                    foreach (var kvp in model.Matrix)
                    {
                        if (prune && kvp.Value.Count <= 1) continue;

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
        }

        private static Q<string, string> Load(Side side, int dimension)
        {
            var modelFilename = $"model.{side}.{dimension}.tsv";
            if (!File.Exists(modelFilename)) return null;

            if (modelFilename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = File.ReadAllText(modelFilename);
                return System.Text.Json.JsonSerializer.Deserialize<Q<string, string>>(json);
            }
            else
            {
                // read the model from disk a line at a time, as it is very large
                Q<string, string> q = null;
                using (var reader = File.OpenText(modelFilename))
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
        }
        #endregion
    }
}
