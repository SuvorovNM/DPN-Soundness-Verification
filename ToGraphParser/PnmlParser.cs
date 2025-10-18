using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using DPN.Models;
using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Parsers
{
    public class PnmlParser
    {
        public XDocument Serialize(DataPetriNet dpn)
        {
            ArgumentNullException.ThrowIfNull(dpn);

            
            XElement dpnStructureElement = new XElement("page");
            XElement variablesElement = new XElement("variables");

            foreach (var domainType in Enum.GetValues<DomainType>())
            {
                foreach (var variable in dpn.Variables[domainType].GetKeys())
                {
                    var variableElement = new XElement("variable",
                        new XElement("name", variable));
                    variableElement.SetAttributeValue("type", domainType.ToString());
                    variablesElement.Add(variableElement);
                }
            }

            foreach (var place in dpn.Places)
            {
                var placeElement = new XElement("place",
                    new XElement("name",
                        new XElement("text", place.Label)));

                placeElement.SetAttributeValue("id", place.Id);

                if (place.Tokens > 0)
                {
                    var initialMarking = new XElement("initialMarking",
                        new XElement("text", place.Tokens));
                    placeElement.Add(initialMarking);
                }
                if (place.IsFinal)
                {
                    var finalMarking = new XElement("finalMarking",
                        new XElement("text", 1));
                    placeElement.Add(finalMarking);
                }
                dpnStructureElement.Add(placeElement);
            }

            var expressionSerializer = new Z3ExpressionSerializer();

            foreach (var transition in dpn.Transitions)
            {
                var transitionElement = new XElement("transition",
                    new XElement("name",
                        new XElement("text", transition.Label)));
                transitionElement.SetAttributeValue("id", transition.Id);

                if (!transition.Guard.ActualConstraintExpression.IsTrue)
                {
                    var stringExpression = expressionSerializer.Serialize(transition.Guard.ActualConstraintExpression);
                    transitionElement.SetAttributeValue("guard", stringExpression);
                }

                dpnStructureElement.Add(transitionElement);
            }

            int arcCounter = 0;
            foreach (var arc in dpn.Arcs)
            {
                var arcElement = new XElement("arc",
                   new XElement("weight",
                       new XElement("text", arc.Weight)));

                arcElement.SetAttributeValue("id", arcCounter++);
                arcElement.SetAttributeValue("source", arc.Source.Id);
                arcElement.SetAttributeValue("target", arc.Destination.Id);

                dpnStructureElement.Add(arcElement);
            }


            var srcTree = new XElement("pnml",
                new XElement("net",
                    new XElement("name",
                        new XElement("text", dpn.Name)),
                    dpnStructureElement,
                    variablesElement));

            var document = new XDocument(srcTree);

            return document;
        }

        public DataPetriNet Deserialize(XmlDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var context = new Context();
            var dpn = new DataPetriNet(context);
            var varTypeDict = new Dictionary<string, DomainType>();
            if (document.LastChild?.Name == "pnml")
            {
                var net = document.LastChild.FirstChild;
                if (net?.Name == "net")
                {
                    XmlNode? name = null;
                    XmlNode? variables = null;
                    XmlNode? page = null;

                    for (var i = 0; i < net.ChildNodes.Count; i++)
                    {
                        switch (net.ChildNodes[i]?.Name)
                        {
                            case "name":
                                name = net.ChildNodes[i];
                                break;
                            case "page":
                                page = net.ChildNodes[i];
                                break;
                            case "variables":
                                variables = net.ChildNodes[i];
                                break;
                            default:
                                break;
                        }
                    }

                    dpn.Name = name?.FirstChild?.InnerText ?? string.Empty;
                    AddVariablesToDpn(dpn, varTypeDict, variables);

                    var readWriteVariablesToTypes = varTypeDict
                        .SelectMany(kvp => new[] { (kvp.Key + "_r", kvp.Value), (kvp.Key + "_w", kvp.Value) })
                        .ToDictionary(kvp=>kvp.Item1, kvp=>kvp.Value);

                    var expressionParser = new Z3ExpressionParser(context, readWriteVariablesToTypes);
                    AddPetriNetElementsToDpn(dpn, page, context, expressionParser);
                }
            }

            return dpn;
        }

        private void AddPetriNetElementsToDpn(DataPetriNet dpn, XmlNode? page, Context context, Z3ExpressionParser expressionParser)
        {
            if (page != null && page.HasChildNodes)
            {
                for (var i = 0; i < page.ChildNodes.Count; i++)
                {
                    switch (page.ChildNodes[i]?.Name)
                    {
                        case "place":
                            dpn.Places.Add(GetPlaceFromXmlNode(page.ChildNodes[i]));
                            break;
                        // Transitions should be executed only after obtaining variables
                        case "transition":
                            dpn.Transitions.Add(GetTransitionFromXmlNode(page.ChildNodes[i], context, expressionParser));
                            break;
                        // Arcs must be executed only after adding all places
                        case "arc":
                            dpn.Arcs.Add(GetArcFromXmlNode(page.ChildNodes[i], dpn.Places.Select(x => x as Node).Union(dpn.Transitions)));
                            break;
                    }
                }
            }
        }

        private void AddVariablesToDpn(DataPetriNet dpn, Dictionary<string, DomainType> varTypeDict, XmlNode? variables)
        {
            if (variables != null && variables.HasChildNodes)
            {
                for (var i = 0; i < variables.ChildNodes.Count; i++)
                {
                    var variableNode = variables.ChildNodes[i];

                    var typeValue = GetDomainType(variableNode.Attributes["type"].Value);
                    var varName = variableNode.FirstChild?.InnerText ?? string.Empty;

                    varTypeDict.Add(varName, typeValue);
                    dpn.Variables[typeValue].Write(varName, GetDefaultValue(typeValue));
                }
            }
        }

        private Place GetPlaceFromXmlNode(XmlNode placeNode)
        {
            if (placeNode == null)
            {
                throw new ArgumentNullException(nameof(placeNode));
            }

            var place = new Place();
            place.Id = placeNode.Attributes["id"].Value;

            for (var i = 0; i < placeNode.ChildNodes.Count; i++)
            {
                switch (placeNode.ChildNodes[i]?.Name)
                {
                    case "name":
                        place.Label = placeNode.ChildNodes[i]?.FirstChild?.InnerText ?? string.Empty;
                        break;
                    case "graphics":
                        break; // Graphics are not supported by current state of affairs
                    case "initialMarking":
                        place.Tokens = int.Parse(placeNode.ChildNodes[i]?.FirstChild?.InnerText ?? "0");
                        break;
                    case "finalMarking":
                        place.IsFinal = int.Parse(placeNode.ChildNodes[i]?.FirstChild?.InnerText ?? "0") > 0;
                        break;
                    default:
                        break;
                }
            }

            return place;
        }

        private Transition GetTransitionFromXmlNode(XmlNode transitionNode, Context context, Z3ExpressionParser expressionParser)
        {
            var transitionId = transitionNode.Attributes["id"].Value;
            var transitionName = string.Empty;

            var expressionString = transitionNode.Attributes["guard"]?.Value;
            var smtExpression = expressionParser.Parse(expressionString);
            for (var i = 0; i < transitionNode.ChildNodes.Count; i++)
            {
                switch (transitionNode.ChildNodes[i]?.Name)
                {
                    case "name":
                        transitionName = transitionNode.ChildNodes[i]?.FirstChild?.InnerText ?? string.Empty;
                        break;
                    case "graphics":
                        break; // Graphics are not supported by current state of affairs
                }
            }

            return new Transition(transitionId, new Guard(context, smtExpression)) { Label = transitionName };
        }

        private Arc GetArcFromXmlNode(XmlNode arcNode, IEnumerable<Node> dpnNodes)
        {
            if (arcNode is null)
            {
                throw new ArgumentNullException(nameof(arcNode));
            }

            var sourceId = arcNode.Attributes["source"].Value;
            var targetId = arcNode.Attributes["target"].Value;

            var sourcePlace = dpnNodes.FirstOrDefault(x => x.Id == sourceId);
            var targetPlace = dpnNodes.FirstOrDefault(x => x.Id == targetId);
            if (sourcePlace == null || targetPlace == null)
            {
                throw new ArgumentException($"Arc cannot be build, at least one place is not found: {sourceId}, {targetId}");
            }

            var weight = 1;
            for (var i = 0; i < arcNode.ChildNodes.Count; i++)
            {
                if (arcNode.ChildNodes[i]?.Name == "name" || arcNode.ChildNodes[i]?.Name == "weight") // for backward compatibility
                {
                    weight = int.Parse(arcNode.ChildNodes[i]?.FirstChild?.InnerText ?? "1");
                }
            }

            return new Arc(sourcePlace, targetPlace, weight);
        }

        private IConstraintExpression FormConstraint(Dictionary<string, DomainType> varTypeDict, string constraint)
        {
            var trimmedConstraint = constraint.Trim();
            var constraintBlocks = trimmedConstraint.Split(" ");
            if (constraintBlocks.Length == 3)
            {
                var variableName = constraintBlocks[0][0..^2];
                var variableType = constraintBlocks[0].EndsWith("_r")
                    ? VariableType.Read
                    : VariableType.Written;

                if (constraintBlocks[2].EndsWith("_r") || constraintBlocks[2].EndsWith("_w"))
                {
                    var secondVariableName = constraintBlocks[2][0..^2];
                    var secondVariableType = constraintBlocks[2].EndsWith("_r")
                    ? VariableType.Read
                    : VariableType.Written;

                    return MakeVOVExpression(varTypeDict[variableName],
                        GetBinaryPredicate(constraintBlocks[1]),
                        variableName,
                        variableType,
                        secondVariableName,
                        secondVariableType);
                }
                return varTypeDict[variableName] switch
                {
                    DomainType.Integer =>
                        MakeVOCConstraint(
                        varTypeDict[variableName],
                        GetBinaryPredicate(constraintBlocks[1]),
                        variableName,
                        variableType,
                        long.Parse(constraintBlocks[2])),
                    DomainType.Real =>
                        MakeVOCConstraint(
                        varTypeDict[variableName],
                        GetBinaryPredicate(constraintBlocks[1]),
                        variableName,
                        variableType,
                        double.Parse(constraintBlocks[2], CultureInfo.InvariantCulture)),
                    DomainType.Boolean =>
                        MakeVOCConstraint(
                        varTypeDict[variableName],
                        GetBinaryPredicate(constraintBlocks[1]),
                        variableName,
                        variableType,
                        bool.Parse(constraintBlocks[2])),
                    _ => throw new ArgumentException($"Expressions for the type {varTypeDict[variableName]} are not supported")
                };
            }

            throw new Exception("Incorrect constraint: " + trimmedConstraint);
        }

        private ConstraintVOCExpression<T> MakeVOCConstraint<T>(
            DomainType domainType,
            BinaryPredicate predicate,
            string variableName,
            VariableType variableType,
            T typedValue)
            where T : IEquatable<T>, IComparable<T>
        {
            return new ConstraintVOCExpression<T>
            {
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = variableType,
                    Name = variableName,
                    Domain = domainType
                },
                LogicalConnective = LogicalConnective.And,
                Constant = new DefinableValue<T>(typedValue),
                Predicate = predicate
            };
        }

        private ConstraintVOVExpression MakeVOVExpression(
            DomainType domainType,
            BinaryPredicate predicate,
            string firstVariableName,
            VariableType firstVariableType,
            string secondVariableName,
            VariableType secondVariableType)
        {
            return new ConstraintVOVExpression
            {
                ConstraintVariable = new ConstraintVariable
                {
                    VariableType = firstVariableType,
                    Name = firstVariableName,
                    Domain = domainType
                },
                LogicalConnective = LogicalConnective.And,
                Predicate = predicate,
                VariableToCompare = new ConstraintVariable
                {
                    VariableType = secondVariableType,
                    Name = secondVariableName,
                    Domain = domainType
                }
            };
        }

        private BinaryPredicate GetBinaryPredicate(string predicateBlock)
        {
            return predicateBlock switch
            {
                "==" => BinaryPredicate.Equal,
                "!=" => BinaryPredicate.Unequal,
                "<" => BinaryPredicate.LessThan,
                ">" => BinaryPredicate.GreaterThan,
                "<=" => BinaryPredicate.LessThanOrEqual,
                ">=" => BinaryPredicate.GreaterThanOrEqual,
                _ => throw new Exception("Unsupported predicate type: " + predicateBlock)
            };
        }

        private DomainType GetDomainType(string typeAsString)
        {
            return typeAsString switch
            {
                "Real" or "Double" => DomainType.Real,
                "Integer" or "Int" => DomainType.Integer,
                "Boolean" => DomainType.Boolean,
                _ => throw new Exception($"Cannot select a type for {typeAsString}")
            };
        }

        private IDefinableValue GetDefaultValue(DomainType domain)
        {
            return domain switch
            {
                DomainType.Real => new DefinableValue<double>(0),
                DomainType.Integer => new DefinableValue<long>(0),
                DomainType.Boolean => new DefinableValue<bool>(false),
                _ => throw new Exception($"Domain type {domain} is not supported")
            };
        }
    }
}
