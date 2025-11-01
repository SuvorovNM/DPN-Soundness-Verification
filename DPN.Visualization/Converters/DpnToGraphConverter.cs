using DPN.Models;
using Microsoft.Msagl.Drawing;

namespace DPN.Visualization.Converters
{
	public class DpnToGraphConverter : IDpnToGraphConverter
	{
		public Graph ConvertToDpn(DataPetriNet dpn)
		{
			var graph = new Graph();

			AddPlacesToGraph(dpn, graph);
			AddTransitionsToGraph(dpn, graph);
			AddArcsToGraph(dpn, graph);

			return graph;
		}

		private static void AddArcsToGraph(DataPetriNet dpn, Graph graph)
		{
			foreach (var arc in dpn.Arcs)
			{
				if (arc.Weight == 1)
					graph.AddEdge(arc.Source.Id, arc.Destination.Id);
				else
					graph.AddEdge(arc.Source.Id, arc.Weight.ToString(), arc.Destination.Id);
			}
		}

		private static void AddTransitionsToGraph(DataPetriNet dpn, Graph graph)
		{
			foreach (var transition in dpn.Transitions)
			{
				var nodeToAdd = new Node(transition.Id)
				{
					Attr =
					{
						Shape = Shape.Box
					},
					Label = new Label(transition.Label),
					LabelText = transition.Label
				};

				graph.AddNode(nodeToAdd);
				
				var edgeToAdd = new Edge(nodeToAdd, nodeToAdd, ConnectionToGraph.Connected)
				{
					Attr =
					{
						LineWidth = 0,
						ArrowheadAtSource = ArrowStyle.None,
						ArrowheadAtTarget = ArrowStyle.None,
						Color = Color.White
					},
					LabelText = transition.Guard.ActualConstraintExpression.ToString()
				};
			}
		}

		private static void AddPlacesToGraph(DataPetriNet dpn, Graph graph)
		{
			foreach (var place in dpn.Places)
			{
				var nodeToAdd = new Node(place.Id)
				{
					Attr =
					{
						Shape = Shape.Circle
					},
					Label = new Label(place.Label),
					LabelText = place.Label
				};

				if (place.Tokens > 0 || place.IsFinal)
				{
					nodeToAdd.Attr.FillColor = Color.LightGray;
					nodeToAdd.Attr.LineWidth = 3;
				}

				graph.AddNode(nodeToAdd);
			}
		}
	}
}