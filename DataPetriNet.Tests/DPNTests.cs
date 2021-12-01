using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DataPetriNet.Tests
{
    [TestClass]
    public class DPNTests
    {
        [TestMethod]
        public void RunDPN()
        {
            var placesList = new List<Place>
            {
                new Place
                {
                    Label = "i",
                    Tokens = 1
                },
                new Place
                {
                    Label = "p1"
                },
                new Place
                {
                    Label = "p2"
                },
                new Place
                {
                    Label = "p3"
                },
                new Place
                {
                    Label = "p4"
                },
                new Place
                {
                    Label = "p5"
                },
                new Place
                {
                    Label = "p6"
                },
                new Place
                {
                    Label = "p7"
                },
                new Place
                {
                    Label = "o"
                }
            };

            var variables = new VariablesStore();
            variables.WriteInteger("amount", new DefinableValue<long> { Value = 0 });
            variables.WriteBool("ok", new DefinableValue<bool> { Value = false });

            var transitionList = new List<Transition>
            {
                new Transition
                {
                    Label = "Credit request",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long> { Value = 0 },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThenOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Written
                                }
                            },
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long> { Value =5000 },
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.LessThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Written
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Verify",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool> { Value = true },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            },
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool> { Value = false },
                                LogicalConnective = LogicalConnective.Or,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Skip assessment",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool> { Value = false },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Read
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Simple assessment",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool> { Value = true },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Read
                                }
                            },
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool> { Value = true },
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            },
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long> { Value =5000 },
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.LessThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        }
                    }
                }
            };

            var dpn = new DataPetriNet
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables,
                PreSetDictionary = new Dictionary<Transition, List<Place>>
                {
                    [transitionList[0]] = new List<Place> { placesList[0] },
                    [transitionList[1]] = new List<Place> { placesList[1] },
                    [transitionList[2]] = new List<Place> { placesList[2] },
                    [transitionList[3]] = new List<Place> { placesList[2] }
                },
                PostSetDictionary = new Dictionary<Transition, List<Place>>
                {
                    [transitionList[0]] = new List<Place> { placesList[1] },
                    [transitionList[1]] = new List<Place> { placesList[2] },
                    [transitionList[2]] = new List<Place> { placesList[3] },
                    [transitionList[3]] = new List<Place> { placesList[3] }
                }
            };

            bool canMakeNextStep;
            do
            {
                canMakeNextStep = dpn.MakeStep();
            } while (canMakeNextStep);
        }
    }
}
