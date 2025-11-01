namespace DPN.Models.Abstractions
{
    interface IIdentifiedElement
    {
        // According to current information, ids can be string values
        string Id { get; set; }
    }
}
