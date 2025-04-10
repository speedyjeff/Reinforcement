using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// todo
//  Epsilon-Greedy (inject some random choices to avoid over fitting)
//  Double-Q Learning (use a shadow Q table to avoid bias while learning)

namespace Learning
{
    // T is the context
    // K is the action
    public class Q<T,K>
    {
        // reward     : cost of taking an action (default -0.04)
        // learning   : 0 [no learning/rely on prior knowledge] ... [1] only most recent info (default 0.5)
        // discount   : 0 [short term rewards] ... 1 [long term rewards] (default 1)  (eg. gamma)
        public Q(double reward, double learning, double discount)
        {
            Rand = new Random();
            Reward = reward;
            Learning = learning;
            Discount = discount;

            // start the learning matrix as empty
            Matrix = new Dictionary<T, Dictionary<K,double>>();
        }

        public void ApplyTerminalCondition(T context, K action, double value)
        {
            // Q(s,a) += Learning * (value - Q(s,a))
            if (!Matrix.TryGetValue(context, out var qvalues))
            {
                qvalues = new Dictionary<K, double>();
                Matrix.Add(context, qvalues);
            }
            if (!qvalues.ContainsKey(action)) qvalues.Add(action, 0d);
            qvalues[action] += Learning * (value - qvalues[action]);
        }

        public K ChooseAction(T context, List<K> actions, [AllowNull]Func<T, K, T> applyActionFunc)
        {
            // https://en.wikipedia.org/wiki/Q-learning
            // Q(s1,a) += (1 - learning) Q(s,a) + learning (reward + discount (max (s2)))

            // establish initial location, if it does not exist
            if (!Matrix.TryGetValue(context, out var curQvalues))
            {
                curQvalues = new Dictionary<K, double>();
                Matrix.Add(context, curQvalues);
            }

            // choose the highest value action
            var possible_a = new List<K>();
            var q_s_a = double.MinValue;
            foreach (var act in actions)
            {
                // ensure this direction is established
                if (!curQvalues.TryGetValue(act, out var qvalue)) curQvalues.Add(act, 0d);

                if (qvalue > q_s_a)
                {
                    q_s_a = qvalue;
                    possible_a.Clear();
                }

                if (qvalue == q_s_a) possible_a.Add(act);
            }

            if (possible_a.Count < 1) throw new Exception("invalid number of possible_a");

            // choose one of the options (if multiple)
            var a = possible_a.Count == 1 ? possible_a[0] : possible_a[Rand.Next() % possible_a.Count];

            if (applyActionFunc != null)
            {
                // get destination location
                T s2 = applyActionFunc(context, a);

                // max(s2)
                if (!Matrix.TryGetValue(s2, out var dstQvalues))
                {
                    dstQvalues = new Dictionary<K, double>();
                    Matrix.Add(s2, dstQvalues);
                }
                var max_s2 = dstQvalues.Count == 0 ? 0 : dstQvalues.Max(d => d.Value);

                // update
                Matrix[context][a] += (Learning * (Reward + Discount * max_s2 - q_s_a));
            }

            return a;
        }

        public bool TryGetQValue(T context, K action, out double qvalue)
        {
            qvalue = 0d;
            if (!Matrix.TryGetValue(context, out var actions)) return false;
            if (!actions.TryGetValue(action, out qvalue)) return false;
            return true;
        }
        public bool TryGetActions(T context, out Dictionary<K, double> actions)
        {
            if (!Matrix.TryGetValue(context, out actions)) return false;
            return true;
        }

        public int ContextCount { get { return Matrix.Keys.Count; } }
        public int ContextActionCount
        {
            get
            {
                var actionCount = 0;
                foreach (var okvp in Matrix) actionCount += okvp.Value.Count;
                return actionCount;
            }
        }
        public double Reward { get; set; }
        public double Learning { get; set; }
        public double Discount { get; set; }

        public Dictionary<T, Dictionary<K, double>> Matrix { get; set; }

        #region private
        private readonly Random Rand;
        #endregion
    }
}