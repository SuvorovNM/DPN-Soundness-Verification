using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using DPN.Models;
using DPN.Models.Abstractions;
using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Parsers
{
	public class PnmlxParser
	{
		private const string xsdSchema = "XsdSchemas\\pnmlx.xsd";
		private readonly XsdValidator validator = new(xsdSchema);

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

		public DataPetriNet Deserialize(XDocument document)
		{
			ArgumentNullException.ThrowIfNull(document);

			var validationResult = validator.Validate(document);
			if (!validationResult.IsValid)
			{
				var errorText = string.Join(Environment.NewLine, validationResult.Errors.Select(e => $"{e.Severity.ToString()}: {e.Message}"));
				throw new SerializationException("Error occurred on deserializing:\n" + errorText);
			}

			var context = new Context();
			var dpn = new DataPetriNet(context);
			var varTypeDict = new Dictionary<string, DomainType>();

			var pnmlElement = document.Root;
			if (pnmlElement?.Name == "pnml")
			{
				var netElement = pnmlElement.Element("net");
				if (netElement != null)
				{
					XElement? nameElement = null;
					XElement? variablesElement = null;
					XElement? pageElement = null;

					foreach (var element in netElement.Elements())
					{
						switch (element.Name.LocalName)
						{
							case "name":
								nameElement = element;
								break;
							case "page":
								pageElement = element;
								break;
							case "variables":
								variablesElement = element;
								break;
							default:
								break;
						}
					}

					dpn.Name = nameElement?.Element("text")?.Value ?? nameElement?.Value ?? string.Empty;
					AddVariablesToDpn(dpn, varTypeDict, variablesElement);

					var readWriteVariablesToTypes = varTypeDict
						.SelectMany(kvp => new[] { (kvp.Key + "_r", kvp.Value), (kvp.Key + "_w", kvp.Value) })
						.ToDictionary(kvp => kvp.Item1, kvp => kvp.Value);

					var expressionParser = new Z3ExpressionParser(context, readWriteVariablesToTypes);
					AddPetriNetElementsToDpn(dpn, pageElement, context, expressionParser);
				}
			}

			return dpn;
		}

		private void AddPetriNetElementsToDpn(DataPetriNet dpn, XElement? pageElement, Context context, Z3ExpressionParser expressionParser)
		{
			if (pageElement != null)
			{
				foreach (var element in pageElement.Elements())
				{
					switch (element.Name.LocalName)
					{
						case "place":
							dpn.Places.Add(GetPlaceFromXElement(element));
							break;
						// Transitions should be executed only after obtaining variables
						case "transition":
							dpn.Transitions.Add(GetTransitionFromXElement(element, context, expressionParser));
							break;
						// Arcs must be executed only after adding all places
						case "arc":
							dpn.Arcs.Add(GetArcFromXElement(element, dpn.Places.Select(x => x as Node).Union(dpn.Transitions)));
							break;
					}
				}
			}
		}

		private void AddVariablesToDpn(DataPetriNet dpn, Dictionary<string, DomainType> varTypeDict, XElement? variablesElement)
		{
			if (variablesElement != null)
			{
				foreach (var variableElement in variablesElement.Elements("variable"))
				{
					var typeAttribute = variableElement.Attribute("type");
					var typeValue = GetDomainType(typeAttribute?.Value ?? "Integer");
					var varName = variableElement.Element("name")?.Value ?? variableElement.Value ?? string.Empty;

					varTypeDict.Add(varName, typeValue);
					dpn.Variables[typeValue].Write(varName, GetDefaultValue(typeValue));
				}
			}
		}

		private Place GetPlaceFromXElement(XElement placeElement)
		{
			if (placeElement == null)
			{
				throw new ArgumentNullException(nameof(placeElement));
			}

			var place = new Place();
			var idAttribute = placeElement.Attribute("id");
			place.Id = idAttribute?.Value ?? throw new ArgumentException("Place element must have an id attribute");

			foreach (var element in placeElement.Elements())
			{
				switch (element.Name.LocalName)
				{
					case "name":
						place.Label = element.Element("text")?.Value ?? element.Value ?? string.Empty;
						break;
					case "graphics":
						break; // Graphics are not supported currently
					case "initialMarking":
						var tokensAttribute = element.Attribute("tokens");
						var tokensValue = tokensAttribute?.Value ?? element.Element("text")?.Value ?? element.Value ?? "0";
						place.Tokens = int.Parse(tokensValue);
						break;
					case "finalMarking":
						var finalTokensAttribute = element.Attribute("tokens");
						var finalTokensValue = finalTokensAttribute?.Value ?? element.Element("text")?.Value ?? element.Value ?? "0";
						place.IsFinal = int.Parse(finalTokensValue) > 0;
						break;
					default:
						break;
				}
			}

			return place;
		}

		private Transition GetTransitionFromXElement(XElement transitionElement, Context context, Z3ExpressionParser expressionParser)
		{
			var idAttribute = transitionElement.Attribute("id");
			var transitionId = idAttribute?.Value ?? throw new ArgumentException("Transition element must have an id attribute");

			var guardAttribute = transitionElement.Attribute("guard");
			var expressionString = guardAttribute?.Value;
			var smtExpression = expressionParser.Parse(expressionString);

			var transitionName = string.Empty;
			var nameElement = transitionElement.Element("name");
			if (nameElement != null)
			{
				transitionName = nameElement.Element("text")?.Value ?? nameElement.Value ?? string.Empty;
			}

			return new Transition(transitionId, new Guard(context, smtExpression)) { Label = transitionName };
		}

		private Arc GetArcFromXElement(XElement arcElement, IEnumerable<Node> dpnNodes)
		{
			if (arcElement == null)
			{
				throw new ArgumentNullException(nameof(arcElement));
			}

			var sourceAttribute = arcElement.Attribute("source");
			var targetAttribute = arcElement.Attribute("target");

			var sourceId = sourceAttribute?.Value ?? throw new ArgumentException("Arc element must have a source attribute");
			var targetId = targetAttribute?.Value ?? throw new ArgumentException("Arc element must have a target attribute");

			var sourcePlace = dpnNodes.FirstOrDefault(x => x.Id == sourceId);
			var targetPlace = dpnNodes.FirstOrDefault(x => x.Id == targetId);
			if (sourcePlace == null || targetPlace == null)
			{
				throw new ArgumentException($"Arc cannot be built, at least one node is not found: {sourceId}, {targetId}");
			}

			var weight = 1;
			var nameElement = arcElement.Element("name") ?? arcElement.Element("weight"); // for backward compatibility
			if (nameElement != null)
			{
				var weightValue = nameElement.Element("text")?.Value ?? nameElement.Value ?? "1";
				weight = int.Parse(weightValue);
			}

			return new Arc(sourcePlace, targetPlace, weight);
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