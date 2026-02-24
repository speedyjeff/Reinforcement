using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using blocks.engine;

public class Human : IPlayer
{
    public Move ChooseMove(Blocks blocks, List<PieceType> pieces)
    {
        while (true)
        {
            // get piece
            Console.WriteLine("Choose a piece:");
            for (int i = 0; i < pieces.Count; i++)
            {
                var shape = pieces[i].GetString(prefix: "   ", suffix: "");
                Console.WriteLine($"{i}: {pieces[i]}");
                Console.WriteLine($"{shape}");
            }
            Console.Write($"Select piece to play (0-{pieces.Count - 1}): ");
            var selInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(selInput)) continue;
            if (!int.TryParse(selInput, out int sel) || sel < 0 || sel >= pieces.Count)
            {
                Console.WriteLine("Invalid selection or already played.");
                continue;
            }

            // get the row,col
            var piece = pieces[sel];
            Console.Write($"Place piece {piece} (row col): ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            var parts = input.Split(' ', ',');
            var rowStr = "";
            var colStr = "";
            if (parts.Length >= 2) 
            {
                rowStr = parts[0];
                colStr = parts[1];
            }
            else if (input.Length == 2) 
            {
                rowStr = input[0].ToString();
                colStr = input[1].ToString();
            }
            else
            {
                Console.WriteLine("Invalid input. Try again.");
                continue;
            }
            if (!int.TryParse(rowStr, out int row) || !int.TryParse(colStr, out int col))
            {
                Console.WriteLine("Invalid input. Try again.");
                continue;
            }

            // done
            return new Move(piece, row, col);
        }
    }
}

