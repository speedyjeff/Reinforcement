Training a computer to play tic-tac-toe (and win) without writing any rules for it to follow.  It will learn how to win and how to block by reinforcing what it learns by playing.

X's go first.

```
   |   |   
---|---|---
   |   |   
---|---|---
   |   |   
```

## How to run

```
// train the models
> ./tictactoe
how many rounds: 100000
who is playing X [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: c
who is playing O [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: c

// play
> ./tictactoe
how many rounds: 0
who is playing X [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: h
who is playing O [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: c

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

## Strategies to learning

### Q Learning
Using a Q learning strategy to teach tic-tac-toe to play the game.  There are many ways to vary the input used to train the model.  The sections below share learnings from using various inputs (random, smart random, and a q learner).

#### Random vs Reinforcement

Randomly choose from the available moves.  The key thing to notice is that a random strategy saw few unique boards as it did not fully walk all possible combinations.

Stats (out of 1,000,000)     | X's    | O's
-----------------------------|--------|--------
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

#### Smart Random vs Reinforcement

The random computer will look for 2 in a row of its own color and win if it can, or look for 2 in a row of the opponent and block.  Else it will choose a random place to put a piece.  More unique boards considered when going first, but fewer when going second (eg. it explored even less of the possible boards than random).

Stats (out of 1,000,000)     | X's     | O's
-----------------------------|---------|--------
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

#### Reinforcement vs Reinforcement

Learn as the computers choose from the available moves.  Each learning at the same time (with seperate models).  Due to how QLearning works, these models explore more fully all possible combinations.

Stats (out of 1,000,000)  | X's    | O's
--------------------------|--------|--------
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

### Neural Network
To train a neural network to learn to play tic-tac-toe, there are several key decisions that must be made.

 * How is the input encoded?
 * How is the output encoded?
 * What input to train the model with?
 * How do tune/optimize the network parameters to achieve the best outcome?
 * How to 'debug' the model if the results seem off?

#### Encoding Input
Neural networks operate on vectors so the first step is to take the 2d matrix and flatten.

```
 0 | 1 | 2 
---|---|---
 3 | 4 | 5 
---|---|---
 6 | 7 | 8 
```

The first attempt was to flatten into a 9 element array and encode 'o's as -1 and 'x's as 1, with 0 being empty.  

```
   | x |   
---|---|---
 o | x |    == [0,1,0,-1,1,0,0,-1,0]
---|---|---
   | o |  
```

This seemed like a resonable approach, but seemed to result in an over fit model that was not training well.  Later experiments likely pointed to a different root cause, but I moved onto a different encoding strategy.

The second attemp, and current approach, is to encode into a 27 element array with sections for 'o's, 'x's, and empties.

```
   | x |   
---|---|---     o's                  x's                  empty
 o | x |    == [0,0,0,1,0,0,0,1,0, 0,1,0,0,1,0,0,0,0, 1,0,1,0,0,1,1,0,1]
---|---|---
   | o |  
```

This approach has yieled models that converge on successfully learning to play tic-tac-toe.  The actual encoding is for 'mine', 'their's, and empty (but conceptually the description above is accurate).

#### Encoding Output
There are 9 possible choices for placing a piece, so the output is a number 0-8, which respresents a cell in the 2d matrix.  It is possible that the model will choose an occupied cell, so the algorithm chooses the most probable winning move.  Over time the model will be trained to not choose cells that are occupied.

#### Input for Training
Similar to the Q Learning examples above, there are various inputs that can be used to train the model.

I initially fell into a trap (same as when training with Q) by using a purely random opponent.  The random opponent taught the model bad habits which ended up making the model look like it was a bad fit for training.  The data suggested that the trained model would win roughly 50% of the time, which is fair from the goal.

To see the model, it works best to explore the space.  The neural network leverages a random opponent for that exploration.  Initially the first 80% of training runs were done random and the remaining 20% used the model.  This was not a good approach as it leads to the network getting 'stuck' and with no further exploration it will never learn anythig better.

The next step was to train 50% and try 50%, interleaving the two throughout the run.  This was a great approach and results in a balanced model.

The way the neural network is trained is using the moves that results in a complete board.  The wins can be positively reinforced and loses can be negatively reinforced.  With such a simple game like tic-tac-toe, where any given move may lead to either a win or a lose, having negative reinforcement is confusing.  As a result, only winnning games were used for training.  Then when training against a much more experienced opponent (Q), the neural network did very poorly - it never won.  The reason is that the game of tic-tac-toe is a cats game for two experienced players (unless they make a mistake).  After also training cats as a positive reinforcement, the nueral network succcessfully got to cats games 100% of the time when competting with Q.

The final ingredient was feeding all successfull games (wins and cats) to the network.  This meant that some of the games predicted by the network were also consiserd during trianging.

#### Parameter Optimization
There are three fundemental parameters to tune for the neural network: batch size, learning rate, and hidden layers.  

After much experimentation, batch sizes of more than 1 resulted in muttled results.  The end is that there is no batching (which is nice in other cases to smooth out the impact of any particular input set), but for tic-tac-toe the precision is needed.  The small learning rate meant that the model takes a bit longer to reach a steady state, but is less influenced by inputs and prefers the aggregate of the past.  Finally, there is no one right answer for the hidden layer size and count.  We ended up with 1 hidden layer that is a multiple of the input - it works well.

#### Debugging the 'model'
The ultimate fitness of the model is - "Does it win?"  But when it does not seem to be winning, how do you decompose the results and debug the outcomes and do smart experimentation with the network inputs?

The neural network captured two pieces of data:
 * Games used to train the model (eg. wins and cats)
 * Predictions made by the model and probabilities for all moves

This data is tabular and can be visualized in Excel.

Consider the following graphs that demonstrate the difference that occurs when changing the learning rate.

The following inputs were used to generate the graph below.
```
> ./tictactoe
how many rounds: 10000
who is playing X [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: r
who is playing O [(c)omputer | (h)uman | (r)andom | (s)mart | (n)eural]: n
```

The neural network is playing as 'o' after 'x' has choose cell 0.
```
 x |   |   
---|---|---
   |   |   
---|---|---
   |   |   
```

Learning rate == 0.15f
![0.15f](https://github.com/speedyjeff/Reinforcement/blob/main/media/learning15.png)

Learning rate == 0.0015f
![0.0015f](https://github.com/speedyjeff/Reinforcement/blob/main/media/learning0015.png)

The wild swing in the large learning rate highlights that it is taking recent wins too much into account.  The smaller learning rate smooths out the learning and helps to identify the most successful (over time) move for each situation.

This same technique can be then used to see where this model would go next. Here is an example of a game played (once trained) and you can watch to see the decision it is making at each step.  This is useful to see if the model is training.

Move 1 - 'x' choose 0, and 'o' choose 4
```
 x |   |   
---|---|---
   | o |   
---|---|---
   |   |   
```
![move1](https://github.com/speedyjeff/Reinforcement/blob/main/media/move1.png)

Move 2 - 'x' choose 3, and 'o' choose 6
```
 x |   |   
---|---|---
 x | o |   
---|---|---
 o |   |   
```
![move2](https://github.com/speedyjeff/Reinforcement/blob/main/media/move2.png)
The graph shows a strong bias for the block early on.

Move 3 - 'x' choose 1, and 'o' choose 2
```
 x | x | o 
---|---|---
 x | o |   
---|---|---
 o |   |   
```
![move3](https://github.com/speedyjeff/Reinforcement/blob/main/media/move3.png)

Using this output data helped me tune all the parameters.  I looked at the context it was considering and made a judgement call if the previous or current tuning yielded a better decision from the model.
