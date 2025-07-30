namespace TwistOfFayte.Data;

public enum PriorityMob
{
    ForlornMaiden = 6737,

    TheForlorn = 6738,
}

public static class PriorityMobsExtensions
{
    public static uint GetDataId(this PriorityMob mob)
    {
        return (uint)mob;
    }
}
