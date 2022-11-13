using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetGeneration
{
    public class DPNConditionsGenerator : IDisposable
    {
        private const int VOC = 0;
        private readonly Random random = new Random();
        public Context Context { get; private set; }

        public DPNConditionsGenerator(Context context)
        {
            Context = context;
        }
        public void GenerateConditions(DataPetriNet dpn, int varsCount, int conditionsCount)
        {
            if ((varsCount < 0) || (conditionsCount < 0))
            {
                throw new ArgumentException("Number of variables and conditions must be non-negative");
            }

            if ((varsCount < 1) && (conditionsCount > 0))
            {
                throw new ArgumentException("Model must have at least one variable");
            }

            // Add variables to DPN
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
                var conditions = new List<IConstraintExpression>(conditionsPerTransition[transitionIndex]);

                for (int i = 0; i < conditionsPerTransition[transitionIndex]; i++)
                {
                    var firstVariableType = GetVarType(varTypesPool);
                    var logicalConnectiveType = GetLogicalConnectiveType(connectivesPool, i);
                    var variableName = GetVarName(varsPool);
                    var predicate = GetPredicate(predicatesPool);

                    if (GetConditionType() == VOC)
                    {
                        var vocExpression = GenerateVOCExpression(
                            constantsPool,
                            firstVariableType,
                            logicalConnectiveType,
                            variableName,
                            predicate);

                        conditions.Add(vocExpression);
                    }
                    else
                    {
                        var vovExpression = GenerateVOVExpression(
                            varsPool,
                            firstVariableType,
                            logicalConnectiveType,
                            variableName,
                            predicate);

                        conditions.Add(vovExpression);
                    }
                }

                if (CheckSatisfiability(conditions) == Status.SATISFIABLE)
                {
                    dpn.Transitions[transitionIndex].Guard = new Guard(dpn.Context, conditions);
                    transitionIndex++;
                }
            }
        }

        private int GetConditionType()
        {
            return random.Next(0, 2);
        }

        private BinaryPredicate GetPredicate(List<BinaryPredicate> predicatesPool)
        {
            return predicatesPool[random.Next(0, predicatesPool.Count)];
        }

        private string GetVarName(List<string> varsPool)
        {
            return varsPool[random.Next(0, varsPool.Count)];
        }

        private VariableType GetVarType(List<VariableType> varTypesPool)
        {
            return varTypesPool[random.Next(0, varTypesPool.Count)];
        }

        private LogicalConnective GetLogicalConnectiveType(List<LogicalConnective> connectivesPool, int i)
        {
            return i == 0
                                    ? LogicalConnective.Empty
                                    : connectivesPool[random.Next(0, connectivesPool.Count)];
        }

        private Status CheckSatisfiability(List<IConstraintExpression> conditions)
        {
            // Check satisfiability
            if (conditions.Count == 0)
            {
                return Status.SATISFIABLE;
            }

            var conjunctedConditions = new List<BoolExpr>();
            conjunctedConditions.Add(conditions[0].GetSmtExpression(Context));
            var j = 0;
            foreach (var condition in conditions.Skip(1))
            {
                var expr = condition.GetSmtExpression(Context);
                if (condition.LogicalConnective == LogicalConnective.And)
                {
                    conjunctedConditions[j] = Context.MkAnd(conjunctedConditions[j], expr);
                }
                else
                {
                    conjunctedConditions.Add(expr);
                    j++;
                }
            }
            var guardExpression = Context.MkOr(conjunctedConditions);
            var solver = Context.MkSimpleSolver();
            var result = solver.Check(guardExpression); // Assert?
            return result;
        }

        private ConstraintVOVExpression GenerateVOVExpression(
            List<string> varsPool, 
            VariableType firstVariableType, 
            LogicalConnective logicalConnectiveType, 
            string variableName, 
            BinaryPredicate predicate)
        {
            var secondVariableName = varsPool[random.Next(0, varsPool.Count)];

            var vovExpression = new ConstraintVOVExpression
            {
                Predicate = predicate,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Real,
                    Name = variableName,
                    VariableType = firstVariableType
                },
                LogicalConnective = logicalConnectiveType,
                VariableToCompare = new ConstraintVariable
                {
                    Domain = DomainType.Real,
                    Name = secondVariableName,
                    VariableType = VariableType.Read
                }
            };
            return vovExpression;
        }

        private ConstraintVOCExpression<double> GenerateVOCExpression(
            List<int> constantsPool, 
            VariableType firstVariableType, 
            LogicalConnective logicalConnectiveType, 
            string variableName, 
            BinaryPredicate predicate)
        {
            var constant = constantsPool[random.Next(0, constantsPool.Count)];

            var vocExpression = new ConstraintVOCExpression<double>
            {
                Constant = new DefinableValue<double>(constant),
                Predicate = predicate,
                ConstraintVariable = new ConstraintVariable
                {
                    Domain = DomainType.Real,
                    Name = variableName,
                    VariableType = firstVariableType
                },
                LogicalConnective = logicalConnectiveType
            };
            return vocExpression;
        }

        private List<int> GetConditionsCountPerTransition(int transitionsCount, int conditionsCount)
        {
            var conditionsPerTransition = new int[transitionsCount];
                //new List<int>(transitionsCount);
            for (int i = 0; i < conditionsCount; i++)
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

            for (int i = 0; i < constsCount; i++)
            {
                constantsPool.Add(random.Next(-1000000, 1000001));
            }

            return constantsPool;
        }

        private List<string> GetVarsPool(int varsCount)
        {
            var varsPool = new List<string>(varsCount);

            for (int i = 0;i < varsCount; i++)
            {
                varsPool.Add($"v{i}");
            }

            return varsPool;
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
