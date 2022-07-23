## Neural Networks
Neural networks are a prediction algorithm that can be trained to reconginize patterns and are used throughout the industry for AI.

This is a sample for using a neural network on The MNIST database of handwritten digits.  You can grab the input from a number of places, but I grabbed it from [here](http://yann.lecun.com/exdb/mnist/).

The Viewer can be used to preview some or all the samples to get a sense of what is being evaluated. Here are a few examples.

![4](https://github.com/speedyjeff/Reinforcement/blob/master/Digits/media/4.png) ![2](https://github.com/speedyjeff/shootmup/blob/master/Reinforcement/blob/master/Digits/media/4.png)

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
Learning rate = 0.15 | 99.99167%     | 99.90833%          | 100%                    |
Mini batch = 100     | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.80 | 99.958336%    | 99.99167%          | 100%                    |
Mini batch = 100     | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.15 | 52.983334%    | 29.841667%         | 0%                      |
Mini batch = 1000    | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|
Learning rate = 0.15 | 100%          | 100%               | 100%                    |
Mini batch = 1       | | | |
Iterations = 1       | | | |
Input size = 60K     | | | |
Split - 80/20        | | | |
---------------------|---------------|--------------------|-------------------------|

