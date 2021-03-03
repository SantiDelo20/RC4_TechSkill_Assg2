using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Copied from Vicente Soler https://github.com/ADRC4/Voxel/blob/master/Assets/Scripts/Util/Util.cs

public enum Axis { X, Y, Z };
public enum BoundaryType { Inside = 0, Left = -1, Right = 1, Outside = 2 }

static class Util
{
    /// <summary>
    /// Extension method to Unities Vector3Int class. Now you can use a Vector3 variable and use the .ToVector3InRound to get the vector rounded to its integer values
    /// </summary>
    /// <param name="v">the Vector3 variable this method is applied to</param>
    /// <returns>the rounded Vector3Int value of the given Vector3</returns>
    public static Vector3Int ToVector3IntRound(this Vector3 v) => new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

    public static Vector3 Average(this IEnumerable<Vector3> vectors)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var vector in vectors)
        {
            sum += vector;
            count++;
        }

        sum /= count;
        return sum;
    }

    public static T MinBy<T>(this IEnumerable<T> items, Func<T, double> selector)
    {
        double minValue = double.MaxValue;
        T minItem = items.First();

        foreach (var item in items)
        {
            var value = selector(item);

            if (value < minValue)
            {
                minValue = value;
                minItem = item;
            }
        }

        return minItem;
    }

    public static float NormalizedDeviation(float[] population)
    {
        float mean = population.Sum() / 16f;

        float stDeviation = Mathf.Sqrt(
            population.Select(s =>
            Mathf.Pow(s - mean, 2f))
            .Sum()
            / population.Sum());
        float maxDeviation = Mathf.Sqrt(
            (Mathf.Pow(population.Sum() - mean, 2) +
            (population.Length - 1) * Mathf.Pow(mean, 2))
            / population.Sum());

        float normalizedDeviation = stDeviation / maxDeviation;

        return normalizedDeviation;
    }
}