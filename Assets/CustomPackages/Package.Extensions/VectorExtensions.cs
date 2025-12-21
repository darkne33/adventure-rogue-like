using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace CustomPackages.Package.Extensions
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CalculateCenter(List<Vector3> points)
        {
            if (points.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i];
            }

            center /= points.Count;
            return center;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ClampVerticalInsideSlicedCircle(this Vector2 vector, float sliceOffset, float radius)
        {
            vector = vector.normalized * radius;
            float absY = Mathf.Abs(vector.y);
            float yMax = Mathf.Lerp(-absY, absY, sliceOffset);
            vector.y = Mathf.Clamp(vector.y, vector.y, yMax);
            return vector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ClampHorizontalInsideSlicedCircle(this Vector2 vector, float sliceOffset, float radius)
        {
            vector = vector.normalized * radius;
            float absX = Mathf.Abs(vector.x);
            float xMax = Mathf.Lerp(-absX, absX, sliceOffset);
            vector.x = Mathf.Clamp(vector.x, vector.x, xMax);
            return vector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(this Vector2 a, Vector2 b, Vector2 t) =>
            new(Mathf.Lerp(a.x, b.x, t.x),
                Mathf.Lerp(a.y, b.y, t.y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 LerpUnclamped(this Vector2 a, Vector2 b, Vector2 t) =>
            new(Mathf.LerpUnclamped(a.x, b.x, t.x),
                Mathf.LerpUnclamped(a.y, b.y, t.y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(this Vector3 a, Vector3 b, Vector3 t) =>
            new(Mathf.Lerp(a.x, b.x, t.x),
                Mathf.Lerp(a.y, b.y, t.y),
                Mathf.Lerp(a.z, b.z, t.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LerpUnclamped(this Vector3 a, Vector3 b, Vector3 t) =>
            new(Mathf.LerpUnclamped(a.x, b.x, t.x),
                Mathf.LerpUnclamped(a.y, b.y, t.y),
                Mathf.LerpUnclamped(a.z, b.z, t.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(this Vector4 a, Vector4 b, Vector4 t) =>
            new(Mathf.Lerp(a.x, b.x, t.x),
                Mathf.Lerp(a.y, b.y, t.y),
                Mathf.Lerp(a.z, b.z, t.z),
                Mathf.Lerp(a.w, b.w, t.w));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 LerpUnclamped(this Vector4 a, Vector4 b, Vector4 t) =>
            new(Mathf.LerpUnclamped(a.x, b.x, t.x),
                Mathf.LerpUnclamped(a.y, b.y, t.y),
                Mathf.LerpUnclamped(a.z, b.z, t.z),
                Mathf.LerpUnclamped(a.w, b.w, t.w));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Multiply(this Vector4 a, Vector4 b) => new(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(this Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Multiply(this Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Divide(this Vector4 a, Vector4 b) => new(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(this Vector3 a, Vector3 b) => new(a.x / b.x, a.y / b.y, a.z / b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Divide(this Vector2 a, Vector2 b) => new(a.x / b.x, a.y / b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int Modulo(this Vector2Int a, int b) => new(a.x % b, a.y % b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(this Vector4 source, Vector4 a, Vector4 b) =>
            new(Mathf.Clamp(source.x, a.x, b.x),
                Mathf.Clamp(source.y, a.y, b.y),
                Mathf.Clamp(source.z, a.z, b.z),
                Mathf.Clamp(source.w, a.w, b.w));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(this Vector3 source, Vector3 a, Vector3 b) =>
            new(Mathf.Clamp(source.x, a.x, b.x),
                Mathf.Clamp(source.y, a.y, b.y),
                Mathf.Clamp(source.z, a.z, b.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(this Vector2 source, Vector2 a, Vector2 b) =>
            new(Mathf.Clamp(source.x, a.x, b.x),
                Mathf.Clamp(source.y, a.y, b.y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(this Vector2 vector) =>
            new(Mathf.Abs(vector.x),
                Mathf.Abs(vector.y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(this Vector3 vector) =>
            new(Mathf.Abs(vector.x),
                Mathf.Abs(vector.y),
                Mathf.Abs(vector.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Abs(this Vector4 vector) =>
            new(Mathf.Abs(vector.x),
                Mathf.Abs(vector.y),
                Mathf.Abs(vector.z),
                Mathf.Abs(vector.w));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int GetSize<T>(this T[,] array) => new(array.GetLength(0), array.GetLength(1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PositionToIndex(this Vector2Int gridSize, Vector2Int pos) => pos.y * gridSize.x + pos.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PositionToIndex(this Vector2Int gridSize, int x, int y) => y * gridSize.x + x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int IndexToPosition(this Vector2Int gridSize, int index) =>
            new() { x = index % gridSize.x, y = index / gridSize.x };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRandom(this Vector2 vector) => Random.Range(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRandom(this Vector2Int vector) => Random.Range(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRandom(this Vector2Int vector, bool inclusive)
        {
            if (inclusive) vector.y += 1;
            return vector.GetRandom();
        }

        public static Vector3 Change3(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            if (x.HasValue) vector.x = x.Value;
            if (y.HasValue) vector.y = y.Value;
            if (z.HasValue) vector.z = z.Value;
            return vector;
        }
    }

    public static class Vector2ToVector3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XY0(this Vector2 vector) => new(vector.x, vector.y, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XY1(this Vector2 vector) => new(vector.x, vector.y, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X1Y(this Vector2 vector) => new(vector.x, 1, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X0Y(this Vector2 vector) => new(vector.x, 0, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YX0(this Vector2 vector) => new(vector.y, vector.x, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YX1(this Vector2 vector) => new(vector.y, vector.x, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Y1X(this Vector2 vector) => new(vector.y, 1, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Y0X(this Vector2 vector) => new(vector.y, 0, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Append(this Vector2 vector, float value) => new(vector.x, vector.y, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int XY0(this Vector2Int vector) => new(vector.x, vector.y, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int XY1(this Vector2Int vector) => new(vector.x, vector.y, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int X1Y(this Vector2Int vector) => new(vector.x, 1, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int X0Y(this Vector2Int vector) => new(vector.x, 0, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int YX0(this Vector2Int vector) => new(vector.y, vector.x, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int YX1(this Vector2Int vector) => new(vector.y, vector.x, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int Y1X(this Vector2Int vector) => new(vector.y, 1, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int Y0X(this Vector2Int vector) => new(vector.y, 0, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Int Append(this Vector2Int vector, int value) => new(vector.x, vector.y, value);
    }

    public static class Vector3ToVector2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XY(this Vector3 vector) => new(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XZ(this Vector3 vector) => new(vector.x, vector.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YX(this Vector3 vector) => new(vector.y, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YZ(this Vector3 vector) => new(vector.y, vector.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ZX(this Vector3 vector) => new(vector.z, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ZY(this Vector3 vector) => new(vector.z, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int XY(this Vector3Int vector) => new(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int XZ(this Vector3Int vector) => new(vector.x, vector.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int YX(this Vector3Int vector) => new(vector.y, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int YZ(this Vector3Int vector) => new(vector.y, vector.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ZX(this Vector3Int vector) => new(vector.z, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ZY(this Vector3Int vector) => new(vector.z, vector.y);
    }

    public static class Swizzle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYZ(this Vector3 a) => new(a.x, a.y, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYY(this Vector3 a) => new(a.x, a.y, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XYX(this Vector3 a) => new(a.x, a.y, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZX(this Vector3 a) => new(a.x, a.z, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZY(this Vector3 a) => new(a.x, a.z, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZZ(this Vector3 a) => new(a.x, a.z, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXX(this Vector3 a) => new(a.x, a.x, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXY(this Vector3 a) => new(a.x, a.x, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XXZ(this Vector3 a) => new(a.x, a.x, a.z);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YXZ(this Vector3 a) => new(a.y, a.x, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YXX(this Vector3 a) => new(a.y, a.x, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YXY(this Vector3 a) => new(a.y, a.x, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YZY(this Vector3 a) => new(a.y, a.z, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YZX(this Vector3 a) => new(a.y, a.z, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YZZ(this Vector3 a) => new(a.y, a.z, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYY(this Vector3 a) => new(a.y, a.y, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYX(this Vector3 a) => new(a.y, a.y, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 YYZ(this Vector3 a) => new(a.y, a.y, a.z);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZYX(this Vector3 a) => new(a.z, a.y, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZYY(this Vector3 a) => new(a.z, a.y, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZYZ(this Vector3 a) => new(a.z, a.y, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZXZ(this Vector3 a) => new(a.z, a.x, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZXY(this Vector3 a) => new(a.z, a.x, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZXX(this Vector3 a) => new(a.z, a.x, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZZZ(this Vector3 a) => new(a.z, a.z, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZZY(this Vector3 a) => new(a.z, a.z, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ZZX(this Vector3 a) => new(a.z, a.z, a.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XY1(this Vector3 a) => new(a.x, a.y, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZ1(this Vector3 a) => new(a.x, a.z, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XY0(this Vector3 a) => new(a.x, a.y, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 XZ0(this Vector3 a) => new(a.x, a.z, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X1Y(this Vector3 a) => new(a.x, 1, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X1Z(this Vector3 a) => new(a.x, 1, a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X0Y(this Vector3 a) => new(a.x, 0, a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 X0Z(this Vector3 a) => new(a.x, 0, a.z);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int XY(this Vector2Int vector) => new(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int X1(this Vector2Int vector) => new(vector.x, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int X0(this Vector2Int vector) => new(vector.x, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int YX(this Vector2Int vector) => new(vector.y, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int Y1(this Vector2Int vector) => new(vector.y, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int Y0(this Vector2Int vector) => new(vector.y, 0);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 XY(this Vector2 vector) => new(vector.x, vector.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 X1(this Vector2 vector) => new(vector.x, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 X0(this Vector2 vector) => new(vector.x, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 YX(this Vector2 vector) => new(vector.y, vector.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Y1(this Vector2 vector) => new(vector.y, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Y0(this Vector2 vector) => new(vector.y, 0);
    }
}