namespace DPN.Models.Extensions
{
    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashset, IEnumerable<T> items)
        {
            foreach(var item in items)
            {
                hashset.Add(item);
            }
        }
    }
}
