using GVS.Networking;
using GVS.World;
using GVS.World.Tiles;
using System;
using Xunit;
using Xunit.Abstractions;

namespace GVS_Tests
{
    public class IsoMapTests : IDisposable
    {
        private readonly ITestOutputHelper testOutputHelper;
        public IsoMap Map;
        public GameServer Server;

        public IsoMapTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
            Map = new IsoMap(100, 100, 3);
            Server = new GameServer(8888, 1);
        }

        [Fact]
        public void Test_IndexToCoordsAndBack()
        {
            for (int index = 0; index < Map.Width * Map.Depth * Map.Height; index++)
            {
                Point3D coords = Map.GetCoordsFromIndex(index);
                Assert.True(Map.IsPointInRange(coords), $"Index input gave out of bounds coords: {coords}");

                int sameIndex = Map.GetIndex(coords.X, coords.Y, coords.Z);

                Assert.Equal(index, sameIndex);
            }
        }

        [Fact]
        public void Test_IndexToCoords()
        {
            Assert.Equal(new Point3D(0, 0, 0), Map.GetCoordsFromIndex(0));
            Assert.Equal(new Point3D(0, 0, 1), Map.GetCoordsFromIndex(100 * 100));
            Assert.Equal(new Point3D(20, 0, 1), Map.GetCoordsFromIndex(100 * 100 + 20));
            Assert.Equal(new Point3D(20, 6, 1), Map.GetCoordsFromIndex(100 * 100 + 20 + 6 * 100));
        }

        [Fact]
        public void Test_SendsCorrectData()
        {
            int messageCountExpected = Map.GetNumberOfNetChunks();
            var messages = Map.NetSerializeAllTiles(Server);
            Assert.Equal(messageCountExpected, messages.Count);

            int tilesSum = 0;
            bool[] coverage = new bool[messageCountExpected];

            foreach (var msg in messages)
            {
                // Remove the header byte.
                byte type = msg.ReadByte();

                int len = msg.ReadInt32();
                int startIndex = msg.ReadInt32();
                ushort id = msg.ReadUInt16();

                coverage[startIndex / len] = true;

                tilesSum += len;
            }

            Assert.Equal(Map.Width * Map.Depth * Map.Height, tilesSum);
            Assert.DoesNotContain(coverage, (b) => !b);
        }

        public void Dispose()
        {
            Server.Dispose();
            Server = null;
            Map.Dispose();
            Map = null;
        }
    }
}
