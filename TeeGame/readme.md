## Triangle Tee Game
The simple 'Triange Tee Game' is straight forward, but can be a challenge to solve.  The game consists of a triangle game board with 15 holes and 14 golf tees.

![game](https://github.com/speedyjeff/Reinforcement/blob/main/TeeGame/media/game.png)

The 14 tees are initially arranged in positions b-o.  The rules are very simple.

 1. to move a piece must jump another piece
 2. the position jumping into must be only, and you can only jump 1 piece at a time
 3. jumps are diagonal, left, or right
 4. jumped Tees are removed

The goal is to move the pieces and only have 1 remaining.

There are thousands of solutions, but even more invalid solutions.  This repro uses a Q learner and neural network to identify solutions.

Here are a few solutions:

 * d->a f->d l->e c->h n->l g->b a->d o->f f->m l->n d->m n->l k->m
 * d->a m->d k->m g->b n->l j->h f->d b->g a->f g->i f->m l->n o->m
 * f->a o->f h->j b->i n->e g->b j->c c->h a->d l->n d->m n->l k->m

### Common Learnings
The Q learner was able to successfully find a solution within ~5K iterations.  After running 10K run of 5k iterations, the Q learner found >7K unique solutions to the puzzle.  That is a lot!  There are even more possible games with 36 valid moves and 13 moves required for a solution.

Within all those combinations a few interesting patterns emerged from the solutions:

 - there are 2 starting moves: 'd->a' and 'f->a'
 - there are 2 finishing moves: 'k->m' and 'o->m'
 - no valid soluiton moves a piece from the position 'e'

This is a Sankey diagram of the first 100 solutions found.

![sankey](https://github.com/speedyjeff/Reinforcement/blob/main/TeeGame/media/sankey.png)

* Note: the numbers are placeholders to represent state transitions, their actual values is not particuarly important (see example solutions above).

It illistrates the consistency of how the games start and stop (always one of the 2 choices).  The middle has a lot of variety on how to get a solution.

### Q
The Q Learner uses a basic Q model that takes a short for the context (each bit representing if a Tee is present), and a string action (encoding the source and destination move).

![qpath](https://github.com/speedyjeff/Reinforcement/blob/main/TeeGame/media/qpath.png)

### Neural Network
The neural network takes in 15 inputs (each representing a position and if a Tee is present) and 36 outputs (reprenting each of the possible moves).

![neuralpath](https://github.com/speedyjeff/Reinforcement/blob/main/TeeGame/media/neuralpath.png)

### How to Build and Play

```
./TeeGame.exe -i 5000 -p 1 -q
```
