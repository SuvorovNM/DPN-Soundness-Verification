using DataPetriNet.Abstractions;
using DataPetriNet.DPNElements;
using DataPetriNet.Enums;
using DataPetriNet.SoundnessVerification;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DataPetriNet.Tests
{
    [TestClass]
    public class DPNTests
    {
        private DataPetriNet dataPetriNet;
        [TestInitialize]
        public void Initialize()
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
                            /*new ConstraintExpression<long>
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
                            }*/
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
                },
                new Transition
                {
                    Label = "Advanced assessment",
                    PreSetPlaces = new List<Place>{placesList[2]},
                    PostSetPlaces = new List<Place>{placesList[3]},
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
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Renegotiate request",
                    PreSetPlaces = new List<Place>{placesList[3]},
                    PostSetPlaces = new List<Place>{placesList[0]},
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long>(5000),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            },
                            new ConstraintExpression<bool>
                            {
                                Constant = new DefinableValue<bool>(false),
                                LogicalConnective = LogicalConnective.And,
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
                    Label = "AND split",
                    PreSetPlaces = new List<Place>{placesList[3]},
                    PostSetPlaces = new List<Place> {placesList[4], placesList[5]},
                    Guard = new Guard()
                },
                new Transition
                {
                    Label = "Inform acceptance customer normal",
                    PreSetPlaces = new List<Place>{placesList[4]},
                    PostSetPlaces = new List<Place> {placesList[6]},
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
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long>(10000),
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
                },
                new Transition
                {
                    Label = "Inform acceptance customer VIP",
                    PreSetPlaces = new List<Place>{placesList[4]},
                    PostSetPlaces = new List<Place> {placesList[6]},
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
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long>(10000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Inform acceptance customer VIP",
                    PreSetPlaces = new List<Place>{placesList[4]},
                    PostSetPlaces = new List<Place> {placesList[6]},
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
                            },
                            new ConstraintExpression<long>
                            {
                                Constant = new DefinableValue<long>(10000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "Inform acceptance customer VIP",
                    PreSetPlaces = new List<Place>{placesList[5]},
                    PostSetPlaces = new List<Place> {placesList[7]},
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
                            }
                        }
                    }
                },
                new Transition
                {
                    Label = "AND join",
                    PreSetPlaces = new List<Place>{placesList[6], placesList[7]},
                    PostSetPlaces = new List<Place> {placesList[8]},
                    Guard = new Guard()
                }
            };

            dataPetriNet = new DataPetriNet
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables
            };
        }

        //[TestMethod]        
        public void RunDPN()
        {          
            bool canMakeNextStep;
            do
            {
                canMakeNextStep = dataPetriNet.MakeStep();
            } while (canMakeNextStep);
        }

        [TestMethod]
        public void BuildConstraintGraphForBanking()
        {
            var constraintGraph = new ConstraintGraph(dataPetriNet);

            constraintGraph.GenerateGraph();

            var analysis = new ConstraintGraphAnalyzer().GetStatesDividedByTypes(constraintGraph, new[] { dataPetriNet.Places[^1] });
        }
    }
}
