
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GVS.World
{
    public class IsoMap : IDisposable
    {
        /// <summary>
        /// The size of this map in tiles, on the X axis. (Towards screen bottom right)
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The size of this map in tiles, on the Y axis. (Towards screen bottom left)
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// The size of this map in tiles, on the Z axis. (Towards screen top)
        /// </summary>
        public int Height { get; private set; }

        public float SingleTileDepth { get; private set; }
        public TileAtlas TileAtlas { get; private set; }

        private readonly Tile[,,] tiles; // TODO convert to 1D array, for speed.

        public IsoMap(int width, int depth, int height)
        {
            this.Width = width;
            this.Depth = depth;
            this.Height = height;
            this.SingleTileDepth = 1f / (width * height * depth - 1);
            TileAtlas = new TileAtlas();

            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Must be greater than zero!");
            if (depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth), "Must be greater than zero!");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Must be greater than zero!");

            tiles = new Tile[width, depth, height];

            Debug.Log($"Created new IsoMap, {width}x{depth}x{height} for total {width * depth * height} tiles.");
        }

        public Point GetTileDrawPosition(Point3D mapCoords)
        {
            const int TILE_SIZE = 256;
            const int HALF_TILE = TILE_SIZE / 2;
            const int QUARTER_TILE = TILE_SIZE / 4;

            int x = HALF_TILE * mapCoords.Y;
            int y = QUARTER_TILE * mapCoords.Y;

            x -= HALF_TILE * mapCoords.X;
            y += QUARTER_TILE * mapCoords.X;

            y -= HALF_TILE * mapCoords.Z;

            return new Point(x, y);
        }

        public float GetTileDrawDepth(Point3D mapCoords)
        {
            const float MAX = 0.9f;

            float heightStep = Width * Depth;
            float index = mapCoords.X + mapCoords.Y * Width + mapCoords.Z * heightStep;
            int maxIndex = Width * Depth * Height - 1;
            return (index / maxIndex) * MAX;
        }

        public Vector2 GetTileCoordinatesFromWorldPoint(Vector2 flatWorldPos)
        {
            const int TILE_SIZE = 256;
            const int H = TILE_SIZE / 2;
            const int Q = TILE_SIZE / 4;

            // Assumes that it is over the z = 0 layer.
            const float Z = 0f;


            (float inX, float inY) = flatWorldPos;
            inX -= H;
            inY -= H;

            // Don't ask, it just works.
            var y = (inY + Z * H) / (2 * Q) + inX / (2 * H);
            var x = y - inX / H;

            return new Vector2(x + 1, y + 1);
        }

        public void Update()
        {
            // URGTODO implement me.
        }

        public bool IsPointInRange(int x, int y, int z)
        {
            return x >= 0 && x < Width && y >= 0 && y < Depth && z >= 0 && z < Height;
        }

        public bool IsPointInRange(Point3D point)
        {
            return IsPointInRange(point.X, point.Y, point.Z);
        }

        public Tile GetTile(int x, int y, int z)
        {
            return IsPointInRange(x, y, z) ? tiles[x, y, z] : null;
        }

        public bool SetTile(int x, int y, int z, Tile tile, bool sendPlace = true, bool sendRemove = true)
        {
            // Check that the tile is not already placed.
            if(tile != null)
            {
                if(tile.Map != null)
                {
                    Debug.Error($"Tile {tile} is already placed on the map somewhere! Cannot place again!");
                    return false;
                }
            }

            if(IsPointInRange(x, y, z))
            {
                // Get the current tile at that position and send message if not null and enabled.
                Tile current = tiles[x, y, z];
                if(current != null && sendRemove)
                {
                    current.UponRemoved(this);
                    current.Map = null;
                    current.Position = Point3D.Zero;
                }

                tiles[x, y, z] = tile;
                if(tile != null)
                {
                    // Set position and map reference.
                    tile.Position = new Point3D(x, y, z);
                    tile.Map = this;

                    // Send message, if enabled.
                    if (sendPlace)
                        tile.UponPlaced(this);
                }

                return true;
            }
            else
            {
                Debug.Error($"Position for tile ({x}, {y}, {z}) is out of bounds!");
                return false;
            }
        }

        public void Draw(SpriteBatch spr)
        {
            var cam = Main.Camera;
            var bounds = cam.WorldViewBounds;
            Vector2 topLeft = GetTileCoordinatesFromWorldPoint(bounds.Location.ToVector2());
            Vector2 bottomRight = GetTileCoordinatesFromWorldPoint(bounds.Location.ToVector2() + bounds.Size.ToVector2());
            Debug.Text($"Top left: {topLeft}");
            Debug.Text($"Bottom right: {bottomRight}");

            // The 'End Y' comes from the bottom left coordinate because screen right to left is positive Y.
            int endY = Depth;
            int endX = Width;

            // Have to draw from bottom layer up, from top to bottom.
            for (int z = 0; z < Height; z++)
            {
                for (int x = 0; x < endX; x++)
                {
                    for (int y = 0; y < endY; y++)
                    {
                        Tile tile = tiles[x, y, z];
                        if (tile == null)
                            continue;

                        tile.Draw(spr);
                    }
                }
            }
        }

        public void Dispose()
        {
            TileAtlas.Dispose();
        }
    }
}
