# Blocks Game with AI Computer Player

This project implements a blocks puzzle game with both human and AI computer players using neural networks for decision making.

## Features

### Players
- **Human Player**: Interactive player that prompts for piece selection and placement
- **Computer Player**: AI player using a neural network to evaluate and choose moves
- **Training Mode**: Evolutionary algorithm to train neural networks

### Neural Network Architecture
- **Input Layer**: 98 neurons
  - 64 for game state (8x8 grid)
  - 33 for available piece types
  - 1 for current score (normalized)
- **Hidden Layers**: 128 → 64 → 32 neurons
- **Output Layer**: 1 neuron (move evaluation score)

## Usage

### Running the Game
```bash
dotnet run --project blocks\blocks.csproj
```

Choose from three options:
1. **Human Player**: Play the game yourself
2. **Computer Player**: Watch the AI play automatically
3. **Train AI**: Use evolutionary algorithms to train a new neural network

### Game Controls (Human Player)
- Select pieces by number (0-2)
- Enter placement position as "row col" (e.g., "3 4")
- Grid coordinates are 0-7 for both rows and columns

## AI Training

The training uses an evolutionary algorithm:
- **Population Size**: 20-50 neural networks
- **Generations**: 50-100 evolution cycles
- **Selection**: Elite selection with top 10% survival
- **Mutation**: Random weight and bias adjustments
- **Fitness**: Average score across multiple games

### Training Parameters
- `populationSize`: Number of neural networks in each generation
- `generations`: Number of evolution cycles
- `gamesPerIndividual`: Number of games each network plays for evaluation

## Computer Player Strategy

The AI evaluates moves by:
1. **State Analysis**: Converting the game board to numerical inputs
2. **Move Generation**: Testing all valid piece placements
3. **Neural Evaluation**: Using the trained network to score moves
4. **Best Move Selection**: Choosing the highest-scored valid move

### Input Features
- **Grid State**: Binary representation of occupied cells
- **Available Pieces**: One-hot encoding of remaining pieces
- **Game Score**: Normalized current score

## Code Structure

### Core Classes
- `Computer`: AI player implementing `IPlayer` interface
- `Learning.NeuralNetwork`: Multi-layer perceptron implementation
- `ComputerTrainer`: Evolutionary training algorithms
- `Human`: Interactive human player
- `Blocks`: Game engine and rules

### Key Methods
- `Computer.ChooseMove()`: Main AI decision making
- `NeuralNetwork.Predict()`: Forward pass through network
- `ComputerTrainer.TrainEvolutionary()`: Evolution-based training
- `Blocks.CanPlacePiece()`: Move validation
- `Blocks.PlacePiece()`: Move execution

## File Output

Training saves the best neural network to `trained_network.txt` with weights and biases in a readable format.

## Future Enhancements

- Load pre-trained networks from files
- Different neural network architectures
- Reinforcement learning algorithms (Q-learning, PPO)
- Tournament play between different AI models
- Advanced training techniques (crossover, adaptive mutation)