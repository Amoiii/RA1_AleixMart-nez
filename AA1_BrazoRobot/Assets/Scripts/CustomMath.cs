using System;
using UnityEngine; 

[System.Serializable]
public struct MyVec3
{
    public float x, y, z;

    public MyVec3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

    // Conversiones
    public Vector3 ToUnity() => new Vector3(x, y, z);
    public static MyVec3 FromUnity(Vector3 v) => new MyVec3(v.x, v.y, v.z);

    // Operadores
    public static MyVec3 operator +(MyVec3 a, MyVec3 b) => new MyVec3(a.x + b.x, a.y + b.y, a.z + b.z);
    public static MyVec3 operator -(MyVec3 a, MyVec3 b) => new MyVec3(a.x - b.x, a.y - b.y, a.z - b.z);
    public static MyVec3 operator *(MyVec3 a, float d) => new MyVec3(a.x * d, a.y * d, a.z * d);
    public static MyVec3 operator /(MyVec3 a, float d) => new MyVec3(a.x / d, a.y / d, a.z / d);

    // Magnitud
    public float Magnitude() => MyMath.Sqrt(x * x + y * y + z * z);
    public static float Distance(MyVec3 a, MyVec3 b) => (a - b).Magnitude();
}

public static class MyMath
{
    public const float PI = 3.14159265f;
    public const float Rad2Deg = 180f / PI;

    public static float Sin(float rad) => (float)Math.Sin(rad);
    public static float Cos(float rad) => (float)Math.Cos(rad);
    public static float Sqrt(float val) => (float)Math.Sqrt(val);
    public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
    public static float Abs(float val) => val < 0 ? -val : val;

    public static float Clamp(float val, float min, float max)
    {
        if (val < min) return min;
        if (val > max) return max;
        return val;
    }

    // Interpolaciones propias
    public static float Lerp(float a, float b, float t)
    {
        if (t < 0) t = 0; if (t > 1) t = 1;
        return a + (b - a) * t;
    }
    //lerp
    public static float LerpAngle(float a, float b, float t)
    {
        float diff = b - a;
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return a + diff * t;
    }
}