
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNetOnSmt
{
    public class DataPetriNet
    {
        private readonly Random randomGenerator;
        private readonly Context context;

        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public List<Arc> Arcs { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet()
        {
            randomGenerator = new Random();
            context = new Context(new Dictionary<string, string> {["proof"] = "true" });            

            Places = new List<Place>();
            Transitions = new List<Transition>();
            Arcs = new List<Arc>();
        }

        public bool MakeStep()
        {
            var canMakeStep = false; // TODO: Find a more quicker way to get random elements?
            foreach (var transition in Transitions)//.OrderBy(x => randomGenerator.Next())
            {
                canMakeStep = transition.TryFire(Variables, Arcs, context);
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

            return state;
        }

        private void AddTypedExpressions<T>(BoolExpr constraintExpressions, DomainType domain)
            where T : IEquatable<T>, IComparable<T>
        {
            var stringKeys = Variables[domain].GetKeys();
            foreach (var stringKey in stringKeys)
            {
                var variable = Variables[domain].Read(stringKey) as DefinableValue<T>;

                var expr = domain switch
                {
                    DomainType.Real => ContextProvider.Context.MkEq(ContextProvider.Context.MkRealConst(stringKey), ContextProvider.Context.MkReal(variable.Value.ToString())),
                    DomainType.Integer => ContextProvider.Context.MkEq(ContextProvider.Context.MkIntConst(stringKey), ContextProvider.Context.MkInt(variable.Value.ToString())),
                    DomainType.Boolean => ContextProvider.Context.MkEq(ContextProvider.Context.MkBoolConst(stringKey), ContextProvider.Context.MkBool(variable.Value as bool? == true)),
                    _ => throw new NotImplementedException("The domain type is not supported")
                };

                constraintExpressions = ContextProvider.Context.MkAnd(constraintExpressions, expr);
            }
        }
    }
}
