using DPN.Models.DPNElements;
using DPN.Models.Enums;
using Microsoft.Z3;

namespace DPN.Models
{
	[Serializable]
	public class DataPetriNet
	{
		[System.Xml.Serialization.XmlIgnoreAttribute]
		public Context Context { get; set; }

		public string Id { get; set; }
		public string Name { get; set; }
		public List<Place> Places { get; set; }
		public List<Transition> Transitions { get; set; }
		public List<Arc> Arcs { get; set; }

		[System.Xml.Serialization.XmlIgnoreAttribute]
		public VariablesStore Variables { get; set; }

		public Marking FinalMarking => Marking.FinalMarkingFromDpnPlaces(Places);

		public DataPetriNet(Context context)
		{
			Context = context;

			Places = new List<Place>();
			Transitions = new List<Transition>();
			Arcs = new List<Arc>();
			Variables = new VariablesStore();
			Name = string.Empty;
			Id = string.Empty;
		}

		public object Clone(bool resetBaseTransitionIds = false)
		{
			var dpn = new DataPetriNet(Context);
			dpn.Name = Name;
			dpn.Id = Id;
			dpn.Places = Places.Select(place => (Place)place.Clone()).ToList();
			dpn.Transitions = Transitions.Select(transition => (Transition)transition.Clone(resetBaseTransitionIds)).ToList();

			var placesDict = dpn.Places.ToDictionary(place => place.Id);
			var transitionsDict = dpn.Transitions.ToDictionary(transition => transition.Id);

			foreach (var arc in Arcs)
			{
				dpn.Arcs.Add(arc.Type == ArcType.PlaceTransition
					? new Arc(placesDict[arc.Source.Id], transitionsDict[arc.Destination.Id], arc.Weight)
					: new Arc(transitionsDict[arc.Source.Id], placesDict[arc.Destination.Id], arc.Weight));
			}

			foreach (DomainType domainType in Enum.GetValues(typeof(DomainType)))
			{
				var varKeys = Variables[domainType].GetKeys();

				foreach (var varKey in varKeys)
				{
					var variable = Variables[domainType].Read(varKey);

					dpn.Variables[domainType].Write(varKey, variable);
				}
			}

			return dpn;
		}
	}
}