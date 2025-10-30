using System.Collections;
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
		private record TransitionRefinementInfo(HashSet<string> ReadVariables, HashSet<Transition> TransitionsToConsiderInSplit);

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

			var transitionsRefinementInfo = transformedDpn
				.Transitions
				.ToDictionary(
					t => t.Id,
					t => new TransitionRefinementInfo(t.Guard.ReadVars.Keys.ToHashSet(), []));

			DefineRefinementBasis(transformedDpn, maximumCycles, transitionsRefinementInfo);

			var transitionsPreset = new Dictionary<string, List<(Place place, int weight)>>();
			var transitionsPostset = new Dictionary<string, List<(Place place, int weight)>>();
			FillTransitionsArcs(sourceDpn, transitionsPreset, transitionsPostset);

			var context = sourceDpn.Context;
			var refinedTransitions = sourceDpn.Transitions
				.ToDictionary(t => t.Id, _ => new HashSet<Transition>());

			foreach (var transition in sourceDpn.Transitions)
			{
				if (transitionsRefinementInfo[transition.Id].TransitionsToConsiderInSplit.Count == 0)
				{
					refinedTransitions[transition.Id] = [transition];
					continue;
				}

				// TODO: добавить проверки, что условие не true/false?

				// Формируем массив формул (не переходов) - определяем взаимно-неэквивалентные, их и используем далее
				// или более пристально смотрим на запись - но как? Проверить, точно ли нам нужны тут записи, или можем обойтись чтением
				var conjunctionsOfExpressions = new List<(BoolExpr expr, string name)>();
				var transitionsToConsiderInSplit = transitionsRefinementInfo[transition.Id].TransitionsToConsiderInSplit
					.Where(t => t.Id != transition.Id)
					.ToArray();

				var expressions = transitionsToConsiderInSplit.Select(t => t.Guard.ActualConstraintExpression).ToArray();
				var readyToJoinExpressions = new List<BoolExpr>(expressions.Length);

				var overwrittenVars = new Dictionary<string, DomainType>();
				var baseTransitionNames = transitionsToConsiderInSplit
					.Select(t => t.Id).ToArray();

				var addedTransitionNames = new List<string>(expressions.Length * 2);
				for (var i = 0; i < expressions.Length; i++)
				{
					var expression = expressions[i];
					var writtenVars = expression.GetTypedVarsDict(VariableType.Written);
					var readVars = expression.GetTypedVarsDict(VariableType.Read);

					if (readVars.Count == 0)
					{
						continue;
					}

					if (!writtenVars.Keys.Intersect(readVars.Keys).Any())
					{
						var expressionWithoutIntersections = expression;
						foreach (var variable in writtenVars)
						{
							var readVar = context.GenerateExpression(variable.Key, variable.Value, VariableType.Read);
							var writeVar = context.GenerateExpression(variable.Key, variable.Value, VariableType.Written);

							expressionWithoutIntersections = (BoolExpr)expressionWithoutIntersections.Substitute(writeVar, readVar);
						}

						readyToJoinExpressions.Add(expressionWithoutIntersections);
						addedTransitionNames.Add(baseTransitionNames[i]);
						continue;
					}

					var expressionWithIntersections = expression;
					var variableIntersection = writtenVars.Intersect(readVars).ToDictionary();

					var expressionWithoutWrittenIntersected = context.GetExistsExpression(expressionWithIntersections, variableIntersection, VariableType.Written);
					var expressionWithoutReadIntersected = context.GetExistsExpression(expressionWithIntersections, variableIntersection, VariableType.Read);

					foreach (var variable in writtenVars)
					{
						var readVar = context.GenerateExpression(variable.Key, variable.Value, VariableType.Read);
						var writeVar = context.GenerateExpression(variable.Key, variable.Value, VariableType.Written);

						expressionWithoutWrittenIntersected = (BoolExpr)expressionWithoutWrittenIntersected.Substitute(writeVar, readVar);
						expressionWithoutReadIntersected = (BoolExpr)expressionWithoutReadIntersected.Substitute(writeVar, readVar);
					}

					if (expressionWithoutReadIntersected is { IsTrue: false, IsFalse: false })
					{
						readyToJoinExpressions.Add(expressionWithoutReadIntersected);
						addedTransitionNames.Add(baseTransitionNames[i]);
					}

					if (expressionWithoutWrittenIntersected is { IsTrue: false, IsFalse: false } &&
					    !context.AreEqual(expressionWithoutWrittenIntersected, expressionWithoutReadIntersected))
					{
						readyToJoinExpressions.Add(expressionWithoutWrittenIntersected);
						addedTransitionNames.Add(baseTransitionNames[i]);
					}
				}

				for (int i = 0; i < Math.Pow(2, readyToJoinExpressions.Count); i++)
				{
					var ba = new BitArray([i]);
					var andExpression = context.MkAnd(readyToJoinExpressions
						.Select((e, j) => ba[j] == false ? e : context.MkNot(e)));
					var splitTransitionNames = string.Join("",
						addedTransitionNames.Select((t, j) => (ba[j] == false ? "+" : "-") + t));
					conjunctionsOfExpressions.Add((andExpression, splitTransitionNames));
				}

				// TODO: Имеет ли смысл на данном этапе мержить обратно переходы, которые не очень полезны?
				foreach (var (expression, splitTransitionNames) in conjunctionsOfExpressions)
				{
					var formulaToConjunct = expression;
					foreach (var overwrittenVar in transition.Guard.WriteVars)
					{
						var readVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Read);
						var writeVar = context.GenerateExpression(overwrittenVar.Key, overwrittenVar.Value, VariableType.Written);

						formulaToConjunct = (BoolExpr)formulaToConjunct.Substitute(readVar, writeVar);
					}

					var resultingFormula = context.MkAnd(transition.Guard.ActualConstraintExpression, formulaToConjunct);
					resultingFormula = context.GetExistsExpression(resultingFormula, overwrittenVars);

					if (!context.CanBeSatisfied(resultingFormula))
					{
						continue;
					}

					if (context.AreEqual(resultingFormula, transition.Guard.ActualConstraintExpression))
					{
						refinedTransitions[transition.Id].Add(transition);
						continue;
					}

					refinedTransitions[transition.Id].Add(
						new Transition(
							transition.Id + splitTransitionNames,
							Guard.MakeRefined(transition.Guard, resultingFormula),
							transition.BaseTransitionId,
							isSplit: true,
							label: transition.Label + splitTransitionNames));
				}
			}

			var refinedArcs = new List<Arc>();
			foreach (var refinedTransition in refinedTransitions)
			{
				var baseTransition = refinedTransition.Key;
				var resultingTransitions = refinedTransition.Value;

				foreach (var updatedTransition in resultingTransitions)
				{
					var updatedConstraint = sourceDpn.Context.SimplifyExpression(updatedTransition.Guard.ActualConstraintExpression);
					updatedTransition.Guard = Guard.MakeSimplified(updatedTransition.Guard, updatedConstraint);

					if (!transitionsPreset.TryGetValue(baseTransition, out var preset))
					{
						preset = transitionsPreset[updatedTransition.Id];
					}

					if (!transitionsPostset.TryGetValue(baseTransition, out var postset))
					{
						postset = transitionsPostset[updatedTransition.Id];
					}

					foreach (var arc in preset)
					{
						refinedArcs.Add(new Arc(arc.place, updatedTransition, arc.weight));
					}

					foreach (var arc in postset)
					{
						refinedArcs.Add(new Arc(updatedTransition, arc.place, arc.weight));
					}
				}
			}

			transformedDpn.Transitions = refinedTransitions.Values.SelectMany(t => t).ToList();

			transformedDpn.Arcs = refinedArcs;


			return new RefinementResult(transformedDpn, ToStateSpaceConverter.Convert(sourceLts));
		}

		// Все переменные, которые где-то записываются, считаем записываемыми (включая текущий переход),
		// затем сделаем substitution
		private void DefineRefinementBasis(
			DataPetriNet sourceDpn,
			List<LtsCycle> cycles,
			Dictionary<string, TransitionRefinementInfo> refinementInfo)
		{
			var initialRefinedTransitionNumber = refinementInfo.Values.Sum(tri => tri.TransitionsToConsiderInSplit.Count);

			var transitionsDict = sourceDpn
				.Transitions
				.ToDictionary(x => x.Id);

			foreach (var sourceTransition in sourceDpn.Transitions)
			{
				if (sourceTransition.IsTau)
				{
					continue;
				}

				var writeVarsInSourceTransition = sourceTransition.Guard.WriteVars;
				var writeVarsNames = writeVarsInSourceTransition.Select(wv => wv.Key).ToHashSet();

				if (writeVarsInSourceTransition.Count > 0)
				{
					var cyclesWithTransition = cycles
						.Where(x => x.CycleArcs.Any(y => y.Transition.Id == sourceTransition.Id))
						.ToArray();

					var transitionsToInvestigate = cyclesWithTransition
						.SelectMany(c => c.CycleArcsWithAdjacent.Select(a => a.Transition))
						.Distinct()
						.Where(x => refinementInfo[x.Id].ReadVariables
							.Intersect(writeVarsNames).Any())
						.Select(x => transitionsDict[x.Id])
						.ToArray();

					foreach (var cycleTransition in transitionsToInvestigate)
					{
						var readVarsInCycleTransition = refinementInfo[cycleTransition.Id].ReadVariables
							.Except(sourceTransition.Guard.WriteVars.Select(v => v.Key));

						refinementInfo[sourceTransition.Id].ReadVariables.AddRange(readVarsInCycleTransition);
						refinementInfo[sourceTransition.Id].TransitionsToConsiderInSplit.Add(cycleTransition);
						refinementInfo[sourceTransition.Id].TransitionsToConsiderInSplit.AddRange(refinementInfo[cycleTransition.Id].TransitionsToConsiderInSplit);
					}
				}
			}

			if (initialRefinedTransitionNumber < refinementInfo.Values.Sum(tri => tri.TransitionsToConsiderInSplit.Count))
			{
				DefineRefinementBasis(sourceDpn, cycles, refinementInfo);
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