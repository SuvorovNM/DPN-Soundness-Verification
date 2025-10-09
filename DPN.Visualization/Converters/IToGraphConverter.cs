using DPN.Visualization.Models;
using Microsoft.Msagl.Drawing;

namespace DPN.Visualization.Converters;

public interface IToGraphConverter
{
    Graph Convert(GraphToVisualize graphToVisualize);
}