## Teaching a learning model to play Checkers
Checkers has been around for a very long time, and learning programs have been built that are capable of winning against human players.  The start of that journey was with [Jonathan Schaeffer's Chinook](https://www.theatlantic.com/technology/archive/2017/07/marion-tinsley-checkers/534111/).  Schaeffer is quoted as saying the following about the complexity of modeling Checkers:

"Imagine draining the Pacific Ocean and then having to fill it back up with a tiny cup, one at a time. The sea is the number of checkers possibilities. The cup is each calculation."

There is a massive 5x10^20 possibilities in a game of Checkers.

The code in this repo build on the experiment of using Q model reinforcement learning on a [Maze](https://github.com/speedyjeff/Reinforcement/tree/main/Maze) and [Tic-tac-toe](https://github.com/speedyjeff/Reinforcement/tree/main/TicTacToe).  There were also experiments of using Neural networks to recognize [Hand written digits](https://github.com/speedyjeff/Reinforcement/tree/main/Digits) and find balance with the [CartPole](https://github.com/speedyjeff/Reinforcement/tree/main/CartPole).  The advantage the earlier experiments had is that the number of possible contexts being considered numbered in the thousands (5x10^3 in the case of tic-tac-toe).  Where as, Checkers (5x10^20) has 17 magnitude more possible boards to consider!

This lead to a series of experiments.

### Running the experiements
The code is buildable and you can run the experiments along with this.

```
./checkers [-dimensions #] [-reward #.#] [-learning #.#] [-discount #.#] [-help] [-showboards] [-showinsights] [-playerwhite C] [-playerblack C] [-iterations #] [-maxturns #] [-initboardfile <file>] [-retainunique]
  -(dim)ensions   : [4-8]
  -(h)elp         : show this help
  -(q)uiet        : provide minimal output
  -(seed)         : seed for psuedo random generators (default not set)
  -(showb)oards   : display the boards while iterating
  -(showi)nsights : show the learning matrix for the computer
  -(playerw)hite
  -(playerb)lack  : specifier for which player type to choose (bypassing user input)
  -(it)erations   : number of iterations (bypassing user input)
  -(maxtu)rns     : max turns until the game is called cats (default 50)
  -(in)itboardfile
              : a file containing -wWbB indicating the initial placement of pieces
                e.g.
                  ---w----
                  b-w---W-
                  --------
                  --b-----
                  -b------
 -(ret)ainunique : print a list of unique series of moves which games happened
 Q Learning
  -(rew)ard      : cost of taking an action (default -0.1)
  -(learni)ng    : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.2)
  -(dis)count    : 0 [short term rewards] ... 1 [long term rewards] (default 0.1)
 Neural Network learning
  -(maxtr)ainpct : maximum training percentage (0.0-1.0) (default 0.5)
  -(n)olearning  : no learning (default false)
  -(dum)pstats   : dump the training data (recommended not to go beyond 5k iterations)
  -(learnst)yle #[,#]
  -(learnsi)tuations  #[,#]
                 : these values represent configuration for [white,black] (or if a single number, both)
                 : learning styles [pos for succes: 1, neg for cats: 2, neg for loss: 4] (can be added to gether) (default: 1,1)
                 : learning situations [neg for loss of piece: 1, pos for king: 2, post for capture: 4, neg for oscillation: 8] (default: 12,12)
  -(t)rainstyle  : training styles [random: 1, predictable: 2] (these cannot be combined) (default: 2)
```

An example of training a model:
```
./checkers.exe -playerw c -playerb n -it 10000
```

## Q Learning

### Outcomes
Adapting the same learning approach applied to tic-tac-toe went relatively smoothly.  The challenge is the amount of time and memory it takes to fully explore the 5x10^20 poossibilities.  That is 100,000 trillion - wow.  This counting is strictly not possible, as it is not possible to have every piece be a king, etc.  Few machines have enough volatile memory to store this data set which would lead to an external storage (like a database - not explored).  Further, to reasonably observe enough of these states, you need to do this many runs - which takes a very long time.

Modern AIs for Checkers use neural networks that approximate and collapse states to achiece learning models in much less space and less time.

Using my 16gb laptop and a finite amount of time invested, I did not train a model capable of playing a full 8x8 game.

However, there were a number of learnings along the way and this model is able to play with a pre-defined (and reduced) starting point.

### Area experimented
Since training a model capable of learning a full 8x8 board become untenable, strategies to reduce the set of possible inputs become required to explore.

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

## Neural Network

### Experiments
The neural network is built very similar to how we trained to play tic-tac-toe.  Complete games were played and the result determined if the steps taken were used to train the resulting network.  Unlike tic-tac-toe more partial outcomes were considered.  You can see the complete is of sub-experiments run in [neural network](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.neural.md).

An advantage of training with a neural network is that it is capable to playing with the full set of possible checkers boards.  Whereas strategies like Q learning, keep state for every state + action pair, which are limited on modern hardware.  

A disadvantage of how this neural network was trained, is that it would need to see every possible game configuration multiple times to become very proficient.  The neural network was very competient at picking a reasonable outcome once it had seen the same situation multiple times.  When in new situations it applies an approximation for choosing an output - at times seemingly random.

The final experiment ran with the most successful configurations for both white and black and let them play for 1,000,000 games.

Optional configurations:
 * White
  * De-incentive cats
  * Incentive capture
 * Black
  * Incentive wins
  * De-incentive cats
  * De-incentive lossing a piece
  * Incentive kings

```
./Checkers.exe -it 1000000 -playerb n -playerw n -learnst 2,3 -learnsi 4,3
```

The results were frankly in-line with two random players: white wins ~17%, black wins 26%, and cats 56%.  This is then seen when the trained model is played against a random player, its win/loss/cats ratios are similar to a random player.  It is very possible that these models have become overfit and no longer are able to successfully win.  The [neural network](https://github.com/speedyjeff/Reinforcement/tree/main/Checkers/data.neural.md) offers further experiments that could be tried.  

As a quick experiment, I trained the sam model with fewer games (10,000).  The results show that the neural network's results are on par with random.  So, there needs to be a whole lot more training, but not to the point where it is over trained.  It is aslo possible that the further experiments of training the model from a different perspective may yield greater results.
