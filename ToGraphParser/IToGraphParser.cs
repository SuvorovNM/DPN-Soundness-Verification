using DataPetriNetVerificationDomain.GraphVisualized;
using Microsoft.Msagl.Drawing;

namespace DataPetriNetParsers;

public interface IToGraphParser
{
    Graph Parse(GraphToVisualize graphToVisualize);
}