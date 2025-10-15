using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.Soundness.Repair.Cycles;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using DPN.Soundness.TransitionSystems.StateSpaceAbstraction;
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
			int sourceDpnTransitionCount;

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
			
			// TODO: пытаемся найти порядок, в котором применять изменения. Иначе уточнений может быть очень много
			// Храним маппинг t -> list(read_vars), list(transitions_to_split_on)
			
			
			var baseTransitionIds = transformedDpn
				.Transitions
				.ToDictionary(t => t.BaseTransitionId, t => new HashSet<string>(){t.BaseTransitionId});

			do
			{
				sourceDpnTransitionCount = transformedDpn.Transitions.Count;

				transformedDpn = PerformTransformationStep(transformedDpn, maximumCycles, baseTransitionIds);

				if (sourceDpnTransitionCount == transformedDpn.Transitions.Count)
				{
					break;
				}

				maximumCycles = EnrichCyclesWithRefinedTransitions(transformedDpn, maximumCycles);
			} while (transformedDpn.Transitions.Count > sourceDpnTransitionCount);

			return new RefinementResult(transformedDpn, ToStateSpaceConverter.Convert(sourceLts));
		}

		private DataPetriNet PerformTransformationStep(DataPetriNet sourceDpn, List<LtsCycle> cycles, Dictionary<string, HashSet<string>> baseTransitionIds)
		{
			var newDpn = (DataPetriNet)sourceDpn.Clone();
			var context = sourceDpn.Context;

			var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
			var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
			FillTransitionsArcs(newDpn, transitionsPreset, transitionsPostset);

			var refinedTransitions = new List<Transition>();
			var refinedArcs = new List<Arc>();

			var transitionsDict = sourceDpn
				.Transitions
				.ToDictionary(x => x.Id);

			foreach (var sourceTransition in newDpn.Transitions)
			{
				var updatedTransitions = new List<Transition> { sourceTransition };

				var writeVarsInSourceTransition = sourceTransition.Guard.WriteVars;
				var writeVarsNames = writeVarsInSourceTransition.Select(wv => wv.Key).ToHashSet();

				if (writeVarsInSourceTransition.Count > 0)
				{
					var cyclesWithTransition = cycles
						.Where(x => x.CycleArcs.Any(y => y.Transition.Id == sourceTransition.Id));

					var transitionsToInvestigate = cyclesWithTransition
						.SelectMany(c => c.CycleArcsWithAdjacent)
						.Where(x => transitionsDict[x.Transition.Id].Guard.ReadVars.Select(v => v.Key)
							.Intersect(writeVarsNames).Any())
						.Select(x => transitionsDict[x.Transition.Id])
						.Distinct()
						.ToArray();

					foreach (var outputTransition in transitionsToInvestigate)
					{
						var updatedTransitionsBasis = new List<Transition>(updatedTransitions);

						var overwrittenVarsInOutTransition = outputTransition.Guard.WriteVars;
						var readFormula = context.GetReadExpression(outputTransition.Guard.ActualConstraintExpression, overwrittenVarsInOutTransition);

						var formulaToConjunct = readFormula;
						foreach (var overwrittenVar in writeVarsInSourceTransition)
						{
							var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
							var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

							formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
						}

						foreach (var baseTransition in updatedTransitionsBasis)
						{
							if (baseTransition.IsTau)
							{
								continue;
							}

							if (baseTransitionIds[baseTransition.BaseTransitionId].Contains(outputTransition.BaseTransitionId))
							{
								updatedTransitions.Add((Transition)baseTransition.Clone());
								continue;
							}

							(var positiveTransition, var negativeTransition) = baseTransition
								.Split(formulaToConjunct, outputTransition.Id);
							if (positiveTransition != null && negativeTransition != null)
							{
								updatedTransitions.Add(positiveTransition);
								updatedTransitions.Add(negativeTransition);

								if (!baseTransitionIds.ContainsKey(positiveTransition.BaseTransitionId))
								{
									baseTransitionIds[positiveTransition.BaseTransitionId] = new HashSet<string>(baseTransitionIds[outputTransition.BaseTransitionId]){positiveTransition.BaseTransitionId};
								}
								else
								{
									baseTransitionIds[positiveTransition.BaseTransitionId].Add(outputTransition.BaseTransitionId);
									baseTransitionIds[positiveTransition.BaseTransitionId].AddRange(baseTransitionIds[outputTransition.BaseTransitionId]);
								}
								
								/*if (!baseTransitionIds.ContainsKey(negativeTransition.BaseTransitionId))
								{
									baseTransitionIds[negativeTransition.BaseTransitionId] = new HashSet<string>(baseTransitionIds[outputTransition.BaseTransitionId]){negativeTransition.BaseTransitionId};
								}
								else
								{
									baseTransitionIds[negativeTransition.BaseTransitionId].Add(outputTransition.BaseTransitionId);
									baseTransitionIds[negativeTransition.BaseTransitionId].AddRange(baseTransitionIds[outputTransition.BaseTransitionId]);
								}*/

							}
							else
							{
								updatedTransitions.Add((Transition)baseTransition.Clone());
							}
						}

						updatedTransitions = updatedTransitions
							.Except(updatedTransitionsBasis)
							.ToList();
					}
				}


				foreach (var updatedTransition in updatedTransitions)
				{
					var updatedConstraint = sourceDpn.Context.SimplifyExpression(updatedTransition.Guard.ActualConstraintExpression);
					updatedTransition.Guard = Guard.MakeSimplified(updatedTransition.Guard, updatedConstraint);

					foreach (var arc in transitionsPreset[sourceTransition])
					{
						refinedArcs.Add(new Arc(arc.place, updatedTransition, arc.weight));
					}

					foreach (var arc in transitionsPostset[sourceTransition])
					{
						refinedArcs.Add(new Arc(updatedTransition, arc.place, arc.weight));
					}
				}

				refinedTransitions.AddRange(updatedTransitions);
			}

			newDpn.Transitions = refinedTransitions;
			newDpn.Arcs = refinedArcs;

			return newDpn;
		}

		private static List<LtsCycle> EnrichCyclesWithRefinedTransitions(DataPetriNet transformedDpn, List<LtsCycle> ltsMaximumCycles)
		{
			foreach (var splitTransitions in transformedDpn.Transitions.GroupBy(t => t.BaseTransitionId))
			{
				var updatedCycles = new List<LtsCycle>(ltsMaximumCycles.Count);
				foreach (var cycle in ltsMaximumCycles)
				{
					var cycleArcs = new HashSet<LtsArc>();
					foreach (var arc in cycle.CycleArcs)
					{
						if (arc.Transition.NonRefinedTransitionId == splitTransitions.Key)
						{
							foreach (var splitTransition in splitTransitions)
							{
								cycleArcs.Add(new LtsArc(
									arc.SourceState,
									new LtsTransition(splitTransition),
									arc.TargetState));
							}
						}
						else
						{
							cycleArcs.Add(arc);
						}
					}

					var outgoingArcs = new HashSet<LtsArc>();
					foreach (var arc in cycle.CycleArcsWithAdjacent)
					{
						if (arc.Transition.NonRefinedTransitionId == splitTransitions.Key)
						{
							foreach (var splitTransition in splitTransitions)
							{
								outgoingArcs.Add(new LtsArc(
									arc.SourceState,
									new LtsTransition(splitTransition),
									arc.TargetState));
							}
						}
						else
						{
							outgoingArcs.Add(arc);
						}
					}

					updatedCycles.Add(new LtsCycle(cycleArcs, outgoingArcs));
				}

				ltsMaximumCycles = updatedCycles;
			}

			return ltsMaximumCycles;
		}


		public static void FillTransitionsArcs(DataPetriNet sourceDpn, Dictionary<Transition, List<(Place place, int weight)>> transitionsPreset, Dictionary<Transition, List<(Place place, int weight)>> transitionsPostset)
		{
			foreach (var transition in sourceDpn.Transitions)
			{
				transitionsPreset.Add(transition, new List<(Place place, int weight)>());
				transitionsPostset.Add(transition, new List<(Place place, int weight)>());
			}

			foreach (var arc in sourceDpn.Arcs)
			{
				if (arc.Type == ArcType.PlaceTransition)
				{
					transitionsPreset[(Transition)arc.Destination].Add(((Place)arc.Source, arc.Weight));
				}
				else
				{
					transitionsPostset[(Transition)arc.Source].Add(((Place)arc.Destination, arc.Weight));
				}
			}
		}
	}
}