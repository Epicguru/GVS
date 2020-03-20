using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Threading;
using GVS.World;

namespace GVS
{
    public static class Loop
    {
        /// <summary>
        /// The target application framerate. The game will be updated and rendered at this frequency, whenever possible.
        /// If set to 0 (zero) then there is no target framerate and the game will update as fast as possible.
        /// </summary>
        public static float TargetFramerate
        {
            get
            {
                return targetFramerate;
            }
            set
            {
                if (value == targetFramerate)
                    return;

                if (value < 0)
                    value = 0;

                targetFramerate = value;
                Debug.Trace($"Updated target framerate to {value} {(value == 0 ? "(none)" : "")}");
            }
        }

        /// <summary>
        /// The current update and draw frequency that the application is running at, calculated each frame.
        /// More accurate at lower framerates, less accurate at higher framerates. For a more stable and reliable value,
        /// see <see cref="Framerate"/>.
        /// </summary>
        public static float ImmediateFramerate { get; private set; }

        /// <summary>
        /// The current update and draw frequency, calculated once per second.
        /// The framerate is affected by <see cref="TargetFramerate"/> and <see cref="VSyncMode"/> and of course
        /// the actual speed of game updating and rendering.
        /// </summary>
        public static float Framerate { get; private set; }

        /// <summary>
        /// If true, then framerate is limited and maintained using a more accurate technique, leading to more
        /// consistent framerates.
        /// </summary>
        public static bool EnablePrecisionFramerate { get; set; } = true;

        /// <summary>
        /// The color to clear the background to.
        /// </summary>
        public static Color ClearColor { get; set; } = Color.CornflowerBlue;

        /// <summary>
        /// Gets or sets the vertical sync mode for the display. Default is disabled.
        /// </summary>
        public static VSyncMode VSyncMode
        {
            get
            {
                return vsm;
            }
            set
            {
                if (value == vsm)
                    return;

                vsm = value;
                switch (value)
                {
                    case VSyncMode.DISABLED:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
                        break;
                    case VSyncMode.ENABLED:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.One;
                        break;
                    case VSyncMode.DOUBLE:
                        Main.GlobalGraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Two;
                        break;
                }

                Debug.Trace($"Updated VSync mode to {value}");
            }
        }

        public static Thread Thread;
        public static bool Running { get; private set; }
        public static bool ThreadQuit { get; private set; }
        public static bool InUIDraw { get; private set; }
        public class Stats
        {
            public double FrameTotalTime;
            public double FrameUpdateTime;
            public double FrameDrawTime;
            public double FrameSleepTime;
            public double FramePresentingTime;
            public bool Waited;
            public GraphicsMetrics DrawMetrics { get; internal set; }
        }
        public static Stats Statistics { get; private set; } = new Stats();

        private static float targetFramerate;
        private static int cumulativeFrames;
        private static readonly Stopwatch frameTimer = new Stopwatch();
        private static VSyncMode vsm = VSyncMode.ENABLED;

        private static double TargetFramerateInterval()
        {
            // Remember physics: f=1/i  so  i=1/f
            return 1.0 / TargetFramerate;
        }

        public static void Start()
        {
            if (Running)
                return;

            Running = true;
            ThreadQuit = false;

            VSyncMode = VSyncMode.ENABLED;

            Thread = new Thread(Run);
            Thread.Name = "Game Loop";
            Thread.Priority = ThreadPriority.Highest;

            Thread.Start();
            frameTimer.Start();
            Framerate = 0;
            ImmediateFramerate = 0;
        }

        public static void Stop()
        {
            if (!Running)
                return;

            Running = false;
        }

        public static void StopAndWait()
        {
            Stop();
            while (!ThreadQuit)
            {
                Thread.Sleep(1);
            }
        }

        private static void Run()
        {
            Begin();

            Debug.Log("Starting game loop...");
            SpriteBatch spr = Main.SpriteBatch;
            Stopwatch watch = new Stopwatch();
            Stopwatch watch2 = new Stopwatch();
            Stopwatch watch3 = new Stopwatch();
            Stopwatch sleepWatch = new Stopwatch();

            double updateTime = 0.0;
            double renderTime = 0.0;
            double presentTime = 0.0;
            double total = 0.0;
            double sleep = 0.0;

            while (Running)
            {
                watch2.Restart();

                // Determine the ideal loop time, in seconds.
                double target = 0.0;
                if (TargetFramerate != 0f)
                    target = TargetFramerateInterval();

                Time.StartFrame();

                watch.Restart();
                Update();
                watch.Stop();
                updateTime = watch.Elapsed.TotalSeconds;
                Statistics.FrameUpdateTime = updateTime;

                watch.Restart();
                Draw(spr);
                watch.Stop();
                renderTime = watch.Elapsed.TotalSeconds;
                Statistics.FrameDrawTime = renderTime;

                watch.Restart();
                Present();
                watch.Stop();
                presentTime = watch.Elapsed.TotalSeconds;
                Statistics.FramePresentingTime = presentTime;

                total = updateTime + renderTime + presentTime;
                sleep = target - total;

                if(sleep > 0.0)
                {
                    sleepWatch.Restart();
                    if (!EnablePrecisionFramerate)
                    {
                        // Sleep using the normal method. Allow the CPU to do whatever it wants.
                        TimeSpan s = TimeSpan.FromSeconds(sleep);
                        Thread.Sleep(s);
                    }
                    else
                    {
                        // Sleep by slowly creeping up to the target time in a loop.
                        watch3.Restart();
                        while (watch3.Elapsed.TotalSeconds + (0.001) < sleep)
                        {
                            Thread.Sleep(1);
                        }
                        watch3.Stop();
                    }
                    sleepWatch.Stop();
                    Statistics.FrameSleepTime = sleepWatch.Elapsed.TotalSeconds;
                    Statistics.Waited = true;
                }
                else
                {
                    Statistics.Waited = false;
                }

                watch2.Stop();
                ImmediateFramerate = (float)(1.0 / watch2.Elapsed.TotalSeconds);
                Statistics.FrameTotalTime = watch2.Elapsed.TotalSeconds;
            }

            ThreadQuit = true;
            Thread = null;
            Debug.Log("Stopped game loop!");
        }

