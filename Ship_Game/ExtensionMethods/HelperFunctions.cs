using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Graphics;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.Data.Yaml;

namespace Ship_Game
{
    public static class HelperFunctions
    {
        public static bool ClickedRect(Rectangle toClick, InputState input)
        {
            return input.InGameSelect && toClick.HitTest(input.CursorPosition);
        }

        private static FleetDesign LoadFleetDesign(string fleetUid)
        {
            string designPath = fleetUid + ".yaml";
            FileInfo info = ResourceManager.GetModOrVanillaFile(designPath) ??
                            new FileInfo(Dir.StarDriveAppData + "/Fleet Designs/" + designPath);
            if (info.Exists)
                return YamlParser.Deserialize<FleetDesign>(info);

            Log.Warning($"Failed to load fleet design '{designPath}'");
            return null;
        }

        /// <summary>
        /// Ony use in debug!
        /// </summary>
        static Fleet DebugCreateFleetFromData(UniverseState u, FleetDesign data, int fleetId, Empire owner, Vector2 position)
        {
            if (data == null)
                return null;

            Fleet fleet = owner.CreateFleet(fleetId, data.Name);
            fleet.FinalPosition = position;
            fleet.FleetIconIndex = data.FleetIconIndex;

            foreach (FleetDataDesignNode node in data.Nodes)
            {
                fleet.DataNodes.Add(new FleetDataNode(node));
            }

            foreach (FleetDataNode node in fleet.DataNodes)
            {
                Ship s = Ship.CreateShipAtPoint(u, node.ShipName, owner, position + node.RelativeFleetOffset);
                if (s == null) 
                    continue;

                if (s.IsDefaultTroopShip)
                {
                    Troop troop = ResourceManager.GetTroopTemplatesFor(owner).First();
                    if (ResourceManager.TryCreateTroop(troop.Name, owner, out Troop newTroop))
                        newTroop.LandOnShip(s);
                }

                s.AI.CombatState = node.CombatState;
                s.RelativeFleetOffset = node.RelativeFleetOffset;
                node.Ship = s;
                node.OrdersRadius = node.OrdersRadius > 1 ? node.OrdersRadius : s.SensorRange * node.OrdersRadius;
                fleet.AddShip(s);
            }
            return fleet;
        }

        public static void DebugCreateFleetAt(UniverseState universe, string fleetUid, Empire owner, Vector2 position)
        {
            DebugCreateFleetFromData(universe, LoadFleetDesign(fleetUid), 1, owner, position);
        }

        public static bool IsInUniverseBounds(float universeSize, Vector2 pos)
        {
            float x = universeSize;
            float y = universeSize;

            return -x < pos.X && pos.X < x
                && -y < pos.Y && pos.Y < y;
        }

        public static void CompressDir(DirectoryInfo dir, string outFile)
        {
            FileInfo file = new FileInfo(outFile);
            if (file.Exists)
                file.Delete();

            ZipFile.CreateFromDirectory(dir.FullName, outFile, CompressionLevel.Fastest, true);
        }

        public static void Compress(FileInfo source, FileInfo destination)
        {
            // unpacked files can be huge, so only read 4MB at a time
            var buffer = new byte[4096*1024];

            using (FileStream inFile = source.OpenRead())
            using (FileStream outFile = destination.OpenWrite())
            using (var compress = new GZipStream(outFile, CompressionMode.Compress))
            {
                int bytesRead;
                do
                {
                    bytesRead = inFile.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                        break;
                    compress.Write(buffer, 0, bytesRead);
                }
                while (bytesRead == buffer.Length);

                Log.Info($"Compressed {source.Name} from {source.Length/(1024*1024.0):0.0}MB"+
                         $" to {outFile.Length/(1024*1024.0):0.0}MB");
            }
        }

