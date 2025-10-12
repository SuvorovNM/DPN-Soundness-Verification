using DPN.Models;
using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Soundness.TransitionSystems.StateSpaceAbstraction
{
    public abstract class AbstractStateSpaceStructure<TAbsState,TAbsTransition, TAbsArc>
        where TAbsState : AbstractState, new()
        where TAbsTransition : AbstractTransition
        where TAbsArc : AbstractArc<TAbsState,TAbsTransition>
    {
        protected ConstraintExpressionService ExpressionService;
        private bool disposedValue;

        public DataPetriNet DataPetriNet { get; init; }
        public TAbsState InitialState { get; set; }
        public List<TAbsState> ConstraintStates { get; set; }
        public List<TAbsArc> ConstraintArcs { get; set; }

        public AbstractStateSpaceStructure(DataPetriNet dataPetriNet)
        {
            ExpressionService = new ConstraintExpressionService(dataPetriNet.Context);

            DataPetriNet = dataPetriNet;

            InitialState = FormInitialState(dataPetriNet);

            ConstraintStates = new List<TAbsState> { InitialState };

            ConstraintArcs = new List<TAbsArc>();
        }

        public abstract void GenerateGraph();

        protected abstract void AddNewState(TAbsState currentState,
                                TAbsTransition transition,
                                BaseStateInfo stateInfo);

        protected abstract TAbsState? FindParentNodeForWhichComparisonResultForCurrentNodeHolds
            (BaseStateInfo stateInfo, TAbsState parentNode, MarkingComparisonResult comparisonResult);

        private static TAbsState FormInitialState(DataPetriNet dpn)
        {
            return new TAbsState()
            {
                Marking = FormInitialStateMarking(dpn),
                Constraints = FormInitialStateConstraint(dpn)
            };
        }

        private static Marking FormInitialStateMarking(DataPetriNet dpn)
        {
            return Marking.FromDpnPlaces(dpn.Places);
        }

        private static BoolExpr FormInitialStateConstraint(DataPetriNet dpn)
        {
            var variables = dpn.Variables;
            var context = dpn.Context;

            var variablesList = variables.GetAllVariables();
            var expressionList = new List<BoolExpr>(variablesList.Length);

            foreach (var variableData in variablesList)
            {
                var variable = variables[variableData.domain].Read(variableData.name);

                expressionList.Add(variableData.domain switch
                {
                    DomainType.Real => context.MkEq(context.MkRealConst(variableData.name + "_r"), context.MkReal(variable.GetStringValue())),
                    DomainType.Integer => context.MkEq(context.MkIntConst(variableData.name + "_r"), context.MkInt(variable.GetStringValue())),
                    DomainType.Boolean => context.MkEq(context.MkBoolConst(variableData.name + "_r"), context.MkBool(variable.GetStringValue() == true.ToString())),
                    _ => throw new NotImplementedException("The domain type is not supported")
                });
            }

            return expressionList.Count > 0 ? context.MkAnd(expressionList) : context.MkTrue();
        }
    }
}
