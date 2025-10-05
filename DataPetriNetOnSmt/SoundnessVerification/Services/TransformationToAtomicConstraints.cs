using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    internal class TransitionInfo
    {
        public Transition Transition { get; set; }
        public List<(Place place, int weight)> Preset { get; set; }
        public List<(Place place, int weight)> Postset { get; set; }
        public TransitionInfo(Transition transition, List<(Place place, int weight)> preset, List<(Place place, int weight)> postset)
        {
            Transition = transition;
            Preset = preset;
            Postset = postset;
        }
    }
}