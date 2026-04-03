# Blocks AI Results and Reproduction Guide

This document records benchmark and training results for the current Blocks AI implementations and shows how to reproduce them from a clean checkout.

## AI players covered

The Blocks project currently contains three distinct computer player implementations:

1. **`HybridComputerPlayer`** (`Blocks\blocks\HybridComputerPlayer.cs`)
   - the main high-performing hybrid agent
   - combines a neural policy prior with heuristic rescoring and shallow lookahead

2. **`ComputerClassicRL`** (`Blocks\blocks\ComputerClassicRL.cs`)
   - the pure neural / classic reinforcement learning path
   - uses only network outputs at runtime, without the hybrid search layer

3. **`MonteCarloPolicyComputerPlayer`** (`Blocks\blocks\MonteCarloPolicyComputerPlayer.cs`)
   - an older experimental Monte Carlo policy-gradient agent
   - kept for comparison and research

## Test environment and methodology

These results were gathered from the repository root on Windows using a Release build.

- **Build command**

```
dotnet build Blocks\blocks\blocks.csproj -c Release
```

- **Fitness metric**
  - `AvgFitness` = average final score across the evaluation sample
  - `MaxFitness` = best score in the evaluation sample
  - `AvgPiecesPlayed` = average number of pieces placed before game over
  - `MaxPiecesPlayed` = best survival length in the sample

- **Important note**
  - Blocks uses random piece generation, so exact numbers will vary slightly from run to run.
  - Comparisons are more reliable when averaged over **20+ games**.

## Current benchmark snapshot

These are the most recent reproducible results collected from the current repo state:

| Player | Run type | Sample | AvgFitness | MaxFitness | AvgPiecesPlayed | MaxPiecesPlayed | Notes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `HybridComputerPlayer` | Fresh instance | 10 games | **695.90** | 1949 | 48.00 | 134 | Strongest current player even without a saved model file |
| `ComputerClassicRL` | Fresh instance | 20 games | **66.40** | 186 | 11.00 | 17 | Pure neural RL baseline with current code |
| `ComputerClassicRL` | Saved checkpoint (`trained_network_classic_rl.txt`) | 20 games | **93.40** | 254 | 13.00 | 20 | Better than fresh RL, still much weaker than hybrid |
| `MonteCarloPolicyComputerPlayer` | Fresh instance | 20 games | **53.90** | 184 | 11.00 | 20 | Older experimental agent |
| `MonteCarloPolicyComputerPlayer` | After 500 self-play training episodes | 20 games | **66.65** | 199 | 11.45 | 18 | Modest improvement after short training |

## Historical milestone results

These numbers are useful for understanding how the project evolved during development:

| Milestone | AvgFitness | MaxFitness | AvgPiecesPlayed | MaxPiecesPlayed | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| Legacy saved model baseline (before the hybrid upgrade) | **58.12** | 216 | 10.00 | 20 | Earlier baseline used to measure improvement |
| Hybrid player after the search / heuristic upgrade | **1242.50** | 4944 | n/a | n/a | Development benchmark that dramatically exceeded the original target |
| Early classic RL smoke test after short training | **96.20** | 188 | 14.00 | 20 | Short-run result from the initial pure RL implementation |
| Best watchable classic RL checkpoint from a 5-attempt sweep | **89.67** | 281 | 12.00 | 24 | Checkpoint later saved to `trained_network_classic_rl.txt` |

## How to watch each player step by step

From the repository root:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release
```

Then choose one of these menu options:

| Option | Mode | What you will see |
| --- | --- | --- |
| `2` | Hybrid computer player | The strongest gameplay; usually the best option if you want to watch smart-looking placements |
| `3` | Trained hybrid player | Loads `trained_network.txt` if present; if you want this mode, generate the file first with option `4` |
| `6` | Fresh classic RL player | Pure neural player without search; useful for observing learned behavior |
| `7` | Trained classic RL player | Loads `trained_network_classic_rl.txt` and pauses after each move |

All computer modes stop after each placement and wait for a key press so you can inspect the board.

## Reproducing the benchmark numbers

The Blocks app now supports direct command-line modes, so you can reproduce these results without using an interactive shell harness.

### 1. Build once

Run this from the repository root:

```
dotnet build Blocks\blocks\blocks.csproj -c Release
```

You can also print the available CLI modes with:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --help
```

### 2. Hybrid player benchmark

Fresh hybrid evaluation:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-hybrid --games 10
```

Evaluate a saved hybrid network:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-hybrid --games 20 --network trained_network.txt
```

### 3. Classic RL player benchmark

Fresh classic RL evaluation:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-classic-rl --games 20
```

Saved classic RL checkpoint evaluation:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-classic-rl --games 20 --network trained_network_classic_rl.txt
```

### 4. Train and save a classic RL checkpoint

Train the neural-only classic RL player and save it:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode train-classic-rl --games 400 --report-interval 200 --save trained_network_classic_rl.txt
```

Then watch the saved model step by step:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode watch-classic-rl-trained --network trained_network_classic_rl.txt
```

### 5. Train and run the hybrid player

Train and save the hybrid model:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode train-hybrid --games 5000 --report-interval 100 --save trained_network.txt
```

Then watch it:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode watch-hybrid-trained --network trained_network.txt
```

### 6. Experimental Monte Carlo player benchmark

Fresh Monte Carlo evaluation:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-monte-carlo --games 20
```

Monte Carlo evaluation after 500 training episodes:

```
dotnet run --project Blocks\blocks\blocks.csproj -c Release -- --mode eval-monte-carlo --games 20 --train-games 500
```

## Practical interpretation

- **Best gameplay quality:** `HybridComputerPlayer`
- **Pure neural experiment:** `ComputerClassicRL`
- **Legacy research path:** `MonteCarloPolicyComputerPlayer`

At the moment, the hybrid player is the only one that consistently looks strategically strong when watched move by move. The pure RL players are useful for experimentation and learning research, but they still lag well behind the hybrid path in long-term piece placement quality.
