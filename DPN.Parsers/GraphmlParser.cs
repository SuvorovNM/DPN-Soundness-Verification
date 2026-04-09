using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml.Linq;
using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;
using Microsoft.Z3;

namespace DPN.Parsers
{
	public class GraphmlParser
	{
		private const string xsdSchema = "XsdSchemas\\graphml.xsd";
		private const string rootElementName = "graphml";
		
		// Graph IDs
		private const string variablesGraphId = "variables";
		private const string transitionsGraphId = "transitions";
		private const string stateSpaceGraphId = "state_space";
		private const string metadataGraphId = "metadata";
		private const string relationshipsGraphId = "relationships";
		
		// Attribute keys
		private const string dMarking = "marking";
		private const string dConstraint = "constraint";
		private const string dLabel = "label";
		private const string dBaseTransitionId = "base_transition_id";
		private const string dIsSilent = "is_silent";
		private const string dGuard = "guard";
		private const string dIsTau = "is_tau";
		private const string dIsSplit = "is_split";
		private const string dVariableId = "variable_id";
		private const string dDataType = "data_type";
		private const string dEdgeType = "edge_type";
		private const string dFinalMarking = "final_marking";
		private const string dVariables = "variables";
		private const string dGraphType = "graph_type";
		private const string dIsFull = "is_full";
		
		
		private readonly XsdValidator validator = new(xsdSchema);

		public StateSpaceGraph Deserialize(Stream stream, Context context)
		{
			var document = XDocument.Load(stream);
			var validationResult = validator.Validate(document);
			if (!validationResult.IsValid)
			{
				var errorText = string.Join(Environment.NewLine, validationResult.Errors.Select(e => $"[{e.LineNumber}:{e.LinePosition}]: {e.Severity.ToString()}: {e.Message}"));
				throw new SerializationException("Error occurred on deserializing:\n" + errorText);
			}
			
			var graphmlRoot = document.Root;
			var graphs = graphmlRoot.Elements("graph").ToList();
			
						
			// Parse variables from variables graph
			var variablesGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == variablesGraphId);
			var typedVariables = ParseVariables(variablesGraph, out var variablesInFormulas);
			
			var expressionParser = new Z3ExpressionParser(context, variablesInFormulas);
						
			// Parse states and arcs from state space graph
			var stateSpaceGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == stateSpaceGraphId);
			var (nodes, arcs) = ParseStateSpace(stateSpaceGraph, expressionParser);
			
