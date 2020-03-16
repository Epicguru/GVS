using Microsoft.Xna.Framework;

namespace GVS
{
    public static class Extensions
    {
        /// <summary>
        /// Returns the normalized version of the vector without modifying the original vector.
        /// If the original vector is (0, 0) then the returned vector will also be
        /// (0, 0).
        /// </summary>
        /// <param name="vector">The input </param>
        /// <returns>The normalized vector value, which always has a length of 1, unless the input is (0, 0).</returns>
        public static Vector2 Normalized(this Vector2 vector)
        {
            if (vector == Vector2.Zero)  // (0, 0)
                return vector;
            if (vector == Vector2.UnitX) // (1, 0)
                return vector;
            if (vector == Vector2.UnitY) // (0, 1)
                return vector;

            float length = vector.Length();
            return new Vector2(vector.X / length, vector.Y / length);
        }

        public static void NormalizeSafe(this ref Vector2 vector)
        {
            vector = vector.Normalized();
        }

        public static int Area(this Rectangle r)
        {
            return r.Width * r.Height;
        }

        public static Color Multiply(this Color c, Color other)
        {
            return new Color(c.ToVector4() * other.ToVector4());
        }

        public static Color LightShift(this Color c, float multiplier)
        {
            Color m = c * multiplier;
            m.A = c.A;
            return m;
        }

        public static Color AlphaShift(this Color c, float multiplier)
        {
            float a = c.A / 255f;
            a *= multiplier;
            byte byteA = (byte)(a * 255);
            c.A = byteA;
            return c;
        }
    }
}
