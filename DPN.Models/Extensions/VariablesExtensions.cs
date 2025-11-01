using DPN.Models.Enums;

namespace DPN.Models.Extensions;

public static class VariablesExtensions
{
	public static Dictionary<string, DomainType> GetVariablesDictionary(this DataPetriNet dpn)
	{
		return dpn.Variables
			.GetAllVariables()
			.ToDictionary(kvp => kvp.name, kvp => kvp.domain);
	}
}