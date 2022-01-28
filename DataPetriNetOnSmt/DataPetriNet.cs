
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataPetriNetOnSmt
{
    public class DataPetriNet
    {
        private readonly Context context;

        public string Name { get; set; }
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public List<Arc> Arcs { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet()
        {
            context = new Context(new Dictionary<string, string> {["proof"] = "true" });            

            Places = new List<Place>();
            Transitions = new List<Transition>();
            Arcs = new List<Arc>();
            Variables = new VariablesStore();
            Name = string.Empty;
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
            //state.Constraints, 
            state.Constraints = ContextProvider.Context.MkAnd(
                AddTypedExpressions<string>(DomainType.String)
                .Union(AddTypedExpressions<bool>(DomainType.Boolean))
                .Union(AddTypedExpressions<long>(DomainType.Integer))
                .Union(AddTypedExpressions<double>(DomainType.Real)));

            if (state.Constraints.Args.Length == 0)
            {
                state.Constraints = ContextProvider.Context.MkTrue();
            }
            return state;
        }

        private IEnumerable<BoolExpr> AddTypedExpressions<T>(DomainType domain)
            where T : IEquatable<T>, IComparable<T>
        {
            var varKeys = Variables[domain].GetKeys();
            var expressionList = new List<BoolExpr>();

            foreach (var varKey in varKeys)
            {
                var variable = Variables[domain].Read(varKey) as DefinableValue<T>;

                expressionList.Add(domain switch
                {
                    DomainType.Real => ContextProvider.Context.MkEq(ContextProvider.Context.MkRealConst(varKey + "_r"), ContextProvider.Context.MkReal(variable.Value.ToString())),
                    DomainType.Integer => ContextProvider.Context.MkEq(ContextProvider.Context.MkIntConst(varKey + "_r"), ContextProvider.Context.MkInt(variable.Value.ToString())),
                    DomainType.Boolean => ContextProvider.Context.MkEq(ContextProvider.Context.MkBoolConst(varKey + "_r"), ContextProvider.Context.MkBool(variable.Value as bool? == true)),
                    _ => throw new NotImplementedException("The domain type is not supported")
                });
            }

            return expressionList;
        }
    }
}
