# Blocks

`Blocks` is an 8x8 tile-placement puzzle game with several playable and trainable AI modes. It includes:

- a human-playable console version
- a high-performing **hybrid** computer player
- an experimental **classic RL / neural-only** computer player
- training and puzzle-test modes for both research and debugging

For measured benchmark data and step-by-step reproduction commands, see [RESULTS.md](RESULTS.md).

## Game rules

The core engine lives in `Blocks\blocks.engine\Blocks.cs`.

| Rule | Details |
| --- | --- |
| Board size | `8 x 8` |
| Pieces shown at once | `3` |
| Turn flow | Choose one of the available pieces and place it on the board |
| Piece orientation | Fixed by the piece type; there is no runtime rotate/flip UI |
| Refill | When all 3 pieces are used, a new set of 3 is generated |
| Game over | The game ends when none of the currently available pieces can be placed anywhere |
| Objective | Survive as long as possible and maximize score |

### Scoring

Scoring is handled in `Blocks.PlacePiece(...)` and works like this:

1. You earn **1 point per cell placed**.
2. Any completed rows and columns are cleared **atomically** after the placement.
3. You earn a **10-point bonus per cleared row or column**.
4. Consecutive clear turns increase a **chain multiplier**:

```text
playScore = (clearedCells + lineBonus) * (previousClearCount + 1)
```

So the game rewards both:

- immediate clears
- setting up future clears
- maintaining long clear streaks without breaking tempo

That is why strong agents try to preserve board flexibility instead of just grabbing the first legal move.

## Running the game

From the repository root:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release
```

All computer-controlled play modes pause after each move with **"Press any key to continue..."**, so you can watch the game step by step.

## Menu options

When you launch the app, these are the available modes:

| Option | Mode | What it does |
| --- | --- | --- |
| 1 | Human Player | Play manually in the console |
| 2 | Computer Player (AI hybrid) | Run a fresh hybrid AI instance |
| 3 | Computer Player (hybrid based on past training) | Load `trained_network.txt` and watch the trained hybrid player |
| 4 | Train AI (hybrid trainer) | Train the hybrid player and save it to `trained_network.txt` |
| 5 | Test Scenarios (verify learning on puzzles) | Run fixed puzzle setups for debugging and learning verification |
| 6 | Computer Player (classic RL, neural only) | Run a fresh neural-only RL agent |
| 7 | Computer Player (classic RL from training) | Load `trained_network_classic_rl.txt` and watch the trained neural-only RL agent |
| 8 | Train AI (classic RL, neural only) | Train the neural-only RL agent and save it to `trained_network_classic_rl.txt` |

## Computer modes in detail

The project intentionally contains **two different AI philosophies**.

### 1. Hybrid computer player (`HybridComputerPlayer` in `HybridComputerPlayer.cs`)

This is the **strongest** player in the project and the best choice if you want the AI to look smart while playing.

#### What makes it "hybrid"

It does **not** rely on the neural network alone. Instead, it combines:

- a neural-network policy prior
- valid move generation across the full board
- short-horizon search / lookahead
- board-quality heuristics
- dead-end avoidance and future mobility estimation

#### Inputs and outputs

- Input size: **139**
  - 64 board cells
  - 75 piece-shape cells (`3 pieces x 5 x 5`)
- Output size: **192**
  - `3 piece slots x 64 board positions`

The neural network suggests promising moves, but the final choice is refined by explicit planning code.

#### Runtime behavior

The hybrid player evaluates candidate moves using signals such as:

- immediate score gain
- line clear bonus potential
- whether the move traps the board
- whether it preserves room for large future pieces
- how fragmented or "healthy" the resulting board shape is

This is why it usually places pieces in ways that look intentional and strategic.

#### Related menu options

- **2** = fresh hybrid player
- **3** = trained hybrid player from disk
- **4** = train and save the hybrid model

### 2. Classic RL / neural-only player (`ComputerClassicRL.cs`)

This is the **experimental pure neural** agent.

#### Design goal

This player was added to answer the question: **"Can the model learn to play Blocks using only the network and reinforcement learning, without a search/planner layer at runtime?"**

#### Important constraint

At runtime, this mode uses:

- the board state
- the available pieces
- the learned network outputs

It does **not** use:

- handcrafted move search
- lookahead tree search
- the hybrid board-evaluation heuristics

That makes it a cleaner RL experiment, but also much harder to train well.

#### Current training approach

The RL-only trainer currently uses:

- policy-gradient style episode learning
- discounted returns
- successful-episode replay
- curriculum-style training progression
- exploration via epsilon-greedy and temperature-based sampling

#### What to expect

This mode is useful if you want to:

- study learned behavior
- experiment with pure RL improvements
- compare end-to-end learning against the stronger hybrid approach

It is **not yet as strong or as consistently intelligent-looking as the hybrid player**. If you watch it play, you will often see weaker piece ordering and poorer long-term space management.

#### Related menu options

- **6** = fresh classic RL player
- **7** = trained classic RL player from disk
- **8** = train and save the classic RL model

### 3. Experimental Monte Carlo policy player (`MonteCarloPolicyComputerPlayer` in `MonteCarloPolicyComputerPlayer.cs`)

This older experimental player learns position preferences from whole-game outcomes using a Monte Carlo style update.
It is kept in the repository for experimentation and comparison, but it is not the main AI path exposed in the menu.

## Scenario test mode

Option **5** runs puzzle-like test scenarios from `BlockGenerator.cs`.

These are useful for:

- verifying that learning code can solve small structured cases
- testing whether a model understands simple clears
- debugging regressions in training behavior

The scenario system supports:

- predefined fixed piece sequences
- optional board pre-fills
- puzzle mode that ends when the scenario pieces are exhausted

## Saved model files

Training writes model files to the repository so they can be loaded later:

| File | Purpose |
| --- | --- |
| `trained_network.txt` | Saved network for the hybrid player |
| `trained_network_classic_rl.txt` | Saved network for the neural-only classic RL player |

If you want to watch a trained model, the usual choices are:

- **Option 3** for the strongest hybrid AI
- **Option 7** for the neural-only RL experiment

## Human controls

In human mode:

- choose a piece by index (`0`, `1`, or `2`)
- enter the placement as `row col`
- coordinates are zero-based (`0` through `7`)

Example:

```text
3 4
```

## Key source files

| File | Responsibility |
| --- | --- |
| `Blocks\blocks.engine\Blocks.cs` | Core game rules, placement checks, scoring, line clearing |
| `Blocks\blocks.engine\BlockGenerator.cs` | Random piece generation and fixed test scenarios |
| `Blocks\blocks\Program.cs` | Console menu and main game loop |
| `Blocks\blocks\HybridComputerPlayer.cs` | `HybridComputerPlayer`, the main hybrid AI player |
| `Blocks\blocks\HybridComputerTrainer.cs` | `HybridComputerTrainer`, training/evaluation for the hybrid player |
| `Blocks\blocks\MonteCarloPolicyComputerPlayer.cs` | `MonteCarloPolicyComputerPlayer`, an older experimental Monte Carlo policy player |
| `Blocks\blocks\ComputerClassicRL.cs` | Neural-only RL player |
| `Blocks\blocks\ComputerClassicRLTrainer.cs` | Training/evaluation for the neural-only RL player |

## Practical guidance

- If your goal is **best gameplay quality**, use the **hybrid** player.
- If your goal is **pure learning research**, use the **classic RL** player.
- If your goal is **watching moves step by step**, run either watch mode and press a key after each move.

At the moment, the hybrid player is the one that most clearly demonstrates strong placement strategy, while the classic RL player is better viewed as an active experiment.
