### TinyGPT
Large Language Models (LLM) are the powerhouse behind the wave of AI transforming many industries.

There are a number of great resources on what these models are and how they are built.  Below are several used to build this TinyGPT.

 - [Let's build GPT: from scratch, in code, spelled out. - Andrej Karpathy](https://www.youtube.com/watch?v=kCc8FmEb1nY)
 - [Deep Dive into LLMs like ChatGPT - Andrej Karpathy](https://www.youtube.com/watch?v=7xTGNNLPyMI)
 - [LLM Visualization](https://bbycroft.net/llm)

This project was taking the learnings from these videos and building a tiny version capable of a sliver of the potential of an LLM, with the goal to gain insight into how the larger projects work.

#### Getting Started
There are many steps towards building a model capable of what an LLM can do today.  Fundamentally, the steps to build, train, and use these models are the same (regardless of size).  Each model is trained on a corpus of input and during inference predicts one token at a time given the current context.

A stair step approach towards building a Tiny Language Model (TLM) has a few simple projects to get started.  Each project leverages a backpropagating neural network as the foundational component.

 - [Sequence alpha](alphabetize.md) characters in order
 - [Shakespeare](shakespeare.md) language model

#### Steps to train Base Models to Reinforced learned models
The videos above layout a training methodology to take a model capable of producing Shakespeare's works (for instance) into one that can produce new work in the tone and phrasing of Shakespeare.

##### TRAINING
Step 1:  Input
Get input. Either a corpus of text (in the case of Shakespeare) or auto generated (in the case of Alphabetize).

Step 2: Tokenize input
 - Leverage the [Byte Pair Encoding](https://en.wikipedia.org/wiki/Byte_pair_encoding) algorithm
 - Determine each unique occurrence of bytes - with length 1+

Step 3: Neural Network training
 - Network configuration
  - Input - The context window
  - Output - The number of tokens
 - A special token is used to delineate padding
  - Eg. if the context window is 8 and the input is only 5 tokens, there are 3 padding tokens occur at the end of the context window
 - Training occurs iteratively using a random substring of input data (length 1 to max [8k])
  - Iterate through 1..N tokens in the substring, using the n+1 token as reinforcement on what should be predicted
  - eg. "ABCA" -> B

Step 4: Inference. 
After a series of trainings, an Inference can be captured to check progress.  An inference provides a token and asks what comes next.  This can be used to gauge if the model is converging towards a solution.

An aspect of inference is to inject some level of variability in what the model replies.  Foundationally, the model will return the most probable next token in a sequence.  However, there are gradient of next most probable tokens that may fit.  The client can then have a weighted choice of choosing the most probable down through the top N predictions.  This provides variability in the reply when asked the same question multiple times.

Fully trained:
At this point, a model can accurately predict a sequence of tokens in a logical order (based on training input).  The following steps are how to take a base model and turn it into an assistant with a prompt.

###### POST-TRAINGING
Builds upon a successfully trained base model.

Step 1: Train with conversations.
 - Tokenize conversation prompts (eg. similar structure to a tcp/ip packet)
   - eg. <|im_start|>user<|im_sep|>question<|im_end|> (these are new tokens - not part of the training set)
   - <|im_start|>assistant<|im_sep|> (this is what the model returns: response<|im_end|>)
 - Create many questions that might be asked of the base model to train replies.
  - Consider using an LLM to get Q&A questions for training (given text, get me 3 Q&A, etc.)

Step 2: Inference.  
Again give the model a try to validate accuracy.

Step 3: Hallucinations
 - Train with the ability to reply "I do not know"
 - Use another model to validate understanding
 - With enough examples, the "I do not know" response will get better

When creating conversations, make it step by step which helps the model learn.

###### REINFOREMENT LEARNING
 - Run question prompts multiple times, assess correct answers
  - Train on the prompts with the right answers

##### General Learnings
Several attempts were made to normalize the inputs correctly for the model.

Input Attempt 1
 - Inputs are the vocab (1 if present, no concept for duplicates)
 - Single char
 - Outcomes
   - Loved spaces (eg. the most common input)
   - Could put a single word together

This approach was abandoned for (in hindsight) obvious reasons, this method did not handle phrases with duplicates.

Input Attempt 2
 - Inputs are the context window with each token normalizing from [0..1]
 - This approach did not yield a converging model

Input Attempt 3 (default)
 - Inputs are the context window and each token is the token numeric value [0..N]
 - Outcomes
  - Still loved spaces and vowels
  - Good approach

When filling the context window, it is important to have a special token used for padding.  The padding occurs after the input and fills the context window to the end.  Without out this approach, the model may confuse the 'empty' spaces as being a token and predictions are incorrect.

When training, the input is sampled at random and sent to the neural network.  The best approach is to be truly random.  A few additional techniques were attempted.
 - Pick a random start and then start the next chunk right after this starting point
 - Wrap around to the start, if hitting the end
 - Not random
 - Random starting point, but not such that it would warp (default)

A minimal window context is the number of tokens that every window contains.  It is simple to visualize 1.  However, only sharing 1 token with the model does not provide much context.  If the minimal window context is set higher (25% or 50% of the context window size) then the model is trained more quickly towards predicting the next token.  (the default is 1)

Context window size refers to the max number of tokens that can be sent to the model.  For LLMs this can be millions of tokens, for TinyGPT this is on the order of 16 (default) to 32.

Input tokenization using BPE can consolidate common bytes (eg. characters) into sequences that become a combined character token.  Experimentally, for TinyGPT, tokenization of length 1 (default) to many train about the same.

There are several configuration options for Neural Networks.  A few common configs are learning factor, hidden layers, and layer initialization strategies.  Through experimentation, a balance can be struck with these configs.



