using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Learning.LanguageModel
{
    public enum TokenizerDefaultVocab : int
    {
        None = 0,
        Alpha = 1,
        Numeric = 2,
        SpecialChars = 4,
        Whitespace = 8,
        Padding = 16
    }

    public enum TokenizerNormalization
    {
        None = 0,
        Lowercase = 1,
        Uppercase = 2
    }

    public struct TokenizerOptions
    {
        public int Iterations;
        public TokenizerNormalization Normalization;
        public TokenizerDefaultVocab DefaultVocab;
        public bool Verbose;
    }

    public class Tokenizer
    {
        public TokenContainer Tokens { get; private set; }
        public const string Padding = "\0";

        public static Tokenizer Create(string text, TokenizerOptions options)
        {
            if (string.IsNullOrEmpty(text)) throw new Exception("text cannot be null or empty");

            // parse the tokens
            var tokens = BytePairEncoding.ParseTokens(text, options);

            // check if default vocab is requested
            if (options.DefaultVocab != TokenizerDefaultVocab.None)
            {
                var defaultVocab = new HashSet<string>();
                if ((options.DefaultVocab & TokenizerDefaultVocab.Alpha) == TokenizerDefaultVocab.Alpha)
                {
                    if (options.Normalization == TokenizerNormalization.Lowercase || options.Normalization == TokenizerNormalization.None)
                    {
                        defaultVocab.UnionWith(new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" });
                    }
                    if (options.Normalization == TokenizerNormalization.Uppercase || options.Normalization == TokenizerNormalization.None)
                    {
                        defaultVocab.UnionWith(new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" });
                    }
                }
                if ((options.DefaultVocab & TokenizerDefaultVocab.Numeric) == TokenizerDefaultVocab.Numeric)
                {
                    defaultVocab.UnionWith(new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" });
                }
                if ((options.DefaultVocab & TokenizerDefaultVocab.SpecialChars) == TokenizerDefaultVocab.SpecialChars)
                {
                    defaultVocab.UnionWith(new string[] { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "_", "+", "=", "{", "}", "[", "]", ":", ";", "\"", "'", "<", ">", ",", ".", "?", "/", "\\", "|", "`", "~" });
                }
                if ((options.DefaultVocab & TokenizerDefaultVocab.Whitespace) == TokenizerDefaultVocab.Whitespace)
                {
                    defaultVocab.UnionWith(new string[] { " ", "\t", "\n", "\r" });
                }
                if ((options.DefaultVocab & TokenizerDefaultVocab.Padding) == TokenizerDefaultVocab.Padding)
                {
                    defaultVocab.UnionWith(new string[] { Padding });
                }
                tokens.UnionWith(defaultVocab);
            }

            // return the BPE tokens
            var tokenizer = new Tokenizer();
            tokenizer.Tokens = new TokenContainer(tokens.ToArray());
            tokenizer.Options = options;
            return tokenizer;
        }

        public void Save(string path)
        {
            // save tokens in a json format
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"options\": {");
            json.AppendLine($"    \"iterations\": {Options.Iterations},");
            json.AppendLine($"    \"normalization\": \"{Options.Normalization}\"");
            json.AppendLine("  },");
            json.AppendLine("  \"tokens\": [");
            foreach (var kvp in Tokens.All())
            {
                json.AppendLine($"    {{ \"token\": {kvp.Key}, \"text\": \"{kvp.Value}\" }},");
            }
            json.AppendLine("  ]");
            json.AppendLine("}");
            System.IO.File.WriteAllText(path, json.ToString());
        }

        public static Tokenizer Load(string path)
        {
            // load tokens from a json format
            var json = System.IO.File.ReadAllText(path);
            var tokenizer = new Tokenizer();
            tokenizer.Options = new TokenizerOptions();
            var vocab = new List<string>();
            // todo - parse the json
            tokenizer.Tokens = new TokenContainer(vocab.ToArray());
            return tokenizer;
        }

        public List<int> Encode(string text)
        {
            var tokens = new List<int>();

            // normalize the text
            switch (Options.Normalization)
            {
                case TokenizerNormalization.Lowercase:
                    text = text.ToLower();
                    break;
                case TokenizerNormalization.Uppercase:
                    text = text.ToUpper();
                    break;
                default:
                    break;
            }

            // use Tokens.MaxTextLength to check for the largest token first
            var chars = text.ToCharArray();

            // encode the text into tokens
            for (int i = 0; i < chars.Length;)
            {
                var foundToken = false;
                for (int size = Tokens.MaxDepth; size > 0; size--)
                {
                    if (i + size > chars.Length) continue;
                    var chunk = new string(chars, i, size);
                    var token = Tokens[chunk];
                    if (token >= 0)
                    {
                        // found a token
                        foundToken = true;
                        tokens.Add(token);
                        i += size;
                        break;
                    }
                }

                if (!foundToken)
                {
                    throw new Exception($"failed to find token : {chars[i]}");
                }
            }

            return tokens;
        }

        public string Decode(List<int> tokens)
        {
            var text = new StringBuilder();
            foreach (var token in tokens)
            {
                var chunk = Decode(token);
                text.Append(chunk);
            }
            return text.ToString();
        }

        public string Decode(int token)
        {
            var chunk = Tokens[token];
            if (chunk == null) throw new Exception($"failed to find token : {token}");
            return chunk;
        }

        #region private
        private TokenizerOptions Options;
        #endregion
    }
}
