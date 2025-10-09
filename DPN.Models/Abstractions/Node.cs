namespace DPN.Models.Abstractions
{
    public abstract class Node : ILabeledElement, IIdentifiedElement
    {
        public string Label { get; set; }
        public string Id { get; set; }
    }
}
