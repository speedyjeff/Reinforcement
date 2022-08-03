using CartPole;
using Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CartPoleDriver
{
    enum QStatePolicy { Small = 0, SmallDetail = 1, F2 = 2, F3 = 3 };

    class QModel : IModel
    {
        public QModel(QStatePolicy policy)
        {
            Model = new Q<string, byte>(
                    -0.04, // reward
                    0.5, // learning rate
                    1.0 // discount factor
                    );
            AllActions = new List<byte>() { 0, 1 };

            Policy = policy;
        }

        public AlgorithmType Type => AlgorithmType.Q;

        public void StartIteration(float m, float M, float l, float minX, float maxX, float minTh, float maxTh)
        {
            // init
            Xmax = maxX;
            Xmin = minX;
            Thmin = minTh;
            Thmax = maxTh;

            // clear
            PreviousQState = "";
            PreviousQAction = 0;
        }

        public CartPoleAction MakeChoice(CartPoleState state)
        {
            // choose without applying learning (as we do not know the next state yet)
            var qstate = CartPoleStateToString(state);
            var qaction = Model.ChooseAction(
                qstate, 
                AllActions, 
                applyActionFunc: null);

            // retain for reward
            PreviousQState = qstate;
            PreviousQAction = qaction;

            // return
            return ByteToCartPoleAction(qaction);
        }

        public void EndChoice(CartPoleState state, bool success)
        {
            if (string.IsNullOrWhiteSpace(PreviousQState)) throw new Exception("must call MakeChoice first");

            var qstate = CartPoleStateToString(state);

            // apply learning now that we know the output state
            Model.ChooseAction(
                PreviousQState,
                new List<byte>() { PreviousQAction },
                (s, a) =>
                {
                    return qstate;
                });

            // provide feedback
            Model.ApplyTerminalCondition(PreviousQState, PreviousQAction, success ? 1.0 : -1.0);
        }

        public void EndIteration(int count)
        {
        }

        public int Stat()
        {
            return Model.ContextActionCount;
        }

        #region private
        private Q<string, byte> Model;
        private List<byte> AllActions;

        private string PreviousQState;
        private byte PreviousQAction;

        private float Xmin;
        private float Xmax;
        private float Thmin;
        private float Thmax;

        private QStatePolicy Policy;

        private string CartPoleStateToString(CartPoleState state)
        {
            if (Policy == QStatePolicy.Small)
            {
                // convert X into a 10 point scale
                var X = Math.Round(state.X + Math.Abs(Xmin) / (Math.Abs(Xmin) + Xmax), 1);

                // encode just the sign of dx
                var dX = state.dX < 0 ? -1f : 1f;

                // encode just the sign of Th
                var Th = state.Th < 0 ? -1f : 1f;

                // encode just the sign of dTh
                var dTh = state.dTh < 0 ? -1f : 1f;

                // reduce overall space by trimming the floating point
                return $"{X:f1}\t{dX:f0}\t{Th:f0}\t{dTh:f0}";
            }
            else if (Policy == QStatePolicy.SmallDetail)
            {
                // convert X into a 10 point scale
                var X = Math.Round(state.X + Math.Abs(Xmin) / (Math.Abs(Xmin) + Xmax), 1);

                // encode just the sign of dx
                var dX = state.dX < 0 ? -1f : 1f;

                // encode just the sign of Th
                var Th = state.Th < 0 ? -1f : 1f;

                // encode just the sign and magnitude of dTh into 10 points
                var dTh = state.dTh;
                if (dTh < (Thmin * 3)) dTh = Thmin * 3;
                else if (dTh > (Thmax * 3)) dTh = Thmax * 3;
                dTh = (float)Math.Round(dTh / (Math.Abs(Thmin) * 3 + Thmax * 3), 1);

                // reduce overall space by trimming the floating point
                return $"{X:f1}\t{dX:f0}\t{Th:f0}\t{dTh:f1}";
            }
            else if (Policy == QStatePolicy.F2)
            {
                return $"{state.X:f2}\t{state.dX:f2}\t{state.Th:f2}\t{state.dTh:f2}";
            }
            else if (Policy == QStatePolicy.F3)
            {
                return $"{state.X:f3}\t{state.dX:f3}\t{state.Th:f3}\t{state.dTh:f3}";
            }
            else
            {
                throw new Exception("unknown policy");
            }
        }

        private CartPoleAction ByteToCartPoleAction(byte qaction)
        {
            return (qaction == 0) ? CartPoleAction.Left : CartPoleAction.Right;
        }

        #endregion
    }
}
