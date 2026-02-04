using System;
using UnityEngine;

// Estructura vectorial propia
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

// NUEVO: Estructura Quaterniones

[System.Serializable]
public struct MyQuat
{
    public float x, y, z, w;

    public MyQuat(float x, float y, float z, float w)
    {
        this.x = x; this.y = y; this.z = z; this.w = w;
    }

    // Solo usamos Unity al final para pasar al Transform
    public Quaternion ToUnity() => new Quaternion(x, y, z, w);
}

public static class MyMath
{
    public const float PI = 3.14159265f;
    public const float Deg2Rad = PI / 180f; 
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

    // Interpolaciones
    public static float Lerp(float a, float b, float t)
    {
        if (t < 0) t = 0; if (t > 1) t = 1;
        return a + (b - a) * t;
    }

    public static float LerpAngle(float a, float b, float t)
    {
        float diff = b - a;
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return a + diff * t;
    }

    //Propia libreria de euler
    public static MyQuat Euler(float x, float y, float z)
    {
        // 1 Convertir a radianes y dividir por 2
        float cx = Cos(x * Deg2Rad * 0.5f);
        float sx = Sin(x * Deg2Rad * 0.5f);
        float cy = Cos(y * Deg2Rad * 0.5f);
        float sy = Sin(y * Deg2Rad * 0.5f);
        float cz = Cos(z * Deg2Rad * 0.5f);
        float sz = Sin(z * Deg2Rad * 0.5f);

        // 2 Construir el Cuaternión 
        MyQuat q;
        q.w = cx * cy * cz + sx * sy * sz;
        q.x = sx * cy * cz - cx * sy * sz;
        q.y = cx * sy * cz + sx * cy * sz;
        q.z = cx * cy * sz - sx * sy * cz;

        return q;
    }
}