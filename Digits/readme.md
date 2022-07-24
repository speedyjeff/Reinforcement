## Neural Networks
Neural networks are a prediction algorithm that can be trained to reconginize patterns and are used throughout the industry for AI.

This is a sample for using a neural network on The MNIST database of handwritten digits.  You can grab the input from a number of places, but I grabbed it from [here](http://yann.lecun.com/exdb/mnist/).

The Viewer can be used to preview some or all the samples to get a sense of what is being evaluated. Here are a few examples.

![4](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/4.png) ![2](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/2.png)

These images are shrunk down to a 28 x 28 pixels and converted to grayscale (each pixel represented by a number between 0.0 and 1.0).  The 784 pixels are fed into the neural network and a prediction of which digit (0-9) is seen is shared.

There are a number of materials online that share how neural networks work.

### Experiments
There are a number of variables that influence the accuracy of the trained model:
 * Learning rate - a value that influences how much the model takes into account new data
 * Hidden layers - how many and how large the hidden layers are adds/removes complexity from the network, potentially increasing accuracy
 * Mini batches - updates can be applied to the model after every training, or in batches to smooth out the impact
 * Shuffling - shuffling the input along with mini batches varies subesequent learning and thus training
 * Iterations - how many times a model is run

All models start with crypto random values between -0.5 and 0.5, for all weights and bias'.



_                    | 784 x 10 x 10 | 784 x 16 x 16 x 10 | 784 x 10 x 10 x 10 x 10 |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.15 | 27.508333%    | 26.991667%         | 18.691668%              |
Mini batch = 100     | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.80 | 45.675%       | 59.625%            | 31.191668%              |
Mini batch = 100     | w/ shuffle    | w/ shuffle         | w/ shuffle              |
Iterations = 1       | 57.325%       | 49.408333%         | 22.983334%              |
Input size = 60K     | 10 itreations | 10 itreations      | 10 itreations           |
Split - 80/20        | 78.525%       | 75.48333%          | 65.59167%               |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.15 | 9.291667%     | 12.233334%         | 12.341666%              |
Mini batch = 1000    | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.15 | 10.05%        | 10.066667%         | 9.808333%               |
Mini batch = 1       | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|

The best performing model has a hidden layer of 10 neurons, learning rate of 0.8f, and enabling shuffling.  This model achieves roughly 85% accuracy when trained for 20 iterations.

Here is some additional data on how these configurations influence the overall model prediction rate.

![learningrate](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/learningrate.png)
![minibatch.png](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/minibatch.png)
![hiddenlayer](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/hiddenlayer.png)
![hiddenlayer2](https://github.com/speedyjeff/Reinforcement/blob/main/Digits/media/hiddenlayer2.png)

### How to run

Download all 4 gz mnist dataset [here](http://yann.lecun.com/exdb/mnist).  The program will unzip and read the data, so nothing beyond downloading is necessary.

```
./mnistdriver.exe -image train-images-idx3-ubyte.gz -label train-labels-idx1-ubyte.gz -hidden 10 -mini 1 -learn 0.15 -it 1 -split 0.2
```
