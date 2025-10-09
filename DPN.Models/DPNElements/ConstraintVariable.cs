using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public struct ConstraintVariable : IEquatable<ConstraintVariable>
    {
        public string Name { get; set; }
        public DomainType Domain { get; set; }
        public VariableType VariableType { get; set; }

        public bool Equals(ConstraintVariable other)
        {
            return Name == other.Name &&
                VariableType == other.VariableType &&
                Domain == other.Domain;
        }

        public override bool Equals(object obj)
        {
            return obj is ConstraintVariable c && this == c;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Domain.GetHashCode() ^ VariableType.GetHashCode();
        }
        public static bool operator ==(ConstraintVariable x, ConstraintVariable y)
        {
            return x.Name == y.Name &&
                x.VariableType == y.VariableType &&
                x.Domain == y.Domain;
        }
        public static bool operator !=(ConstraintVariable x, ConstraintVariable y)
        {
            return !(x == y);
        }
    }
}
