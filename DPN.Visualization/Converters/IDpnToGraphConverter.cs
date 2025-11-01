using DPN.Models;
using Microsoft.Msagl.Drawing;

namespace DPN.Visualization.Converters;

public interface IDpnToGraphConverter
{
    Graph ConvertToDpn(DataPetriNet dpn);
}