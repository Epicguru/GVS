using GVS.Entities;
using GVS.Sprites;
using GVS.World;
using GVS.World.Generation;
using GVS.World.Tiles;
using GVS.World.Tiles.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GVS
{
    public class Main : Game
    {
        public static ContentManager ContentManager
        {
            get { return main.Content; }
        }
        public static GraphicsDeviceManager Graphics;
        public static GraphicsDevice GlobalGraphicsDevice;
        public static GameWindow GameWindow;
        public static SpriteBatch SpriteBatch;
        public static Camera Camera;
        public static SpriteFont MediumFont;
        public static Sprite MissingTexture;

        // TODO fix this, need a better way for each tile to load content before packing atlas.
        public static Sprite GrassTile, MountainTile, TreeTile, StoneTile, StoneTopTile;
        public static Sprite WaterTile, SandTile, HouseTile;
        public static Sprite TileShadowTopLeft, TileShadowTopRight, TileShadowBottomLeft, TileShadowBottomRight;

        public static TileAtlas TileAtlas { get; private set; }
        public static IsoMap Map { get; private set; }
        public static Process GameProcess
        {
            get
            {
                return main.thisProcess;
            }
        }

        public static string ContentDirectory { get; private set; }
        public static Rectangle ClientBounds { get; private set; }

        private static Main main;

        private Process thisProcess;

        public Main()
        {
            main = this;
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            GameWindow = base.Window;
            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            thisProcess = Process.GetCurrentProcess();
        }

        protected override void Update(GameTime gameTime)
        {
            ClientBounds = Window.ClientBounds;
        }

        protected override void Initialize()
        {
            Thread.CurrentThread.Name = "Monogame Thread";
            Main.GlobalGraphicsDevice = base.GraphicsDevice;
            Camera = new Camera();
            Camera.Zoom = 0.5f;
            Debug.Init();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Find content directory...
            ContentDirectory = Path.Combine(Environment.CurrentDirectory, Content.RootDirectory);

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // Load some default fonts.
            MediumFont = Content.Load<SpriteFont>("Fonts/MediumFont");

            // Create an instance of the isometric map.
            Map = new IsoMap(100, 100, 3);

            // Create the main sprite atlas.
            TileAtlas = new TileAtlas(1024, 1024);

            // Loading missing texture sprite.
            MissingTexture = TileAtlas.Add("Textures/MissingTexture");
            MissingTexture.Pivot = new Vector2(0.5f, 1f); // Bottom center.

            // Temporarily load tiles here.
            GrassTile = TileAtlas.Add("Textures/GrassTile");
            MountainTile = TileAtlas.Add("Textures/Mountain");
            TreeTile = TileAtlas.Add("Textures/Trees");
            StoneTile = TileAtlas.Add("Textures/StoneTile");
            StoneTopTile = TileAtlas.Add("Textures/StoneTop");
            WaterTile = TileAtlas.Add("Textures/WaterTile");
            SandTile = TileAtlas.Add("Textures/SandTile");
            TileShadowTopRight = TileAtlas.Add("Textures/TileShadowTopRight");
            TileShadowTopLeft = TileAtlas.Add("Textures/TileShadowTopLeft");
            TileShadowBottomRight = TileAtlas.Add("Textures/TileShadowBottomRight");
            TileShadowBottomLeft = TileAtlas.Add("Textures/TileShadowBottomLeft");
            HouseTile = TileAtlas.Add("Textures/HouseTile");

            TileAtlas.Pack(false);

            // Generate isometric map.
            GenerateMap();

            Loop.Start();           
        }

        private void GenerateMap()
        {
            Random r = new Random(400);
            Noise n = new Noise(400);

            // First iteration to place tiles.
            for (int x = 0; x < Map.Width; x++)
            {
                for (int y = 0; y < Map.Depth; y++)
                {
                    for (int z = 0; z < Map.Height; z++)
                    {
                        const float SCALE = 0.035f;
                        Vector2 offset = new Vector2(500, 300);
                        Color c = (x + y) % 2 == 0 ? Color.White : Color.Lerp(Color.Black, Color.White, 0.95f);
                        float perlin = n.GetPerlin(x * SCALE + offset.X, y * SCALE + offset.Y, z * SCALE);
                        bool place = z == 0 || perlin >= 0.7f;

                        // Prevent floating tiles.
                        var below = Map.GetTile(x, y, z - 1);
                        if (z != 0 && (below == null || below is WaterTile))
                            place = false;

                        if (place)
                        {
                            const float WATER_HEIGHT = 0.5f;
                            const float SAND_HEIGHT = 0.55f;

                            Tile t;
                            if (z == 0 && perlin < WATER_HEIGHT)
                                t = new WaterTile();
                            else if (perlin < SAND_HEIGHT)
                                t = new SandTile();
                            else
                                t = new GrassTile();
                            
                            Map.SetTile(x, y, z, t);

                            //if (t is WaterTile)
                            //    c = Color.White;

                            t.BaseSpriteTint = c.LightShift(0.85f + 0.15f * ((z + 1f) / Map.Height));
                            if(t is WaterTile)
                            {
                                t.BaseSpriteTint = t.BaseSpriteTint.Multiply(Color.DeepSkyBlue);
                                t.BaseSpriteTint = t.BaseSpriteTint.LightShift(0.45f + (perlin / WATER_HEIGHT) * 0.8f);
                            }
                        }
                    }
                }
            }

            // Second iteration to place mountains, trees and all that stuff.
            for (int x = 0; x < Map.Width; x++)
            {
                for (int y = 0; y < Map.Depth; y++)
                {
                    for (int z = 0; z < Map.Height; z++)
                    {
                        Tile t = Map.GetTile(x, y, z);
                        if (t == null)
                            continue;
                        if (t is WaterTile || t is SandTile)
                            continue;

                        Tile above = Map.GetTile(x, y, z + 1);
                        if (above == null)
                        {
                            if (r.NextDouble() < 0.1f)
                            {
                                t.AddComponent(new Mountain(), 0);
                            }
                            else if (r.NextDouble() < 0.15f)
                            {
                                t.AddComponent(new Trees(), 0);
                            }
                            else if(r.NextDouble() < 0.02f)
                            {
                                t.AddComponent(new House(), 0);
                            }
                        }
                    }
                }
            }
        }

        protected override void UnloadContent()
        {
            Debug.Log("Unloading content...");
        }

        protected override void EndRun()
        {
            Loop.StopAndWait();
            Debug.Shutdown();
            Map.Dispose();
            thisProcess.Dispose();
            thisProcess = null;
            base.EndRun();
        }

        protected override void EndDraw()
        {
            // Do not present the device to the screen, this is handled in the Loop class.
        }

        internal static void MainUpdate()
        {
            // Debug update camera movement. Allows to move using WASD and zoom using E and Q.
            UpdateCameraMove();

            if (Input.KeyDown(Keys.F))
            {
                Camera.UpdateViewBounds = !Camera.UpdateViewBounds;
                Debug.Log($"Toggled update view bounds: {Camera.UpdateViewBounds}");
            }

            Map.Update();
            Entity.UpdateAll();
        }

        internal static void MainDraw()
        {
            Map.Draw(Main.SpriteBatch);
            Entity.DrawAll(Main.SpriteBatch);
            SpriteBatch.Draw(MissingTexture, new Rectangle(0, 0, 64, 32), Color.White, 1f, (float)Math.Sin(Time.time * 0.5f) * 2f, 1f + 5 * Input.MousePos.X / 800f, SpriteEffects.FlipVertically);
            SpriteBatch.Draw(Debug.Pixel, new Rectangle(0, 0, 5, 5), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, 1f);
        }

        private static void UpdateCameraMove()
        {
            Vector2 input = Vector2.Zero;
            if (Input.KeyPressed(Keys.A))
                input.X -= 1;
            if (Input.KeyPressed(Keys.D))
                input.X += 1;
            if (Input.KeyPressed(Keys.S))
                input.Y += 1;
            if (Input.KeyPressed(Keys.W))
                input.Y -= 1;
            input.NormalizeSafe();

            const float CHANGE_SPEED = 0.9f;
            const float CHANGE_UP_SPEED = 1f / 0.9f;
            int zoomChange = 0;
            if (Input.KeyPressed(Keys.E))
                zoomChange += 1;
            if (Input.KeyPressed(Keys.Q))
                zoomChange -= 1;
            if (Input.KeyDown(Keys.NumPad0))
                zoomChange = 420;
            if (Input.KeyDown(Keys.NumPad1))
                zoomChange = 69;
            if (Input.KeyDown(Keys.NumPad2))
                zoomChange = 69420;

            if(zoomChange != 0)
            {
                switch (zoomChange)
                {
                    case 420:
                        Main.Camera.Zoom = 0.5f;
                        break;
                    case 69:
                        Main.Camera.Zoom *= 2f;
                        break;
                    case 69420:
                        Main.Camera.Zoom *= 0.5f;
                        break;
                    default:
                        Main.Camera.Zoom *= (zoomChange > 0 ? CHANGE_UP_SPEED : CHANGE_SPEED);
                        break;
                }

                Main.Camera.Zoom = MathHelper.Clamp(Main.Camera.Zoom, 0.02f, 10f);
            }

            const float BASE_SPEED = 128f * 5f;
            Main.Camera.Position += input * BASE_SPEED * Time.deltaTime * (1f / Main.Camera.Zoom);
        }

        internal static void MainDrawUI()
        {
            Entity.DrawAllUI(Main.SpriteBatch);

            //if (TileAtlas.Texture != null)
            //    SpriteBatch.Draw(TileAtlas.Texture, Vector2.One * 20, Color.White);
        }
    }
}