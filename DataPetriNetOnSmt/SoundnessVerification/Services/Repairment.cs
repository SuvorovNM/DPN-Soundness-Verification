using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.Extensions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    public class Repairment
    {
        private TransformerToRefined transformerToRefined;

        public Repairment()
        {
            transformerToRefined = new TransformerToRefined();
        }
        public DataPetriNet RepairDpn(DataPetriNet sourceDpn)
        {
            // Perform until stabilization
            // Termination - either if all are green or all are red

            var dpnToConsider = (DataPetriNet)sourceDpn.Clone();
            var repairmentFinished = false;
            (dpnToConsider,_) = transformerToRefined.Transform(dpnToConsider);
            var transitionsDict = dpnToConsider.Transitions.ToDictionary(x => x.Id, y => y);

            do
            {
                var ct = new CoverabilityTree(dpnToConsider);
                ct.GenerateGraph();
                repairmentFinished = ct.ConstraintStates.All(x => x.StateColor == CtStateColor.Green)
                    || ct.ConstraintStates.All(x => x.StateColor == CtStateColor.Red);

                if (!repairmentFinished)
                    dpnToConsider = MakeRepairStep(dpnToConsider, ct, transitionsDict);
            } while (!repairmentFinished);

            return dpnToConsider;
        }
        public DataPetriNet MakeRepairStep(DataPetriNet sourceDpn, CoverabilityTree ct, Dictionary<string,Transition> transitionsDict)
        {
            var arcsDict = ct.ConstraintArcs.ToDictionary(x => (x.SourceState, x.TargetState), y => y.Transition);

            // Find green nodes which contain red nodes
            var criticalNodes = ct.ConstraintStates
                .Where(x => x.StateColor == CtStateColor.Red && x.ParentNode != null && x.ParentNode.StateColor == CtStateColor.Green)
                .GroupBy(x => x.ParentNode)
                .ToList();
            
            foreach(var nodeGroup in criticalNodes)
            {
                foreach(var childNode in nodeGroup)
                {
                    var arcBetweenNodes = arcsDict[(nodeGroup.Key, childNode)];
                    if (arcBetweenNodes.IsSilent)
                    {
                        // Take nearest ordinary (non-silent) transition
                        // The case when no such transition exists (tau from initial state) cannot exist
                        var parentNode = nodeGroup.Key;
                        Transition predecessorTransition = null;
                        while (parentNode.ParentNode != null && predecessorTransition == null)
                        {
                            var arcToConsider = arcsDict[(parentNode.ParentNode, parentNode)];
                            if (!arcToConsider.IsSilent)
                            {
                                predecessorTransition = transitionsDict[arcToConsider.Id];
                            }
                            parentNode = parentNode.ParentNode;
                        }

                        var overwrittenVars = predecessorTransition.Guard.WriteVars;
                        var formulaToConjunct = sourceDpn.Context.MkNot(childNode.Constraints);

                        foreach(var overwrittenVar in overwrittenVars)
                        {
                            var readVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
                            var writeVar = sourceDpn.Context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

                            formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
                        }

                        var updatedConstraint = 
                            sourceDpn.Context.MkAnd(predecessorTransition.Guard.ActualConstraintExpression, formulaToConjunct);

                        predecessorTransition.Guard = new Guard(sourceDpn.Context, predecessorTransition.Guard.BaseConstraintExpressions, updatedConstraint);
                    }
                    else
                    {
                        var transitionToUpdate = transitionsDict[arcBetweenNodes.Id];

                        var updatedConstraint = sourceDpn.Context.MkAnd(
                            sourceDpn.Context.MkNot(childNode.Constraints),
                            transitionToUpdate.Guard.ActualConstraintExpression
                            );

                        transitionToUpdate.Guard = new Guard
                            (sourceDpn.Context, transitionToUpdate.Guard.BaseConstraintExpressions, updatedConstraint);
                    }
                }
            }

            return sourceDpn;
        }
    }
}
