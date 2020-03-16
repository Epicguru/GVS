using GVS.Sprites;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GVS.World
{
    public class TileAtlas : IDisposable
    {
        public int MaxTextureWidth
        {
            get { return maxTextureWidth; }
            set
            {
                if (value > 0)
                    maxTextureWidth = value;
                else
                    Debug.Error($"Cannot set max texture width of this atlas to {value}! Must be at least 1.");
            }
        }
        public int ExpectedTileSize
        {
            get { return expectedTileSize; }
            set
            {
                if (value > 0)
                    expectedTileSize = value;
                else
                    Debug.Error($"Cannot set tile size of this atlas to {value}! Must be at least 1.");
            }
        }
        public int TilesInRow
        {
            get { return MaxTextureWidth / ExpectedTileSize; }
        }
        public Texture2D Texture { get; private set; }

        private int expectedTileSize = 256;
        private int maxTextureWidth = 2048;
        private Dictionary<string, Sprite> packedSprites = new Dictionary<string, Sprite>();
        private List<Texture2D> texturesToPack = new List<Texture2D>();

        public Sprite Add(string contentPath)
        {
            return Add(Main.ContentManager.Load<Texture2D>(contentPath));
        }

        public Sprite Add(Texture2D texture)
        {
            if (texture == null)
                return null;

            string name = texture.Name;
            if (packedSprites.ContainsKey(name))
            {
                Debug.Warn($"Texture {name} has already been packed into this atlas!");
                return packedSprites[name];
            }

            int index = packedSprites.Count;
            int x = index % TilesInRow * ExpectedTileSize;
            int y = index / TilesInRow * ExpectedTileSize;

            if(texture.Width != ExpectedTileSize || texture.Height != ExpectedTileSize)
            {
                Debug.Error($"Unexpected tile size in atlas: Size is {texture.Width}x{texture.Height}, expected {ExpectedTileSize}x{ExpectedTileSize}.");
                return null;
            }

            Sprite newSprite = new Sprite(null, new Rectangle(x, y, texture.Width, texture.Height));
            packedSprites.Add(name, newSprite);
            texturesToPack.Add(texture);

            return newSprite;
        }

        public void Pack()
        {
            Debug.StartTimer("Pack tiles atlas");

            int packedCount = packedSprites.Count;
            int width;
            if (packedCount >= TilesInRow)
                width = ExpectedTileSize * TilesInRow;
            else
                width = packedCount * ExpectedTileSize;
            int height = (packedCount / TilesInRow + 1) * ExpectedTileSize;

            Debug.Log($"Packing the tile atlas, {packedCount} tiles for {width}x{height} texture.");

            Texture2D tex = new Texture2D(Main.GlobalGraphicsDevice, width, height, false, SurfaceFormat.Color);

            // Write all textures to the atlas.
            Color[] colors = new Color[ExpectedTileSize * ExpectedTileSize];
            for (int i = 0; i < texturesToPack.Count; i++)
            {
                var toPack = texturesToPack[i];
                Debug.Assert(!toPack.IsDisposed, "toPack.IsDisposed is true, expected false");

                int x = i % TilesInRow;
                int y = i / TilesInRow;

                Rectangle region = new Rectangle(x * ExpectedTileSize, y * ExpectedTileSize, ExpectedTileSize, ExpectedTileSize);
                toPack.GetData(colors);
                tex.SetData(0, region, colors, 0, colors.Length);

                toPack.Dispose();
            }
            texturesToPack.Clear();

            this.Texture = tex;
            // Assign texture to all the sprites that were created.
            foreach (var pair in packedSprites)
            {
                Sprite s = pair.Value;
                s.Texture = tex;
            }

            Debug.StopTimer(true);
        }

        public void Dispose()
        {
            if(texturesToPack != null)
            {
                texturesToPack.Clear();
                texturesToPack = null;
            }
            if (packedSprites != null)
            {
                // Remove the texture from all the sprites, to make sure that the sprites are not
                // keeping the texture in memory.
                foreach (var pair in packedSprites)
                {
                    var sprite = pair.Value;
                    if (sprite != null)
                        sprite.Texture = null;
                }
                packedSprites.Clear();
                packedSprites = null;
            }
            if (Texture != null)
            {
                Texture.Dispose();
                Texture = null;
            }
        }
    }
}
