using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.Repair.Cycles;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using Microsoft.Z3;

namespace DPN.Soundness.Transformations
{
	public static class RefinementSettingsConstants
	{
		public const string BaseStructure = nameof(BaseStructure);
		public const string CoverabilityGraph = nameof(CoverabilityGraph);
		public const string FiniteReachabilityGraph = nameof(FiniteReachabilityGraph);
	}

	public class TransformerToRefined
	{

		public RefinementResult Transform(DataPetriNet sourceDpn, Dictionary<string, string> transformationProperties)
		{
			transformationProperties.TryGetValue(RefinementSettingsConstants.BaseStructure, out var baseStructure);

			var transformedDpn = (DataPetriNet)sourceDpn.Clone();

			LabeledTransitionSystem sourceLts;
			if (baseStructure is null or RefinementSettingsConstants.CoverabilityGraph)
			{
				sourceLts = new CoverabilityGraph(transformedDpn);
			}
			else if (baseStructure == RefinementSettingsConstants.FiniteReachabilityGraph)
			{
				sourceLts = new ReachabilityGraph(transformedDpn);
			}
			else
			{
				throw new ArgumentException($"{nameof(TransformerToRefined)} does not support base structure {baseStructure}");
			}

			sourceLts.GenerateGraph();
			var maximumCycles = CyclesFinder.GetCycles(sourceLts);

			Refine(transformedDpn, maximumCycles, sourceLts.ConstraintArcs.ToArray());
			return new RefinementResult(transformedDpn, ToStateSpaceConverter.Convert(sourceLts));
		}


