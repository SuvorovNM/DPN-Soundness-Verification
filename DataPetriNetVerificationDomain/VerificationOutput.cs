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
    public class MainVerificationInfo
    {
        public bool SatisfiesCounditions { get; init; }
        public ushort Places { get; init; }
        public ushort Transitions { get; init; }
        public ushort Arcs { get; init; }
        public ushort Variables { get; init; }
        public ushort Conditions { get; init; }
        public bool Boundedness { get; init; }
        public int LtsStates { get; init; }
        public int LtsArcs { get; init; }
        public int CgRefStates { get; init; }
        public int CgRefArcs { get; init; }
        public ushort DeadTransitions { get; init; }
        public bool Deadlocks { get; init; }
        public bool Soundness { get; init; }
        public string VerificationTime { get; init; }
        public string LtsTime { get; init; }
        public string CgRefTime { get; init; }
        public string Id { get; init; }
        public MainVerificationInfo()
        {

        }
        public MainVerificationInfo(MainVerificationInfo verificationOutput)
        {
            SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
            Places = verificationOutput.Places;
            Transitions = verificationOutput.Transitions;
            Arcs = verificationOutput.Arcs;
            Variables = verificationOutput.Variables;
            Conditions = verificationOutput.Conditions;
            Boundedness = verificationOutput.Boundedness;
            LtsStates = verificationOutput.LtsStates;
            LtsArcs = verificationOutput.LtsArcs;
            CgRefStates = verificationOutput.CgRefStates;
            CgRefArcs = verificationOutput.CgRefArcs;
            DeadTransitions = verificationOutput.DeadTransitions;
            Deadlocks = verificationOutput.Deadlocks;
            Soundness = verificationOutput.Soundness;
            VerificationTime = verificationOutput.VerificationTime;
            LtsTime = verificationOutput.LtsTime;
            CgRefTime = verificationOutput.CgRefTime;
            Id = verificationOutput.Id;
        }
        public MainVerificationInfo(BasicVerificationOutput verificationOutput)
        {
            SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
            Places = verificationOutput.Places;
            Transitions = verificationOutput.Transitions;
            Arcs = verificationOutput.Arcs;
            Variables = verificationOutput.Variables;
            Conditions = verificationOutput.Conditions;
            Boundedness = verificationOutput.Boundedness;
            LtsStates = verificationOutput.LtsStates;
            LtsArcs = verificationOutput.LtsArcs;
            CgRefStates = verificationOutput.CgRefStates;
            CgRefArcs = verificationOutput.CgRefArcs;
            DeadTransitions = verificationOutput.DeadTransitions;
            Deadlocks = verificationOutput.Deadlocks;
            Soundness = verificationOutput.Soundness;
            VerificationTime = verificationOutput.VerificationTime;
            LtsTime = verificationOutput.LtsTime;
            CgRefTime = verificationOutput.CgRefTime;
            Id = verificationOutput.Id;
        }
        public MainVerificationInfo(OptimizedVerificationOutput verificationOutput)
        {
            SatisfiesCounditions = verificationOutput.SatisfiesCounditions;
            Places = verificationOutput.Places;
            Transitions = verificationOutput.Transitions;
            Arcs = verificationOutput.Arcs;
            Variables = verificationOutput.Variables;
            Conditions = verificationOutput.Conditions;
            Boundedness = verificationOutput.Boundedness;
            LtsStates = verificationOutput.LtsStates;
            LtsArcs = verificationOutput.LtsArcs;
            CgRefStates = verificationOutput.CgRefStates;
            CgRefArcs = verificationOutput.CgRefArcs;
            DeadTransitions = verificationOutput.DeadTransitions;
            Deadlocks = verificationOutput.Deadlocks;
            Soundness = verificationOutput.Soundness;
            VerificationTime = verificationOutput.VerificationTime;
            LtsTime = verificationOutput.LtsTime;
            CgRefTime = verificationOutput.CgRefTime;
            Id = verificationOutput.Id;
        }
    }
    public class BasicVerificationOutput : MainVerificationInfo
    {
        public string TransformationTime { get; init; }

        public BasicVerificationOutput(
            DataPetriNet dpn,
            bool satisfiesConditions,
            ClassicalLabeledTransitionSystem lts,
            ConstraintGraph? cgRefined,
            SoundnessProperties? soundnessProperties,
            long millisecondsForLts,
            long millisecondsForTransformation,
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
            Boundedness = soundnessProperties?.Boundedness ?? false;
            LtsStates = lts.ConstraintStates.Count;
            LtsArcs = lts.ConstraintArcs.Count;
            DeadTransitions = (ushort)(soundnessProperties?.DeadTransitions.Count ?? 0);
            Deadlocks = soundnessProperties?.Deadlocks ?? false;
            Soundness = soundnessProperties?.Soundness ?? false;
            VerificationTime = (millisecondsForLts + millisecondsForTransformation + millisecondsForCgRefined).ToString();
            SatisfiesCounditions = satisfiesConditions;
            Id = dpn.Name;
            LtsTime = millisecondsForLts.ToString();
            TransformationTime = millisecondsForTransformation.ToString();
            CgRefTime = millisecondsForCgRefined.ToString();
            CgRefArcs = cgRefined?.ConstraintArcs.Count ?? -1;
            CgRefStates = cgRefined?.ConstraintStates.Count ?? -1;
        }
        public BasicVerificationOutput()
        {

        }
    }
    public class OptimizedVerificationOutput : MainVerificationInfo
    {
        public int CgStates { get; init; }
        public int CgArcs { get; init; } 
        public string CgTime { get; init; }

        public OptimizedVerificationOutput()
        {

        }

        public OptimizedVerificationOutput(
            DataPetriNet dpn, 
            bool satisfiesConditions,
            ClassicalLabeledTransitionSystem lts,
            ConstraintGraph? cg,
            ConstraintGraph? cgRefined,
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
            CgStates = cg?.ConstraintStates.Count ?? -1;
            CgArcs = cg?.ConstraintArcs.Count ?? -1;
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
            CgRefArcs = cgRefined?.ConstraintArcs?.Count ?? -1;
            CgRefStates= cgRefined?.ConstraintStates?.Count ?? -1;
        }
    }
    public class VerificationOutputWithNumber : MainVerificationInfo
    {
        public int Number { get; init; }

        public VerificationOutputWithNumber(MainVerificationInfo verificationOutput, int number)
        {
            Places = verificationOutput.Places;
            Transitions = verificationOutput.Transitions;
            Arcs = verificationOutput.Arcs;
            Variables = verificationOutput.Variables;
            Conditions = verificationOutput.Conditions;
            Boundedness = verificationOutput.Boundedness;
            //CgStates = verificationOutput.CgStates;
            //CgArcs = verificationOutput.CgArcs;
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
            LtsTime = verificationOutput.LtsTime;
            CgRefTime = verificationOutput.CgRefTime;
            //VerificationType = verificationOutput.VerificationType;
        }
    }
}
