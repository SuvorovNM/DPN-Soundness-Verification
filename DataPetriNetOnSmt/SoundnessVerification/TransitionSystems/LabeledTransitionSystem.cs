﻿using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.TransitionSystems
{
    public abstract class LabeledTransitionSystem
    {
        public Context Context { get; init; }
        protected AbstractConstraintExpressionService expressionService;
        protected DataPetriNet DataPetriNet { get; init; }
        public ConstraintState InitialState { get; set; }
        public List<ConstraintState> ConstraintStates { get; set; }
        public List<ConstraintArc> ConstraintArcs { get; set; }
        public bool IsFullGraph { get; set; }

        protected Stack<ConstraintState> StatesToConsider { get; set; }

        public LabeledTransitionSystem(DataPetriNet dataPetriNet, AbstractConstraintExpressionService abstractConstraintExpressionService)
        {
            expressionService = abstractConstraintExpressionService;
            Context = dataPetriNet.Context;

            DataPetriNet = dataPetriNet;

            InitialState = dataPetriNet.GenerateInitialConstraintState();

            ConstraintStates = new List<ConstraintState> { InitialState };

            ConstraintArcs = new List<ConstraintArc>();

            IsFullGraph = false;

            StatesToConsider = new Stack<ConstraintState>();
            StatesToConsider.Push(InitialState);
        }

        public LabeledTransitionSystem()
        {
            ConstraintArcs = new List<ConstraintArc>();
            ConstraintStates = new List<ConstraintState>();
            StatesToConsider = new Stack<ConstraintState>();
        }

        public abstract void GenerateGraph(bool removeRedundantBlocks = false);

        protected BoolExpr GetReadExpression(BoolExpr smtExpression, Dictionary<string, DomainType> overwrittenVarNames)
        {
            var variablesToOverwrite = new Expr[overwrittenVarNames.Count];
            var currentArrayIndex = 0;
            foreach (var keyValuePair in overwrittenVarNames)
            {
                variablesToOverwrite[currentArrayIndex++] = Context.GenerateExpression(keyValuePair.Key, keyValuePair.Value, VariableType.Written);
            }

            if (variablesToOverwrite.Length > 0)
            {
                var existsExpression = Context.MkExists(variablesToOverwrite, smtExpression);

                Goal g = Context.MkGoal(true, true, false);
                g.Assert((BoolExpr)existsExpression);
                Tactic tac = Context.MkTactic("qe");
                ApplyResult a = tac.Apply(g);

                return a.Subgoals[0].AsBoolExpr();
            }
            else
            {
                return smtExpression;
            }
        }

        protected BoolExpr GetSmtExpression(List<IConstraintExpression> constraints)
        {
            List<BoolExpr> expressions = new List<BoolExpr>();

            var j = -1;

            for (int i = 0; i < constraints.Count; i++)
            {
                if (constraints[i].LogicalConnective == LogicalConnective.Or ||
                    constraints[i].LogicalConnective == LogicalConnective.Empty)
                {
                    j++;
                    var smtExpr = constraints[i].GetSmtExpression(Context);
                    expressions.Add(smtExpr);
                }
                else
                {
                    expressions[j] = Context.MkAnd(expressions[j], constraints[i].GetSmtExpression(Context));
                }
            }

            return expressions.Count > 0
                ? Context.MkOr(expressions)
                : Context.MkTrue();
        }

        protected void AddNewState(ConstraintState currentState,
                                ConstraintTransition transition,
                                Dictionary<Node, int> marking,
                                BoolExpr constraintsIfFires)
        // TODO: Consider using less parameters
        {
            var equalStateInGraph = FindEqualStateInGraph(marking, constraintsIfFires);
            if (equalStateInGraph != null)
            {
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, equalStateInGraph));
                equalStateInGraph.ParentStates = equalStateInGraph.ParentStates.Union(currentState.ParentStates).ToHashSet();
                equalStateInGraph.ParentStates.Add(currentState);
            }
            else
            {
                var stateIfTransitionFires = new ConstraintState(marking, constraintsIfFires, currentState);
                ConstraintArcs.Add(new ConstraintArc(currentState, transition, stateIfTransitionFires));
                ConstraintStates.Add(stateIfTransitionFires);
                StatesToConsider.Push(stateIfTransitionFires);
            }
        }

        protected IEnumerable<Transition> GetTransitionsWhichCanFire(Dictionary<Node, int> marking)
        {
            var transitionsWhichCanFire = new List<Transition>();

            foreach (var transition in DataPetriNet.Transitions)
            {
                var preSetArcs = DataPetriNet.Arcs.Where(x => x.Destination == transition).ToList();

                var canFire = preSetArcs.All(x => marking[x.Source] >= x.Weight);

                if (canFire)
                {
                    transitionsWhichCanFire.Add(transition);
                }
            }

            return transitionsWhichCanFire;
        }

        protected bool IsMonotonicallyIncreasedWithUnchangedConstraints(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires, ConstraintState parentNode)
        {
            foreach (var stateInGraph in parentNode.ParentStates.Union(new[] { parentNode }))
            {
                var isConsideredStateTokensGreater = stateInGraph.PlaceTokens.Values.Sum() < tokens.Values.Sum() &&
                    tokens.Keys.All(key => tokens[key] >= stateInGraph.PlaceTokens[key]);

                if (isConsideredStateTokensGreater && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return true;
                }
            }

            return false;
        }

        protected ConstraintState FindEqualStateInGraph(Dictionary<Node, int> tokens, BoolExpr constraintsIfFires)
        {
            foreach (var stateInGraph in ConstraintStates)
            {
                var isConsideredStateTokensEqual = tokens.Keys
                    .All(key => tokens[key] == stateInGraph.PlaceTokens[key]);

                if (isConsideredStateTokensEqual && expressionService.AreEqual(constraintsIfFires, stateInGraph.Constraints))
                {
                    return stateInGraph;
                }
            }

            return null;
        }
    }
}
