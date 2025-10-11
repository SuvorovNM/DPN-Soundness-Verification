using System.Xml.Linq;
using DPN.Models.Enums;
using DPN.Models.Extensions;
using DPN.SoundnessVerification;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.Parsers
{
	public class CgmlParser
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

			var cgElement = document.Root?
				.Element(stateSpaceElementName);
			if (cgElement == null)
			{
				throw new Exception("Document is incorrect. Can't find tag <cg>");
			}

			var constraintStates = new List<StateToVisualize>();
			var statesElement = cgElement.Element("states");
			foreach (var xmlState in statesElement.Elements())
			{
				var stateId = int.Parse(xmlState.Attribute("id").Value);
				var stateType = Enum.Parse<ConstraintStateType>(xmlState.Attribute("type").Value);

				var constraintFormula = xmlState.Element("constraint").Value;

				var tokensState = xmlState.Element("tokens");
				var placeTokensDict = new Dictionary<string, int>();
				foreach (var element in tokensState.Elements())
				{
					placeTokensDict.Add(element.Name.ToString(), int.Parse(element.Value));
				}

				var constraintState = new StateToVisualize
				{
					ConstraintFormula = constraintFormula,
					Id = stateId,
					StateType = stateType,
					Tokens = placeTokensDict
				};

				constraintStates.Add(constraintState);
			}

			var constraintArcs = new List<ArcToVisualize>();
			var arcsElement = cgElement.Element("arcs");
			foreach (var xmlArc in arcsElement.Elements())
			{
				var transitionName = xmlArc.Attribute("transition_name").Value;
				var isSilent = bool.Parse(xmlArc.Attribute("is_silent").Value);
				var sourceStateId = int.Parse(xmlArc.Attribute("source_id").Value);
				var targetStateId = int.Parse(xmlArc.Attribute("target_id").Value);

				var constraintArc = new ArcToVisualize
				{
					TransitionName = transitionName,
					SourceStateId = sourceStateId,
					TargetStateId = targetStateId,
					IsSilent = isSilent
				};

				constraintArcs.Add(constraintArc);
			}

			var deadTransitions = new List<string>();
			var deadTransitionsElement = cgElement.Element("dead_transitions");
			foreach (var xmlDeadTransition in deadTransitionsElement.Elements())
			{
				deadTransitions.Add(xmlDeadTransition.Value);
			}


			var graphTypeAttribute = cgElement.Attribute("graph_type");
			if (graphTypeAttribute == null ||
			    !Enum.TryParse(graphTypeAttribute.Value, out GraphType graphType))
			{
				graphType = GraphType.Lts;
			}

			if (cgElement.Attribute("is_full") == null &&
			    !bool.TryParse(cgElement.Attribute("is_full").Value, out var isFullGraph)) ;
			{
				isFullGraph = true;
			}

			if (cgElement.Attribute("soundness_type") == null ||
			    !Enum.TryParse(cgElement.Attribute("soundness_type").Value, out SoundnessType soundnessType))
			{
				soundnessType = SoundnessType.Classical;
			}

			return new GraphToVisualize
			{
				States = constraintStates,
				Arcs = constraintArcs,
				GraphType = graphType,
				IsFull = isFullGraph,
				SoundnessProperties =
					new SoundnessPropertiesToVisualize(isBounded, deadTransitions.ToArray(), isClassicalSound, isRelaxedLazySound)
			};
		}

		public XDocument Serialize(StateSpaceAbstraction stateSpace)
		{
			var statesElement = new XElement(statesElementName);
			var expressionSerializer = new Z3ExpressionSerializer();

			foreach (var state in stateSpace.Nodes)
			{
				var tokensElement = new XElement(tokensElementName);
				foreach (var node in state.Marking.AsDictionary())
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
			foreach (var (place, tokens) in stateSpace.FinalDpnMarking.AsDictionary())
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
				foreach (var variable in variables[domainType])
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