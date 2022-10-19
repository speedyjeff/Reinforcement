using System;
using System.Collections.Generic;
using System.Text;

namespace Learning.Helpers
{
    public class OutcomeTracker
    {
        public OutcomeTracker(int size)
        {
            Index = 0;
            Outcomes = new byte[size];
        }

        public void AddWin() { Outcomes[Index] = 1; Index = (Index + 1) % Outcomes.Length; }

        public void AddLoss() { Outcomes[Index] = 0; Index = (Index + 1) % Outcomes.Length; }

        public float WinPercentage
        {
            get
            {
                var sum = 0f;
                for (int i = 0; i < Outcomes.Length; i++) sum += Outcomes[i];
                return sum / (float)Outcomes.Length;
            }
        }

        #region private
        private byte[] Outcomes;
        private int Index;
        #endregion
    }
}
