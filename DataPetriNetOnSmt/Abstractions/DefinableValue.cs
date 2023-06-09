namespace DataPetriNetOnSmt.Abstractions
{
    public interface IDefinableValue
    {
        string? GetStringValue();
    }
    public class DefinableValue<T> : IDefinableValue//, IEquatable<DefinableValue<T>>
        where T : IEquatable<T>, IComparable<T>
    {
        public DefinableValue()
        {

        }
        public DefinableValue(T value)
        {
            Value = value;
        }

        public T Value
        {
            get
            {
                return definableValue;
            }
            set
            {
                definableValue = value;
            }
        }
        private T definableValue;

        public string? GetStringValue()
        {
            return definableValue.ToString();
        }
        
    }
}
