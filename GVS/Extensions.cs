using GeonBit.UI;
using GeonBit.UI.Entities;
using GVS.World;
using Lidgren.Network;
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
            byte byteA = (byte)MathHelper.Clamp((a * 255), 0, 255);
            c.A = byteA;
            return c;
        }

        #region UI

        public static Entity GetTopParent(this Entity e)
        {
            Entity current = e;
            while(current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }

        public static void ExtendToParentBounds(this Entity e, bool width, bool height)
        {
            if (!width && !height)
                return;

            if(e.Parent == null)
            {
                Debug.Warn($"Cannot extend {e} to parent bounds because it has no parent! Make sure to assign parent first!");
                return;
            }

            float cw = e.Size.X;
            float ch = e.Size.Y;

            var topParent = e.GetTopParent();
            bool putInRoot = false;
            if(topParent != UserInterface.Active.Root)
            {
                UserInterface.Active.AddEntity(topParent);
                putInRoot = true;
            }

            e.CalcDestRect();
            var offset = e.GetRelativeOffset();
            if (width)
            {
                cw = e.Parent.Size.X - offset.X - e.Parent.Padding.X;
            }
            if (height)
            {
                ch = e.Parent.Size.Y - offset.Y - e.Parent.Padding.Y;
            }

            e.Size = new Vector2(cw, ch);

            if (putInRoot)
            {
                // Remove from root.
                UserInterface.Active.RemoveEntity(topParent);
            }
        }

        public static void MatchTextWidth(this Label label)
        {
            label.CalcTextActualRectWithWrap();
            label.WrapWords = false;
            float y = label.Size.Y;
            label.Size = new Vector2(label.GetActualDestRect().Width + 4, y);
        }

        #endregion

        #region Networking

        public static void Write(this NetOutgoingMessage msg, Point3D point)
        {
            msg.Write(point.X);
            msg.Write(point.Y);
            msg.Write(point.Z);
        }

        public static Point3D ReadPoint3D(this NetIncomingMessage msg)
        {
            int x = msg.ReadInt32();
            int y = msg.ReadInt32();
            int z = msg.ReadInt32();

            return new Point3D(x, y, z);
        }

        #endregion
    }
}
