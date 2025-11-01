using DPN.Models;

namespace DPN.Soundness.Verification;

public interface ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings);
}