using DPN.Models;
using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using Microsoft.Z3;

namespace DataPetriNetGeneration
{
	internal class DPNConditionsGenerator(Context context)
	{
		private const int VOV = 0;
		private readonly Random random = new();

		public void GenerateConditions(DataPetriNet dpn, int varsCount, int conditionsCount, bool soundnessPreference = false)
		{
			if ((varsCount < 0) || (conditionsCount < 0))
			{
				throw new ArgumentException("Number of variables and conditions must be non-negative");
			}

			if ((varsCount < 1) && (conditionsCount > 0))
			{
				throw new ArgumentException("Model must have at least one variable");
			}

			var varsPool = GetVarsPool(varsCount);
			var variables = new VariablesStore();
			foreach (var variable in varsPool)
			{
				variables[DomainType.Real].Write(variable, new DefinableValue<double>(0));
			}

			dpn.Variables = variables;

			if (conditionsCount == 0)
			{
				return;
			}

			var constantsPool = GetConstantsPool(GetConstantsCount(conditionsCount));

			var predicatesPool = GetPredicatesPool();
			var connectivesPool = GetConnectivesPool();
			var varTypesPool = GetVariableTypesPool();
			var conditionsPerTransition = GetConditionsCountPerTransition(dpn.Transitions.Count, conditionsCount);

			var transitionIndex = 0;
			while (transitionIndex < dpn.Transitions.Count)
			{
				BoolExpr? expression = null;

				for (var i = 0; i < conditionsPerTransition[transitionIndex]; i++)
				{
					var firstVariableType = GetVarType(varTypesPool, soundnessPreference);
					var logicalConnectiveType = GetLogicalConnectiveType(connectivesPool, i);
					var variableName = GetVarName(varsPool);
					var predicate = GetPredicate(predicatesPool);

					if (GetConditionType() == VOV)
					{
						expression = GenerateVOVExpression(
							varsPool,
							firstVariableType,
							logicalConnectiveType,
							variableName,
							predicate,
							expression);
					}
					else
					{
						expression = GenerateVOCExpression(
							constantsPool,
							firstVariableType,
							logicalConnectiveType,
							variableName,
							predicate,
							expression);
					}
				}

				if (context.CanBeSatisfied(expression ?? context.MkTrue()))
				{
					dpn.Transitions[transitionIndex].Guard = new Guard(dpn.Context, expression);
					transitionIndex++;
				}
			}
		}

		private int GetConditionType()
		{
			return random.Next(0, 4);
		}

		private BinaryPredicate GetPredicate(List<BinaryPredicate> predicatesPool)
		{
			return predicatesPool[random.Next(0, predicatesPool.Count)];
		}

		private string GetVarName(List<string> varsPool)
		{
			return varsPool[random.Next(0, varsPool.Count)];
		}

		private VariableType GetVarType(List<VariableType> varTypesPool, bool soundnessPreference)
		{
			if (soundnessPreference)
			{
				var chosenOption = random.Next(0, 4); // 75% chance of write condition

				return chosenOption == 0
					? VariableType.Read
					: VariableType.Written;
			}

			return varTypesPool[random.Next(0, varTypesPool.Count)];
		}

		private LogicalConnective GetLogicalConnectiveType(List<LogicalConnective> connectivesPool, int i)
		{
			return i == 0
				? LogicalConnective.Empty
				: connectivesPool[random.Next(0, connectivesPool.Count)];
		}

