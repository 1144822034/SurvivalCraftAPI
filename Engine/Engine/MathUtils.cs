namespace Engine
{
    public static class MathUtils
    {
        const float PI = MathF.PI;

        const float E = MathF.E;

        public static int Min(int x1, int x2)
        {
            return Math.Min(x1, x2);
        }

        public static int Min(int x1, int x2, int x3)
        {
            return Min(Min(x1, x2), x3);
        }

        public static int Min(int x1, int x2, int x3, int x4)
        {
            return Min(Min(Min(x1, x2), x3), x4);
        }

        public static int Max(int x1, int x2)
        {
            return Math.Max(x1, x2);
        }

        public static int Max(int x1, int x2, int x3)
        {
            return Max(Max(x1, x2), x3);
        }

        public static int Max(int x1, int x2, int x3, int x4)
        {
            return Max(Max(Max(x1, x2), x3), x4);
        }

        public static int Sqr(int x)
        {
            return x * x;
        }

        public static bool IsPowerOf2(uint x)
        {
            return x != 0 && (x & (x - 1)) == 0;
        }

        public static uint NextPowerOf2(uint x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;
            return x;
        }

        public static uint Hash(uint key)
        {
            key ^= key >> 16;
            key *= 2146121005;
            key ^= key >> 15;
            key *= 2221713035u;
            key ^= key >> 16;
            return key;
        }

        public static int Hash(int key)
        {
            return (int)Hash((uint)key);
        }

        public static uint HashInverse(uint key)
        {
            key ^= key >> 16;
            key *= 1124208931;
            key ^= (key >> 15) ^ (key >> 30);
            key *= 493478565;
            key ^= key >> 16;
            return key;
        }

        public static long Sqr(long x)
        {
            return x * x;
        }

        public static bool IsPowerOf2(long x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }

        public static ulong NextPowerOf2(ulong x)
        {
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;
            x++;
            return x;
        }

        public static float Min(float x1, float x2)
        {
            return MathF.Min(x1, x2);
        }

        public static float Min(float x1, float x2, float x3)
        {
            return Min(Min(x1, x2), x3);
        }

        public static float Min(float x1, float x2, float x3, float x4)
        {
            return Min(Min(Min(x1, x2), x3), x4);
        }

        public static float Max(float x1, float x2)
        {
            return MathF.Max(x1, x2);
        }

        public static float Max(float x1, float x2, float x3)
        {
            return Max(Max(x1, x2), x3);
        }

        public static float Max(float x1, float x2, float x3, float x4)
        {
            return Max(Max(Max(x1, x2), x3), x4);
        }

        public static float Saturate(float x)
        {
            return Math.Clamp(x, 0, 1);
        }
        public static float Remainder(float x, float y)
        {
            return x - (MathF.Floor(x / y) * y);
        }

        public static float Sqr(float x)
        {
            return x * x;
        }
        public static float PowSign(float x, float n)
        {
            return MathF.Sign(x) * MathF.Pow(MathF.Abs(x), n);
        }

        public static float Lerp(float x1, float x2, float f)
        {
            return x1 + ((x2 - x1) * f);
        }

        public static float SmoothStep(float f)
        {
            return f * f * (3f - 2f * f);
        }

        public static float SmoothStep(float zero, float one, float f)
        {
            return SmoothStep(LinearStep(zero, one, f));
        }

        public static float SmootherStep(float f)
        {
            return f * f * f * (f * (6f * f - 15f) + 10f);
        }

        public static float SmootherStep(float zero, float one, float f)
        {
            return SmootherStep(LinearStep(zero, one, f));
        }

        public static float CatmullRom(float v1, float v2, float v3, float v4, float f)
        {
            float num = f * f;
            float num2 = num * f;
            return 0.5f * ((2f * v2) + ((v3 - v1) * f) + (((2f * v1) - (5f * v2) + (4f * v3) - v4) * num) + (((3f * v2) - v1 - (3f * v3) + v4) * num2));
        }

        public static float NormalizeAngle(float angle)
        {
            angle = (float)Math.IEEERemainder(angle, 6.2831854820251465);
            if (angle > PI)
            {
                angle -= PI * 2f;
            }
            else if (angle <= -PI)
            {
                angle += PI * 2f;
            }
            return angle;
        }

        public static float Sigmoid(float x, float steepness)
        {
            if (x <= 0f)
            {
                return 0f;
            }
            if (x >= 1f)
            {
                return 1f;
            }
            float num = MathF.Exp(steepness);
            float num2 = MathF.Exp(2f * steepness * x);
            return num * (num2 - 1f) / ((num - 1f) * (num2 + num));
        }

        public static float DegToRad(float degrees)
        {
            return degrees / 180f * PI;
        }

        public static float RadToDeg(float radians)
        {
            return radians * 180f / PI;
        }

        public static double Saturate(double x)
        {
            return Math.Clamp(x, 0.0, 1.0);
        }

        public static double Remainder(double x, double y)
        {
            return x - (Math.Floor(x / y) * y);
        }

        public static double PowSign(double x, double n)
        {
            return Math.Sign(x) * Math.Pow(Math.Abs(x), n);
        }

        public static double Lerp(double x1, double x2, double f)
        {
            return x1 + ((x2 - x1) * f);
        }

        public static double LinearStep(double zero, double one, double f)
        {
            return Saturate((f - zero) / (one - zero));
        }

        public static double SmoothStep(double f)
        {
            return f * f * (3.0 - 2.0 * f);
        }

        public static double SmoothStep(double zero, double one, double f)
        {
            return SmoothStep(LinearStep(zero, one, f));
        }

        public static double SmootherStep(double f)
        {
            return f * f * f * (f * (6.0 * f - 15.0) + 10.0);
        }

        public static double SmootherStep(double zero, double one, double f)
        {
            return SmootherStep(LinearStep(zero, one, f));
        }

        public static double CircleStep(double f)
        {
            return Math.Sqrt(2.0 * f - f * f);
        }

        public static double CircleStep(double zero, double one, double f)
        {
            return CircleStep(LinearStep(zero, one, f));
        }

        public static double CatmullRom(double v1, double v2, double v3, double v4, double f)
        {
            double num = f * f;
            double num2 = num * f;
            return 0.5 * ((2.0 * v2) + ((v3 - v1) * f) + (((2.0 * v1) - (5.0 * v2) + (4.0 * v3) - v4) * num) + (((3.0 * v2) - v1 - (3.0 * v3) + v4) * num2));
        }

        public static double NormalizeAngle(double angle)
        {
            angle = Math.IEEERemainder(angle, Math.PI * 2.0);
            if (angle > 3.1415927410125732)
            {
                angle -= Math.PI * 2.0;
            }
            else if (angle <= -Math.PI)
            {
                angle += Math.PI * 2.0;
            }
            return angle;
        }

        public static double DegToRad(double degrees)
        {
            return degrees / 180.0 * Math.PI;
        }

        public static double RadToDeg(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        public static float LinearStep(float zero, float one, float f)
        {
            return Saturate((f - zero) / (one - zero));
        }


        [Obsolete("Use Math.Clamp instead")]
        public static int Clamp(int x, int min, int max)
        {
            return x < min ? min : x <= max ? x : max;
        }

        [Obsolete("Use Math.Sign instead")]
        public static int Sign(int x) => Math.Sign(x);

        [Obsolete("Use Math.Abs instead")]
        public static int Abs(int x) => Math.Abs(x);

        [Obsolete("Use Math.Clamp instead")]
        public static long Clamp(long x, long min, long max)
        {
            return x < min ? min : x <= max ? x : max;
        }

        [Obsolete("Use Math.Sign instead")]
        public static long Sign(long x) => Math.Sign(x);

        [Obsolete("Use Math.Abs instead")]
        public static long Abs(long x) => Math.Abs(x);

        [Obsolete("Use Math.Ceiling instead")]
        public static float Ceiling(float x) => (float)Math.Ceiling((double)x);

        [Obsolete("Use Math.Round instead")]
        public static float Round(float x) => (float)Math.Round((double)x);

        [Obsolete("Use Math.Sqrt instead")]
        public static float Sqrt(float x) => (float)Math.Sqrt((double)x);

        [Obsolete("Use Math.Sin instead")]
        public static float Sin(float x) => (float)Math.Sin((double)x);

        [Obsolete("Use Math.Cos instead")]
        public static float Cos(float x) => (float)Math.Cos((double)x);

        [Obsolete("Use Math.Tan instead")]
        public static float Tan(float x) => (float)Math.Tan((double)x);

        [Obsolete("Use Math.Asin instead")]
        public static float Asin(float x) => (float)Math.Asin((double)x);

        [Obsolete("Use Math.Acos instead")]
        public static float Acos(float x) => (float)Math.Acos((double)x);

        [Obsolete("Use Math.Atan instead")]
        public static float Atan(float x) => (float)Math.Atan((double)x);

        [Obsolete("Use Math.Atan2 instead")]
        public static float Atan2(float y, float x) => (float)Math.Atan2((double)y, (double)x);

        [Obsolete("Use Math.Log instead")]
        public static float Log(float x) => (float)Math.Log((double)x);

        [Obsolete("Use Math.Log10 instead")]
        public static float Log10(float x) => (float)Math.Log10((double)x);

        [Obsolete("Use Math.Pow instead")]
        public static float Pow(float x, float n) => (float)Math.Pow((double)x, (double)n);

        [Obsolete("Use Math.Clamp instead")]
        public static double Clamp(double x, double min, double max)
        {
            return x < min ? min : x <= max ? x : max;
        }

        [Obsolete("Use Math.Sign instead")]
        public static double Sign(double x) => Math.Sign(x);

        [Obsolete("Use Math.Abs instead")]
        public static double Abs(double x) => Math.Abs(x);

        [Obsolete("Use Math.Floor instead")]
        public static double Floor(double x) => Math.Floor(x);

        [Obsolete("Use Math.Ceiling instead")]
        public static double Ceiling(double x) => Math.Ceiling(x);

        [Obsolete("Use Math.Round instead")]
        public static double Round(double x) => Math.Round(x);

        [Obsolete("Use x * x instead")]
        public static double Sqr(double x) => x * x;

        [Obsolete("Use Math.Sqrt instead")]
        public static double Sqrt(double x) => Math.Sqrt(x);

        [Obsolete("Use Math.Sin instead")]
        public static double Sin(double x) => Math.Sin(x);

        [Obsolete("Use Math.Cos instead")]
        public static double Cos(double x) => Math.Cos(x);

        [Obsolete("Use Math.Tan instead")]
        public static double Tan(double x) => Math.Tan(x);

        [Obsolete("Use Math.Asin instead")]
        public static double Asin(double x) => Math.Asin(x);

        [Obsolete("Use Math.Acos instead")]
        public static double Acos(double x) => Math.Acos(x);

        [Obsolete("Use Math.Atan instead")]
        public static double Atan(double x) => Math.Atan(x);

        [Obsolete("Use Math.Atan2 instead")]
        public static double Atan2(double y, double x) => Math.Atan2(y, x);

        [Obsolete("Use Math.Exp instead")]
        public static double Exp(double n) => Math.Exp(n);

        [Obsolete("Use Math.Log instead")]
        public static double Log(double x) => Math.Log(x);

        [Obsolete("Use Math.Log10 instead")]
        public static double Log10(double x) => Math.Log10(x);

        [Obsolete("Use Math.Pow instead")]
        public static double Pow(double x, double n) => Math.Pow(x, n);

        [Obsolete("Use MathF.Floor instead")]
        public static float Floor(float x) => MathF.Floor(x);
    }
}
