using System;
using System.Text;
using Learning.LanguageModel;

namespace Learning.Tests
{
    internal class LanguageModelTokenizer
    {
        // tests for the LanguageModelTokenizer class
        //   Create
        //     string tests - null, "", all the same, even, odd
        //     options: default vocab - none, alpha + None, alpha + Lower, alpha + Upper, Numeric, SpecialChars, WhiteSpace, Padding
        //     normalization - none, lower, upper
        //     iterations
        //     encode
        //     decode

        public static void TestCreateText()
        {
            // default options
            var options = new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.None
            };
            Tokenizer tokenizer;

            // test null
            try
            {
                tokenizer = Tokenizer.Create(null, options);
                throw new Exception("expected exception");
            }
            catch (Exception)
            {
            }

            // test empty string
            try
            { 
            tokenizer = Tokenizer.Create("", options);
            throw new Exception("expected exception");
            }
            catch (Exception)
            {
            }

            // test all the same
            tokenizer = Tokenizer.Create("aaaaaa", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 1) throw new Exception("tokenizer count is not 1");
            if (tokenizer.Tokens[0] != "a") throw new Exception("tokenizer token is not 'a'");

            // test even length
            tokenizer = Tokenizer.Create("abcdef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 6) throw new Exception("tokenizer count is not 6");

            // test odd length
            tokenizer = Tokenizer.Create("abcde", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 5) throw new Exception("tokenizer count is not 5");
        }

        public static void TestCreateOptions()
        {
            // default options
            var options = new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.None
            };
            Tokenizer tokenizer;