        public static string Decompress(FileInfo fi)
        {
            string curFile  = fi.FullName;
            string origName = curFile.Remove(curFile.Length - fi.Extension.Length); // remove ".gz"

            using (FileStream inFile = fi.OpenRead())
            using (GZipStream decompress = new GZipStream(inFile, CompressionMode.Decompress))
            using (FileStream outFile = File.Create(origName))
            {
                var buffer = new byte[4096*1024]; // average savegame is 4MB, so try and get this done in one go
                int numRead;
                while ((numRead = decompress.Read(buffer, 0, buffer.Length)) > 0)
                    outFile.Write(buffer, 0, numRead);
                Log.Info($"Decompressed: {fi.Name}");
                return origName;
            }
        }

        public static void DrawDropShadowImage(this SpriteBatch batch, Rectangle rect, SubTexture texture, Color topColor)
        {
            var offsetRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            batch.Draw(texture, offsetRect, Color.Black);
            batch.Draw(texture, rect, topColor);
        }
        public static void DrawDropShadowText(this SpriteBatch batch, string text, Vector2 pos, Graphics.Font font)
        {
            DrawDropShadowText(batch, text, pos, font, Color.White);
        }
        public static void DrawDropShadowText1(this SpriteBatch batch, string text, Vector2 pos, Graphics.Font font, Color c)
        {
            DrawDropShadowText(batch, text, pos, font, c, 1f);
        }
        public static void DrawDropShadowText(this SpriteBatch batch, string text, Vector2 pos, Graphics.Font font, Color c, float shadowOffset = 2f)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            batch.DrawString(font, text, pos + new Vector2(shadowOffset), Color.Black);
            batch.DrawString(font, text, pos, c);
        }
        public static void DrawOutlineText(this SpriteBatch batch, string text, Vector2 pos, Font font, Color c, Color outlineC, float outlineR)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            batch.DrawString(font, text, pos + new Vector2(-outlineR, -outlineR), outlineC);
            batch.DrawString(font, text, pos + new Vector2(-outlineR, +outlineR), outlineC);
            batch.DrawString(font, text, pos + new Vector2(+outlineR, -outlineR), outlineC);
            batch.DrawString(font, text, pos + new Vector2(+outlineR, +outlineR), outlineC);
            batch.DrawString(font, text, pos, c);
        }

        public static void DrawGrid(SpriteBatch spriteBatch, int xpos, int ypos, int xGridSize, int yGridSize, int numberXs, int numberYs)
        {
            int xsize = xGridSize / numberXs;
            int ysize = yGridSize / numberYs;
            var color  = new Color(211, 211, 211, 70);
            var origin = new Vector2(xpos + 1, ypos);
            var end    = new Vector2(xpos, ypos + yGridSize - 1);
            for (int x = 0; x < numberXs; ++x)
            {
                spriteBatch.DrawLine(origin, end, color, 2f);
                origin.X += xsize;
                end.X    += xsize;
            }
            origin = new Vector2(xpos, ypos);
            end    = new Vector2(xpos + xGridSize - 3, ypos);
            for (int y = 0; y < numberYs; ++y)
            {
                spriteBatch.DrawLine(origin, end, color, 2f);
                origin.Y += ysize;
                end.Y    += ysize;
            }
        }

        public static int RoundTo(float amount1, int roundTo)
        {
            int rounded = (int)((amount1 + 0.5 * roundTo) / roundTo) * roundTo;
            return rounded;
        }

        // Added by RedFox: blocking full blown GC to reduce memory fragmentation
        public static void CollectMemory()
        {
            // collect memory silently in Unit tests
            if (StarDriveGame.Instance == null)
            {
                CollectMemorySilent();
                return;
            }

            // the GetTotalMemory full collection loop is pretty good, so we use it instead of GC.Collect()
            float before = GC.GetTotalMemory(forceFullCollection: false) / (1024f * 1024f);
            CollectMemorySilent();
            float after  = GC.GetTotalMemory(forceFullCollection: true) / (1024f * 1024f);
            float processMemory = Process.GetCurrentProcess().WorkingSet64 / (1024f * 1024f);

            Log.Write(ConsoleColor.DarkYellow, $"CollectMemory:  Before={before:0.0}MB  After={after:0.0}MB  ProcessMemory={processMemory:0.0}MB");
        }

        public static void CollectMemorySilent()
        {
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Gets a more human readable number string that also supports large numbers
        // ex: 950.7k 
        public static string GetNumberString(this float stat)
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            if (Math.Abs(stat) < 100f)   return stat.ToString("0.##", invariant); // 95.75  or 0.25
            if (Math.Abs(stat) < 1000f)  return stat.ToString("0.#", invariant);  // 950.7  or 0.5
            if (Math.Abs(stat) < 10000f) return stat.ToString("#", invariant);    // 9500
            float single = stat / 1000f;
            if (Math.Abs(single) < 100f)  return single.ToString("0.##", invariant) + "k"; // 57.75k or 0.5k
            if (Math.Abs(single) < 1000f) return single.ToString("0.#", invariant) + "k";  // 950.7k
            return single.ToString("#", invariant) + "k"; // 1000k
        }

        public static bool DataVisibleToPlayer(Empire empire)
        {
            if (empire.isPlayer || empire.IsAlliedWith(empire.Universe.Player) || empire.Universe.Debug)
                return true;

            return empire.DifficultyModifiers.DataVisibleToPlayer;
        }

        public static bool GetLoneSystem(UniverseState u, out SolarSystem system, bool includeReseachable)
        {
            system = u.Random.ItemFilter(u.Systems, s => s.RingList.Count == 0 
                                                         && !s.PiratePresence
                                                         && includeReseachable || !s.IsResearchable);
            return system != null;
        }

        // This also Filters Researchable systems
        public static bool GetUnownedNormalSystems(UniverseState u, out SolarSystem[] systems)
        {
            systems = u.Systems.Filter(s => s.OwnerList.Count == 0
                                         && s.RingList.Count > 0
                                         && !s.IsResearchable
                                         && !s.PiratePresence
                                         && !s.PlanetList.Any(p => p.IsResearchable)
                                         && !s.ShipList.Any(g => g.IsGuardian));
            return systems.Length > 0;
        }

        public static bool GetRadiatingStars(UniverseState u, out SolarSystem[] systems)
        {
            systems = u.Systems.Filter(s => s.OwnerList.Count == 0
                                         && !s.PiratePresence
                                         && s.Sun.RadiationRadius.Greater(0));
            return systems.Length > 0;
        }

        public static bool DesignInQueue(ShipDesignScreen screen, string shipOrHullName, out string playerPlanets)
        {
            bool designInQueue = false;
            playerPlanets = "";
            foreach (Planet planet in screen.ParentUniverse.UState.Planets)
            {
                if (planet.Construction.ContainsShipDesignName(shipOrHullName))
                {
                    designInQueue = true;
                    if (planet.Owner?.isPlayer == true)
                        playerPlanets = playerPlanets.IsEmpty() ? planet.Name : $"{playerPlanets}, {planet.Name}";
                }
            }

            return designInQueue;
        }

        static public float ExponentialMovingAverage(float oldValue, float newValue, float oldWeight = 0.9f)
        {
            return (oldValue * oldWeight) + (newValue * (1 - oldWeight));
        }

        static public bool InGoodDistanceForReseachOrMiningOps(Empire owner, SolarSystem system, float averageDist, InfluenceStatus influence)
        {
            return system.HasPlanetsOwnedBy(owner)
                   || system.Position.SqDist(owner.WeightedCenter) < averageDist * 1.5f
                   || system.FiveClosestSystems.Any(s => s.HasPlanetsOwnedBy(owner))
                   || influence == InfluenceStatus.Friendly;
        }
    }
}