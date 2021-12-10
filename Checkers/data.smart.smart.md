### White (smart) v Black (smart)
Training a reinforcement model on the following board with a learning opponent.

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
 * The data suggests that the most common outcome for this configuration is a cats game.
 * Having a random loop inbetween two runs with a learning opponent helped to incrase the contexts seen.
 * With all 50 turns, Black was consistently winning.
 * There was a sweet spot at 24 turns where cats was most likely to occur. 
 * This method is more effective at finding more unique contexts as compared with Random (almost 2x at max moves of 14).

Max Moves | White Context | Ratio w/ Random | Black Context | White Win | Black Win | Cats
----------|---------------|-----------------|---------------|-----------|-----------|------
8 | 4,389 | 1.19 | 6,187 | 0 | 0 | 1,000,000
10 | 11,190 | 1.05 | 22,327 | 0 | 0 | 1,000,000
12 | 17,830 | 1.79 | 46,649 | 0 | 0 | 1,000,000
14 | 32,256 | 1.89 | 102,948 | 0 | 0 | 1,000,000
16 | 41,990 |  | 159,399 | 0 | 0 | 1,000,000
18 | 67,379 |  | 223,521 | 0 | 0 | 1,000,000
20 | 54,691 |  | 229,337 | 0 | 0 | 1,000,000
22 | 121,961 |  | 270,734 | 0 | 0 | 1,000,000
24 | 97,197 |  | 299,054 | 0 | 0 | 1,000,000
50 | 366,587 |  | 473,988 | 0 | 1,000,000 | 0


