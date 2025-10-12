using System.Xml.Linq;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.SoundnessVerification;
using DPN.SoundnessVerification.TransitionSystems;
using Microsoft.Z3;

namespace DPN.Parsers
{
	public class AsmlParser
	{
		private const string rootElementName = "cgml";
		private const string stateSpaceElementName = "state_space";
		private const string statesElementName = "states";
		private const string arcsElementName = "arcs";
		private const string transitionsElementName = "transitions";
		private const string finalMarkingElementName = "final_marking";
		private const string tokensElementName = "tokens";
		private const string constraintElementName = "constraint";
		private const string stateElementName = "state";
		private const string arcElementName = "arc";
		private const string placeElementName = "place";
		private const string transitionElementName = "transition";
		private const string nameElementName = "name";
		private const string textElementName = "text";
		private const string variablesElementName = "variables";
		private const string variableElementName = "variable";
		private const string idAttributeName = "id";
		private const string labelAttributeName = "label";
		private const string isSilentAttributeName = "is_silent";
		private const string sourceIdAttributeName = "source_id";
		private const string targetIdAttributeName = "target_id";
		private const string baseTransitionIdAttributeName = "base_transition_id";
		private const string tokensAttributeName = "tokens";
		private const string isTauAttributeName = "is_tau";
		private const string isSplitAttributeName = "is_split";
		private const string guardAttributeName = "guard";
		private const string typeAttributeName = "type";
		private const string graphTypeAttributeName = "graph_type";
		private const string isFullAttributeName = "is_full";

		public StateSpaceAbstraction Deserialize(XDocument document)
		{
			ArgumentNullException.ThrowIfNull(document);
			var cgElement = document.Root?.Element(stateSpaceElementName);
			if (cgElement == null)
				throw new Exception("Document is incorrect. Can't find <state_space> tag");

			// Deserialize States
			var statesElement = cgElement.Element(statesElementName);
			var nodes = new List<StateSpaceNode>();

			// Deserialize Variables
			var variablesElement = cgElement.Element(variablesElementName);
			var typedVariables = new Dictionary<string, DomainType>();
			foreach (var variableElem in variablesElement.Elements(variableElementName))
			{
				var name = variableElem.Element(nameElementName)?.Value ?? "";
				var typeStr = variableElem.Attribute(typeAttributeName)?.Value ?? "Integer";
				if (Enum.TryParse<DomainType>(typeStr, out var domainType))
				{
					typedVariables[name + "_r"] = domainType;
					typedVariables[name + "_w"] = domainType;
				}
			}

			var context = new Context();
			var expressionParser = new Z3ExpressionParser(context, typedVariables);

			foreach (var xmlState in statesElement.Elements(stateElementName))
			{
				var id = int.Parse(xmlState.Attribute(idAttributeName)?.Value ?? "0");
				var tokensElement = xmlState.Element(tokensElementName);
				var markingDict = new Dictionary<string, int>();
				foreach (var placeElem in tokensElement.Elements())
				{
					markingDict[placeElem.Name.LocalName] = int.Parse(placeElem.Value);
				}

				var constraintStr = xmlState.Element(constraintElementName)?.Value ?? "true";
				// NOTE: Actual deserialization of BoolExpr may require a parser, here we keep as string
				// You may want to parse constraintStr to BoolExpr if needed
				nodes.Add(new StateSpaceNode(
					markingDict,
					expressionParser.Parse(constraintStr), // Constraint parsing to BoolExpr can be added if needed
					id
				));
			}

			// Deserialize Arcs
			var arcsElement = cgElement.Element(arcsElementName);
			var arcs = new List<StateSpaceArc>();
			foreach (var xmlArc in arcsElement.Elements(arcElementName))
			{
				arcs.Add(new StateSpaceArc(
					bool.Parse(xmlArc.Attribute(isSilentAttributeName)?.Value ?? "false"),
					xmlArc.Attribute(baseTransitionIdAttributeName)?.Value ?? "",
					int.Parse(xmlArc.Attribute(sourceIdAttributeName)?.Value ?? "0"),
					int.Parse(xmlArc.Attribute(targetIdAttributeName)?.Value ?? "0"),
					xmlArc.Attribute(labelAttributeName)?.Value ?? ""
				));
			}

			// Deserialize Final Marking
			var finalMarkingElement = cgElement.Element(finalMarkingElementName);
			var finalMarkingDict = new Dictionary<string, int>();
			foreach (var placeElem in finalMarkingElement.Elements(placeElementName))
			{
				finalMarkingDict[placeElem.Value] = int.Parse(placeElem.Attribute(tokensAttributeName)?.Value ?? "0");
			}

			// Deserialize Transitions
			var transitionsElement = cgElement.Element(transitionsElementName);
			var transitions = new List<DPN.Models.DPNElements.Transition>();
			foreach (var xmlTransition in transitionsElement.Elements(transitionElementName))
			{
				var id = xmlTransition.Attribute(idAttributeName)?.Value ?? "";
				var label = xmlTransition.Element(nameElementName)?.Element(textElementName)?.Value ?? "";
				var baseTransitionId = xmlTransition.Attribute(baseTransitionIdAttributeName)?.Value ?? id;
				var isTau = bool.Parse(xmlTransition.Attribute(isTauAttributeName)?.Value ?? "false");
				var isSplit = bool.Parse(xmlTransition.Attribute(isSplitAttributeName)?.Value ?? "false");
				var constraintStr = xmlTransition.Attribute(guardAttributeName)?.Value ?? "true";
				// Guard deserialization is skipped for brevity
				var guard = new DPN.Models.DPNElements.Guard(context, expressionParser.Parse(constraintStr)); // You may want to parse guard from attribute
				transitions.Add(new DPN.Models.DPNElements.Transition(id, guard, baseTransitionId)
				{
					Label = label,
					IsTau = isTau,
					IsSplit = isSplit
				});
			}

			// Get attributes
			var isFullGraph = bool.Parse(cgElement.Attribute(isFullAttributeName)?.Value ?? "true");
			var graphTypeStr = cgElement.Attribute(graphTypeAttributeName)?.Value ?? "AbstractReachabilityGraph";
			var stateSpaceType = Enum.TryParse<TransitionSystemType>(graphTypeStr, out var type) ? type : TransitionSystemType.AbstractReachabilityGraph;

			return new StateSpaceAbstraction(
				nodes.ToArray(),
				arcs.ToArray(),
				isFullGraph,
				stateSpaceType,
				finalMarkingDict,
				transitions.ToArray(),
				typedVariables
			);
		}

