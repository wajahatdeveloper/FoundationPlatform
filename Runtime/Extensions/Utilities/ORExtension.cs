public static class ORExtension
{
    public static bool IsOneOf<TThis>(this TThis o, params TThis[] orList)
    {
        foreach (var obj in orList)
        {
            if (o.Equals(obj))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsOneOf(this bool o, params bool[] orList)
    {
        foreach (var obj in orList)
        {
            if (o == obj)
            {
                return true;
            }
        }

        return false;
    }
}