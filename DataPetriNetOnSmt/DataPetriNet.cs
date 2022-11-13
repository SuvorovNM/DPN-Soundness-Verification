
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;

namespace DataPetriNetOnSmt
{
    [Serializable]
    public class DataPetriNet : IDisposable, ICloneable
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public Context Context { get; set; }

        public string Name { get; set; }
        public List<Place> Places { get; set; }
        public List<Transition> Transitions { get; set; }
        public List<Arc> Arcs { get; set; }
        [System.Xml.Serialization.XmlIgnoreAttribute]
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

        public DataPetriNet()
        {

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

        public object Clone()
        {
            var dpn = new DataPetriNet(Context);
            dpn.Name = Name;
            dpn.Places = this.Places.Select(place => (Place)place.Clone()).ToList();
            dpn.Transitions = this.Transitions.Select(transition => (Transition)transition.Clone()).ToList();

            var placesDict = dpn.Places.ToDictionary(place => place.Id);
            var transitionsDict = dpn.Transitions.ToDictionary(transition => transition.Id);

            foreach (var arc in Arcs)
            {
                dpn.Arcs.Add(arc.Type == ArcType.PlaceTransition
                    ? new Arc(placesDict[arc.Source.Id], transitionsDict[arc.Destination.Id], arc.Weight)
                    : new Arc(transitionsDict[arc.Source.Id], placesDict[arc.Destination.Id], arc.Weight));
            }

            foreach (DomainType domainType in Enum.GetValues(typeof(DomainType)))
            {
                var varKeys = Variables[domainType].GetKeys();

                foreach (var varKey in varKeys)
                {
                    var variable = Variables[domainType].Read(varKey);

                    dpn.Variables[domainType].Write(varKey, variable);
                }
            }

            return dpn;
        }
    }
}