			// Parse transitions from transitions graph
			var transitionsGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == transitionsGraphId);
			var transitions = ParseTransitions(transitionsGraph, expressionParser, context);
			
			// Parse metadata from metadata graph
			var metadataGraph = graphs.FirstOrDefault(g => g.Attribute("id")?.Value == metadataGraphId);
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
			
			if (variablesGraph == null) return typedVariables;
			
			foreach (var variableNode in variablesGraph.Elements("node"))
			{
				var variableId = variableNode.Attribute("id")?.Value;
				var dataTypeElem = variableNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dDataType);
				
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

		private List<DPN.Models.DPNElements.Transition> ParseTransitions(XElement transitionsGraph, Z3ExpressionParser expressionParser, Context context)
		{
			var transitions = new List<DPN.Models.DPNElements.Transition>();
			
			if (transitionsGraph == null) return transitions;
			
			foreach (var transitionNode in transitionsGraph.Elements("node"))
			{
				var id = transitionNode.Attribute("id")?.Value ?? "";
				
				// Get transition properties from data elements
				var labelElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dLabel);
				var guardElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dGuard);
				var isTauElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dIsTau);
				var isSplitElem = transitionNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dIsSplit);
				
				var label = labelElem?.Value ?? id;
				var isTau = bool.Parse(isTauElem?.Value ?? "false");
				var isSplit = bool.Parse(isSplitElem?.Value ?? "false");
				var guardStr = guardElem?.Value ?? "true";
				
				// Note: Need to re-parse guard with proper variables context
				var guard = new DPN.Models.DPNElements.Guard(context, expressionParser.Parse(guardStr));
				
				transitions.Add(new DPN.Models.DPNElements.Transition(id, guard, id)
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
			
			if (stateSpaceGraph == null) return (nodes, arcs);
			
			// Parse state nodes
			foreach (var stateNode in stateSpaceGraph.Elements("node"))
			{
				var stateId = int.Parse(stateNode.Attribute("id")?.Value ?? "0");
				
				var markingElem = stateNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dMarking);
				var constraintElem = stateNode.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dConstraint);
				
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
			
			// Parse arcs
			foreach (var edge in stateSpaceGraph.Elements("edge"))
			{
				var sourceId = int.Parse(edge.Attribute("source")?.Value ?? "0");
				var targetId = int.Parse(edge.Attribute("target")?.Value ?? "0");
				
				var labelElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dLabel);
				var isSilentElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dIsSilent);
				var baseTransitionIdElem = edge.Elements("data")
					.FirstOrDefault(e => e.Attribute("key")?.Value == dBaseTransitionId);
				
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
			
			if (metadataGraph == null) return (isFull, type, finalMarking);
			
			// Parse data elements from metadata graph
			var graphTypeElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == dGraphType);
			var isFullElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == dIsFull);
			var finalMarkingElem = metadataGraph.Elements("data")
				.FirstOrDefault(e => e.Attribute("key")?.Value == dFinalMarking);
			
			if (graphTypeElem != null)
			{
				Enum.TryParse<TransitionSystemType>(graphTypeElem.Value, out type);
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
			var graphmlRoot = new XElement(rootElementName);
			
			AddKeys(graphmlRoot);
			
			var stateSpaceGraph = CreateStateSpaceGraph(stateSpace);
			graphmlRoot.Add(stateSpaceGraph);

			var variablesGraph = CreateVariablesGraph(stateSpace.TypedVariables);
			graphmlRoot.Add(variablesGraph);

			var transitionsGraph = CreateTransitionsGraph(stateSpace.DpnTransitions);
			graphmlRoot.Add(transitionsGraph);

			var metadataGraph = CreateMetadataGraph(stateSpace);
			graphmlRoot.Add(metadataGraph);
			
			var xDocument =  new XDocument(graphmlRoot);
			xDocument.Save(stream);
		}

		private void AddKeys(XElement graphmlRoot)
		{
			// Add keys for different data types
			var keys = new[]
			{
				new { Id = dMarking, For = "node", Type = "string" },
				new { Id = dConstraint, For = "node", Type = "string" },
				new { Id = dLabel, For = "edge", Type = "string" },
				new { Id = dBaseTransitionId, For = "edge", Type = "string" },
				new { Id = dIsSilent, For = "edge", Type = "boolean" },
				new { Id = dGuard, For = "node", Type = "string" },
				new { Id = dIsTau, For = "node", Type = "boolean" },
				new { Id = dIsSplit, For = "node", Type = "boolean" },
				new { Id = dVariableId, For = "node", Type = "string" },
				new { Id = dDataType, For = "node", Type = "string" },
				new { Id = dEdgeType, For = "edge", Type = "string" },
				new { Id = dFinalMarking, For = "graph", Type = "string" },
				new { Id = dVariables, For = "graph", Type = "string" },
				new { Id = dGraphType, For = "graph", Type = "string" },
				new { Id = dIsFull, For = "graph", Type = "boolean" }
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
				new XAttribute("id", variablesGraphId),
				new XAttribute("edgedefault", "undirected"));
			
			// Group variables by removing suffixes and get unique variable names
			var uniqueVariables = typedVariables
				.Select(kvp => kvp.Key.Replace("_r", "").Replace("_w", ""))
				.Distinct()
				.ToList();
			
			// Group by type
			var variablesByType = new Dictionary<DomainType, List<string>>();
			foreach (var variable in uniqueVariables)
			{
				var type = typedVariables[variable];
					/*typedVariables.ContainsKey(variable + "_r") 
					? typedVariables[variable + "_r"] 
					: typedVariables.ContainsKey(variable + "_w") 
						? typedVariables[variable + "_w"] 
						: DomainType.Integer;*/
				
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
						new XAttribute("key", dVariableId),
						variable));
					
					variableNode.Add(new XElement("data",
						new XAttribute("key", dDataType),
						type.ToString()));
					
					variablesGraph.Add(variableNode);
				}
			}
			
			return variablesGraph;
		}

		private XElement CreateTransitionsGraph(DPN.Models.DPNElements.Transition[] dpnTransitions)
		{
			var transitionsGraph = new XElement("graph",
				new XAttribute("id", transitionsGraphId),
				new XAttribute("edgedefault", "undirected"));
			
			var expressionSerializer = new Z3ExpressionSerializer();
			
			foreach (var transition in dpnTransitions)
			{
				var transitionNode = new XElement("node",
					new XAttribute("id", transition.Id));
				
				transitionNode.Add(new XElement("data",
					new XAttribute("key", dLabel),
					transition.Label));
				
				if (!transition.Guard.ActualConstraintExpression.IsTrue)
				{
					var guardStr = expressionSerializer.Serialize(transition.Guard.ActualConstraintExpression);
					transitionNode.Add(new XElement("data",
						new XAttribute("key", dGuard),
						guardStr));
				}
				
				transitionNode.Add(new XElement("data",
					new XAttribute("key", dIsTau),
					transition.IsTau.ToString().ToLowerInvariant()));
				
				transitionNode.Add(new XElement("data",
					new XAttribute("key", dIsSplit),
					transition.IsSplit.ToString().ToLowerInvariant()));
				
				transitionsGraph.Add(transitionNode);
			}
			
			return transitionsGraph;
		}

		private XElement CreateStateSpaceGraph(StateSpaceGraph stateSpace)
		{
			var stateSpaceGraph = new XElement("graph",
				new XAttribute("id", stateSpaceGraphId),
				new XAttribute("edgedefault", "directed"));
			
			var expressionSerializer = new Z3ExpressionSerializer();
			
			// Add state nodes
			foreach (var state in stateSpace.Nodes)
			{
				var stateNode = new XElement("node",
					new XAttribute("id", state.Id));
				
				// Serialize marking as string: "i=1,p1=0,p2=0,..."
				var markingStr = string.Join(",",
					state.Marking.Select(kvp => $"{kvp.Key}={kvp.Value}"));
				stateNode.Add(new XElement("data",
					new XAttribute("key", dMarking),
					markingStr));
				
				if (state.StateConstraint != null)
				{
					var constraintStr = expressionSerializer.Serialize(state.StateConstraint);
					stateNode.Add(new XElement("data",
						new XAttribute("key", dConstraint),
						constraintStr));
				}
				
				stateSpaceGraph.Add(stateNode);
			}
			
			// Add arcs
			foreach (var arc in stateSpace.Arcs)
			{
				var edge = new XElement("edge",
					new XAttribute("source", arc.SourceNodeId),
					new XAttribute("target", arc.TargetNodeId));
				
				edge.Add(new XElement("data",
					new XAttribute("key", dLabel),
					arc.Label));
				
				edge.Add(new XElement("data",
					new XAttribute("key", dIsSilent),
					arc.IsSilent.ToString().ToLowerInvariant()));
				
				edge.Add(new XElement("data",
					new XAttribute("key", dBaseTransitionId),
					arc.BaseTransitionId));
				
				stateSpaceGraph.Add(edge);
			}
			
			return stateSpaceGraph;
		}

		private XElement CreateMetadataGraph(StateSpaceGraph stateSpace)
		{
			var metadataGraph = new XElement("graph",
				new XAttribute("id", metadataGraphId),
				new XAttribute("edgedefault", "undirected"));
			
			// Add metadata as data elements
			metadataGraph.Add(new XElement("data",
				new XAttribute("key", dGraphType),
				stateSpace.StateSpaceType.ToString()));
			
			metadataGraph.Add(new XElement("data",
				new XAttribute("key", dIsFull),
				stateSpace.IsFullGraph.ToString().ToLowerInvariant()));
			
			// Serialize final marking
			var finalMarkingStr = string.Join(",",
				stateSpace.FinalDpnMarking.Select(kvp => $"{kvp.Key}={kvp.Value}"));
			metadataGraph.Add(new XElement("data",
				new XAttribute("key", dFinalMarking),
				finalMarkingStr));
			
			return metadataGraph;
		}

		// Optional: Create relationships graph
		private XElement CreateRelationshipsGraph(StateSpaceGraph stateSpace)
		{
			var relationshipsGraph = new XElement("graph",
				new XAttribute("id", relationshipsGraphId),
				new XAttribute("edgedefault", "directed"));
			
			// This would contain edges linking transitions to variables they use,
			// states to variables in constraints, etc.
			// Implementation depends on specific relationship needs
			
			return relationshipsGraph;
		}
	}
}