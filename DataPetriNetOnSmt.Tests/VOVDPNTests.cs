using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetParsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DataPetriNetOnSmt.Tests
{
    [TestClass]
    public class VOVDPNTests
    {
        private DataPetriNet dataPetriNet;
        [TestInitialize]
        public void Initialize()
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
            variables[DomainType.Integer].Write("reqd", new DefinableValue<long>(0));
            variables[DomainType.Integer].Write("granted", new DefinableValue<long>(0));
            variables[DomainType.Boolean].Write("ok", new DefinableValue<bool>(false));

            var transitionList = new List<Transition>
            {
                new Transition("Credit request", new Guard(context, new List<IConstraintExpression>
                        {
                            new ConstraintVOCExpression<long>
                            {
                                Constant = new DefinableValue<long>(0),
                                LogicalConnective = LogicalConnective.Empty,
                                Predicate = BinaryPredicate.GreaterThan,
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
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
                                    Domain = DomainType.Integer,
                                    Name = "granted",
                                    VariableType = VariableType.Written
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
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
                                    Domain = DomainType.Integer,
                                    Name = "granted",
                                    VariableType = VariableType.Read
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
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
                                    Domain = DomainType.Integer,
                                    Name = "reqd",
                                    VariableType = VariableType.Written
                                },
                                VariableToCompare = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
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
                            new ConstraintVOCExpression<long>
                            {
                                ConstraintVariable = new ConstraintVariable
                                {
                                    Domain = DomainType.Integer,
                                    Name = "granted",
                                    VariableType = VariableType.Read
                                },
                                Constant = new DefinableValue<long>(10000),
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

            dataPetriNet = new DataPetriNet(context)
            {
                Places = placesList,
                Transitions = transitionList,
                Variables = variables,
                Arcs = arcsList
            };
        }

        [TestMethod]
        public void BuildConstraintGraphForBanking()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var constraintGraph = new ConstraintGraph(dataPetriNet);
            //ConstraintExpressionServiceForRealsWithManualConcat

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            constraintGraph.GenerateGraph();
            stopwatch.Stop();
            var resultTime = stopwatch.Elapsed;

            File.WriteAllText("VOV_man.txt", resultTime.ToString());
            var typedStates = SoundnessAnalyzer.GetStatesDividedByTypes(constraintGraph, new[] { dataPetriNet.Places[^1] });

            Assert.AreEqual(69, constraintGraph.ConstraintStates.Count);
            Assert.AreEqual(88, constraintGraph.ConstraintArcs.Count);

            Assert.AreEqual(1, typedStates[StateType.Initial].Count);
            Assert.AreEqual(13, typedStates[StateType.Deadlock].Count);
            Assert.AreEqual(0, typedStates[StateType.UncleanFinal].Count);
            Assert.AreEqual(26, typedStates[StateType.NoWayToFinalMarking].Count);
            Assert.AreEqual(2, typedStates[StateType.CleanFinal].Count);
            Assert.AreEqual(40, typedStates[StateType.SoundIntermediate].Count);
        }

        [TestMethod]
        public void BuildConstraintGraphForNewBanking()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var pnmlParser = new PnmlParser();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("VOV-expressions.pnmlx");

            var dpn = pnmlParser.DeserializeDpn(xDoc);

            var constraintGraph = new ConstraintGraph(dpn);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            constraintGraph.GenerateGraph();
            stopwatch.Stop();
            var resultTime = stopwatch.Elapsed;

            File.WriteAllText("VOV_new_man.txt", resultTime.ToString());

            var typedStates = SoundnessAnalyzer.GetStatesDividedByTypes(constraintGraph, new[] { dpn.Places[^1] });
        }
    }
}
