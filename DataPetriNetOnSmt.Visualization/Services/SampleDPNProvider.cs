﻿using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System.Collections.Generic;

namespace DataPetriNetOnSmt.Visualization.Services
{
    public class SampleDPNProvider
    {
        public DataPetriNet GetVOVDataPetriNet()
        {
            var context = new Context();
            var placesList = new List<Place>
            {
                new Place("i", PlaceType.Initial),
                new Place("p1", PlaceType.Intermediary),
                new Place("p2", PlaceType.Intermediary),
                new Place("p3", PlaceType.Intermediary),
                new Place("p4", PlaceType.Intermediary),
                new Place("p5", PlaceType.Intermediary),
                new Place("p6", PlaceType.Intermediary),
                new Place("p7", PlaceType.Intermediary),
                new Place("p8", PlaceType.Intermediary),
                new Place("p9", PlaceType.Intermediary),
                new Place("o", PlaceType.Final)
            };

            var variables = new VariablesStore();
            variables[DomainType.Real].Write("reqd", new DefinableValue<double>(0));
            variables[DomainType.Real].Write("granted", new DefinableValue<double>(0));
            variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

            var transitionList = new List<Transition>
            {
                new Transition("Credit request", new Guard(context, new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(0),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "reqd",
                                    VariableType = VariableType.Written
                                }
                            }
                        })),
                new Transition("Verify", new Guard(context, new List<IConstraintExpression>
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
                            },
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
                            }
                    }
                )),
                new Transition("Prepare", new Guard(context, new List<IConstraintExpression>
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
                        })),
                new Transition("Skip", new Guard(context, new List<IConstraintExpression>{
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
                        })),
                new Transition("Make proposal", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOVExpression
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "granted",
                                    VariableType = VariableType.Written
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "reqd",
                                    VariableType = VariableType.Read
                                },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.LessThanOrEqual
                            }
                        })),
                new Transition("Refuse proposal", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOVExpression
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "granted",
                                    VariableType = VariableType.Read
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "reqd",
                                    VariableType = VariableType.Read
                                },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Unequal
                            }
                        })),
                new Transition("Update request", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOVExpression
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "reqd",
                                    VariableType = VariableType.Written
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "reqd",
                                    VariableType = VariableType.Read
                                },
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.LessThan
                            }
                        })),
                new Transition("AND split",new Guard(context)),
                new Transition("Inform acceptance VIP", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOCExpression<double>
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "granted",
                                    VariableType = VariableType.Read
                                },
                                Constant = new DefinableValue<double>(10000),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThan
                            }
                        })),
                new Transition("Inform rejection", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOCExpression<bool>
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType= VariableType.Read
                                },
                                Constant = new DefinableValue<bool>(false),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal
                            }
                        })),
                new Transition("Open credit loan", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOCExpression<bool>
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Boolean,
                                    Name = "ok",
                                    VariableType= VariableType.Read
                                },
                                Constant = new DefinableValue<bool>(true),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.Equal
                            }
                        })),
                new Transition("AND join",new Guard(context)),
            };

            var arcsList = new List<Arc>
            {
                new Arc(placesList[0], transitionList[0]),
                new Arc(transitionList[0], placesList[1]),
                new Arc(placesList[1], transitionList[1]),
                new Arc(transitionList[1], placesList[2]),
                new Arc(placesList[2], transitionList[2]),
                new Arc(placesList[2], transitionList[3]),
                new Arc(transitionList[3], placesList[3]),
                new Arc(transitionList[2],placesList[8]),
                new Arc(placesList[8],transitionList[4]),
                new Arc(transitionList[4], placesList[3]),
                new Arc(placesList[3], transitionList[5]),
                new Arc(transitionList[5], placesList[9]),
                new Arc(placesList[9], transitionList[6]),
                new Arc(transitionList[6],placesList[1]),
                new Arc(placesList[3],transitionList[7]),
                new Arc(transitionList[7],placesList[4]),
                new Arc(transitionList[7], placesList[5]),
                new Arc(placesList[4], transitionList[8]),
                new Arc(placesList[4], transitionList[9]),
                new Arc(transitionList[8], placesList[6]),
                new Arc(transitionList[9], placesList[6]),
                new Arc(placesList[5], transitionList[10]),
                new Arc(transitionList[10], placesList[7]),
                new Arc(placesList[6], transitionList[11]),
                new Arc(placesList[7], transitionList[11]),
                new Arc(transitionList[11], placesList[10]),
            };

            return new DataPetriNet(context)
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables,
                Arcs = arcsList
            };
        }

        public DataPetriNet GetVOCDataPetriNet()
        {
            var context = new Context();
            var placesList = new List<Place>
            {
                new Place("i", PlaceType.Initial),
                new Place("p1", PlaceType.Intermediary),
                new Place("p2", PlaceType.Intermediary),
                new Place("p3", PlaceType.Intermediary),
                new Place("p4", PlaceType.Intermediary),
                new Place("p5", PlaceType.Intermediary),
                new Place("p6", PlaceType.Intermediary),
                new Place("p7", PlaceType.Intermediary),
                new Place("o", PlaceType.Final)
            };

            var variables = new VariablesStore();
            variables[DomainType.Real].Write("amount", new DefinableValue<double>(0));
            variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

            var transitionList = new List<Transition>
            {
                new Transition("Credit request", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(0),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Written
                                }
                            }
                        })),
                new Transition("Verify", new Guard(context, new List<IConstraintExpression>
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
                            },
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
                            }
                        })),
                new Transition("Skip assessment", new Guard(context, new List<IConstraintExpression>
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
                        })),
                new Transition("Simple assessment", new Guard(context, new List<IConstraintExpression>
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
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(5000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.LessThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        })),
                new Transition("Advanced assessment", new Guard(context, new List<IConstraintExpression>
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
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(5000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        })),
                new Transition("Renegotiate request", new Guard(context, new List<IConstraintExpression>
                {
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(15000),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
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
                        })),
                new Transition("AND split",new Guard(context)),
                new Transition("Inform acceptance customer normal", new Guard(context, new List<IConstraintExpression>
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
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(10000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.LessThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        })),
                new Transition("Inform acceptance customer VIP", new Guard(context, new List<IConstraintExpression>
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
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(10000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        })),
                new Transition("Inform rejection customer VIP", new Guard(context, new List<IConstraintExpression>
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
                            new ConstraintVOCExpression<double>
                            {
                                Constant = new DefinableValue<double>(10000),
                                LogicalConnective = LogicalConnective.And,
                                Predicate = BinaryPredicate.GreaterThanOrEqual,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Real,
                                    Name = "amount",
                                    VariableType = VariableType.Read
                                }
                            }
                        })),
                new Transition("Open credit loan", new Guard(context, new List<IConstraintExpression>
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
                        })),
                new Transition("AND join",new Guard(context))
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
                new Arc(placesList[5], transitionList[10]),
                new Arc(transitionList[10], placesList[7]),
                new Arc(placesList[6], transitionList[11]),
                new Arc(placesList[7], transitionList[11]),
                new Arc(transitionList[11], placesList[8]),
            };

            return new DataPetriNet(context)
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables,
                Arcs = arcsList
            };
        }
    }
}
