### Adjusting the learning parameters
There are three learning paramters that control how long or short rewards are considered and how much piror knowledge should be taken into account.

 * Reward  : cost of taking an action (a negative, non-zero, value)
 * Learning: At 0 no learning/rely on prior knowledge and at 1 only the most recent info is used
 * Discount: At 0 short term rewards are valued and at 1 long term rewards are valued

This training was done with the following setup.  The measure of success was to count how many unique contexts were observed (more is better).

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
 * A sweet spot of observed: learning(0.2) discount(0.1) reward(-0.1)
 * Using these settings 16% to 19% more contexts were discovered
 * These settings value more exploration, and as a result the resulting model did not settle and sought a cats over a win.

##### Adjusting Learning
The learning parameter adjusts how much prior konwledge is leveraged.  Running experimental data suggests that relying more on prior knowledge (less learning) yielded more contexts being discovered.  This was a surprising outcome.

![learning](https://github.com/speedyjeff/Reinforcement/blob/main/Media/learning.png)

```
Max turns: 8, Iterations: 10,000

Learning | Iteration | White Context | Black Context
---------|-----------|---------------|--------------
0.0 | 0 | 3228 | 4139
0.0 | 1 | 3277 | 4180
0.0 | 2 | 3232 | 4128
0.0 | 3 | 3198 | 4087
0.0 | 4 | 3242 | 4154
0.1 | 0 | 5183 | 6655
0.1 | 1 | 5148 | 6607
0.1 | 2 | 5130 | 6598
0.1 | 3 | 5136 | 6601
0.1 | 4 | 5165 | 6643
0.2 | 0 | 5357 | 6800
0.2 | 1 | 5385 | 6842
0.2 | 2 | 5326 | 6775
0.2 | 3 | 5342 | 6777
0.2 | 4 | 5395 | 6861
0.3 | 0 | 5320 | 6750
0.3 | 1 | 5249 | 6676
0.3 | 2 | 5325 | 6773
0.3 | 3 | 5335 | 6766
0.3 | 4 | 5339 | 6744
0.4 | 0 | 5234 | 6663
0.4 | 1 | 4159 | 5463
0.4 | 2 | 4906 | 6298
0.4 | 3 | 5004 | 6365
0.4 | 4 | 4647 | 6020
0.5 | 0 | 3582 | 4715
0.5 | 1 | 4757 | 6082
0.5 | 2 | 4594 | 5930
0.5 | 3 | 4884 | 6249
0.5 | 4 | 3290 | 4308
0.6 | 0 | 4279 | 5468
0.6 | 1 | 3763 | 4857
0.6 | 2 | 4537 | 5888
0.6 | 3 | 4099 | 5302
0.6 | 4 | 4085 | 5358
0.7 | 0 | 4413 | 5691
0.7 | 1 | 3665 | 4789
0.7 | 2 | 4198 | 5458
0.7 | 3 | 4612 | 5968
0.7 | 4 | 2724 | 3610
0.8 | 0 | 4136 | 5314
0.8 | 1 | 3664 | 4752
0.8 | 2 | 2951 | 3870
0.8 | 3 | 3831 | 4912
0.8 | 4 | 3183 | 4123
0.9 | 0 | 4441 | 5675
0.9 | 1 | 2965 | 3890
0.9 | 2 | 4074 | 5310
0.9 | 3 | 2798 | 3689
0.9 | 4 | 2988 | 3943
```

##### Adjusting Discount
The discount parameter adjusts if short or long term outcomes should be valued more.  Experimental data suggests that valuing shorter term outcomes yielded more contexts being discovered.  This is not very surprising.

![discount](https://github.com/speedyjeff/Reinforcement/blob/main/Media/discount.png)

```
Max turns: 8, Iterations: 10,000

Discount | Iteration | White Context | Black Context
---------|-----------|---------------|--------------
0 | 0 | 3612 | 4595
0 | 1 | 3589 | 4577
0 | 2 | 3611 | 4598
0 | 3 | 3615 | 4605
0 | 4 | 3607 | 4598
0.1 | 0 | 4953 | 6366
0.1 | 1 | 5148 | 6614
0.1 | 2 | 5136 | 6572
0.1 | 3 | 5067 | 6474
0.1 | 4 | 4799 | 6182
0.2 | 0 | 4228 | 5525
0.2 | 1 | 4592 | 5892
0.2 | 2 | 5024 | 6424
0.2 | 3 | 4427 | 5802
0.2 | 4 | 4313 | 5668
0.3 | 0 | 3750 | 4981
0.3 | 1 | 4287 | 5606
0.3 | 2 | 5118 | 6570
0.3 | 3 | 5041 | 6495
0.3 | 4 | 4378 | 5755
0.4 | 0 | 4072 | 5312
0.4 | 1 | 3807 | 5048
0.4 | 2 | 5059 | 6476
0.4 | 3 | 5057 | 6490
0.4 | 4 | 4881 | 6348
0.5 | 0 | 4175 | 5452
0.5 | 1 | 4809 | 6174
0.5 | 2 | 4158 | 5454
0.5 | 3 | 4204 | 5531
0.5 | 4 | 5060 | 6482
0.6 | 0 | 2983 | 3924
0.6 | 1 | 3851 | 5101
0.6 | 2 | 4862 | 6263
0.6 | 3 | 3289 | 4332
0.6 | 4 | 3744 | 4956
0.7 | 0 | 4510 | 5939
0.7 | 1 | 3978 | 5273
0.7 | 2 | 3708 | 4912
0.7 | 3 | 4803 | 6200
0.7 | 4 | 3766 | 5005
0.8 | 0 | 3631 | 4833
0.8 | 1 | 3079 | 4070
0.8 | 2 | 4081 | 5386
0.8 | 3 | 3440 | 4572
0.8 | 4 | 4195 | 5528
0.9 | 0 | 5050 | 6488
0.9 | 1 | 4841 | 6257
0.9 | 2 | 4796 | 6173
0.9 | 3 | 4459 | 5804
0.9 | 4 | 4557 | 5861
```

##### Adjusting Reward
The reward is a value added every time an action is taken.  In our case, it is a negative value (eg. it costs to move).  The outcome is that more contexts were found when the reward is the lowest.  Given how long the chains are from a win to first move, this is not very surprising.

![reward](https://github.com/speedyjeff/Reinforcement/blob/main/Media/reward.png)

```
Max turns: 8, Iterations: 10,000

Reward | Iteration | White Context | Black Context
---------|-----------|---------------|--------------
-0.1 | 0 | 4527 | 5887
-0.1 | 1 | 4540 | 5826
-0.1 | 2 | 4306 | 5566
-0.1 | 3 | 4422 | 5711
-0.1 | 4 | 4928 | 6282
-0.2 | 0 | 4612 | 5875
-0.2 | 1 | 4487 | 5786
-0.2 | 2 | 4199 | 5455
-0.2 | 3 | 4193 | 5399
-0.2 | 4 | 3920 | 5026
-0.3 | 0 | 4306 | 5429
-0.3 | 1 | 4024 | 5124
-0.3 | 2 | 4543 | 5748
-0.3 | 3 | 4573 | 5796
-0.3 | 4 | 4142 | 5306
-0.4 | 0 | 3860 | 4898
-0.4 | 1 | 3847 | 4878
-0.4 | 2 | 3969 | 4987
-0.4 | 3 | 3891 | 4905
-0.4 | 4 | 4145 | 5217
-0.5 | 0 | 3739 | 4728
-0.5 | 1 | 3251 | 4104
-0.5 | 2 | 3630 | 4643
-0.5 | 3 | 3610 | 4597
-0.5 | 4 | 3550 | 4532
-0.6 | 0 | 3683 | 4723
-0.6 | 1 | 2527 | 3224
-0.6 | 2 | 3531 | 4501
-0.6 | 3 | 2848 | 3623
-0.6 | 4 | 3259 | 4253
-0.7 | 0 | 2212 | 2809
-0.7 | 1 | 3033 | 3914
-0.7 | 2 | 3264 | 4247
-0.7 | 3 | 3143 | 4060
-0.7 | 4 | 3128 | 4007
-0.8 | 0 | 3553 | 4556
-0.8 | 1 | 3368 | 4305
-0.8 | 2 | 3696 | 4804
-0.8 | 3 | 3509 | 4480
-0.8 | 4 | 3571 | 4610
-0.9 | 0 | 3961 | 5088
-0.9 | 1 | 2094 | 2640
-0.9 | 2 | 2953 | 3789
-0.9 | 3 | 2780 | 3546
-0.9 | 4 | 2073 | 2628
-1 | 0 | 3153 | 4095
-1 | 1 | 3587 | 4570
-1 | 2 | 3174 | 4029
-1 | 3 | 2751 | 3474
-1 | 4 | 2033 | 2561
```

##### Adjusting Learning + Discount + Reward
Putting all these learning togehter yields 16% to 19% more contexts.  However, the resulting model tends to favor a cats game and was not able to settle on a model that would not occasionally allow black to win (which in this scenario it should not).

![together](https://github.com/speedyjeff/Reinforcement/blob/main/Media/together.png)

```
Max turns: 8, Iterations: 10,000

Data Set | Iteration | White Context | Black Context
---------|-----------|---------------|--------------
After | 0 | 5787 | 7195
After | 1 | 5957 | 7427
After | 2 | 5894 | 7385
After | 3 | 5757 | 7250
After | 4 | 5850 | 7274
Before | 0 | 4997 | 6326
Before | 1 | 4947 | 6320
Before | 2 | 4695 | 5963
Before | 3 | 4909 | 6380
Before | 4 | 4936 | 6254
```

