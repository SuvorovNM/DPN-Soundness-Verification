using Microsoft.Msagl.Drawing;
using System.Linq;

namespace DataPetriNetOnSmt.Visualization.Services
{
    public class DPNToGraphParser
    {
        public Graph FormGraphBasedOnDPN(DataPetriNet dpn)
        {
            Graph graph = new Graph();

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
                    graph.AddEdge(arc.Source.Label, arc.Destination.Label);
                else
                    graph.AddEdge(arc.Source.Label, arc.Weight.ToString(), arc.Destination.Label);
            }
        }

        private static void AddTransitionsToGraph(DataPetriNet dpn, Graph graph)
        {
            foreach (var transition in dpn.Transitions)
            {
                var nodeToAdd = new Node(transition.Label);
                nodeToAdd.Attr.Shape = Shape.Box;

                graph.AddNode(nodeToAdd);

                if (transition.Guard.ConstraintExpressions.Any())
                {
                    var edgeToAdd = new Edge(nodeToAdd, nodeToAdd, ConnectionToGraph.Connected);
                    edgeToAdd.Attr.LineWidth = 0;
                    edgeToAdd.Attr.ArrowheadAtSource = ArrowStyle.None;
                    edgeToAdd.Attr.ArrowheadAtTarget = ArrowStyle.None;
                    edgeToAdd.LabelText = string.Join(" ", transition.Guard.ConstraintExpressions.Select(x => x.ToString()));
                    edgeToAdd.Attr.Color = Color.White;
                }
            }
        }

        private static void AddPlacesToGraph(DataPetriNet dpn, Graph graph)
        {
            foreach (var place in dpn.Places)
            {
                var nodeToAdd = new Node(place.Label);
                nodeToAdd.Attr.Shape = Shape.Circle;

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
