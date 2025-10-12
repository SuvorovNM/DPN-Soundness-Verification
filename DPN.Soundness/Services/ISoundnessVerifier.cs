using DPN.Models;

namespace DPN.Soundness.Services;

public interface ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings);
}