```
Max moves: 8
First 1m iterations (white: 2,663 contexts, black: 3,514 contexts)
wins : White [15] Black [33] Cats [999952]

Play histogram:
  6 14 0.00%
  7 15 0.00%
  8 999971 100.00%

Second 1m iterations (white: 2,663 contexts, black: 3,514 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  8 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 4,389 contexts, black: ~ contexts)
wins : White [123013] Black [10] Cats [876977]

Play histogram:
  7 123013 12.30%
  8 876987 87.70%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 6,187 contexts)
wins : White [0] Black [186566] Cats [813434]

Play histogram:
  6 115788 11.58%
  8 884212 88.42%

Max moves: 10
First 1m iterations (white: 4,739 contexts, black: 5,710 contexts)
wins : White [45] Black [95] Cats [999860]

Play histogram:
  6 14 0.00%
  7 14 0.00%
  8 26 0.00%
  9 20 0.00%
  10 999926 99.99%

Second 1m iterations (white: 4,739 contexts, black: 5,710 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  10 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 11,190 contexts, black: ~ contexts)
wins : White [182119] Black [132] Cats [817749]

Play histogram:
  7 122865 12.29%
  8 10 0.00%
  9 59247 5.92%
  10 817878 81.79%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 22,327 contexts)
wins : White [82] Black [248466] Cats [751452]

Play histogram:
  6 122295 12.23%
  7 1 0.00%
  8 43223 4.32%
  9 64 0.01%
  10 834417 83.44%

Max moves: 12
First 1m iterations (white: 14,316 contexts, black: 16,853 contexts)
wins : White [224] Black [254] Cats [999522]

Play histogram:
  6 14 0.00%
  7 13 0.00%
  8 41 0.00%
  9 72 0.01%
  10 135 0.01%
  11 120 0.01%
  12 999605 99.96%

Second 1m iterations (white: 14,316 contexts, black: 16,853 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  12 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 17,830 contexts, black: ~ contexts)
wins : White [131830] Black [49] Cats [868121]

Play histogram:
  7 20979 2.10%
  9 110715 11.07%
  10 61 0.01%
  11 83 0.01%
  12 868162 86.82%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 46,649 contexts)
wins : White [149] Black [335940] Cats [663911]

Play histogram:
  6 161735 16.17%
  8 63946 6.39%
  9 19 0.00%
  10 63182 6.32%
  11 115 0.01%
  12 711003 71.10%


Max moves: 14
First 1m iterations (white: 27,114 contexts, black: 31,231 contexts)
wins : White [506] Black [519] Cats [998975]

Play histogram:
  6 14 0.00%
  7 15 0.00%
  8 47 0.00%
  9 95 0.01%
  10 196 0.02%
  11 170 0.02%
  12 150 0.01%
  13 181 0.02%
  14 999132 99.91%

Second 1m iterations (white: 27,114 contexts, black: 31,231 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  14 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 32,256 contexts, black: ~ contexts)
wins : White [593591] Black [663] Cats [405746]

Play histogram:
  7 124834 12.48%
  8 1 0.00%
  9 43193 4.32%
  10 34948 3.49%
  11 209316 20.93%
  12 9344 0.93%
  13 166966 16.70%
  14 411398 41.14%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 102,948 contexts)
wins : White [1220] Black [370370] Cats [628410]

Play histogram:
  6 136146 13.61%
  8 32085 3.21%
  9 8 0.00%
  10 67643 6.76%
  11 79 0.01%
  12 63626 6.36%
  13 1062 0.11%
  14 699351 69.94%

Max moves: 16
First 1m iterations (white: 35,934 contexts, black: 40,287 contexts)
wins : White [632] Black [801] Cats [998567]

Play histogram:
  6 14 0.00%
  7 15 0.00%
  8 61 0.01%
  9 94 0.01%
  10 206 0.02%
  11 151 0.02%
  12 240 0.02%
  13 146 0.01%
  14 188 0.02%
  15 166 0.02%
  16 998719 99.87%

Second 1m iterations (white: 36,865 contexts, black: 41,414 contexts)
wins : White [63] Black [17] Cats [999920]

Play histogram:
  10 5 0.00%
  11 2 0.00%
  12 7 0.00%
  13 17 0.00%
  14 4 0.00%
  15 44 0.00%
  16 999921 99.99%

Running 1m iterations with White (smart) v Black (random)
(white: 41,990 contexts, black: ~ contexts)
wins : White [776702] Black [168] Cats [223130]

Play histogram:
  7 125463 12.55%
  8 1 0.00%
  9 57525 5.75%
  10 34537 3.45%
  11 199231 19.92%
  12 16434 1.64%
  13 208467 20.85%
  14 5748 0.57%
  15 125736 12.57%
  16 226858 22.69%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 159,399 contexts)
wins : White [1799] Black [420308] Cats [577893]

Play histogram:
  6 133841 13.38%
  8 30951 3.10%
  9 13 0.00%
  10 83248 8.32%
  11 102 0.01%
  12 67937 6.79%
  13 622 0.06%
  14 72813 7.28%
  15 961 0.10%
  16 609512 60.95%

Max moves: 18
First 1m iterations (white: 50,788 contexts, black: 55,432 contexts)
wins : White [1151] Black [1145] Cats [997704]

Play histogram:
  6 15 0.00%
  7 15 0.00%
  8 52 0.01%
  9 99 0.01%
  10 257 0.03%
  11 191 0.02%
  12 277 0.03%
  13 250 0.03%
  14 227 0.02%
  15 211 0.02%
  16 240 0.02%
  17 301 0.03%
  18 997865 99.79%

Second 1m iterations (white: 50,803 contexts, black: 55,449 contexts)
wins : White [5] Black [0] Cats [999995]

Play histogram:
  15 1 0.00%
  17 4 0.00%
  18 999995 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 67,379 contexts, black: ~ contexts)
wins : White [538808] Black [2644] Cats [458548]

Play histogram:
  9 36874 3.69%
  10 42706 4.27%
  11 160662 16.07%
  12 18804 1.88%
  13 59686 5.97%
  14 9207 0.92%
  15 122076 12.21%
  16 4270 0.43%
  17 84952 8.50%
  18 460763 46.08%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 223,521 contexts)
wins : White [4241] Black [483948] Cats [511811]

Play histogram:
  6 131614 13.16%
  8 69601 6.96%
  9 7 0.00%
  10 62399 6.24%
  11 107 0.01%
  12 67300 6.73%
  13 676 0.07%
  14 68968 6.90%
  15 1290 0.13%
  16 45438 4.54%
  17 1979 0.20%
  18 550621 55.06%

Max moves: 20
First 1m iterations (white: 40,224 contexts, black: 43,092 contexts)
wins : White [974] Black [758] Cats [998268]

Play histogram:
  6 14 0.00%
  7 14 0.00%
  8 44 0.00%
  9 90 0.01%
  10 170 0.02%
  11 122 0.01%
  12 143 0.01%
  13 149 0.01%
  14 123 0.01%
  15 151 0.02%
  16 117 0.01%
  17 226 0.02%
  18 107 0.01%
  19 160 0.02%
  20 998370 99.84%

Second 1m iterations (white: 40,240 contexts, black: 43,111 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  20 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 54,691 contexts, black: ~ contexts)
wins : White [841695] Black [1163] Cats [157142]

Play histogram:
  8 7 0.00%
  9 171700 17.17%
  10 35526 3.55%
  11 213362 21.34%
  12 17503 1.75%
  13 155429 15.54%
  14 6085 0.61%
  15 101072 10.11%
  16 1755 0.18%
  17 92421 9.24%
  18 1474 0.15%
  19 45910 4.59%
  20 157756 15.78%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 229,337 contexts)
wins : White [4274] Black [578200] Cats [417526]

Play histogram:
  6 162347 16.23%
  8 70948 7.09%
  9 7 0.00%
  10 76721 7.67%
  11 107 0.01%
  12 62822 6.28%
  13 497 0.05%
  14 76023 7.60%
  15 685 0.07%
  16 53019 5.30%
  17 1551 0.16%
  18 46134 4.61%
  19 1218 0.12%
  20 447921 44.79%

Max moves: 22
First 1m iterations (white: 77,328 contexts, black: 82,197 contexts)
wins : White [1937] Black [1862] Cats [996201]

Play histogram:
  6 14 0.00%
  7 14 0.00%
  8 54 0.01%
  9 101 0.01%
  10 239 0.02%
  11 206 0.02%
  12 312 0.03%
  13 287 0.03%
  14 281 0.03%
  15 286 0.03%
  16 241 0.02%
  17 330 0.03%
  18 234 0.02%
  19 284 0.03%
  20 360 0.04%
  21 334 0.03%
  22 996423 99.64%

Second 1m iterations (white: 77,328 contexts, black: 82,197 contexts)
wins : White [0] Black [0] Cats [1000000]

Play histogram:
  22 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 121,961 contexts, black: ~ contexts)
wins : White [819846] Black [977] Cats [179177]

Play histogram:
  7 73 0.01%
  9 89256 8.93%
  10 13895 1.39%
  11 134895 13.49%
  12 2188 0.22%
  13 120292 12.03%
  14 11856 1.19%
  15 148994 14.90%
  16 182 0.02%
  17 139358 13.94%
  18 3307 0.33%
  19 84803 8.48%
  20 325 0.03%
  21 70257 7.03%
  22 180319 18.03%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 270,734 contexts)
wins : White [5402] Black [621408] Cats [373190]

Play histogram:
  6 160542 16.05%
  8 64967 6.50%
  9 4 0.00%
  10 97236 9.72%
  11 54 0.01%
  12 61675 6.17%
  13 373 0.04%
  14 76285 7.63%
  15 526 0.05%
  16 54647 5.46%
  17 1463 0.15%
  18 43978 4.40%
  19 916 0.09%
  20 30737 3.07%
  21 1901 0.19%
  22 404696 40.47%

Max moves: 24
First 1m iterations (white: 86,942 contexts, black: 91,502 contexts)
wins : White [2347] Black [26259] Cats [971394]

Play histogram:
  6 14 0.00%
  7 14 0.00%
  8 58 0.01%
  9 104 0.01%
  10 258 0.03%
  11 227 0.02%
  12 285 0.03%
  13 273 0.03%
  14 277 0.03%
  15 316 0.03%
  16 260 0.03%
  17 349 0.03%
  18 202 0.02%
  19 306 0.03%
  20 192 0.02%
  21 335 0.03%
  22 201 0.02%
  23 322 0.03%
  24 996007 99.60%

Second 1m iterations (white: contexts, black: contexts)
wins : White [0] Black [24877] Cats [975123]

Play histogram:
  24 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 97,196 contexts, black: ~ contexts)
wins : White [922933] Black [409] Cats [76658]

Play histogram:
  9 139154 13.92%
  10 7 0.00%
  11 234517 23.45%
  12 53243 5.32%
  13 179308 17.93%
  14 47 0.00%
  15 107392 10.74%
  16 10446 1.04%
  17 80139 8.01%
  18 56 0.01%
  19 50109 5.01%
  20 2470 0.25%
  21 41996 4.20%
  22 89 0.01%
  23 23726 2.37%
  24 77301 7.73%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 299,024 contexts)
wins : White [5249] Black [659810] Cats [334941]

Play histogram:
  6 162400 16.24%
  8 67568 6.76%
  9 2 0.00%
  10 87882 8.79%
  11 42 0.00%
  12 72915 7.29%
  13 350 0.04%
  14 72748 7.27%
  15 608 0.06%
  16 48323 4.83%
  17 875 0.09%
  18 42424 4.24%
  19 749 0.07%
  20 35909 3.59%
  21 1287 0.13%
  22 33784 3.38%
  23 1157 0.12%
  24 370977 37.10%

Running 1m iterations with White (smart) v Black (smart)
(white: 97,197 contexts, black: 299,054 contexts)
wins : White [12] Black [0] Cats [999988]

Play histogram:
  12 2 0.00%
  15 5 0.00%
  17 3 0.00%
  19 1 0.00%
  23 1 0.00%
  24 999988 100.00%

Max moves: 50
First 1m iterations (white: 247,393 contexts, black: 249,006 contexts)
wins : White [10350] Black [969264] Cats [20386]

Play histogram:
  6 14 0.00%
  7 11 0.00%
  8 59 0.01%
  9 109 0.01%
  10 362 0.04%
  11 295 0.03%
  12 699 0.07%
  13 610 0.06%
  14 665 0.07%
  15 557 0.06%
  16 656 0.07%
  17 659 0.07%
  18 537 0.05%
  19 565 0.06%
  20 535 0.05%
  21 590 0.06%
  22 536 0.05%
  23 614 0.06%
  24 958454 95.85%
  25 578 0.06%
  26 1357 0.14%
  27 634 0.06%
  28 2745 0.27%
  29 1394 0.14%
  30 1362 0.14%
  31 1436 0.14%
  32 1260 0.13%
  33 1350 0.14%
  34 1324 0.13%
  35 1646 0.16%
  36 1376 0.14%
  37 1658 0.17%
  38 1083 0.11%
  39 1380 0.14%
  40 895 0.09%
  41 1136 0.11%
  42 726 0.07%
  43 1031 0.10%
  44 739 0.07%
  45 827 0.08%
  46 601 0.06%
  47 718 0.07%
  48 555 0.06%
  49 585 0.06%
  50 5077 0.51%

Second 1m iterations (white: 247,393 contexts, black: 249,006 contexts)
wins : White [0] Black [1000000] Cats [0]

Play histogram:
  24 1000000 100.00%

Running 1m iterations with White (smart) v Black (random)
(white: 366,587 contexts, black: ~ contexts)
wins : White [954902] Black [4329] Cats [40769]

Play histogram:
  7 19901 1.99%
  9 100506 10.05%
  10 13397 1.34%
  11 86759 8.68%
  12 386 0.04%
  13 99354 9.94%
  14 543 0.05%
  15 62765 6.28%
  16 426 0.04%
  17 105078 10.51%
  18 498 0.05%
  19 71998 7.20%
  20 799 0.08%
  21 75680 7.57%
  22 351 0.04%
  23 75803 7.58%
  24 407 0.04%
  25 57567 5.76%
  26 827 0.08%
  27 47489 4.75%
  28 3131 0.31%
  29 34225 3.42%
  30 1007 0.10%
  31 27248 2.72%
  32 708 0.07%
  33 22391 2.24%
  34 1127 0.11%
  35 16879 1.69%
  36 610 0.06%
  37 15277 1.53%
  38 1320 0.13%
  39 10847 1.08%
  40 569 0.06%
  41 10479 1.05%
  42 608 0.06%
  43 6379 0.64%
  44 477 0.05%
  45 9380 0.94%
  46 429 0.04%
  47 3101 0.31%
  48 372 0.04%
  49 3937 0.39%
  50 8965 0.90%

Running 1m iterations with White (random) v Black (smart)
(white: ~ contexts, black: 473,988 contexts)
wins : White [8678] Black [883566] Cats [107756]

Play histogram:
  6 160298 16.03%
  8 61850 6.18%
  10 83466 8.35%
  11 7 0.00%
  12 90669 9.07%
  13 181 0.02%
  14 81447 8.14%
  15 395 0.04%
  16 50970 5.10%
  17 675 0.07%
  18 52543 5.25%
  19 552 0.06%
  20 39069 3.91%
  21 782 0.08%
  22 34730 3.47%
  23 621 0.06%
  24 33601 3.36%
  25 757 0.08%
  26 29294 2.93%
  27 588 0.06%
  28 35993 3.60%
  29 7920 0.79%
  30 28285 2.83%
  31 5340 0.53%
  32 25856 2.59%
  33 9351 0.94%
  34 19683 1.97%
  35 7757 0.78%
  36 21195 2.12%
  37 4736 0.47%
  38 14864 1.49%
  39 5661 0.57%
  40 15090 1.51%
  41 5711 0.57%
  42 10126 1.01%
  43 2749 0.27%
  44 9650 0.96%
  45 2681 0.27%
  46 6871 0.69%
  47 1624 0.16%
  48 6408 0.64%
  49 1699 0.17%
  50 28255 2.83%
```