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
        public static Texture2D MissingTexture;

        // TODO fix this, need a better way for each tile to load content before packing atlas.
        public static Sprite GrassTile, MountainTile, TreeTile;

        public static IsoMap Map { get; private set; }

        public static string ContentDirectory { get; private set; }
        public static Rectangle ClientBounds { get; private set; }

        private static Main main;

        public Main()
        {
            main = this;
            Graphics = new GraphicsDeviceManager(this);
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            GameWindow = base.Window;
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
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

            Debug.Init();

            Map = new IsoMap(100, 100, 20);
            //Map.SetTile(0, 0, 0, new GrassTile());
            //Map.SetTile(1, 0, 0, new GrassTile());
            //Map.SetTile(2, 0, 0, new GrassTile());
            //Map.SetTile(0, 1, 0, new GrassTile());
            //Map.SetTile(2, 0, 1, new GrassTile());
            //Map.SetTile(2, 2, 2, new GrassTile());

            Random r = new Random();
            Noise n = new Noise(r.Next(0, 100000));

            // First iteration to place tiles.
            for (int x = 0; x < Map.Width; x++)
            {
                for (int y = 0; y < Map.Depth; y++)
                {
                    for (int z = 0; z < Map.Height; z++)
                    {
                        const float SCALE = 0.1f;
                        Color c = (x + y) % 2 == 0 ? Color.White : Color.Gray.LightShift(1.8f);
                        bool place = z == 0 || n.GetPerlin(x * SCALE, y * SCALE, z * SCALE) >= 0.7f;

                        // Prevent floating tiles.
                        if (z != 0 && Map.GetTile(x, y, z - 1) == null)
                            place = false;

                        if (place)
                        {
                            Tile t = new GrassTile();
                            Map.SetTile(x, y, z, t);

                            t.BaseSpriteTint = c.LightShift(0.7f + 0.3f * ((z + 1f) / Map.Height));
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
                        Tile above = Map.GetTile(x, y, z + 1);
                        if(t != null && above == null)
                        {
                            if(r.NextDouble() < 0.15f)
                            {
                                t.AddComponent(new Mountain(), 0);
                            }
                            else if(r.NextDouble() < 0.2f)
                            {
                                t.AddComponent(new Trees(), 0);
                            }
                        }
                    }
                }
            }

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

            // Missing texture.
            MissingTexture = Content.Load<Texture2D>("Textures/MissingTexture");

            // Temporarily load tiles here.
            GrassTile = Map.TileAtlas.Add("Textures/BaseCube");
            MountainTile = Map.TileAtlas.Add("Textures/Mountain");
            TreeTile = Map.TileAtlas.Add("Textures/Trees");

            Map.TileAtlas.Pack();

            Loop.Start();           
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

            if(zoomChange != 0)
            {
                if (zoomChange == 420)
                    Main.Camera.Zoom = 1f;
                else
                    Main.Camera.Zoom = MathHelper.Clamp(Main.Camera.Zoom * (zoomChange > 0 ? CHANGE_UP_SPEED : CHANGE_SPEED), 0.05f, 10f);
            }

            const float BASE_SPEED = 128f * 5f;
            Main.Camera.Position += input * BASE_SPEED * Time.deltaTime * (1f / Main.Camera.Zoom);
        }

        internal static void MainDrawUI()
        {
            Entity.DrawAllUI(Main.SpriteBatch);

            if(Map.TileAtlas.Texture != null)
                SpriteBatch.Draw(Map.TileAtlas.Texture, Vector2.Zero, Color.White);
        }
    }
}