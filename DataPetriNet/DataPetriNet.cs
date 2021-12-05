using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using DataPetriNet.SoundnessVerification;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNet
{
    public class DataPetriNet // TODO: Add Randomness
    {
        private Random randomGenerator;
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet()
        {
            randomGenerator = new Random();
            Places = new List<Place>();
            Transitions = new List<Transition>();
        }

        public bool MakeStep()
        {
            var canMakeStep = false; // TODO: Find a more quicker way to get random elements?
            foreach (var transition in Transitions.OrderBy(x => randomGenerator.Next()))
            {
                canMakeStep = transition.TryFire(Variables);
                if (canMakeStep)
                {
                    return canMakeStep;
                }
            }

            return canMakeStep;
        }

        public ConstraintState GenerateInitialConstraintState()
        {
            var state = new ConstraintState();
            Places.ForEach(x => state.PlaceTokens.Add(x, x.Tokens));

            AddTypedExpressions<string>(state.Constraints, DomainType.String);
            AddTypedExpressions<bool>(state.Constraints, DomainType.Boolean);
            AddTypedExpressions<long>(state.Constraints, DomainType.Integer);
            AddTypedExpressions<double>(state.Constraints, DomainType.Real);
            state.Constraints[0].LogicalConnective = LogicalConnective.Empty;

            return state;
        }

        private void AddTypedExpressions<T>(List<IConstraintExpression> constraintExpressions, DomainType domain)
            where T : IEquatable<T>, IComparable<T>
        {
            var stringKeys = Variables[domain].GetKeys();
            foreach (var stringKey in stringKeys)
            {
                var variable = Variables[domain].Read(stringKey) as DefinableValue<T>;
                constraintExpressions.Add(new ConstraintExpression<T>
                {
                    Constant = variable,
                    ConstraintVariable = new ConstraintVariable
                    {
                        Domain = domain,
                        Name = stringKey,
                        VariableType = VariableType.Read // Define if Read / Write
                    },
                    LogicalConnective = LogicalConnective.And,
                    Predicate = BinaryPredicate.Equal
                });
            }
        }
    }
}
