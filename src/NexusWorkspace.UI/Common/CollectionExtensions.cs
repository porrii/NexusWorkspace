using System.Collections.ObjectModel;

namespace NexusWorkspace.UI;

internal static class CollectionExtensions
{
    /// <summary>Replaces every item in the collection, raising a single practical refresh.</summary>
    public static void Reset<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
