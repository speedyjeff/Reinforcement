### Booop
Booop is a 2 player board game played on a 6x6 grid with 8 small and 8 large pieces.  The rules are simple but strategy is similar to checkers or chess.

#### Contents

Board

```
    a  b  c  d  e  f
  --------------------
1 |  |  |  |  |  |  |
  --------------------
2 |  |  |  |  |  |  |
  --------------------
3 |  |  |  |  |  |  |
  --------------------
4 |  |  |  |  |  |  |
  --------------------
5 |  |  |  |  |  |  |
  --------------------
6 |  |  |  |  |  |  |
  --------------------
```

8 Small pieces
8 Large pieces
1 piece which traverses in-between spaces (in the Seam)

#### Rules
The game is played by turns.  You start the game with 8 small pieces and 1 Seam piece.

On your turn you do the following:
 1. Place a piece
 2. Resolve any 'Booop'ing
 3. Resolve any 3+ in a row
 4. Move your Seam piece and resolve any 'Booop'ing
 5. (optional) place a Seam piece, at any edge of the game

##### 1. Place a piece
All unoccupied cells on the board are valid places for pieces.

##### 2. Resolve any 'Booop'ing
When a piece is placed next to another piece (horizontal, vertical, or diagnoal) it will 'Booop' the pieces around it - moving them one away.

Special rules:
 - Do not 'Booop' if the piece's move space is occupied by another piece
 - 'Booop'ing impacts pieces of both players
 - If a piece is on the edge, it is 'Booop'd off the board and is returned to the hand of the player
 - Small pieces can only 'Booop' small pieces, but large pieces can 'Booop' both small and large pieces

##### 3. Resolve any 3+ in a row
Combinations of 3 of the same players pieces must be resolved.

 - 3+ large pieces (of the same player) wins the game
 - 3+ pieces in a row results in 3 coming off the board (player chooses) and the small pieces are discarded and large pieces replace them in the hand

At all times, each player has 8 pieces either in their hand or in-play.

##### 4. Move your Seam piece and resolve any 'Booop'ing
The Seam piece moves between spaces by moving at the end of the players turn 1 space.  It does not turn corners and moves until it falls off the other end of the table.

After moving, each piece immediately next to the Seam piece (not diagnoal) is 'Booop'd.  The same rules apply as rule #2, except if there is a piece in the place where it would move.  In the case where the space immediately next to the piece is occupied the search continues until the next available is open.  This available space may be off the board, in which case the piece is taken off the board and returned to the players hand.

##### 5. (optional) place a Seam piece, at any edge of the game
At the end of your turn, the Seam piece may be placed on any edge of the board.  It does not move this turn and will remain on the board until the end of the players sixth turn.

#### To-do
This experiment is incomplete
