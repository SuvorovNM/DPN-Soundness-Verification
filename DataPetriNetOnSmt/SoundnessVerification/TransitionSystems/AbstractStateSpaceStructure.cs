using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public abstract class AbstractStateSpaceStructure<AbsState,AbsTransition, AbsArc>
        where AbsState : AbstractState, new()
        where AbsTransition : AbstractTransition
        where AbsArc : AbstractArc<AbsState,AbsTransition>
    {
        protected ConstraintExpressionService expressionService;
        private bool disposedValue;

        protected DataPetriNet DataPetriNet { get; init; }
        public AbsState InitialState { get; set; }
        public List<AbsState> ConstraintStates { get; set; }
        public List<AbsArc> ConstraintArcs { get; set; }

        public AbstractStateSpaceStructure(DataPetriNet dataPetriNet)
        {
            expressionService = new ConstraintExpressionService(dataPetriNet.Context);

            DataPetriNet = dataPetriNet;

            InitialState = FormInitialState(dataPetriNet);

            ConstraintStates = new List<AbsState> { InitialState };

            ConstraintArcs = new List<AbsArc>();
        }

        public abstract void GenerateGraph();

        protected abstract void AddNewState(AbsState currentState,
                                AbsTransition transition,
                                BaseStateInfo stateInfo);

        protected abstract AbsState? FindParentNodeForWhichComparisonResultForCurrentNodeHolds
            (BaseStateInfo stateInfo, AbsState parentNode, MarkingComparisonResult comparisonResult);

        private static AbsState FormInitialState(DataPetriNet dpn)
        {
            return new AbsState()
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
            var expressionList = new List<BoolExpr>(variablesList.Count);

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
