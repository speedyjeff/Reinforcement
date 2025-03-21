using System;
using System.Collections.Generic;

namespace Learning.LanguageModel
{
    // quick lookup for byte pair encoding tokens
    public class TokenContainer
    {
        public TokenContainer(string[] vocab)
        {
            if (vocab == null || vocab.Length == 0) throw new ArgumentException("vocab is null or empty");

            // setup quick lookup
            RootByVocab = new BytePairToken();
            RootByToken = new Dictionary<int, string>();

            // add all the vocab and assign a token (order matters)
            for(var i=0; i<vocab.Length; i++) Add(vocab[i], i);
        }

        public int Count { get { return RootByToken.Count; } }
        public int MaxDepth { get; private set; }

        public int this[string vocab]
        {
            get
            {
                return Lookup(vocab);
            }
        }

        public string this[int token]
        {
            get
            {
                return Lookup(token);
            }
        }

        public IEnumerable<KeyValuePair<int,string>> All()
        {
            // todo - return a copy?
            foreach (var kvp in RootByToken) yield return kvp;
        }

        #region private
        class BytePairToken
        {
            public char Char;
            public int Token;
            public Dictionary<char, BytePairToken> Children;

            public BytePairToken()
            {
                Char = '\0';
                Token = -1;
                Children = new Dictionary<char, BytePairToken>();
            }
        }
        private BytePairToken RootByVocab;
        private Dictionary<int, string> RootByToken;

        private void Add(string vocab, int token)
        {
            // add for quick lookup by text
            var node = RootByVocab;
            foreach (var c in vocab.ToCharArray())
            {
                // insert this part of the token into the tree
                if (!node.Children.TryGetValue(c, out BytePairToken child))
                {
                    child = new BytePairToken() { Char = c };
                    node.Children.Add(c, child);
                }
                // keep walking the tree
                node = child;
            }
            // set the id
            node.Token = token;

            // add for quick lookup by token
            if (RootByToken.ContainsKey(token)) throw new ArgumentException("token already exists");
            RootByToken[token] = vocab;

            // keep track of maxes
            MaxDepth = Math.Max(MaxDepth, vocab.Length);
        }

        private int Lookup(string text)
        {
            var node = RootByVocab;
            foreach (var c in text.ToCharArray())
            {
                if (!node.Children.TryGetValue(c, out BytePairToken child))
                {
                    // not found
                    return -1;
                }
                node = child;
            }
            return node.Token;
        }

        private string Lookup(int token)
        {
            if (!RootByToken.TryGetValue(token, out string vocab)) return null;
            return vocab;
        }
        #endregion
    }
}