        private static void Begin()
        {

        }

        private static void Update()
        {
            cumulativeFrames++;
            if(frameTimer.Elapsed.TotalSeconds >= 1.0)
            {
                frameTimer.Restart();
                Framerate = cumulativeFrames;
                cumulativeFrames = 0;
            }

            Debug.Update();
            Input.StartFrame();

            Debug.Text($"FPS: {Framerate:F0} (Target: {(TargetFramerate == 0 ? "uncapped" : TargetFramerate.ToString("F0"))}, VSync: {VSyncMode})");
            Debug.Text($"Time Scale: {Time.TimeScale}");
            Debug.Text($"Screen Res: ({Screen.Width}x{Screen.Height})");
            Debug.Text($"Used memory: {Main.GameProcess.VirtualMemorySize64 / 1024 / 1024}MB.");
            Debug.Text($"Texture Swap Count: {Loop.Statistics.DrawMetrics.TextureCount}");
            Debug.Text($"Draw Calls: {Loop.Statistics.DrawMetrics.DrawCount}");
            Debug.Text($"Sprites Drawn: {Loop.Statistics.DrawMetrics.SpriteCount}");
            //Debug.Text($"Total Entities: {JEngine.Entities.EntityCount} of {JEngine.Entities.MaxEntityCount}.");

            Tile selectedTile = GetTileFromWorldPosition(Input.MouseWorldPos);
            Debug.Text($"Tile under mouse: {(selectedTile == null ? "null" : selectedTile.ToString())}");
            if (selectedTile != null)
                selectedTile.TemporarySpriteTint = Color.Orange;

            // Update currently active screen.
            Main.MainUpdate();

            Input.EndFrame();
        }

        private static Tile GetTileFromWorldPosition(Vector2 flatWorldPosition)
        {
            int maxZ = Main.Map.Height - 1; // The maximum Z to consider. Allow for selection when top layers are hidden.

            Vector2 pos = Main.Map.GetGroundPositionFromWorldPosition(flatWorldPosition, out IsoMap.TileSide side);
            Point groundPos = new Point((int)pos.X, (int)pos.Y);

            // Identify the columns to consider.
            Point topA = new Point(groundPos.X, groundPos.Y);
            Point topB = side == IsoMap.TileSide.Right ? topA + new Point(0, 1) : topA + new Point(1, 0);

            // Start from bottom upwards, top to down, B then A.
            // First tile to 'collide' is the selected tile.

            // How far down the columns should we go?
            int downA = maxZ;
            int downB = maxZ - 1;

            Point bottomA = topA + new Point(downA, downA);
            Point bottomB = topB + new Point(downB, downB);

            Point bPoint = bottomB;
            Point aPoint = bottomA;
            int i = 0;
            var map = Main.Map;
            while (true)
            {
                // Start with B, then A, then B...
                Tile t;

                // A! Two tiles to check!
                int aZ = maxZ + 1 - i; // This is out of bounds when i = 0, because of the way the universe works.
                if (aZ <= maxZ)
                {
                    t = map.GetTile(aPoint.X, aPoint.Y, aZ);
                    if (IsSelectable(t))
                        return t;
                }
                aZ = maxZ - i; // This will also be out of bounds on the last iteration.
                if (aZ < 0)
                    return null; // Quit, tile not found anywhere.

                t = map.GetTile(aPoint.X, aPoint.Y, aZ);
                if (IsSelectable(t))
                    return t;

                // B!
                int bZ = maxZ - i;
                if(bZ != 0)
                {
                    t = map.GetTile(bPoint.X, bPoint.Y, bZ);
                    if (IsSelectable(t))
                        return t;
                }

                // Increase counter and move the two points upwards.
                i++;
                aPoint -= new Point(1, 1);
                bPoint -= new Point(1, 1);
            }

            bool IsSelectable(Tile t)
            {
                return t != null;
            }
        }

        private static void Draw(SpriteBatch spr)
        {
            Main.Camera.UpdateMatrix(Main.GlobalGraphicsDevice);
            Main.GlobalGraphicsDevice.Clear(ClearColor);

            SamplerState s = Main.Camera.Zoom >= 1 ? SamplerState.PointClamp : SamplerState.LinearClamp;
            spr.Begin(SpriteSortMode.FrontToBack, null, s, null, null, null, Main.Camera.GetMatrix());

            // Main world draw.
            Main.MainDraw();

            spr.End();

            spr.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Main.Camera.GetMatrix());
            Debug.Draw(spr);
            spr.End();

            InUIDraw = true;
            spr.Begin(SpriteSortMode.Deferred, null, null, null, null, null, null);

            // Draw the UI.
            Main.MainDrawUI();
            Debug.DrawUI(spr);

            spr.End();
            InUIDraw = false;
        }

        private static void Present()
        {
            Statistics.DrawMetrics = Main.GlobalGraphicsDevice.Metrics;
            Main.GlobalGraphicsDevice.Present();
        }
    }
}
