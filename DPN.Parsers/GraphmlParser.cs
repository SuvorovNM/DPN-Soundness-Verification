using System.Runtime.Serialization;
using System.Xml.Linq;
using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;
using Microsoft.Z3;

namespace DPN.Parsers
{
	public class GraphmlParser
	{
		private const string XsdSchema = "XsdSchemas\\graphml.xsd";
		private const string RootElementName = "graphml";

		// Graph IDs
		private const string VariablesGraphId = "variables";
		private const string TransitionsGraphId = "transitions";
		private const string StateSpaceGraphId = "state_space";
		private const string MetadataGraphId = "metadata";

		// Attribute keys
		private const string DMarking = "marking";
		private const string DConstraint = "constraint";
		private const string DLabel = "label";
		private const string DBaseTransitionId = "base_transition_id";
		private const string DIsSilent = "is_silent";
		private const string DGuard = "guard";
		private const string DIsTau = "is_tau";
		private const string DIsSplit = "is_split";
		private const string DVariableId = "variable_id";
		private const string DDataType = "data_type";
		private const string DEdgeType = "edge_type";
		private const string DFinalMarking = "final_marking";
		private const string DVariables = "variables";
		private const string DGraphType = "graph_type";
		private const string DIsFull = "is_full";


		private readonly XsdValidator validator = new(XsdSchema);

		public StateSpaceGraph Deserialize(Stream stream, Context context)
		{
			var document = XDocument.Load(stream);
			var validationResult = validator.Validate(document);
			if (!validationResult.IsValid)
			{
				var errorText = string.Join(Environment.NewLine, validationResult.Errors.Select(e => $"[{e.LineNumber}:{e.LinePosition}]: {e.Severity.ToString()}: {e.Message}"));
				throw new SerializationException("Error occurred on deserializing:\n" + errorText);
			}

			var graphmlRoot = document.Root!;
			var graphs = graphmlRoot.Elements("graph").ToList();

			var variablesGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == VariablesGraphId);
			if (variablesGraph == null)
			{
				throw new SerializationException($"Not found graph with id = {VariablesGraphId}");
			}

			var typedVariables = ParseVariables(variablesGraph, out var variablesInFormulas);

			var expressionParser = new Z3ExpressionParser(context, variablesInFormulas);

			var stateSpaceGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == StateSpaceGraphId);
			if (stateSpaceGraph == null)
			{
				throw new SerializationException($"Not found graph with id = {StateSpaceGraphId}");
			}

			var (nodes, arcs) = ParseStateSpace(stateSpaceGraph, expressionParser);

