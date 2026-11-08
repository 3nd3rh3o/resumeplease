using System.Collections.Generic;

public static class ListUtils
{
    /// <summary>
    /// Pop the first element from the array and return it, it is removed from the array.
    /// </summary>
    /// <param name="arr"></param>
    /// <returns>The first element of the array.</returns>
    public static ref T PopFirst<T>(ref T[] arr)
    {
        ref T first = ref arr[0];
        T[] newArr = new T[arr.Length - 1];
        for (int i = 1; i < arr.Length; i++)
        {
            newArr[i - 1] = arr[i];
        }
        arr = newArr;
        return ref first;
    }

    // same for List<T>
    public static T PopFirst<T>(ref List<T> list)
    {
        T first = list[0];
        list.RemoveAt(0);
        return first;
    }
}