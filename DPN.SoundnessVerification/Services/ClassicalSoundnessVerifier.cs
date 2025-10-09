using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification.Services;

public class ClassicalSoundnessVerifier : ISoundnessVerifier
{
    // TODO: подумать, может и явно передавать настройки
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
    {
        
    }
}