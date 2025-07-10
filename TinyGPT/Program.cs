using System;
using System.Diagnostics;
using Learning;
using Learning.LanguageModel;

namespace TinyGPT
{
    class Program
    {
        public static void Main(string[] args)
        {
            var options = Options.Parse(args);

            if (options.Help)
            {
                Options.ShowHelp();
                return;
            }

            // alphabetize
            if (options.Dataset == DatasetType.Alphabet)
            {
                var alpha = new Alphabetize(new AlphabetizeOptions()
                {
                    WeightInitialization = options.WeightInitialization,
                    BiasInitialization = options.BiasInitialization,
                    Vocabulary = options.Vocabulary,
                    SequenceLength = options.SequenceLength,
                    HiddenLayers = options.LayerMultipliers,
                    LearningFactor = options.LearningFactor
                });

                var successfulRunCount = 0;
                var timer = new Stopwatch();
                Console.WriteLine("Iteration\tCorrect/Total");
                timer.Start();
                for (int i = 0; i < options.TrainingIterations; i++)
                {
                    // train
                    if (options.ConcurrentInstances <= 1) alpha.Train(options.IterationsPerTraining);
                    else alpha.TrainParallel(options.IterationsPerTraining, options.Inferences, options.ConcurrentInstances);

                    var correct = alpha.Inference(options.Inferences, verbose: false);
                    // show periodic progress
                    if (options.Verbose || i == options.TrainingIterations - 1 || i % (options.TrainingIterations / 1000) == 0)
                    {
                        Console.WriteLine($"{i}\t{correct}/{options.Inferences}");
                    }

                    // track success
                    var percentage = (float)correct/(float)options.Inferences;
                    if (Math.Abs(percentage - options.SuccessCriteria) < 0.0001f) successfulRunCount++;
                    else successfulRunCount = 0;
                    if (successfulRunCount == options.StopAfterNumSuccesses)
                    {
                        Console.WriteLine(i);
                        break;
                    }
                }
                timer.Stop();
                Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
            }

            // shakespeare
            else if (options.Dataset == DatasetType.Shakespeare)
            {
                var shakespeare = new Shakespeare(new ShakespeareOptions()
                {
                    WeightInitialization = options.WeightInitialization,
                    BiasInitialization = options.BiasInitialization,
                    PercentOfText = options.PercentOfText,
                    SequenceLength = options.SequenceLength,
                    InputFile = options.ShakespeareFilepath,
                    TokenizerIterations = options.TokenizerIterations,
                    NormalizeTokens = (TokenizerNormalization)options.NormalizeTokens,
                    MinTokenCount = options.MinTrainSequenceLength,
                    StaticStartingPoint = false,
                    HiddenLayers = options.LayerMultipliers,
                    LearningFactor = options.LearningFactor
                });

                // train and infer
                var successfulRunCount = 0;
                for (int i = 0; i < options.TrainingIterations; i++)
                {
                    // train
                    if (options.ConcurrentInstances <= 1) shakespeare.Train(options.IterationsPerTraining);
                    else shakespeare.TrainParallel(options.IterationsPerTraining, options.Inferences, options.ConcurrentInstances);

                    // inference
                    var percentCorrect = shakespeare.Inference(options.Inferences, verbose: false);
                    if (options.Verbose || i == options.TrainingIterations - 1 || i % (options.TrainingIterations / 1000) == 0)
                    {
                        Console.WriteLine($"{i}\t{percentCorrect * 100f}%");

                        // display an inferred paragraph
                        shakespeare.InferStream(length: 1000);
                    }

                    // track success
                    if (percentCorrect > options.SuccessCriteria || Math.Abs(percentCorrect - options.SuccessCriteria) < 0.01f) successfulRunCount++;
                    else successfulRunCount = 0;

                    // stop if we have enough successes
                    if (successfulRunCount == options.StopAfterNumSuccesses)
                    {
                        Console.WriteLine(i);
                        break;
                    }
                }

                // dump stats
                Console.WriteLine("Stats:");
                for (int i = 0; i < shakespeare.Debug_InferenceStats.InferenceCount.Length; i++)
                {
                    Console.WriteLine($"{i}\t{shakespeare.Debug_InferenceStats.InferenceCount[i]}\t{shakespeare.Debug_InferenceStats.InferenceCorrect[i]}\t{((float)shakespeare.Debug_InferenceStats.InferenceCorrect[i] * 100f)/(float)shakespeare.Debug_InferenceStats.InferenceCount[i]}%");
                }
                Console.WriteLine("Top N Stats:");
                for (int i = 0; i < shakespeare.Debug_InferenceStats.TopNCorrect.Length; i++)
                {
                    Console.WriteLine($"{i}\t{shakespeare.Debug_InferenceStats.TopNCorrect[i]}");
                }

                // todo - save the network and tokenizer to disk
            }
            else throw new Exception("nyi");
        }
    }
}