		private void Refine(
			DataPetriNet sourceDpn,
			List<LtsCycle> cycles,
			LtsArc[] allArcs)
		{
			var arcToStates = allArcs
				.GroupBy(a => a.SourceState)
				.ToDictionary(a => a.Key, a => a.ToArray());

			var baseToRefinedTransitions = sourceDpn
				.Transitions
				.GroupBy(t => t.BaseTransitionId)
				.ToDictionary(t => t.Key, t => t.ToArray());

			var initialRefinedTransitionNumber = sourceDpn.Transitions.Count;

			var refinedTransitions = new List<Transition>(sourceDpn.Transitions.Count * sourceDpn.Transitions.Count);

			var context = sourceDpn.Context;

			foreach (var sourceTransition in sourceDpn.Transitions)
			{
				if (sourceTransition.IsTau)
				{
					refinedTransitions.Add(sourceTransition);
				}

				var writeVarsInSourceTransition = sourceTransition.Guard.WriteVars;
				if (writeVarsInSourceTransition.Count > 0)
				{
					var writeVarsNames = writeVarsInSourceTransition.Select(wv => wv.Key).ToHashSet();

					var cyclesWithTransition = cycles
						.Where(x => x.CycleArcs.Any(y => y.Transition.Id == sourceTransition.BaseTransitionId))
						.ToArray();

					// Если имеем, что из конечной позиции не может быть выхода, то можно сделать определять переходы к разделению как:
					// SelectMany(c => c.OutputArcs.SelectMany(a => baseToRefinedTransitions[a.Transition.Id])
					//		.Union(c.CycleArcs.SelectMany(a => baseToRefinedTransitions[a.Transition.Id].Where(t => t.IsSplit))))
					// Но в общем случае это неверно. Также можем упустить лайвлоки внутри маленьких циклов
					var transitionsToInvestigate = cyclesWithTransition
						.SelectMany(c => c.CycleArcsWithAdjacent
							.Where(a => arcToStates[a.SourceState].Length > 1) // Очень дешевая эвристика, которая отработает в большой части случаев
							.SelectMany(a => baseToRefinedTransitions[a.Transition.Id])
							.Union(c.CycleArcs.SelectMany(a => baseToRefinedTransitions[a.Transition.Id].Where(t => t.IsSplit))))
						.Distinct()
						.Where(x => x.Guard.ReadVars.Keys.Intersect(writeVarsNames).Any())
						.ToArray();

					var transitionsToRefine = new List<Transition> { sourceTransition };
					var transitionToRefineWriteVars = sourceTransition.Guard.WriteVars;

					foreach (var cycleTransition in transitionsToInvestigate)
					{
						var cycleTransitionWriteVars = cycleTransition.Guard.WriteVars;
						var inputCondition = context.GetExistsExpression(cycleTransition.Guard.ActualConstraintExpression, cycleTransitionWriteVars);
						if (inputCondition is not { IsTrue: false, IsFalse: false })
						{
							continue;
						}

						foreach (var overwrittenVar in transitionToRefineWriteVars)
						{
							var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
							var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

							inputCondition = (BoolExpr)inputCondition.Substitute(readVar, writeVar);
						}

						var updatedTransitions = new List<Transition>();

						foreach (var transitionToRefine in transitionsToRefine)
						{
							var positiveCondition = context.MkAnd(transitionToRefine.Guard.ActualConstraintExpression, inputCondition);
							if (context.AreEqual(transitionToRefine.Guard.ActualConstraintExpression, positiveCondition) || !context.CanBeSatisfied(positiveCondition))
							{
								continue;
							}

							var negativeCondition = context.MkAnd(transitionToRefine.Guard.ActualConstraintExpression, context.MkNot(inputCondition));

							var positiveTransition = new Transition(
								transitionToRefine.Id + "+[" + cycleTransition.Id + "]",
								Guard.MakeRefined(transitionToRefine.Guard, context.SimplifyExpression(positiveCondition)),
								transitionToRefine.BaseTransitionId,
								isSplit: true);


							var negativeTransition = new Transition(
								transitionToRefine.Id + "-[" + cycleTransition.Id + "]",
								Guard.MakeRefined(transitionToRefine.Guard, context.SimplifyExpression(negativeCondition)),
								transitionToRefine.BaseTransitionId,
								isSplit: true);

							updatedTransitions.Add(positiveTransition);
							updatedTransitions.Add(negativeTransition);
						}

						transitionsToRefine = updatedTransitions.Count == 0 ? transitionsToRefine : updatedTransitions;
					}

					refinedTransitions.AddRange(transitionsToRefine);
				}
				else
				{
					refinedTransitions.Add(sourceTransition);
				}
			}

			var transitionsPreset = new Dictionary<string, List<(Place place, int weight)>>();
			var transitionsPostset = new Dictionary<string, List<(Place place, int weight)>>();
			FillTransitionsArcs(sourceDpn, transitionsPreset, transitionsPostset);
			var refinedArcs = new List<Arc>();
			foreach (var refinedTransition in refinedTransitions)
			{
				var parentTransitionId = baseToRefinedTransitions[refinedTransition.BaseTransitionId].First().Id;
				var preset = transitionsPreset[parentTransitionId];
				var postset = transitionsPostset[parentTransitionId];

				foreach (var arc in preset)
				{
					refinedArcs.Add(new Arc(arc.place, refinedTransition, arc.weight));
				}

				foreach (var arc in postset)
				{
					refinedArcs.Add(new Arc(refinedTransition, arc.place, arc.weight));
				}
			}

			sourceDpn.Transitions = refinedTransitions;
			sourceDpn.Arcs = refinedArcs;

			if (initialRefinedTransitionNumber < refinedTransitions.Count)
			{
				Refine(sourceDpn, cycles, allArcs);
			}
		}


		public static void FillTransitionsArcs(DataPetriNet sourceDpn, Dictionary<string, List<(Place place, int weight)>> transitionsPreset, Dictionary<string, List<(Place place, int weight)>> transitionsPostset)
		{
			foreach (var transition in sourceDpn.Transitions)
			{
				transitionsPreset.Add(transition.Id, new List<(Place place, int weight)>());
				transitionsPostset.Add(transition.Id, new List<(Place place, int weight)>());
			}

			foreach (var arc in sourceDpn.Arcs)
			{
				if (arc.Type == ArcType.PlaceTransition)
				{
					transitionsPreset[arc.Destination.Id].Add(((Place)arc.Source, arc.Weight));
				}
				else
				{
					transitionsPostset[arc.Source.Id].Add(((Place)arc.Destination, arc.Weight));
				}
			}
		}
	}
}