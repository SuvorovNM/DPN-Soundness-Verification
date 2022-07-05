
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using Microsoft.Z3;

namespace DataPetriNetOnSmt
{
    public class DataPetriNet : IDisposable
    {
        public Context Context { get; private set; }

        public string Name { get; set; }
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public List<Arc> Arcs { get; set; }
        public VariablesStore Variables { get; set; }

        public DataPetriNet(Context context)
        {
            Context = context;

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
                canMakeStep = transition.TryFire(Variables, Arcs, Context);
                if (canMakeStep)
                {
                    return canMakeStep;
                }
            }

            return canMakeStep;
        }

        public ConstraintState GenerateInitialConstraintState()
        {
            var state = new ConstraintState(Context);
            Places.ForEach(x => state.PlaceTokens.Add(x, x.Tokens));
            //state.Constraints, 
            state.Constraints = Context.MkAnd(
                AddTypedExpressions<string>(DomainType.String)
                .Union(AddTypedExpressions<bool>(DomainType.Boolean))
                .Union(AddTypedExpressions<long>(DomainType.Integer))
                .Union(AddTypedExpressions<double>(DomainType.Real)));

            if (state.Constraints.Args.Length == 0)
            {
                state.Constraints = Context.MkTrue();
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
                    DomainType.Real => Context.MkEq(Context.MkRealConst(varKey + "_r"), Context.MkReal(variable.Value.ToString())),
                    DomainType.Integer => Context.MkEq(Context.MkIntConst(varKey + "_r"), Context.MkInt(variable.Value.ToString())),
                    DomainType.Boolean => Context.MkEq(Context.MkBoolConst(varKey + "_r"), Context.MkBool(variable.Value as bool? == true)),
                    _ => throw new NotImplementedException("The domain type is not supported")
                });
            }

            return expressionList;
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