            // test default vocab - none
            tokenizer = Tokenizer.Create("abcdef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 6) throw new Exception("tokenizer count is not 6");

            // test default vocab - alpha + None
            options.DefaultVocab = TokenizerDefaultVocab.Alpha;
            tokenizer = Tokenizer.Create("abcdef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != (26*2)) throw new Exception("tokenizer count is not (26*2))");
            foreach(var c in new string[] {
                "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test default vocab - alpha + Lower
            options.DefaultVocab = TokenizerDefaultVocab.Alpha;
            options.Normalization = TokenizerNormalization.Lowercase;
            tokenizer = Tokenizer.Create("abCdef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 26) throw new Exception("tokenizer count is not 26");
            foreach (var c in new string[] {
                "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test default vocab - alpha + Upper
            options.DefaultVocab = TokenizerDefaultVocab.Alpha;
            options.Normalization = TokenizerNormalization.Uppercase;
            tokenizer = Tokenizer.Create("abcdef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 26) throw new Exception("tokenizer count is not 26");
            foreach (var c in new string[] {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test default vocab - Numeric
            options.DefaultVocab = TokenizerDefaultVocab.Numeric;
            options.Normalization = TokenizerNormalization.None;
            tokenizer = Tokenizer.Create("a", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 10 + 1) throw new Exception("tokenizer count is not 10+1");
            foreach (var c in new string[] {
                "a", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test default vocab - SpecialChars
            options.DefaultVocab = TokenizerDefaultVocab.SpecialChars;
            tokenizer = Tokenizer.Create("abc", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");

            // test default vocab - WhiteSpace
            options.DefaultVocab = TokenizerDefaultVocab.Whitespace;
            tokenizer = Tokenizer.Create("abc", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            foreach (var c in new string[] {
                " ", "\t", "\n", "\r"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test default vocab - Padding
            options.DefaultVocab = TokenizerDefaultVocab.Padding;
            tokenizer = Tokenizer.Create("abc", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens[Tokenizer.Padding] < 0) throw new Exception("did not find padding");
        }

        public static void TestNormalization()
        {
            // default options
            var options = new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.None
            };
            Tokenizer tokenizer;

            // test none
            tokenizer = Tokenizer.Create("AbCDef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 6) throw new Exception("tokenizer count is not 6");
            foreach (var c in new string[] {
                "A", "b", "C", "D", "e", "f"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test lower
            options.Normalization = TokenizerNormalization.Lowercase;
            tokenizer = Tokenizer.Create("AbCDef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 6) throw new Exception("tokenizer count is not 6");
            foreach (var c in new string[] {
                "a", "b", "c", "d", "e", "f"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }

            // test upper
            options.Normalization = TokenizerNormalization.Uppercase;
            tokenizer = Tokenizer.Create("AbCDef", options);
            if (tokenizer == null) throw new Exception("tokenizer is null");
            if (tokenizer.Tokens.Count != 6) throw new Exception("tokenizer count is not 6");
            foreach (var c in new string[] {
                "A", "B", "C", "D", "E", "F"
            })
            {
                if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
            }
        }

        public static void TestIterations()
        {
            // default options
            var options = new TokenizerOptions()
            {
                Iterations = 0,
                Normalization = TokenizerNormalization.None,
                DefaultVocab = TokenizerDefaultVocab.None
            };
            Tokenizer tokenizer;

            // test iterations
            var input = "aaabdaaabac";
            var output = new string[][]
            {
                new string[] {"a", "b", "c", "d" },
                new string[] {"aa", "a", "b", "c", "d" },
                new string[] {"aa", "ab", "a", "c", "d" },
                new string[] {"aaab", "a", "c", "d" },
                new string[0]
            };
            // the algorithm should have no work to do on iteration 4
            output[4] = output[3];
            for (int i = 0; i < output.Length; i++)
            {
                options.Iterations = i;
                tokenizer = Tokenizer.Create(input, options);
                if (tokenizer == null) throw new Exception("tokenizer is null");
                if (tokenizer.Tokens.Count != output[i].Length) throw new Exception($"tokenizer count is not {output[i].Length}");
                // check the tokens
                foreach (var c in output[i])
                {
                    if (tokenizer.Tokens[c] < 0) throw new Exception($"tokenizer does not contain {c}");
                }
            }
        }

        public static void TestRoundTrip(int seed = 0)
        {
            // any text should be encodable and decodable (to the exact same text)
            //  1. randomly generate text of a certain length
            //  2. encode the text
            //  3. decode the text
            //  4. check that the decoded text is the same as the original text

            for (var iterations = 0; iterations < 10; iterations++)
            {
                // default options
                var options = new TokenizerOptions()
                {
                    Iterations = iterations,
                    Normalization = TokenizerNormalization.None,
                    DefaultVocab = TokenizerDefaultVocab.None
                };
                var random = seed > 0 ? new Random(seed) : new Random();

                // build a series of random chunks and then paste them together into a string
                // trying to simulate a real world scenario where the text has repetition
                var length = iterations + 1;
                var chunks = new string[100 / length];
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunk = new char[length];
                    for (int j = 0; j < chunk.Length; j++) chunk[j] = (char)random.Next(32, 127);  // space to ~
                    chunks[i] = new string(chunk);
                }

                // paste the chunks together
                var sb = new StringBuilder();
                for (int i = 0; i < 1000; i++) sb.Append(chunks[random.Next() % chunks.Length]);
                var input = sb.ToString();

                // create the tokenizer
                var tokenizer = Tokenizer.Create(input, options);
                if (tokenizer == null) throw new Exception("tokenizer is null");
                if (tokenizer.Tokens.Count == 0) throw new Exception("tokenizer count is 0");

                // encode the text
                var encoded = tokenizer.Encode(input);
                if (encoded == null) throw new Exception("encoded is null");
                if (encoded.Count == 0) throw new Exception("encoded length is 0");

                // decode the text
                var decoded = tokenizer.Decode(encoded);
                if (decoded == null) throw new Exception("decoded is null");
                if (decoded.Length == 0) throw new Exception("decoded length is 0");
                if (!decoded.Equals(input)) throw new Exception("decoded text is not the same as the original text");
            }
        }
    }
}
