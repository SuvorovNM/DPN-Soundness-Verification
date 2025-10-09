namespace DPN.Experiments.Common
{
    public class VerificationInputForRandom : VerificationInputBasis
    {
        public int MinPlaces { get; set; }
        public int MaxPlaces { get; set; }
        public int MinTransitions { get; set; }
        public int MaxTransitions { get; set; }
        public int MinArcs { get; set; }
        public int MaxArcs { get; set; }
        public int MinVars { get; set; }
        public int MaxVars { get; set; }
        public int MinConditions { get; set; }
        public int MaxConditions { get; set; }
    }
}
