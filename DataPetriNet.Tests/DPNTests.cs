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
            variables[DomainType.Integer].Write("amount", new DefinableValue<long>(0));
            variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

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
                                Constant = new DefinableValue<long>(0),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Written
                                }
                            },
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long>(5000),
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
                    },
                    PreSetPlaces = new List<Place>{placesList[0]},
                    PostSetPlaces = new List<Place>{placesList[1]}
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
                                Constant = new DefinableValue<bool>(),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Unequal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            }
                        }
                    },
                    PreSetPlaces = new List<Place>{placesList[1]},
                    PostSetPlaces = new List<Place>{placesList[2]}
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
                                Constant = new DefinableValue<bool>(false),
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
                    },
                    PreSetPlaces = new List<Place>{placesList[2]},
                    PostSetPlaces = new List<Place>{placesList[3]}
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
                                Constant = new DefinableValue<bool>(true),
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
                                Constant = new DefinableValue<bool>(true),
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
                                Constant = new DefinableValue<long>(5000),
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
                    },
                    PreSetPlaces = new List<Place>{placesList[2]},
                    PostSetPlaces = new List<Place>{placesList[3]}
                }
            };

            var dpn = new DataPetriNet
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables
            };

            bool canMakeNextStep;
            do
            {
                canMakeNextStep = dpn.MakeStep();
            } while (canMakeNextStep);
        }

    }
}
