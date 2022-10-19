## Neural Network Learning 
This experiment was to train a neural network to learn how to play checkers.  The result is that it is possible, and on average plays better than random chance.  However, it has not shown its ability to out perform a human.  A hypothesis is that this is due to being trained with random input, which results the model learning 'bad habits.'

### Training Partner
This project has learning checkers players with the following algorithms: random, psuedo random, neural, and Q learners.  Past experiments showed the limitations of both random (teaching bad habits) and Q (not enough time or memory to fully explore the possible checkers combinations.

### Input/Output Encoding
Checkers is a two dimensional board with rows and columns.  A traditional checkers board is 8 rows with 8 columns - 64 cells.  Given the rules of checkers, only half of the cells are valid places for checkers - leaving 32 cells.  The neural network things of things in one dimensional arrays of real numbers.

The input is encoded as 5 one dimensional arrays that represents the current boards state.  The 5 arrays represent, the models piece locations, the models king locations, the opponents piece locations, the opponents king locations, and empty cells.

```
Board indices (8x8):
   0  1  2  3  4  5  6  7
0  -  0  -  1  -  2  -  3
1  4  -  5  -  6  -  7  -
2  -  8  -  9  - 10  - 11
3  12 - 13  - 14  - 15  -
4  - 16  - 17  - 18  - 19
5  20 - 21  - 22  - 23  -
6  - 24  - 25  - 26  - 27
7  28 - 29  - 30  - 31  -

Input:
[
 // model's pieces - cells 0,1,2,3,4,...
 0,1,1,0,0,...

 // model's kings - cells 0,1,2,3,4,...
 0,0,0,0,1,...

 // opponent's pieces - cells ...,27,28,29,30,31
 ...,0,1,1,0,0

 // opponent's kings - cells ...,27,28,29,30,31
 ...,1,0,0,0,0

 // empty - cells 0,...,31
 ...,0,1,0,1,...
]
```

The output is encoded in a similar fashion, a one dimensional array that represents each place on the board along with a potential direction to move.  This includes invalid moves, but those are trimmed when comparing with valid moves.

```
Output:
[
 // cell 0
 0, // Up Left
 0, // Up Right
 1, // Down Left
 0, // Down Right

 // cell 1
 ...
]

```

### Learning Parameters
A neural network has three primary configuration points: learning rate, batch size, and hidden layers.

Given the depth of the checkers game (~25 moves) and the large number of training runs (100K+) a smaller learning rate was preferred to smooth out sharp changes as new boards were explored.  With a larger learning rate, the model will quickly adjust and choose new values in familiar situations - leading to a model that is hard to trend to a stable outcome.

Two batch sizes were choosen, 1 and 100.  The results were pretty indistinguishable between the two.  Both seemed to offer resaonble results in the trained model.

Two hidden layer topologies were tried: input*3 and 128,256,128.  The later seemed to provide a marginally better network.  

The final ingredient was what behaviors to incentive for the model.

#### What to reinforce during learning?

The incentives and dis-incentives were applied after a game was complete.  The reward was either an incentive (+1) or a dis-incentive (-1) for the move(s) that involved.

Incentives
 * [style] Winning (Win)
 * [situation] Gaining a King (King)
 * [situation] Capturing a piece (Capture)

Dis-incentives
 * [style] Cats (DCats)
 * [style] Loosing (DLoss)
 * [situation] Loosing a piece (Loss)
 * [situation] Oscillating piece (Oscillation)

### Experiment 
A series of experiments were run with varying incentives to understand which incentives were most profitable in the given situation.

To accomodate playing a smart learning program, a smaller checkers setup was used.

```
White goes first (down) and should win within 12 moves.
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

Two different trainings were done to see which incentives were the most impactful.  The first set of trainings were done with a smart opponent (Q Learning).  These were checking to see if the neural network could learn from lossing.  The other experiment was done with two brand new neural networks, and the learning was biased towards learning from winning. 

The end result was that each model was able to dramatically improve winning percentages compared with a random opponent.  However, they were not able to win against a human opponent.

#### Trian by Loosing to White
The Q model was a fully trained White opponent and was supposed to win.  The Neural network would need to learn from loosing on what best strategy it could take.  The best possible outcome would be a Cats game.

The following were the most successful inceptive combinations.

 * DLoss, King (cats 93%)
 * Win, King, Capture (cats 91%)
 * Win, King, Oscillation (cats 81%)
 * DLoss, King, Capture, Oscillation (cats 77%)
 * DCats, DLoss, King, Capture (cats (74%)

![checkers.loss.black](https://github.com/speedyjeff/Reinforcement/blob/main/Media/checkers.loss.black.png)

Based on the most successful runs, models that incentive kings and captures, and dis-incentive loosing pieces yielded the strongest models.

#### Trian by Loosing to Black
The Q model was a fully trained Black opponent, though should be beatable.  The Neural network would need to start by learning from loosing and evetually start to win and learn from winning.  The Neural model never was able to start winning, but was able to get the Q model to Cats often.

The following were the most successful inceptive combinations.

 * Win, King, Capture, Oscillation (cats 92%)
 * DLoss, King, Capture (cats 92%)
 * DLoss, Capture, Oscillation (cats 91%)
 * Win, Capture, Oscillation (cats 91%)
 * Win, Capture (cats 91%)

![checkers.loss.white](https://github.com/speedyjeff/Reinforcement/blob/main/Media/checkers.loss.white.png)

Based on the most successful runs, models that incentive winning and captures, and dis-incentive oscillating pieces yielded the strongest models.

#### Train by Winning
The next experiment, allowed the model to learn from a novice player, there by learning by winning.  Runs were conducted where an untrained neural mode played another untrained neural model.

##### White wins
White winning was the least likely outcome.  The best neural network only won 48% of the time.  For reference, the White opponent between two psuedo random opponents would win roughly 9% of the time, where as the neural network would often win above 20%.

 * Win, DCats, Capture, Oscillation (win 48%)
 * Win, DCats, DLoss, Loss, Capture (win 42%)
 * DCats, DLoss, King, Capture, Oscillation (win 36%)
 * DCats, DLoss, Loss, Capture, Oscillation (win 29%)
 * DCats, Capture (win 20%)

![checkers.win.white](https://github.com/speedyjeff/Reinforcement/blob/main/Media/checkers.win.white.png)

Based on the most successful runs, models that incentive captures, and dis-incentive cats games yielded the strongest models. (-learningstyle 2 -learningsituations 4)

##### Black wins
Black winning had a high probability, with the most successful model winning >64%.  For reference, the Black opponent between two psuedo random opponents would win roughly 9% of the time, where as the neural network would often win above 60%. 

 * DCats, Loss (win 64%)
 * DCats, DLoss, Loss, King, Oscillation (win 64%)
 * Win, DCats, DLoss, Loss (win 64%)
 * Win, DCats, DLoss, Loss, King, Oscillation (win 64%)
 * DCats, DLoss, Oscillation (win 64%)

![checkers.win.black](https://github.com/speedyjeff/Reinforcement/blob/main/Media/checkers.win.black.png)

Based on the most successful runs, models that incentive winning and kings, and dis-incentive cats and loosing pieces yielded the strongest models. (-learningstyle 3 -learningsituations 3)

##### Cats
Cats games were the most likely outcome.  For reference, two pseudo random agents would hit cats 82% of the time, where as the neural network would often hit above 90%.

 * Win, Catpure, Oscillation (cats 95%)
 * DLoss, Capture, Oscillation (cats 95%)
 * Win, Loss, King, Capture, Oscillation (cats 95%)
 * DLoss, Capture (cats 93%)
 * King (cats 92%)

![checkers.loss.cats](https://github.com/speedyjeff/Reinforcement/blob/main/Media/checkers.loss.cats.png)

Based on the most successful runs, models that incentive captures and kings, and dis-incentive lossing a piece, losing, and oscillating yields the strongest model.

### Recreate the experiment
There is a lot of randomization happening in the learning to help with area exploration.  

To get started a series of models are created and used as baselines.

```
checkers.exe -q -it 1 -playerb n -playerw n -in board1.txt -maxturn 12 -learnst %%i -learnsi %%j -trainst %%k -seed 123456
rename Black.neural Black.neural.baseline
rename White.neural White.neural.baseline
```

Then train the Q model to run against.

```
checkers.exe -it 500000 -playerb c -playerw c -in board1.txt -maxturn 12
```

Now run an exhaustive run against all the possible inputs of the incentives and their combinations.

```
ECHO OFF
REM -learnst 1 2 3 4 5 6 7
REM -learnsi 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
REM -trainst 1 2

del results.tsv

for /l %%i in (0,1,7) do (
for /l %%j in (0,1,15) do (
for /l %%k in (1,1,2) do (
        REM del model.* 2> nul
        del *.neural 2> nul
        echo f | xcopy /y Black.neural.baseline Black.neural > nul
        echo f | xcopy /y White.neural.baseline White.neural > nul
        echo checkers.exe -q -it 100000 -playerb n -playerw c -in board1.txt -maxturn 12 -learnst %%i -learnsi %%j -trainst %%k -seed 123456 >> results.tsv
        checkers.exe -q -it 100000 -playerb n -playerw c -in board1.txt -maxturn 12 -learnst %%i -learnsi %%j -trainst %%k -seed 123456 >> results.tsv
)
)
)
```

### Future Experiments
 * train a model for each piece that can move (it is the center of the view)
 * use randomization at smaller granularity - at per move choice and at for the entire game

