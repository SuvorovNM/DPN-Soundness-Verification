using DataPetriNetOnSmt.Enums;
using System.Xml.Linq;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetVerificationDomain.GraphVisualized;

namespace DataPetriNetParsers
{
    public class CgmlParser
    {
        public GraphToVisualize Deserialize(XDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var cgElement = document.Root?
                .Element("cg");
            if (cgElement == null)
            {
                throw new Exception("Document is incorrect. Can't find tag <cg>");
            }

            var constraintStates = new List<StateToVisualize>();
            var statesElement = cgElement.Element("states");
            foreach (var xmlState in statesElement.Elements())
            {
                var stateId = int.Parse(xmlState.Attribute("id").Value);
                var stateType = Enum.Parse<ConstraintStateType>(xmlState.Attribute("type").Value);

                var constraintFormula = xmlState.Element("constraint").Value;

                var tokensState = xmlState.Element("tokens");
                var placeTokensDict = new Dictionary<string, int>();
                foreach (var element in tokensState.Elements())
                {
                    placeTokensDict.Add(element.Name.ToString(), int.Parse(element.Value));
                }

                var constraintState = new StateToVisualize
                {
                    ConstraintFormula = constraintFormula,
                    Id = stateId,
                    StateType = stateType,
                    Tokens = placeTokensDict
                };

                constraintStates.Add(constraintState);
            }

            var constraintArcs = new List<ArcToVisualize>();
            var arcsElement = cgElement.Element("arcs");
            foreach (var xmlArc in arcsElement.Elements())
            {
                var transitionName = xmlArc.Attribute("transition_name").Value;
                var isSilent = bool.Parse(xmlArc.Attribute("is_silent").Value);
                var sourceStateId = int.Parse(xmlArc.Attribute("source_id").Value);
                var targetStateId = int.Parse(xmlArc.Attribute("target_id").Value);

                var constraintArc = new ArcToVisualize
                {
                    TransitionName = transitionName,
                    SourceStateId = sourceStateId,
                    TargetStateId = targetStateId,
                    IsSilent = isSilent
                };

                constraintArcs.Add(constraintArc);
            }

            var deadTransitions = new List<string>();
            var deadTransitionsElement = cgElement.Element("dead_transitions");
            foreach (var xmlDeadTransition in deadTransitionsElement.Elements())
            {
                deadTransitions.Add(xmlDeadTransition.Value);
            }

            var isBounded = bool.Parse(cgElement.Attribute("is_bounded").Value);
            bool? isRelaxedLazySound = cgElement.Attribute("is_relaxed_lazy_sound") != null
                ? bool.Parse(cgElement.Attribute("is_relaxed_lazy_sound").Value)
                : null;
            bool? isClassicalSound = cgElement.Attribute("is_classical_sound") != null
                ? bool.Parse(cgElement.Attribute("is_classical_sound").Value)
                : null;

            var graphTypeAttribute = cgElement.Attribute("graph_type");
            if (graphTypeAttribute == null ||
                !Enum.TryParse(graphTypeAttribute.Value, out GraphType graphType))
            {
                graphType = GraphType.Lts;
            }

            if (cgElement.Attribute("is_full") == null &&
                !bool.TryParse(cgElement.Attribute("is_full").Value, out var isFullGraph)) ;
            {
                isFullGraph = true;
            }

            if (cgElement.Attribute("soundness_type") == null ||
                !Enum.TryParse(cgElement.Attribute("soundness_type").Value, out SoundnessType soundnessType))
            {
                soundnessType = SoundnessType.Classical;
            }

            return new GraphToVisualize
            {
                States = constraintStates,
                Arcs = constraintArcs,
                GraphType = graphType,
                IsFull = isFullGraph,
                SoundnessProperties =
                    new SoundnessPropertiesToVisualize(isBounded, deadTransitions.ToArray(), isClassicalSound, isRelaxedLazySound)
            };
        }

        public XDocument Serialize(GraphToVisualize cg)
        {
            ArgumentNullException.ThrowIfNull(cg);

            var statesElement = new XElement("states");
            foreach (var state in cg.States)
            {
                var tokensElement = new XElement("tokens");
                foreach (var node in state.Tokens)
                {
                    var nodeElement = new XElement(node.Key, node.Value);
                    tokensElement.Add(nodeElement);
                }

                var constraintElement = new XElement("constraint", state.ConstraintFormula);

                var stateElement = new XElement("state");
                stateElement.Add(tokensElement);
                stateElement.Add(constraintElement);
                stateElement.SetAttributeValue("id", state.Id.ToString());
                stateElement.SetAttributeValue("type", state.StateType.ToString()); // Maybe better to int-value?

                statesElement.Add(stateElement);
            }

            var arcsElement = new XElement("arcs");
            foreach (var arc in cg.Arcs)
            {
                var arcElement = new XElement("arc");
                arcElement.SetAttributeValue("transition_name", arc.TransitionName);
                arcElement.SetAttributeValue("is_silent", arc.IsSilent);
                arcElement.SetAttributeValue("source_id", arc.SourceStateId);
                arcElement.SetAttributeValue("target_id", arc.TargetStateId);

                arcsElement.Add(arcElement);
            }

            var deadTransitionsElement = new XElement("dead_transitions");
            foreach (var deadTransition in cg.SoundnessProperties!.DeadTransitions)
            {
                deadTransitionsElement.Add(new XElement("transition"), deadTransition);
            }

            var cgElement = new XElement("cg",
                statesElement, arcsElement, deadTransitionsElement);
            cgElement.SetAttributeValue("is_bounded", cg.SoundnessProperties.Boundedness);
            cgElement.SetAttributeValue("is_classical_sound", cg.SoundnessProperties.ClassicalSoundness);
            cgElement.SetAttributeValue("is_relaxed_lazy_sound", cg.SoundnessProperties.RelaxedLazySoundness);
            cgElement.SetAttributeValue("graph_type", cg.GraphType.ToString());
            cgElement.SetAttributeValue("is_full", cg.IsFull.ToString());

            //cgElement.SetAttributeValue("dead_transitions", cg.DeadTransitions);

            var srcTree = new XElement("cgml", cgElement);

            var document = new XDocument(srcTree);

            return document;
        }
    }
}