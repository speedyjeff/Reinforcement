## Teaching a learning model to play Checkers
Checkers has been around for a very long time, and learning programs have been built that are capable of winning against human players.  The start of that journey was with [Jonathan Schaeffer's Chinook](https://www.theatlantic.com/technology/archive/2017/07/marion-tinsley-checkers/534111/).  Schaeffer is quotes as saying the following about the complexity of modeling Checkers:

"Imagine draining the Pacific Ocean and then having to fill it back up with a tiny cup, one at a time. The sea is the number of checkers possibilities. The cup is each calculation."

There is a massive 5x10^20 possibilities in Checkers games.

The code in this repo build on the experiment of using reinforcement learning on a [Maze](https://github.com/speedyjeff/Reinforcement/tree/main/Maze) and [Tic-tac-toe](https://github.com/speedyjeff/Reinforcement/tree/main/TicTacToe).  The advantage the earlier experiments had is that the number of possible contexts being considered numbered in the thousands (5x10^3).  Checkers (5x10^20) had 17 magnitude more possible boards to consider.

This lead to a series of experiments.

### Running the experiements
The code is buildable and you can run the experiments along with this.

Help
```
./checkers [-dimensions #] [-reward #.#] [-learning #.#] [-discount #.#] [-help] [-showboards] [-showinsights] [-playerwhite C] [-playerblack C] [-iterations #] [-maxturns #] [-initboardfile <file>] [-retainunique]
  -(dim)ensions   : [4-8]
  -(rew)ard       : cost of taking an action (default -0.04)
  -(l)earning     : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.5)
  -(dis)count     : 0 [short term rewards] ... 1 [long term rewards] (default 1)
  -(h)elp         : show this help
  -(showb)oards   : display the boards while iterating
  -(showi)nsights : show the learning matrix for the computer
  -(playerw)hite
  -(playerb)lack  : specifier for which player type to choose (bypassing user input)
  -(it)erations   : number of iterations (bypassing user input)
  -(m)axturns     : max turns until the game is called cats (default 50)
  -(in)itboardfile
              : a file containing -wWbB indicating the initial placement of pieces
                e.g.
                  ---w----
                  b-w---W-
                  --------
                  --b-----
                  -b------
 -(ret)ainunique : print a list of unique series of moves which games happened
```

An example of training a model:
```
./checkers.exe -playerw c -playerb -it 10000
```

### Outcomes
Adapting the same learning approach applied to tic-tac-toe went relatively smoothly.  The challenge is the amount of time and memory it takes to fully explore the 5x10^20 poossibilities.  That is 100,000 trillion - wow.  In reality, this can be modeled as having roughly 1-100 trillion, but that is still a lot.  This reduction is possible, as it is theoretically possible to have every piece be a king, but practically that is not possible (along with a number of other configurations).  Few machines have enough volatile memory to store this data set which would lead to an external storage (like a database - not explored).  Further, to reasonably observe enough of these states, you need to do this many runs - which takes a very long time.

Modern AIs for Checkers use neural networks that approximate and collapse states to achiece learning models in much less space and less time.

Using my 16gb laptop and a finite amount of time invested, I did not train a model capable of playing a full 8x8 game.

However, there were a number of learnings along the way and this model is able to play with a pre-defined (and reduced) starting point.

### Area experimented
Since training a model capable of learning a full 8x8 board become untenable, strategies to reduce the st of possible inputs become required to explore.

The default set of rules are used for checkers and can be seen in [CheckersBoard.cs](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/CheckersBoard.cs).  Two limting rules were added to constrain run away AI.  The first is a maxturns policy - after 50 turns the game would result in a cats.  A turn is defined as a complete turn (multiple jumps are 1 turn).  The other rule is a stalemate rule, that states that if within 25 turns if no pieces were captured it was declared a cats game.

 * [Reducing dimensions](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.dimensions.md)
 * [Training with a random opponent](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.smart.random.md)
 * [Training with a learning opponent](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.smart.smart.md)
 * [Adjusting the learning parameters](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.learning.md)
 * [De-incetivizing Cats](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.nocats.md)

A number of the experiments were played with a starting board configuration - specified in [board1.txt](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/board1.txt).

White goes first (down) and should win:
```
   0  1  2  3  4  5  6  7
0  -  -  -  w  -  -  -  -
1  b  -  w  -  -  -  W  -
2  -  -  -  -  -  -  -  -
3  -  -  b  -  -  -  -  -
4  -  b  -  -  -  -  -  -
5  -  -  -  -  -  -  -  -
6  -  -  -  -  -  -  -  -
7  -  -  -  -  -  -  -  -
```

Possible set of moves to a White win:
w: 1,2 DownLeft
b: 3,2 UpRight
w: 1,6 UpLeft
b: 4,1 UpLeft
w: 0,3 DownLeft
b: 2,3 UpLeft (jump)
w: 0,5 DownLeft
b: 3,0 UpRight (jump)
w: 1,4 UpRight

These can be played in the following way:
```
./checkers.exe -initb board1.txt
```

Based on research, I feel that there are many cats outcomes when playing as Black.  This needs to be validated with a head-to-head with a heuristic based approach.
