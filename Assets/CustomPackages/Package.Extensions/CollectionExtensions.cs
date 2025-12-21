using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace CustomPackages.Package.Extensions
{
    public static class CollectionExtensions
    {
        public static IList<T> GetRandomUnique<T>(this IList<T> list, int count)
        {
            IList<T> listDuplicate = new List<T>(list);
            
            List<T> result = new List<T>(count);

            for (int i = 0; i < count; i++)
            {
                T item = listDuplicate.GetRandom();
                listDuplicate.Remove(item);
                result.Add(item);
            }
            
            return result;
        }
        
        public static T GetRandom<T>(this ICollection<T> collection)
        {
            if (collection == null || collection.Count == 0) return default;
            int rnd = Random.Range(0, collection.Count);
            return collection.ElementAt(rnd);
        }

        public static void Shuffle<T>(this IList<T> list) => list.Shuffle(1);
        public static void Shuffle<T>(this IList<T> list, int detours)
        {
            for (; detours > 0; detours--)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    int randomIndex = Random.Range(0, list.Count);
                    (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
                }
            }
        }
        
        public static void BubbleSort<T>(this IList<T> list) where T : IComparable<T> => list.BubbleSort((a, b) => a.CompareTo(b));
        public static void BubbleSort<T>(this IList<T> list, bool descending) where T : IComparable<T>
        {
            if (!descending) list.BubbleSort((a, b) => a.CompareTo(b));
            else list.BubbleSort((a, b) => -a.CompareTo(b));
        }

        public static void BubbleSort<T>(this IList<T> list, IComparer<T> comparer) => list.BubbleSort(comparer.Compare);
        public static void BubbleSort<T>(this IList<T> list, IComparer<T> comparer, bool descending)
        {
            if (!descending) list.BubbleSort(comparer.Compare);
            else list.BubbleSort((a, b) => -comparer.Compare(a, b));
        }

        public static void BubbleSort<T>(this IList<T> list, Func<T, T, int> comparator)
        {
            int count = list.Count;
            for (int i = count; i > 0; i--)
            {
                bool sortedThisTurn = false;
                for (int j = 0; j < i - 1; j++)
                {
                    T current = list[j];
                    T next = list[j + 1];
                    int relation = comparator(current, next);
                    if (relation <= 0) continue;
                    list[j] = next;
                    list[j + 1] = current;
                    sortedThisTurn = true;
                }

                if (!sortedThisTurn) break;
            }
        }

        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int index = Random.Range(0, list.Count);
            return list[index];
        }
        
        public static T GetRandom<T>(this IList<T> list, out int index)
        {
            if (list == null || list.Count == 0)
            {
                index = 0;
                return default;
            }
            
            index = Random.Range(0, list.Count);
            return list[index];
        }

        public static T ExtractRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int index = Random.Range(0, list.Count);
            T element = list[index];
            list.RemoveAt(index);
            return element;
        }
        
        public static int IndexOf<T>(this IEnumerable<T> obj, T value)
        {
            return obj
                .Select((a, i) => (a.Equals(value)) ? i : -1)
                .Max();
        }

        public static int IndexOf<T>(this IEnumerable<T> obj, T value
            , IEqualityComparer<T> comparer)
        {
            return obj
                .Select((a, i) => (comparer.Equals(a, value)) ? i : -1)
                .Max();
        }
        
        public static T Next<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            bool flag = false;

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (flag) return enumerator.Current;

                    if(predicate(enumerator.Current))
                    {
                        flag = true;
                    }
                }
            }
            return default(T);
        }
        
        
        public static UnityEngine.Vector3 Average(IEnumerable<UnityEngine.Vector3> vectors)
        {
            var average = vectors.Aggregate(UnityEngine.Vector3.zero, (current, vector) => current + vector);
            return average / vectors.Count();
        }
        
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) 
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            // ReSharper disable once NotDisposedResource
            return list == null || list.Count == 0;
        }
    }
}