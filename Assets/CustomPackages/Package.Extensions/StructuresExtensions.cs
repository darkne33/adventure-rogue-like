using System.Collections.Generic;

namespace CustomPackages.Package.Extensions
{
    public static class StructuresExtensions
    {
        public static void AddWithoutNull<T>(this List<T> list, T item)
        {
            if (item == null)
                return;
            
            list.Add(item);
        }
        
        public static void AddRangeWithoutNull<T>(this List<T> list, IEnumerable<T> items)
        {
            if (items == null)
                return;
            
            list.AddRange(items);
        }
    }
}