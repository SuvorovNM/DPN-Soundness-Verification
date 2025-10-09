using DPN.Models;

namespace DPN.SoundnessVerification.Services;

public interface ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings);
}