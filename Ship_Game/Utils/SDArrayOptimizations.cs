using SDUtils;

namespace Ship_Game;

public static class SDArrayOptimizations
{
    // @note This is a stable remove, maintaining object ordering
    public static void RemoveInActiveObjects<T>(this Array<T> list) where T : GameplayObject
    {
        int size = 0;
        int count = list.Count;
        T[] items = list.GetInternalArrayItems();
        for (int i = 0; i < count; ++i)
        {
            if (items[i].Active)
            {
                items[size] = items[i];
                ++size;
            }
        }
        list.Resize(size);
    }
}