namespace DPN.Visualization.Models;

public class GraphToVisualize
{
    public StateToVisualize[] States { get; init; }
    public ArcToVisualize[] Arcs { get; init; }
    public SoundnessPropertiesToVisualize? SoundnessProperties { get; init; }
    public bool IsFull { get; init; }
    public GraphType GraphType { get; init; }
}