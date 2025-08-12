using Engine;

namespace Game;

public static class IntervalUtils
{
	public static float Normalize(float t)
	{
		return t - MathF.Floor(t);
	}

	public static float Add(float t, float interval)
	{
		return Normalize(t + interval);
	}

	public static float Interval(float t1, float t2)
	{
		return Normalize(t2 - t1);
	}

	public static float Distance(float t1, float t2)
	{
		return MathF.Min(Interval(t1, t2), Interval(t2, t1));
	}

	public static float Midpoint(float t1, float t2, float factor = 0.5f)
	{
		return Add(t1, Interval(t1, t2) * factor);
	}

	public static bool IsBetween(float t, float t1, float t2)
	{
		return Interval(t1, t) < Interval(t1, t2);
	}
}
