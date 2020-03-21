using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AnimationPacker
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // TODO allow for passing of max width and output by prefixing '-' (-1024, -somepathhere.png, file1.png, file2.png...)
                Bitmap texture = Pack(1024, args);
                string parent = new FileInfo(args[0]).Directory.FullName;
                string file = Path.Combine(parent, "Output.png");

                using (FileStream f = new FileStream(file, FileMode.Create))
                {
                    texture.Save(f, ImageFormat.Png);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static Bitmap Pack(int maxWidth, string[] fileNames)
        {
            int width = 0;
            int height = 0;
            int inRow = 0;
            int inColumn = 0;

            Bitmap atlas = null;
            Color[] pixels = null;

            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];
                Bitmap tex;

                Console.Write($"Loading ({i}) {fileName}... ");
                using(FileStream stream = new FileStream(fileName, FileMode.Open))
                {
                    tex = new Bitmap(stream);
                }

                if(i == 0)
                {
                    // Load it up.
                    width = tex.Width;
                    height = tex.Height;
                    inRow = maxWidth / width;
                    inColumn = (int) Math.Ceiling((double)fileNames.Length / inRow);

                    pixels = new Color[width * height];
                    atlas = new Bitmap(inRow * width, inColumn * height);
                }

                int ix = i % inRow;
                int iy = i / inRow;

                int px = ix * width;
                int py = iy * height;

                Rectangle bounds = new Rectangle(px, py, width, height);
                Console.Write("reading... ");
                tex.GetData(pixels);

                Console.Write("writing... ");
                atlas.SetData(bounds, pixels);

                Console.Write("cleaning...");
                tex.Dispose();

                Console.WriteLine("Done!");
            }

            return atlas;
        }

        private static void GetData(this Bitmap b, Color[] data)
        {
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    data[x + y * b.Width] = b.GetPixel(x, y);
                }
            }
        }

        private static void SetData(this Bitmap b, Rectangle bounds, Color[] data)
        {
            for (int x = 0; x < bounds.Width; x++)
            {
                for (int y = 0; y < bounds.Height; y++)
                {
                    b.SetPixel(x + bounds.X, y + bounds.Y, data[x + y * bounds.Width]);
                }
            }
        }
    }
}