		private BoolExpr GenerateVOVExpression(
			List<string> varsPool,
			VariableType firstVariableType,
			LogicalConnective logicalConnectiveType,
			string variableName,
			BinaryPredicate predicate,
			BoolExpr? previousExpression = null)
		{
			var secondVariableName = varsPool[random.Next(0, varsPool.Count)];

			var firstVarPrefix = firstVariableType == VariableType.Read ? "_r" : "_w";
			var secondVarPrefix = "_r";

			var firstVarName = $"{variableName}{firstVarPrefix}";
			var secondVarName = $"{secondVariableName}{secondVarPrefix}";

			var firstVar = context.MkRealConst(firstVarName);
			var secondVar = context.MkRealConst(secondVarName);

			var currentExpr = predicate switch
			{
				BinaryPredicate.Equal => context.MkEq(firstVar, secondVar),
				BinaryPredicate.Unequal => context.MkNot(context.MkEq(firstVar, secondVar)),
				BinaryPredicate.GreaterThan => context.MkGt(firstVar, secondVar),
				BinaryPredicate.GreaterThanOrEqual => context.MkGe(firstVar, secondVar),
				BinaryPredicate.LessThan => context.MkLt(firstVar, secondVar),
				BinaryPredicate.LessThanOrEqual => context.MkLe(firstVar, secondVar),
				_ => throw new NotSupportedException($"Predicate {predicate} not supported")
			};

			if (previousExpression != null)
			{
				return logicalConnectiveType switch
				{
					LogicalConnective.And => context.MkAnd(previousExpression, currentExpr),
					LogicalConnective.Or => context.MkOr(previousExpression, currentExpr),
					LogicalConnective.Empty => currentExpr,
					_ => throw new NotSupportedException($"Logical connective {logicalConnectiveType} not supported")
				};
			}

			return currentExpr;
		}

		private BoolExpr GenerateVOCExpression(
			List<int> constantsPool,
			VariableType firstVariableType,
			LogicalConnective logicalConnectiveType,
			string variableName,
			BinaryPredicate predicate,
			BoolExpr? previousExpression = null)
		{
			var constant = constantsPool[random.Next(0, constantsPool.Count)];

			var varPrefix = firstVariableType == VariableType.Read ? "_r" : "_w";
			var varName = $"{variableName}{varPrefix}";

			var variable = context.MkRealConst(varName);
			var constantExpr = context.MkReal(constant);

			var currentExpr = predicate switch
			{
				BinaryPredicate.Equal => context.MkEq(variable, constantExpr),
				BinaryPredicate.Unequal => context.MkNot(context.MkEq(variable, constantExpr)),
				BinaryPredicate.GreaterThan => context.MkGt(variable, constantExpr),
				BinaryPredicate.GreaterThanOrEqual => context.MkGe(variable, constantExpr),
				BinaryPredicate.LessThan => context.MkLt(variable, constantExpr),
				BinaryPredicate.LessThanOrEqual => context.MkLe(variable, constantExpr),
				_ => throw new NotSupportedException($"Predicate {predicate} not supported")
			};

			if (previousExpression != null)
			{
				return logicalConnectiveType switch
				{
					LogicalConnective.And => context.MkAnd(previousExpression, currentExpr),
					LogicalConnective.Or => context.MkOr(previousExpression, currentExpr),
					LogicalConnective.Empty => currentExpr,
					_ => throw new NotSupportedException($"Logical connective {logicalConnectiveType} not supported")
				};
			}

			return currentExpr;
		}


		private List<int> GetConditionsCountPerTransition(int transitionsCount, int conditionsCount)
		{
			var conditionsPerTransition = new int[transitionsCount];
			for (var i = 0; i < conditionsCount; i++)
			{
				conditionsPerTransition[random.Next(transitionsCount)]++;
			}

			return conditionsPerTransition.ToList();
		}


		private int GetConstantsCount(int conditionsCount)
		{
			return Math.Max(conditionsCount / 4, 2); // 4 is taken empirically
		}

		private List<VariableType> GetVariableTypesPool()
		{
			return Enum.GetValues<VariableType>().ToList();
		}

		private List<LogicalConnective> GetConnectivesPool()
		{
			return Enum.GetValues<LogicalConnective>().Except(new[] { LogicalConnective.Empty }).ToList();
		}

		private List<BinaryPredicate> GetPredicatesPool()
		{
			return Enum.GetValues<BinaryPredicate>().ToList();
		}

		private List<int> GetConstantsPool(int constsCount)
		{
			var constantsPool = new List<int>(constsCount);

			for (var i = 0; i < constsCount; i++)
			{
				constantsPool.Add(random.Next(-1000000, 1000001));
			}

			return constantsPool;
		}

		private List<string> GetVarsPool(int varsCount)
		{
			var varsPool = new List<string>(varsCount);

			for (var i = 0; i < varsCount; i++)
			{
				varsPool.Add($"v{i}");
			}

			return varsPool;
		}
	}
}