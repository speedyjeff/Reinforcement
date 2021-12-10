### White (Smart) v Black (Random)
Training a reinforcement model on the following board with a random opponent.

```
White goes first (down) and should win.
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

#### Take-aways
 * White was not able to learn sufficiently from Random beyond 12
 * At 14, White would not consistently win
 * Random often teaches a learning algorithm bad habits that are easily exploited by a human.
 * Not a viable approach to train

Max Moves | White Context | White Win | Black Win | Cats
----------|---------------|-----------|-----------|------
8 | 3,686 | 20,819 | 0 | 979,181
10 | 10,637 | 194,737 | 0 | 805,263
12 | 9,964 | 478,895 | 0 | 521,105
14 | 17,052 | 467,639 | 4,699 | 594,800

```
Max moves: 8
First 1m iterations (white: 3,686 contexts)
wins : White [28246] Black [58] Cats [971696]

Play histogram:
  6 14 0.00%
  7 28246 2.82%
  8 971740 97.17%

Second 1m iterations (white: 3,686 contexts)
wins : White [20819] Black [0] Cats [979181]

Play histogram:
  7 20819 2.08%
  8 979181 97.92%

Max moves: 10
First 1m iterations (white: 10,637 contexts)
wins : White [188873] Black [233] Cats [810894]

Play histogram:
  6 14 0.00%
  7 123047 12.30%
  8 48 0.00%
  9 65812 6.58%
  10 811079 81.11%

Second 1m iterations (white: 10,637 contexts)
wins : White [194737] Black [0] Cats [805263]

Play histogram:
  7 124975 12.50%
  9 69762 6.98%
  10 805263 80.53%

Max moves: 12
First 1m iterations (white: 9,896 contexts)
wins : White [476081] Black [292] Cats [523627]

Play histogram:
  6 14 0.00%
  7 125360 12.54%
  8 43 0.00%
  9 262042 26.20%
  10 149 0.01%
  11 60325 6.03%
  12 552067 55.21%

Second 1m iterations (white: 9,964 contexts)
wins : White [478895] Black [0] Cats [521105]

Play histogram:
  7 125617 12.56%
  9 264483 26.45%
  10 1 0.00%
  11 60303 6.03%
  12 549596 54.96%

Max moves: 14
First 1m iterations (white: 16,636 contexts)
wins : White [461047] Black [5146] Cats [533807]

Play histogram:
  6 14 0.00%
  7 124904 12.49%
  8 46 0.00%
  9 68692 6.87%
  10 140 0.01%
  11 118948 11.89%
  12 45858 4.59%
  13 107332 10.73%
  14 534066 53.41%

Second 1m iterations (white: 17,052 contexts)
wins : White [467473] Black [4536] Cats [527991]

Play histogram:
  7 125313 12.53%
  9 41675 4.17%
  11 155091 15.51%
  12 42618 4.26%
  13 107311 10.73%
  14 527992 52.80%

Third 1m iterations (white: 17,083 contexts)
wins : White [467639] Black [4551] Cats [527810]

Play histogram:
  7 124802 12.48%
  9 41377 4.14%
  11 155302 15.53%
  12 42489 4.25%
  13 108220 10.82%
  14 527810 52.78%

Ran 10m iterations (white: 17,557 contexts)
wins : White [4035100] Black [46375] Cats [5918525]

Play histogram:
  7 1249979 12.50%
  9 75449 0.75%
  11 1350544 13.51%
  12 343979 3.44%
  13 1061524 10.62%
  14 5918525 59.19%

Fourth 1m iterations (white: 17,575 contexts)
wins : White [400501] Black [4699] Cats [594800]

Play histogram:
  7 125053 12.51%
  9 6953 0.70%
  11 129087 12.91%
  12 34279 3.43%
  13 109828 10.98%
  14 594800 59.48%
```