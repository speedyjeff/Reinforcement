### Shakespeare
The next larger language model experiment was to train a model on all of [Shakespeare's works](https://raw.githubusercontent.com/karpathy/char-rnn/refs/heads/master/data/tinyshakespeare/input.txt).  This is a sizable step up from the Alphabetize experiment used to get the basics of a language model correct.  Even though this is a much larger training set, this will still yield Tiny Language Models (for reasons explained below).

Many experiments were run to find the right parameters in order to train a base model.  No post-training or reinforcement have been done.  Each of these models are trained on a CPU bound neural network, which is very computationally expensive to reproduce. More work is needed to speed up the training and achieve larger model size.

The following command is a reasonable run to give this a try.

```
tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 16 -p 0.001
```

##### Successful Attempts
Several combinations of parameters could yield a successful model.  Success is defined as reaching at least 80% inference accuracy with training set.  To further test the model, a stream inference was produced and inspected visually.  Could the model produce a 1k sequence of tokens that were English, with proper grammar and spelling.

##### Tiny Input size and Large Min Context Size
The model was trained a trivially small section of Shakespeare's "The Winters Tale" Act 4.

```
e than you can dream of yet;
Enough then for your wonder. But, come on,
Contract us 'fore these witne
```

Given the small input size, the small hidden layer was able to infer correctly.

```
tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 16 -m 8 -p 0.0005 -la 4,4,2 -w 4 -b 2 
```

The resulting inference stream looked lik the following (note it started strong and then just hallucinated)

```
e than you can dream of yet;\nEnough then for your wonder. But, come on,\nContract us 'fore these witnetto sou yayrnaose atswheoho\noyo thatteet ...
```

###### Larger Hidden Layers and 1% of Input
This model was trained with ~1k of Shakespeare's Coriolanus, Act 1, Scene 6.

```
e in a dove-cote, I\nFlutter'd your Volscians in Corioli:\nAlone I did it. Boy!\n\nAUFIDIUS:\nWhy, noble lords,\nWill you be put in mind of his blind fortune,\nWhich was your shame, by this unholy braggart,\n'Fore your own eyes and ears?...
```

Part of Shakespeare's text include a chant of 'kill' by 'All Conspirators'.  During training, the model would frequently repeat this chant over and over again. :p

```
tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 16 -p 0.01 -w 4 -b 2 -la 4,4,4,4,4,4,4,4
```

This model did converge to 80% inference in about 1.4k iterations.  Giving the model a starting token, it was able to write correct spelling and grammar for 1k tokens.

This is an example of one of the model's final inferences:
```
,\nWill you be put in mind of his blind fortune,\nWhich was your shame, by this unholy braggart,\n'Fore your own eyes and ears?\n\nAll Conspirators:\nKill, kill, kill, kill him!\n\nLords:\nHold, hold, hold!\n\nAUFIDIUS:\nMy noble masters, hear me speak.\n\nFirst Lord:\nO Tullus,--\n\nSecond Lord:\nTeace, ho! no outrage: peace!\nThe man is noble and his fame folds-in\nThis orb o' the earth. His last offences to us\nShall have judicious hearing. Stand, Aufidius,\nAnd trouble not the peace.\n\nCOOIOLANUS:\nO that I had him,\nWith six Aufidiuses, or more, his tribe,\nTo use my lawful sword!\n\nAUFIDIUS:\nInsolent villain!\n\nAll Conspirators:\nKill, kill, kill, kill him!\n\nLords:\nHold, hold, hold!\n\nAUFIDIUS:\nMy noble masters, hear me speak.\n\nFirst Lord:\nO Tullus,--\n\nSecond Lord:\nTeace, ho! no outrage: peace!\nThe man is noble and his fame folds-in\nThis orb o' the earth. His last offences to us\nShall have judicious hearing. Stand, Aufidius,\nAnd trouble not the peace.\n\nCOOIOLANUS:\nO that I had him,\nWith six Aufidiuses, or more
```

There is repetition, but it produced correct grammar and spelling.  The model produced a version of this text that uses strings of words straight from the original text, but moves around the order.  

Taking a deeper look at the stats of when inference was more successful (eg. how much context was needed before inference had a high chance of being correct).

Context Length | Number of Attempts | Successful Attempts | Percentage
---------------|--------------------|---------------------|-----------
1              | 146,200            |  22,777             | 15.5
2              | 146,200            |  31,944             | 21.8%
3              | 146,200            |  41,480             | 28.3%
4              | 146,200            |  50,275             | 34.3%
5              | 146,200            |  59,849             | 40.9%
6              | 146,200            |  70,619             | 48.3%
7              | 146,200            |  79,137             | 54.1%
8              | 146,200            |  85,925             | 58.7%
9              | 146,200            |  91,851             | 62.8%
10             | 146,200            |  96,871             | 66.2%
11             | 146,200            | 101,015             | 69.0%
12             | 146,200            | 102,417             | 70.0%
13             | 146,200            | 104,439             | 71.4%
14             | 146,200            | 107,878             | 73.7%
15             | 146,200            | 109,207             | 74.6%
16             | 146,200            | 111,012             | 75.9%

The model provides a probability for all output.  The default is to choose the output with the highest probability, but the lower probability options may also provide correct grammar and spelling.  The results below highlight how often the 1st, 2nd, 3rd, and 4th probability was what the training data was seeking.  (note the denominator for this stat is number of attempts * sequence length - so larger than the table above)

Probability Order | Occurrence
------------------|----------
0                 | 1,266,696
1                 |   240,999
2                 |   141,137
3                 |    99,032

Walking through the inference above, the model adlibbed a few parts and skipped others.  The whole stream started with just a ','.  Below is a representation of what lines were replayed from the original text with a few modifications (likely due to the small context window).  Keep in mind that this whole stream was inferred 1 character at a time.

Line Numbers | Observation
-------------|------------------------
5-10         | Started with a ',' and faithfully replayed the subsequent text ending with a statement by 'All Conspirators', which is seen multiple times in the text.
34           | The repeated chant is shortened to 4 (instead of 5).
36           | 
37           | The repeated chant is shortened to 4 (instead of 5).
38-45        | The text ends with a statement by 'Second Lord', which is seem multiple times in the next.
19-45        | Two mis-spellings - "Teace" instead of "Peace" and "COOIOLANUS" instead of "CORIOLANUS".  These sections also included the chants with fewer repeated phrases.
19-27        | (same mis-spelling as the chunk above)

This portion of Shakespeare's work was chosen at random and contained a number of repetitive phrases.  The model, with the context given, ended up not re-telling the story linearly as a result of multiple duplicated phrases leading to different parts of the text.

#### Next Steps
 1. Investigate ways to train parallel models and merge results
 2. Increase token length (from 1)
 3. Explore output that is not the top probability, but the 2nd or 3rd.
 3. Go beyond a base model to post-training steps

#### Learning Attempts
Attempts that did not yield a successful base model, but offered insight.  Below is just a sample of all the attempts, there were many more.

##### Tiny Hidden Layer
Shakespeare's works often tokenize into 60-70 tokens (depending on how of the text is considered for training).  This example had a hidden network layer that was insufficient and the model never converged (just thrashed).

Layers:
 - Input - 16
 - Hidden[0] - 120
 - Output - 60

A sequent of 16 characters with 60 options each, has WAY MORE combinations than 16*120*60.

```
tinygpt.exe -d 2 -sh tinyshakespeare.txt -p 1.0 -lay 2
```

##### Input based Hidden Layer
Similar outcome when an insufficient hidden layer was chosen based on context length (in this case x4).

##### Larger hidden network, Reduced Input, Min Context Window
Using a smaller portion of the input (eg. fewer patterns to learn) is heading in the right direction.  The hidden layers continue to be too small.

 - Input - 16
 - Hidden[0] - 240
 - Hidden[1] - 240
 - Hidden[2] - 120
 - Output - 60


```
tinygpt.exe -d 2 -sh tinyshakespeare.txt -p 0.1 -lay 4,4,2 -se 16 -m 8
```

###### Crank up the Hidden Layers
Training took forever, and did not converge.  Training with 10% of the input was very large.

 - Input - 32
 - Hidden[0] - 600
 - Hidden[1] - 600
 - Hidden[2] - 900
 - Hidden[2] - 960
 - Hidden[2] - 1020
 - Hidden[2] - 1080
 - Hidden[2] - 1140
 - Hidden[2] - 1200
 - Hidden[2] - 1140
 - Hidden[2] - 1080
 - Hidden[2] - 1020
 - Hidden[2] - 960
 - Hidden[2] - 900
 - Hidden[2] - 600
 - Hidden[2] - 600
 - Output - 60


```
>tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 32 -p 0.1 -w 4 -b 2 -l 10,10,15,16,17,18,19,20,19,18,17,16,15,10,10 
```

###### Large Hidden Layers but smaller Context Window
Training took forever and did not converge.

```
>tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 16 -p 0.1 -w 4 -b 2 -l 10,10,15,16,17,18,19,20,19,18,17,16,15,10,10
```

###### Smaller Hidden Layers and Larger Context Window
Training took forever and did not converge.

```
>tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 32 -p 0.1 -w 4 -b 2 -l 14,16,18,20,20,18,16,14
```

###### Fewer Hidden Layers, Smaller Context Window, and Smaller Learning Factor
Learning from the previous attempts, this attempt kept the same amount of input (10%) - coming from Measure for Measure, specifically Act 2, Scene 4.  It is leveraging a smaller context window, fewer hidden layers, and a faster learning factor.

Text: 'wenty bloody blocks, he'ld yield them up,\nBefore his sister should her body stoop...'

```
>tinygpt.exe -d 2 -sh tinyshakespeare.txt -se 16 -p 0.1 -w 4 -b 2 -l 14,16,18,20,20,18,16,14 -le 0.001
```

This model is able to infer words, but it is not coherent (after 1000 iterations).

```
nh he tou seoie the the the the  o               \n aaot t u l he tour h r do he tour h r d me tour h r s me tour  he toure tour  he tour aed surn tour  he toure tour  he tour aed surn tour  he toure tour  he tour aed surn tour  he toure tour  he tour aed surn tour  he toure tour  he tour aed surn tour...
```




