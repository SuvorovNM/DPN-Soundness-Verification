using DataPetriNetOnSmt;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetVerificationDomain
{
    public class VerificationOutput
    {
        public bool SatisfiesCounditions { get; init; }
        //public VerificationTypeEnum VerificationType { get; init; }
        public ushort Places { get; init; }
        public ushort Transitions { get; init; }
        public ushort Arcs { get; init; }
        public ushort Variables { get; init; }
        public ushort Conditions { get; init; }
        public bool Boundedness { get; init; }
        public int LtsStates { get; init; }
        public int LtsArcs { get; init; }
        public int CgStates { get; init; }
        public int CgArcs { get; init; } 
        public int CgRefStates { get; init; }
        public int CgRefArcs { get; init; }
        public ushort DeadTransitions { get; init; }
        public bool Deadlocks { get; init; }
        public bool Soundness { get; init; }
        public string VerificationTime { get; init; }
        public string LtsTime { get; init; }
        public string CgTime { get; init; }
        public string CgRefTime { get; init; }

        public string Id { get; init; }

        public VerificationOutput()
        {

        }

        public VerificationOutput(
            DataPetriNet dpn, 
            bool satisfiesConditions,
            ClassicalLabeledTransitionSystem lts,
            ConstraintGraph cg,
            ConstraintGraph cgRefined,
            SoundnessProperties? soundnessProperties,
            long millisecondsForLts,
            long millisecondsForCg,
            long millisecondsForCgRefined)
        {
            Places = (ushort)dpn.Places.Count;
            Transitions = (ushort)dpn.Transitions.Count;
            Arcs = (ushort)dpn.Arcs.Count;
            Variables = (ushort)dpn.Variables.GetAllVariables().Count;
            Conditions = (ushort)dpn.Transitions
                .SelectMany(x => x.Guard.BaseConstraintExpressions.Select(y => y.GetSmtExpression(dpn.Context)))
                .Distinct()
                .Count();
            Boundedness = soundnessProperties?.Boundedness ?? false ;
            LtsStates = lts.ConstraintStates.Count;
            LtsArcs = lts.ConstraintArcs.Count;
            CgStates = cg.ConstraintStates.Count;
            CgArcs = cg.ConstraintArcs.Count;
            DeadTransitions = (ushort)(soundnessProperties?.DeadTransitions.Count ?? 0);
            Deadlocks = soundnessProperties?.Deadlocks ?? false;
            Soundness = soundnessProperties?.Soundness ?? false;
            VerificationTime = (millisecondsForLts + millisecondsForCg + millisecondsForCgRefined).ToString();
            SatisfiesCounditions = satisfiesConditions;
            Id = dpn.Name;
            //VerificationType = verificationType;
            LtsTime = millisecondsForLts.ToString();
            CgTime = millisecondsForCg.ToString();
            CgRefTime = millisecondsForCgRefined.ToString();
            CgRefArcs = cgRefined.ConstraintArcs.Count;
            CgRefStates= cgRefined.ConstraintStates.Count;
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
            CgStates = verificationOutput.CgStates;
            CgArcs = verificationOutput.CgArcs;
            LtsStates = verificationOutput.LtsStates;
            LtsArcs = verificationOutput.LtsArcs;
            CgRefArcs = verificationOutput.CgRefArcs;
            CgRefStates= verificationOutput.CgRefStates;
            DeadTransitions = verificationOutput.DeadTransitions;
            Deadlocks = verificationOutput.Deadlocks;
            Soundness = verificationOutput.Soundness;
            VerificationTime = verificationOutput.VerificationTime;
            Number = number;
            SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
            Id = verificationOutput.Id;
            //VerificationType = verificationOutput.VerificationType;
        }
    }
}
