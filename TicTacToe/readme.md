Training a computer to play tic-tac-toe (and win) without writing any rules for it to follow.  It will learn how to win and how to block by reinforcing what it learns by playing.

X's go first.

```
   |   |   
---|---|---
   |   |   
---|---|---
   |   |   
```

## Connecting the learning (stitch)

## Strategies to learning

### Random vs Reinforcement

Randomly choose from the available moves.  The key thing to notice is that a random strategy saw few unique boards as it did not fully walk all possible combinations.

Stats (out of 1,000,000)     | X's    | O's
-----------------------------------------------
More wins than loses (after) |      2 |   2,095
Last lose                    | 15,603 | 958,696
Unique boards seen           |  1,664 |   2,867
Possible moves considered    |  5,242 |   9,018

X's first move preference:
```
 9 | 7 | 1  
---|---|---
 5 | 2 | 3 
---|---|---
 8 | 4 | 6 
```

O's first move preference:
```
   (0,0)        (0,1)        (0,2)    
 x | 2 | 6    2 | x | 8    3 | 5 | x 
---|---|---  ---|---|---  ---|---|---
 8 | 1 | 4    7 | 1 | 4    7 | 1 | 8 
---|---|---  ---|---|---  ---|---|---
 3 | 5 | 7    5 | 6 | 3    6 | 2 | 4 

   (1,0)        (1,1)        (1,2)    
 2 | 8 | 5    4 | 7 | 6    7 | 8 | 6 
---|---|---  ---|---|---  ---|---|---
 x | 1 | 3    2 | x | 8    2 | 1 | x 
---|---|---  ---|---|---  ---|---|---
 7 | 4 | 6    1 | 3 | 5    3 | 5 | 4 

   (2,0)        (2,1)        (2,2)    
 2 | 6 | 8    4 | 6 | 3    6 | 2 | 8 
---|---|---  ---|---|---  ---|---|---
 7 | 1 | 4    8 | 1 | 7    7 | 1 | 4 
---|---|---  ---|---|---  ---|---|---
 x | 3 | 5    5 | x | 2    5 | 3 | x 
```

### Smart Random vs Reinforcement

The random computer will look for 2 in a row of its own color and win if it can, or look for 2 in a row of the opponent and block.  Else it will choose a random place to put a piece.  More unique boards considered when going first, but fewer when going second (eg. it explored even less of the possible boards than random).

Stats (out of 1,000,000)     | X's     | O's
------------------------------------------------
More wins than loses (after) |   8,054 |   1,513
Last lose                    | 976,266 | 785,420
Unique boards seen           |   1,982 |   2,109
Possible moves considered    |   6,327 |   7,048

When they played eachother they hit a cat's game at game 8,130.

X's first move preference (all equal possibilities):
```
 1 | 1 | 1  
---|---|---
 1 | 1 | 1 
---|---|---
 1 | 1 | 1 
```

O's first move preference:
```
   (0,0)        (0,1)        (0,2)    
 x | 5 | 8    1 | x | 1    4 | 8 | x 
---|---|---  ---|---|---  ---|---|---
 2 | 1 | 7    8 | 1 | 6    6 | 1 | 7 
---|---|---  ---|---|---  ---|---|---
 3 | 6 | 4    5 | 1 | 7    2 | 3 | 5 

   (1,0)        (1,1)        (1,2)    
 1 | 6 | 5    1 | 6 | 1    7 | 5 | 1 
---|---|---  ---|---|---  ---|---|---
 x | 1 | 1    7 | x | 8    1 | 1 | x 
---|---|---  ---|---|---  ---|---|---
 1 | 7 | 8    1 | 5 | 1    6 | 8 | 1 

   (2,0)        (2,1)        (2,2)    
 4 | 5 | 2    7 | 1 | 6    2 | 8 | 7 
---|---|---  ---|---|---  ---|---|---
 7 | 1 | 3    5 | 1 | 8    4 | 1 | 5 
---|---|---  ---|---|---  ---|---|---
 x | 6 | 8    1 | x | 1    6 | 3 | x 
```

### Reinforcement vs Reinforcement

Learn as the computers choose from the available moves.  Each learning at the same time (with seperate models).  Due to how QLearning works, these models explore more fully all possible combinations.

Stats (out of 1,000,000)  | X's    | O's
--------------------------------------------
Start to hit only cat's   |  8,538 | 8,538
Unique boards seen        |  3,299 | 2,975
Possible moves considered | 10,573 | 9,484

X's first move preference:
```
 2 | 2 | 2  
---|---|---
 2 | 2 | 2 
---|---|---
 2 | 1 | 2 
```

O's first move preference:
```
   (0,0)        (0,1)        (0,2)    
 x | 5 | 3    1 | x | 1    8 | 7 | x 
---|---|---  ---|---|---  ---|---|---
 2 | 1 | 8    7 | 1 | 5    6 | 1 | 5 
---|---|---  ---|---|---  ---|---|---
 7 | 4 | 6    6 | 1 | 8    3 | 2 | 4 

   (1,0)        (1,1)        (1,2)    
 1 | 8 | 6    1 | 7 | 1    5 | 7 | 1 
---|---|---  ---|---|---  ---|---|---
 x | 1 | 1    6 | x | 5    1 | 1 | x 
---|---|---  ---|---|---  ---|---|---
 1 | 7 | 5    1 | 8 | 1    6 | 8 | 1 

   (2,0)        (2,1)        (2,2)    
 4 | 7 | 6    6 | 1 | 8    8 | 2 | 4 
---|---|---  ---|---|---  ---|---|---
 5 | 1 | 3    7 | 1 | 5    5 | 1 | 7 
---|---|---  ---|---|---  ---|---|---
 x | 8 | 2    1 | x | 1    6 | 3 | x 
```

## How to run

```
> ./tictactoe
who is playing X [(c)omputer or (h)uman]: h
who is playing O [(c)omputer or (h)uman]: c
how many rounds: 0
 | |
------
 | |
------
 | |


Enter a row,column pair to play (or enter for computer to play): 1,1
 | |
------
 |x|
------
 | |
```