			var transitionsGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == TransitionsGraphId);
			if (transitionsGraph == null)
			{
				throw new SerializationException($"Not found graph with id = {TransitionsGraphId}");
			}

			var transitions = ParseTransitions(transitionsGraph, expressionParser, context);

			var metadataGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == MetadataGraphId);
			if (metadataGraph == null)
			{
				throw new SerializationException($"Not found graph with id = {MetadataGraphId}");
			}

			var (isFullGraph, stateSpaceType, finalMarkingDict) = ParseMetadata(metadataGraph);

			return new StateSpaceGraph(
				nodes.ToArray(),
				arcs.ToArray(),
				isFullGraph,
				stateSpaceType,
				finalMarkingDict,
				transitions.ToArray(),
				typedVariables
			);
		}

		private Dictionary<string, DomainType> ParseVariables(XElement variablesGraph, out Dictionary<string, DomainType> variablesInFormulas)
		{
			var typedVariables = new Dictionary<string, DomainType>();
			variablesInFormulas = new Dictionary<string, DomainType>();

			foreach (var variableNode in variablesGraph.Elements("node"))
			{
				var variableId = variableNode.Attribute("id")?.Value;
				var dataTypeElem = variableNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DDataType);

				if (variableId != null && dataTypeElem != null)
				{
					if (Enum.TryParse<DomainType>(dataTypeElem.Value, out var domainType))
					{
						typedVariables[variableId] = domainType;
						variablesInFormulas[variableId + "_r"] = domainType;
						variablesInFormulas[variableId + "_w"] = domainType;
					}
				}
			}

			return typedVariables;
		}

		private List<Models.DPNElements.Transition> ParseTransitions(XElement transitionsGraph, Z3ExpressionParser expressionParser, Context context)
		{
			var transitions = new List<Models.DPNElements.Transition>();

			foreach (var transitionNode in transitionsGraph.Elements("node"))
			{
				var id = transitionNode.Attribute("id")?.Value ?? "";

				// Get transition properties from data elements
				var labelElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DLabel);
				var guardElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DGuard);
				var isTauElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DIsTau);
				var isSplitElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DIsSplit);

				var label = labelElem?.Value ?? id;
				var isTau = bool.Parse(isTauElem?.Value ?? "false");
				var isSplit = bool.Parse(isSplitElem?.Value ?? "false");
				var guardStr = guardElem?.Value ?? "true";

				// Need to re-parse guard with proper variables context
				var guard = new Models.DPNElements.Guard(context, expressionParser.Parse(guardStr));

				transitions.Add(new Models.DPNElements.Transition(id, guard, id)
				{
					Label = label,
					IsTau = isTau,
					IsSplit = isSplit
				});
			}

			return transitions;
		}

		private (List<StateSpaceNode> nodes, List<StateSpaceArc> arcs) ParseStateSpace(
			XElement stateSpaceGraph, Z3ExpressionParser expressionParser)
		{
			var nodes = new List<StateSpaceNode>();
			var arcs = new List<StateSpaceArc>();

			foreach (var stateNode in stateSpaceGraph.Elements("node"))
			{
				var stateId = int.Parse(stateNode.Attribute("id")?.Value ?? "0");

				var markingElem = stateNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DMarking);
				var constraintElem = stateNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DConstraint);

				if (markingElem == null) continue;

				// Parse marking string format: "i=1,p1=0,p2=0,..."
				var markingDict = ParseMarkingString(markingElem.Value);
				var constraintStr = constraintElem?.Value ?? "true";

				nodes.Add(new StateSpaceNode(
					markingDict,
					expressionParser.Parse(constraintStr),
					stateId
				));
			}

			foreach (var edge in stateSpaceGraph.Elements("edge"))
			{
				var sourceId = int.Parse(edge.Attribute("source")?.Value ?? "0");
				var targetId = int.Parse(edge.Attribute("target")?.Value ?? "0");

				var labelElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DLabel);
				var isSilentElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DIsSilent);
				var baseTransitionIdElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == DBaseTransitionId);

				var label = labelElem?.Value ?? "";
				var isSilent = bool.Parse(isSilentElem?.Value ?? "false");
				var baseTransitionId = baseTransitionIdElem?.Value ?? "";

				arcs.Add(new StateSpaceArc(
					isSilent,
					baseTransitionId,
					sourceId,
					targetId,
					label
				));
			}

			return (nodes, arcs);
		}

		private (bool isFull, TransitionSystemType type, Dictionary<string, int> finalMarking) ParseMetadata(XElement metadataGraph)
		{
			var isFull = true;
			var type = TransitionSystemType.AbstractReachabilityGraph;
			var finalMarking = new Dictionary<string, int>();

			// Parse data elements from metadata graph
			var graphTypeElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == DGraphType);
			var isFullElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == DIsFull);
			var finalMarkingElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == DFinalMarking);

			if (graphTypeElem != null)
			{
				Enum.TryParse(graphTypeElem.Value, out type);
			}

			if (isFullElem != null)
			{
				bool.TryParse(isFullElem.Value, out isFull);
			}

			if (finalMarkingElem != null)
			{
				finalMarking = ParseMarkingString(finalMarkingElem.Value);
			}

			return (isFull, type, finalMarking);
		}

		private Dictionary<string, int> ParseMarkingString(string markingStr)
		{
			var markingDict = new Dictionary<string, int>();

			if (string.IsNullOrEmpty(markingStr)) return markingDict;

			var parts = markingStr.Split(',');
			foreach (var part in parts)
			{
				var keyValue = part.Split('=');
				if (keyValue.Length == 2)
				{
					var place = keyValue[0].Trim();
					if (int.TryParse(keyValue[1].Trim(), out var tokens))
					{
						markingDict[place] = tokens;
					}
				}
			}

			return markingDict;
		}

		public void Serialize(StateSpaceGraph stateSpace, Stream stream)
		{
			var graphmlRoot = new XElement(RootElementName);

			AddKeys(graphmlRoot);

			var stateSpaceGraph = CreateStateSpaceGraph(stateSpace);
			graphmlRoot.Add(stateSpaceGraph);

			var variablesGraph = CreateVariablesGraph(stateSpace.TypedVariables);
			graphmlRoot.Add(variablesGraph);

			var transitionsGraph = CreateTransitionsGraph(stateSpace.DpnTransitions);
			graphmlRoot.Add(transitionsGraph);

			var metadataGraph = CreateMetadataGraph(stateSpace);
			graphmlRoot.Add(metadataGraph);

			var xDocument = new XDocument(graphmlRoot);
			xDocument.Save(stream);
		}

		private void AddKeys(XElement graphmlRoot)
		{
			var keys = new[]
			{
				new { Id = DMarking, For = "node", Type = "string" },
				new { Id = DConstraint, For = "node", Type = "string" },
				new { Id = DLabel, For = "edge", Type = "string" },
				new { Id = DBaseTransitionId, For = "edge", Type = "string" },
				new { Id = DIsSilent, For = "edge", Type = "boolean" },
				new { Id = DGuard, For = "node", Type = "string" },
				new { Id = DIsTau, For = "node", Type = "boolean" },
				new { Id = DIsSplit, For = "node", Type = "boolean" },
				new { Id = DVariableId, For = "node", Type = "string" },
				new { Id = DDataType, For = "node", Type = "string" },
				new { Id = DEdgeType, For = "edge", Type = "string" },
				new { Id = DFinalMarking, For = "graph", Type = "string" },
				new { Id = DVariables, For = "graph", Type = "string" },
				new { Id = DGraphType, For = "graph", Type = "string" },
				new { Id = DIsFull, For = "graph", Type = "boolean" }
			};

			foreach (var key in keys)
			{
				var keyElement = new XElement("key",
					new XAttribute("id", key.Id),
					new XAttribute("for", key.For),
					new XAttribute("attr.name", key.Id),
					new XAttribute("attr.type", key.Type));
				graphmlRoot.Add(keyElement);
			}
		}

		private XElement CreateVariablesGraph(Dictionary<string, DomainType> typedVariables)
		{
			var variablesGraph = new XElement("graph",
				new XAttribute("id", VariablesGraphId),
				new XAttribute("edgedefault", "undirected"));

			var uniqueVariables = typedVariables
				.Select(kvp => kvp.Key.Replace("_r", "").Replace("_w", ""))
				.Distinct()
				.ToList();

			var variablesByType = new Dictionary<DomainType, List<string>>();
			foreach (var variable in uniqueVariables)
			{
				var type = typedVariables[variable];

				if (!variablesByType.ContainsKey(type))
					variablesByType[type] = new List<string>();

				variablesByType[type].Add(variable);
			}

			foreach (var (type, variableList) in variablesByType)
			{
				foreach (var variable in variableList)
				{
					var variableNode = new XElement("node",
						new XAttribute("id", variable));

					variableNode.Add(new XElement("data",
						new XAttribute("key", DVariableId),
						variable));

					variableNode.Add(new XElement("data",
						new XAttribute("key", DDataType),
						type.ToString()));

					variablesGraph.Add(variableNode);
				}
			}

			return variablesGraph;
		}

		private XElement CreateTransitionsGraph(Models.DPNElements.Transition[] dpnTransitions)
		{
			var transitionsGraph = new XElement("graph",
				new XAttribute("id", TransitionsGraphId),
				new XAttribute("edgedefault", "undirected"));

			var expressionSerializer = new Z3ExpressionSerializer();

			foreach (var transition in dpnTransitions)
			{
				var transitionNode = new XElement("node",
					new XAttribute("id", transition.Id));

				transitionNode.Add(new XElement("data",
					new XAttribute("key", DLabel),
					transition.Label));

				if (!transition.Guard.ActualConstraintExpression.IsTrue)
				{
					var guardStr = expressionSerializer.Serialize(transition.Guard.ActualConstraintExpression);
					transitionNode.Add(new XElement("data",
						new XAttribute("key", DGuard),
						guardStr));
				}

				transitionNode.Add(new XElement("data",
					new XAttribute("key", DIsTau),
					transition.IsTau.ToString().ToLowerInvariant()));

				transitionNode.Add(new XElement("data",
					new XAttribute("key", DIsSplit),
					transition.IsSplit.ToString().ToLowerInvariant()));

				transitionsGraph.Add(transitionNode);
			}

			return transitionsGraph;
		}

		private XElement CreateStateSpaceGraph(StateSpaceGraph stateSpace)
		{
			var stateSpaceGraph = new XElement("graph",
				new XAttribute("id", StateSpaceGraphId),
				new XAttribute("edgedefault", "directed"));

			var expressionSerializer = new Z3ExpressionSerializer();

			foreach (var state in stateSpace.Nodes)
			{
				var stateNode = new XElement("node",
					new XAttribute("id", state.Id));

				// Serialize marking as string: "i=1,p1=0,p2=0,..."
				var markingStr = string.Join(",",
					state.Marking.Select(kvp => $"{kvp.Key}={kvp.Value}"));
				stateNode.Add(new XElement("data",
					new XAttribute("key", DMarking),
					markingStr));

				if (state.StateConstraint != null)
				{
					var constraintStr = expressionSerializer.Serialize(state.StateConstraint);
					stateNode.Add(new XElement("data",
						new XAttribute("key", DConstraint),
						constraintStr));
				}

				stateSpaceGraph.Add(stateNode);
			}

			foreach (var arc in stateSpace.Arcs)
			{
				var edge = new XElement("edge",
					new XAttribute("source", arc.SourceNodeId),
					new XAttribute("target", arc.TargetNodeId));

				edge.Add(new XElement("data",
					new XAttribute("key", DLabel),
					arc.Label));

				edge.Add(new XElement("data",
					new XAttribute("key", DIsSilent),
					arc.IsSilent.ToString().ToLowerInvariant()));

				edge.Add(new XElement("data",
					new XAttribute("key", DBaseTransitionId),
					arc.BaseTransitionId));

				stateSpaceGraph.Add(edge);
			}

			return stateSpaceGraph;
		}

		private XElement CreateMetadataGraph(StateSpaceGraph stateSpace)
		{
			var metadataGraph = new XElement("graph",
				new XAttribute("id", MetadataGraphId),
				new XAttribute("edgedefault", "undirected"));

			metadataGraph.Add(new XElement("data",
				new XAttribute("key", DGraphType),
				stateSpace.StateSpaceType.ToString()));

			metadataGraph.Add(new XElement("data",
				new XAttribute("key", DIsFull),
				stateSpace.IsFullGraph.ToString().ToLowerInvariant()));

			var finalMarkingStr = string.Join(",",
				stateSpace.FinalDpnMarking.Select(kvp => $"{kvp.Key}={kvp.Value}"));
			metadataGraph.Add(new XElement("data",
				new XAttribute("key", DFinalMarking),
				finalMarkingStr));

			return metadataGraph;
		}
	}
}