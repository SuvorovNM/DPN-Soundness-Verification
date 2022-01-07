using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DataPetriNetOnSmt.Tests
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
            variables[DomainType.Integer].Write("remained", new DefinableValue<long>(10000));
            variables[DomainType.Real].Write("test_real", new DefinableValue<double>(1111.444));
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
                            new ConstraintVOCExpression<long>
                            {
                                Constant = new DefinableValue<long>(1000),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "amount",
                                    VariableType = VariableType.Written
                                }
                            },
                            new ConstraintVOVExpression
                            {
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "remained",
                                    VariableType = VariableType.Written
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "remained",
                                    VariableType = VariableType.Read
                                }
                            },
                            new ConstraintVOCExpression<double>
                            {
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "test_real",
                                    VariableType = VariableType.Written
                                },
                                Constant = new DefinableValue<double>(4444.111)
                            }
                        }
                    },
                },
                new Transition
                {
                    Label = "Verify",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
                            {
                                Constant = new DefinableValue<bool>(true),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            }/*,
                            new ConstraintVOCExpression<bool>
                            {
                                Constant = new DefinableValue<bool>(false),
                                LogicalConnective = LogicalConnective.Or,
                                Predicate = BinaryPredicate.Equal,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType = VariableType.Written
                                }
                            }*/
                        }
                    },
                },
                new Transition
                {
                    Label = "Skip assessment",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                },
                new Transition
                {
                    Label = "Simple assessment",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<long>
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
                },
                new Transition
                {
                    Label = "Advanced assessment",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<long>
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
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<long>
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
                            new ConstraintVOCExpression<bool>
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
                    Guard = new Guard()
                },
                new Transition
                {
                    Label = "Inform acceptance customer normal",
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<long>
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
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<long>
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
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                            new ConstraintVOCExpression<long>
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
                    Guard = new Guard
                    {
                        ConstraintExpressions = new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<bool>
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
                    Guard = new Guard()
                }
            };

            var arcsList = new List<Arc>
            {
                new Arc(placesList[0], transitionList[0]),
                new Arc(transitionList[0], placesList[1]),
                new Arc(placesList[1], transitionList[1]),
                new Arc(transitionList[1], placesList[2]),
                new Arc(placesList[2], transitionList[2]),
                new Arc(placesList[2], transitionList[3]),
                new Arc(placesList[2], transitionList[4]),
                new Arc(transitionList[2], placesList[3]),
                new Arc(transitionList[3], placesList[3]),
                new Arc(transitionList[4], placesList[3]),
                new Arc(placesList[3], transitionList[5]),
                new Arc(transitionList[5], placesList[1]),
                new Arc(placesList[3], transitionList[6]),
                new Arc(transitionList[6], placesList[4]),
                new Arc(transitionList[6], placesList[5]),
                new Arc(placesList[4], transitionList[7]),
                new Arc(placesList[4], transitionList[8]),
                new Arc(placesList[4], transitionList[9]),
                new Arc(transitionList[7], placesList[6]),
                new Arc(transitionList[8], placesList[6]),
                new Arc(transitionList[9], placesList[6]),
                new Arc(transitionList[5], placesList[5]),
                new Arc(placesList[5], transitionList[10]),
                new Arc(transitionList[10], placesList[7]),
                new Arc(placesList[6], transitionList[11]),
                new Arc(placesList[7], transitionList[11]),
                new Arc(transitionList[11], placesList[8]),
            };

            dataPetriNet = new DataPetriNet
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables,
                Arcs = arcsList
            };
        }

        [TestMethod]        
        public void RunDPN()
        {
            bool canMakeNextStep;
            do
            {
                canMakeNextStep = dataPetriNet.MakeStep();
            } while (canMakeNextStep);
        }
    }
}
