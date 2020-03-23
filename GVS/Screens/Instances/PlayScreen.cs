using GVS.Entities;
using GVS.Entities.Instances;
using GVS.World;
using GVS.World.Generation;
using GVS.World.Tiles;
using GVS.World.Tiles.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using GVS.Networking;

namespace GVS.Screens.Instances
{
    public class PlayScreen : GameScreen
    {
        public PlayScreen() : base("Play Screen")
        {
        }

        public override void Load()
        {
            // Create an instance of the isometric map.
            LoadingScreenText = "Creating map...";
            Main.Map = new IsoMap(100, 100, 3);

            // Generate isometric map.
            LoadingScreenText = "Generating map...";
            GenerateMap();

            LoadingScreenText = "Creating server...";
            Main.Server = new GameServer(7777, 8);
            Main.Server.Start();

            LoadingScreenText = "Connecting local client...";
            Main.Client = new GameClient();
            Main.Client.Connect("localhost", 7777);
        }

        public override void Unload()
        {
            Main.Server.Dispose();
            Main.Server = null;

            var map = Main.Map;
            Main.Map = null;
            map.Dispose();
        }

        public override void Update()
        {
            if (Input.KeyDown(Keys.F))
            {
                Main.Camera.UpdateViewBounds = !Main.Camera.UpdateViewBounds;
                Debug.Log($"Toggled update view bounds: {Main.Camera.UpdateViewBounds}");
            }

            if (Input.KeyDown(Keys.R))
            {
                Tile underMouse = Input.TileUnderMouse;
                if (underMouse != null)
                {
                    var spawned = new DevTroop();
                    spawned.Position = underMouse.Position;

                    spawned.Activate();
                }
            }

            // Debug update camera movement. Allows to move using WASD and zoom using E and Q.
            UpdateCameraMove();

            // Update client and server if they are not null.
            // TODO quit game if client disconnects.
            Main.Server?.Update();
            Main.Client?.Update();

            Main.Map.Update();
            Entity.UpdateAll();
        }

        public override void Draw(SpriteBatch sb)
        {
            Main.Map.Draw(sb);
            Entity.DrawAll(sb);
        }

        public override void DrawUI(SpriteBatch sb)
        {
            Entity.DrawAllUI(sb);
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

            if (zoomChange != 0)
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

        private void GenerateMap()
        {
            var map = Main.Map;
            Random r = new Random(400);
            Noise n = new Noise(400);

            LoadingScreenText = "Generating map... Placing tiles...";

            // First iteration to place tiles.
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Depth; y++)
                {
                    for (int z = 0; z < map.Height; z++)
                    {
                        const float SCALE = 0.035f;
                        Vector2 offset = new Vector2(500, 300);
                        Color c = (x + y) % 2 == 0 ? Color.White : Color.Lerp(Color.Black, Color.White, 0.95f);
                        float perlin = n.GetPerlin(x * SCALE + offset.X, y * SCALE + offset.Y, z * SCALE);
                        bool place = z == 0 || perlin >= 0.7f;

                        // Prevent floating tiles.
                        var below = map.GetTile(x, y, z - 1);
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

                            map.SetTile(x, y, z, t);

                            //if (t is WaterTile)
                            //    c = Color.White;

                            t.BaseSpriteTint = c.LightShift(0.85f + 0.15f * ((z + 1f) / map.Height));
                            if (t is WaterTile)
                            {
                                t.BaseSpriteTint = t.BaseSpriteTint.Multiply(Color.DeepSkyBlue);
                                t.BaseSpriteTint = t.BaseSpriteTint.LightShift(0.45f + (perlin / WATER_HEIGHT) * 0.8f);
                            }
                        }
                    }
                }
            }

            LoadingScreenText = "Generating map... Decorating tiles...";

            // Second iteration to place mountains, trees and all that stuff.
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Depth; y++)
                {
                    for (int z = 0; z < map.Height; z++)
                    {
                        Tile t = map.GetTile(x, y, z);
                        if (t == null)
                            continue;
                        if (t is WaterTile || t is SandTile)
                            continue;

                        Tile above = map.GetTile(x, y, z + 1);
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
                            else if (r.NextDouble() < 0.02f)
                            {
                                t.AddComponent(new House(), 0);
                            }
                        }
                    }
                }
            }
        }

    }
}