		public XDocument Serialize(StateSpaceAbstraction stateSpace)
		{
			var statesElement = new XElement(statesElementName);
			var expressionSerializer = new Z3ExpressionSerializer();

			foreach (var state in stateSpace.Nodes)
			{
				var tokensElement = new XElement(tokensElementName);
				foreach (var node in state.Marking)
				{
					var nodeElement = new XElement(node.Key, node.Value);
					tokensElement.Add(nodeElement);
				}

				var constraintFormula = expressionSerializer.Serialize(state.StateConstraint!);
				var constraintElement = new XElement(constraintElementName, constraintFormula);

				var stateElement = new XElement(stateElementName);
				stateElement.Add(tokensElement);
				stateElement.Add(constraintElement);
				stateElement.SetAttributeValue(idAttributeName, state.Id.ToString());

				statesElement.Add(stateElement);
			}

			var arcsElement = new XElement(arcsElementName);
			foreach (var arc in stateSpace.Arcs)
			{
				var arcElement = new XElement(arcElementName);
				arcElement.SetAttributeValue(labelAttributeName, arc.Label);
				arcElement.SetAttributeValue(isSilentAttributeName, arc.IsSilent);
				arcElement.SetAttributeValue(sourceIdAttributeName, arc.SourceNodeId);
				arcElement.SetAttributeValue(targetIdAttributeName, arc.TargetNodeId);
				arcElement.SetAttributeValue(baseTransitionIdAttributeName, arc.BaseTransitionId);

				arcsElement.Add(arcElement);
			}

			var finalMarkingElement = new XElement(finalMarkingElementName);
			foreach (var (place, tokens) in stateSpace.FinalDpnMarking)
			{
				var placeElement = new XElement(placeElementName, place);
				placeElement.SetAttributeValue(tokensAttributeName, tokens);

				finalMarkingElement.Add(placeElement);
			}

			var transitionsElement = new XElement(transitionsElementName);

			foreach (var transition in stateSpace.DpnTransitions)
			{
				var transitionElement = new XElement(transitionElementName,
					new XElement(nameElementName,
						new XElement(textElementName, transition.Label)));
				transitionElement.SetAttributeValue(idAttributeName, transition.Id);
				transitionElement.SetAttributeValue(baseTransitionIdAttributeName, transition.BaseTransitionId);
				transitionElement.SetAttributeValue(isTauAttributeName, transition.IsTau);
				transitionElement.SetAttributeValue(isSplitAttributeName, transition.IsTau);

				if (!transition.Guard.ActualConstraintExpression.IsTrue)
				{
					var stringExpression = expressionSerializer.Serialize(transition.Guard.ActualConstraintExpression);
					transitionElement.SetAttributeValue(guardAttributeName, stringExpression);
				}

				transitionsElement.Add(transitionElement);
			}

			XElement variablesElement = new XElement(variablesElementName);
			var variables = stateSpace.TypedVariables
				.Select(kvp => (kvp.Key, kvp.Value))
				.GroupBy(kvp => kvp.Value)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToArray());

			foreach (var domainType in Enum.GetValues<DomainType>())
			{
				if (!variables.TryGetValue(domainType, out var typedVariables))
				{
					continue;
				}
				
				foreach (var variable in typedVariables)
				{
					var variableElement = new XElement(variableElementName,
						new XElement(nameElementName, variable));
					variableElement.SetAttributeValue(typeAttributeName, domainType.ToString());
					variablesElement.Add(variableElement);
				}
			}

			var cgElement = new XElement(
				stateSpaceElementName,
				statesElement,
				arcsElement,
				finalMarkingElement,
				transitionsElement,
				variablesElement);
			cgElement.SetAttributeValue(graphTypeAttributeName, stateSpace.StateSpaceType.ToString());
			cgElement.SetAttributeValue(isFullAttributeName, stateSpace.IsFullGraph.ToString());

			var srcTree = new XElement(rootElementName, cgElement);

			var document = new XDocument(srcTree);

			return document;
		}
	}
}