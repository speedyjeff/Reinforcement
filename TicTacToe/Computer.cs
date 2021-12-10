using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Learning;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TicTacToe
{
    class Computer : IPlayer
    {
        public Computer(int dim, Piece piece)
        {
            // init
            Rand = new Random();
            Previous = new PreviousMove() { Initialized = false };

            if (dim > 4) throw new Exception("the board is limited to a 4x4");

            // attempt to load from disk
            Details = Load(dim, piece);

            // else create
            if (Details == null)
            {
                Details = new ModelDetails()
                {
                    Dimension = dim,
                    Piece = piece,
                    Model = new Q<int, int>(
                            -0.04, // reward
                            0.5, // learning rate
                            1.0 // discount factor
                    )
                };
            }
        }

        public Coordinate ChooseAction(TicTacToeBoard board)
        {
            // convert board to context
            var context = BoardToContext(board, Details.Piece);

            // encode all the possible moves
            var actions = new List<int>();
            foreach (var loc in board.GetAvailble())
            {
                // translate into indexes
                var laction = CoordinateToAction(loc);
                actions.Add(laction);
            }

            if (actions.Count == 0) throw new Exception("must have at least 1 valid move");

            // stitch this to the previous context
            StitchToPrevious(context);

            // get next move (apply learning later once we have the other players move)
            var action = Details.Model.ChooseAction(
                context,
                actions,
                applyActionFunc: null);

            // set context for next move
            SavePrevious(Details.Piece, context, action);

            // return this as the choosen action
            return ActionToCoordinate(action);
        }

        public void Finish(TicTacToeBoard board, Piece winningPiece, Coordinate lastCoordinate)
        {
            // get the last action that lead to the end
            var action = CoordinateToAction(lastCoordinate);

            // get context
            var context = BoardToContext(board, Details.Piece);

            // apply terminal reward
            Details.Model.ApplyTerminalCondition(
                context,
                action,
                value: (winningPiece == Piece.Empty ? 0d : (winningPiece == Details.Piece ? 1d : -2d)));
 
            // stitch with previous
            StitchToPrevious(context);

            // clear out the previous
            Previous.Initialized = false;
        }

        public int ContextCount { get { return Details.Model.ContextCount; } }
        public int ContextActionCount { get { return  Details.Model.ContextActionCount; } }

        public string GetContext(TicTacToeBoard board)
        {
            var sb = new StringBuilder();
            sb.AppendLine("*******************");

            // get the board and context (for validation)
            var context = BoardToContext(board, Details.Piece);
            sb.AppendLine($"{context}");
            if (Details.Model.TryGetActions(context, out Dictionary<int, double> actions))
            {
                // convert context back to board
                var lboard = ContextToBoard(context, Details.Piece);

                if (!lboard.ToString().Equals(board.ToString())) throw new Exception("boards do not match");

                // package up the information
                foreach (var kvp in actions)
                {
                    var lcoord = ActionToCoordinate(kvp.Key);
                    sb.AppendLine($"{lcoord} : {kvp.Value}");
                }
            }

            sb.AppendLine("*******************");

            return sb.ToString();
        }

        public void SaveModel()
        {
            Save(Details);
        }

        #region private
        private readonly Random Rand;

        class PreviousMove
        {
            public bool Initialized;
            public Piece Piece;
            public int Context;
            public int Action;
        }
        private readonly PreviousMove Previous;

        // context: int - encode cells as 2 bits - 2^(dim*dim * 2) combos
        // action: 0-8 (1 == 0,0, | 2 == 0,1 | etc.)
        class ModelDetails
        {
            [AllowNull]
            public Q<int, int> Model { get; set; }
            public int Dimension { get; set; }
            public Piece Piece { get; set; }
        }
        [AllowNull] 
        private readonly ModelDetails Details;

        //
        // Previous model
        //
        private void StitchToPrevious(int context)
        {
            // stitch the previous move to the current context
            // this is necessary as the computer did not observe all the 
            // changes since we last decided on the board
            // eg.
            //  (1)    (2)    (3)    (4)
            //   |     o|     o|     o|o
            //  --- -> --- -> --- -> ---
            //   |      |     x|     x|
            // 1,3,and 4 are observed, but 2 is 'skipped' as it happened elsewhere
            // this code will stitch 1 and 3 together
            if (Previous.Initialized)
            {
                // force to choose the same action
                var fakeaction = Details.Model.ChooseAction(
                    Previous.Context,
                    new List<int>() { Previous.Action },
                    applyActionFunc: (lcontext, laction) =>
                    {
                        // return the current board to 'stitch' them together
                        return context;
                    });

                // reset
                Previous.Initialized = false;
            }
        }

        private void SavePrevious(Piece piece, int context, int action)
        {
            Previous.Initialized = true;
            Previous.Piece = piece;
            Previous.Context = context;
            Previous.Action = action;
        }

        //
        // Conversions from logical to model data
        //
        private static int BoardToContext(TicTacToeBoard board, Piece piece)
        {
            // encode X's and O's as 01 and 10 and encode within an int
            //   piece == Empty => 00
            //   piece == piece => 01
            //   piece != piece => 10
            int context = 0;
            int mask = 0;
            var coord = new Coordinate();
            for (coord.Row = 0; coord.Row < board.Dimension; coord.Row++)
            {
                if (mask > 31) throw new Exception("beyond capacity for encoding");
                for (coord.Column = 0; coord.Column < board.Dimension; coord.Column++)
                {
                    if (!board.TryGetPiece(coord, out Piece lpiece)) throw new Exception("not able to get piece");
                    if (lpiece != Piece.Empty)
                    {
                        context |= ((piece == lpiece ? 1 : 2) << mask);
                    }
                    mask += 2;
                }
            }

            return context;
        }

        private TicTacToeBoard ContextToBoard(int context, Piece piece)
        {
            // todo - assume board dimension
            var board = new TicTacToeBoard(Details.Dimension);

            int mask = 0;
            var coord = new Coordinate();
            for (coord.Row = 0; coord.Row < board.Dimension; coord.Row++)
            {
                if (mask > 31) throw new Exception("beyond capacity for encoding");
                for (coord.Column = 0; coord.Column < board.Dimension; coord.Column++)
                {
                    var code = (context >> mask) & 3;
                    var lpiece = code == 0 ? Piece.Empty : (code == 1 ? piece : (piece == Piece.X ? Piece.O : Piece.X));
                    if (!board.TryPutPiece(coord, lpiece)) throw new Exception("not able to get piece");
                    mask += 2;
                }
            }

            return board;
        }

        private int CoordinateToAction(Coordinate coord)
        {
            return (coord.Row * Details.Dimension) + coord.Column;
        }

        private Coordinate ActionToCoordinate(int action)
        {
            return new Coordinate()
            {
                Row = action / Details.Dimension,
                Column = action % Details.Dimension
            };
        }

        // 
        // Save/Load Model
        //

        private static void Save([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] ModelDetails model)
        {
            var modelFilename = $"model.{model.Dimension}.{model.Piece}.json";
            var json = System.Text.Json.JsonSerializer.Serialize(model, typeof(ModelDetails));
            File.WriteAllText(modelFilename, json);
        }

        [return: MaybeNull]
        private static ModelDetails Load(int dim, Piece piece)
        {
            var modelFilename = $"model.{dim}.{piece}.json";
            if (!File.Exists(modelFilename)) return null;

            var json = File.ReadAllText(modelFilename);
            return System.Text.Json.JsonSerializer.Deserialize<ModelDetails>(json);
        }
        #endregion
    }
}
