using DataPetriNetOnSmt;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    public class VerificationOutput
    {
        public bool SatisfiesCounditions { get; init; }
        public VerificationTypeEnum VerificationType { get; init; }
        public ushort Places { get; init; }
        public ushort Transitions { get; init; }
        public ushort Arcs { get; init; }
        public ushort Variables { get; init; }
        public ushort Conditions { get; init; }
        public bool Boundedness { get; init; }
        public int ConstraintStates { get; init; }
        public int ConstraintArcs { get; init; }
        public ushort DeadTransitions { get; init; }
        public bool Deadlocks { get; init; }
        public bool Soundness { get; init; }
        public string Time { get; init; }

        public string Identifier { get; init; }

        public VerificationOutput()
        {

        }

        public VerificationOutput(
            DataPetriNet dpn, 
            VerificationTypeEnum verificationType,
            bool satisfiesConditions,
            ConstraintGraph cg, 
            SoundnessProperties soundnessProperties, 
            long milliseconds)
        {
            Places = (ushort)dpn.Places.Count;
            Transitions = (ushort)dpn.Transitions.Count;
            Arcs = (ushort)dpn.Arcs.Count;
            Variables = (ushort)dpn.Variables.GetAllVariables().Count;
            Conditions = (ushort)dpn.Transitions
                .SelectMany(x => x.Guard.BaseConstraintExpressions.Select(y => y.GetSmtExpression(dpn.Context)))
                .Distinct()
                .Count();
            Boundedness = soundnessProperties.Boundedness;
            ConstraintStates = cg.ConstraintStates.Count;
            ConstraintArcs = cg.ConstraintArcs.Count;
            DeadTransitions = (ushort)soundnessProperties.DeadTransitions.Count;
            Deadlocks = soundnessProperties.Deadlocks;
            Soundness = soundnessProperties.Soundness;
            Time = milliseconds.ToString();
            SatisfiesCounditions = satisfiesConditions;
            Identifier = dpn.Name;
            VerificationType = verificationType;
        }
    }
    public class VerificationOutputWithNumber : VerificationOutput
    {
        public int Number { get; init; }

        public VerificationOutputWithNumber(VerificationOutput verificationOutput, int number)
        {
            Places = verificationOutput.Places;
            Transitions = verificationOutput.Transitions;
            Arcs = verificationOutput.Arcs;
            Variables = verificationOutput.Variables;
            Conditions = verificationOutput.Conditions;
            Boundedness = verificationOutput.Boundedness;
            ConstraintStates = verificationOutput.ConstraintStates;
            ConstraintArcs = verificationOutput.ConstraintArcs;
            DeadTransitions = verificationOutput.DeadTransitions;
            Deadlocks = verificationOutput.Deadlocks;
            Soundness = verificationOutput.Soundness;
            Time = verificationOutput.Time;
            Number = number;
            SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
            Identifier = verificationOutput.Identifier;
            VerificationType = verificationOutput.VerificationType;
        }
    }
}
