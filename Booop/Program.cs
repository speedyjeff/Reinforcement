
// add game options to loop for players that win

using System.Data;
using System.Transactions;

namespace Booop
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // quick tests
            Tests.All();

            if (false)
            {
                // play game
                var winner = TrainingLoopTournament(numPlayers: 100, gamesPerRound: 100);

                // now play a game against the winner
                var human = new PlayerHuman(winner.Player == PlayerType.Orange ? PlayerType.Purple : PlayerType.Orange, verbose: true);

                // play a game
                GameLoop(winner, human, verbose: true, maxTurns: 100);
            }
            else if (true)
            {
                // play game
                var winner = TrainingLoop(iterations: 50000);
                // now play a game against the winner
                var human = new PlayerHuman(winner.Player == PlayerType.Orange ? PlayerType.Purple : PlayerType.Orange, verbose: true);
                // play a game
                GameLoop(winner, human, verbose: true, maxTurns: 100);
            }
            else
            {
                GameLoop(
                    new PlayerHeuristics(PlayerType.Orange, verbose: true, deterministic: true),
                    new PlayerHuman(PlayerType.Purple, verbose: true),
                    verbose: true,
                    maxTurns: 100);
            }
        }
        #region private
        private static void PrintPlayer(Board board, PlayerBase player)
        {
            if (!board.TryGetAvailablePieces(player.Player, out int smallCount, out int largeCount, out bool canUseSeam)) throw new Exception("failed to get piece counts");
            Console.WriteLine($"{player.Player}: Small={smallCount} Large={largeCount} Seam={(canUseSeam ? 1 : 0)}");
        }

        private static PlayerBase TrainingLoopTournament(int numPlayers, int gamesPerRound)
        {
            // tournament style - the winner will have iterations games by the end
            if (gamesPerRound % 2 == 0) gamesPerRound++;
            if (numPlayers % 2 != 0) numPlayers++;

            // play iterations games and the player with the highest wins is returned
            var verbose = false;
            var maxTurnCount = 100;
            var randomReduction = 0.99f;

            // add the initial set of players
            var orangeWinners = new List<PlayerBase>();
            var purpleWinners = new List<PlayerBase>();
            Console.WriteLine($"Creating {numPlayers} models");
            for (int i = 0; i < numPlayers; i += 2) 
            {
                orangeWinners.Add(new PlayerHeuristics(PlayerType.Orange, verbose, deterministic: false));
                purpleWinners.Add(new PlayerNeural(PlayerType.Purple, verbose));
            }

            // play each of the rounds
            var round = 0;
            while (orangeWinners.Count > 0 && purpleWinners.Count > 0)
            {
                Console.WriteLine($"round: {round} ");

                // grab the players from the last round
                var currentOrange = orangeWinners;
                var currentPurple = purpleWinners;
                orangeWinners = new List<PlayerBase>();
                purpleWinners = new List<PlayerBase>();

                // play gamesPerRound games and determine which is the winner
                for (int i = 0; i < Math.Max(currentOrange.Count, currentPurple.Count); i++)
                {
                    // get the players
                    var orangePlayer = (i < currentOrange.Count) ? currentOrange[i] : new PlayerNeural(PlayerType.Orange, verbose);
                    var purplePlayer = (i < currentPurple.Count) ? currentPurple[i] : new PlayerNeural(PlayerType.Purple, verbose);

                    // play gamesPerRound games
                    var wins = new int[3]; // none, orange, purple
                    for (int g = 0; g < gamesPerRound; g++)
                    {
                        var winner = GameLoop(orangePlayer, purplePlayer, verbose: false, maxTurnCount);
                        wins[(int)winner]++;
                    }

                    Console.WriteLine($"wins: none: {wins[(int)PlayerType.None]} orange[{(orangePlayer is PlayerNeural orangeNeu ? orangeNeu.Generation : 0)}]: {wins[(int)PlayerType.Orange]} purple[{(purplePlayer is PlayerNeural purpleNeu ? purpleNeu.Generation : 0)}]: {wins[(int)PlayerType.Purple]}");

                    // adjust both randomizations
                    if (orangePlayer is PlayerNeural nop)
                    { 
                        nop.Randomization *= randomReduction;
                        nop.Generation++;
                    }
                    if (purplePlayer is PlayerNeural pnp)
                    {
                        pnp.Randomization *= randomReduction;
                        pnp.Generation++;
                    }

                    // add the winners to last round
                    if (wins[(int)PlayerType.Orange] > wins[(int)PlayerType.Purple]) orangeWinners.Add(orangePlayer);
                    else if (wins[(int)PlayerType.Purple] > wins[(int)PlayerType.Orange]) purpleWinners.Add(purplePlayer);
                    else if (wins[(int)PlayerType.Orange] == wins[(int)PlayerType.Purple] && wins[(int)PlayerType.Orange] != 0)
                    {
                        // add them both?
                        orangeWinners.Add(orangePlayer);
                        purpleWinners.Add(purplePlayer);
                    }
                    else throw new Exception("no winner");
                }

                round++;
            }

            if (purpleWinners.Count == 0 && orangeWinners.Count == 0) throw new Exception("invalid exit state");

            // the winner should be the last one standing
            var winners = orangeWinners;
            if (purpleWinners.Count > 0) winners = purpleWinners;
            if (winners.Count == 0) throw new Exception("no winners");

            // find the one with the highest generation
            var maxGen = -1;
            PlayerBase winningPlayer = winners[0];
            foreach(var play in winners)
            {
                if (play is PlayerNeural p && p.Generation > maxGen)
                {
                    maxGen = p.Generation;
                    p.Randomization = 0f;
                    winningPlayer = p;
                }
            }

            return winningPlayer;
        }

        private static PlayerBase TrainingLoop(int iterations)
        {
            // iterations with the same player

            // play iterations games and the player with the highest wins is returned
            var verbose = false;
            var maxTurnCount = 100;
            var randomReduction = 0.9999f;

            // add the initial set of players
            PlayerBase orange = new PlayerHeuristics(PlayerType.Orange, verbose, deterministic: true);
            PlayerBase purple = new PlayerNeural(PlayerType.Purple, verbose);

            // play each of the rounds
            var wins = new int[3]; // none, orange, purple
            for (int itr = 0; itr < iterations; itr++)
            {
                var winner = GameLoop(orange, purple, verbose: false, maxTurnCount);
                wins[(int)winner]++;

                if (itr % 100 == 0) Console.WriteLine($"{itr}: wins: none: {wins[(int)PlayerType.None]} orange[{(orange is PlayerNeural orangeNeu ? orangeNeu.Generation : 0)}]: {wins[(int)PlayerType.Orange]} purple[{(purple is PlayerNeural purpleNeu ? purpleNeu.Generation : 0)}]: {wins[(int)PlayerType.Purple]}");

                // adjust both randomizations
                if (orange is PlayerNeural nop)
                {
                    nop.Randomization *= randomReduction;
                    nop.Generation++;
                }
                if (purple is PlayerNeural pnp)
                {
                    pnp.Randomization *= randomReduction;
                    pnp.Generation++;
                }
            }

            // return the neural player
            if (orange is PlayerNeural) return orange;
            if (purple is PlayerNeural) return purple;
            throw new Exception("no neural player");
        }

        private static PlayerType GameLoop(PlayerBase player1, PlayerBase player2, bool verbose, int maxTurns)
        {
            var board = new Board(rows: 6, columns: 6, initialSmall: Board.MaxPieceCount, initialLarge: 0);
            var turnCount = 0;

            // loop through and have them play
            while (board.Winner == PlayerType.None)
            {
                // player 1
                if (verbose) Utility.Print(board);
                if (verbose) PrintPlayer(board, player1);
                if (!player1.TryMakeMove(board)) throw new Exception("player 1 failed to make a move");
                if (!board.TryResolveOutOfPieces(player1.Player, player1.ChooseUpgradePiece)) throw new Exception("failed to resolve post-turn actions");

                // call into other play (for learning)
                player2.Feedback(board, complete: false);

                // check if there has been a winner
                if (board.Winner != PlayerType.None) break; // check for a winner

                // player 2
                if (verbose) Utility.Print(board);
                if (verbose) PrintPlayer(board, player2);
                if (!player2.TryMakeMove(board)) throw new Exception("player 2 failed to make a move");
                if (!board.TryResolveOutOfPieces(player2.Player, player2.ChooseUpgradePiece)) throw new Exception("failed to resolve post-turn actions");

                // call into other play (for learning)
                player1.Feedback(board, complete: false);

                // increment the turn count
                turnCount++;

                if (maxTurns > 0 && turnCount >= maxTurns) break;
            }

            // final feedback
            player1.Feedback(board, complete: true);
            player2.Feedback(board, complete: true);

            // game over
            if (verbose) Console.WriteLine($"Winner: {board.Winner}");
            if (verbose) Utility.Print(board);

            return board.Winner;
        }

        #endregion
    }

}
