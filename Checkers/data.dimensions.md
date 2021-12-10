### Reducing the dimensions
The full 8x8 checkers board has too many combinations for Q Learning to model and be effective.  This series of experiments reduces the dimensions to see if it can model it.  The result was that for 4x4 and 5x5 it is possible, but starting at 6x6 it becomes a very long process to train and store all the states (likely possible, but I did not wait for it to happen).

#### Dimension: 4
```
   0  1  2  3
0  -  w  -  w
1  -  -  -  -
2  -  -  -  -
3  b  -  b  -
```

Contexts | White | Black
---------|-------|------
         | 755   | 776

 * Trends to 100% cats within 10,000 iterations
 * When plyaing against a random player, the learning opponent will win >99% of the time

#### Dimension: 5
```
   0  1  2  3  4
0  -  w  -  w  -
1  w  -  w  -  w
2  -  -  -  -  -
3  b  -  b  -  b
4  -  b  -  b  -
```

Contexts | White | Black
---------|-------|------
         | 34,982| 32,737

 * Trends to 100% black wins within 20,2000 iterations
 * When playing against a random player, the learning oppnent will win >99% of the time

#### Dimension: 6
```
   0  1  2  3  4  5
0  -  w  -  w  -  w
1  w  -  w  -  w  -
2  -  -  -  -  -  -
3  -  -  -  -  -  -
4  -  b  -  b  -  b
5  b  -  b  -  b  -
```

Contexts | White | Black
---------|-------|------
 | 6,504,604+ | 6,518,064+

 * After 2,000,000+ iterations, no clear winnner was found.  White, black, and cats occur at about the same rate.
 * Reducing the max turns to 24 still did not have a clear winner within 1,000,000 iterations.
 * Reducing the max turns to 12 results in >99% being cats, which is not very surprising as that is not many moves to capture all the pieces.



