using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Audio;
using Ship_Game.Data.Yaml;
using Ship_Game.Fleets;
using Ship_Game.Graphics;
using Ship_Game.Ships;
using Ship_Game.Universe;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

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
                    // Owner may have no unlocked troop templates (no tech, or a
                    // race whose troops are all locked) — leave the troop ship
                    // empty instead of throwing inside fleet spawn.
                    Troop[] templates = ResourceManager.GetTroopTemplatesFor(owner);
                    if (templates.Length > 0
                        && ResourceManager.TryCreateTroop(templates[0].Name, owner, out Troop newTroop))
                    {
                        newTroop.LandOnShip(s);
                    }
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
            var color  = new Color(211, 211, 211, 70).Premultiplied();
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

        // Gets a more human readable number string that also supports large numbers.
        // Default:        0.25, 95.75, 950.7, 9500, 57.75k, 950.7k, 1.25M, 950.7M, 1000M
        // compact:true:   0.25, 95.75, 950.7, 9500, 57.8k,  950.7k, 1.3M,  950.7M, 1000M
        public static string GetNumberString(this float stat, bool compact = false)
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            float abs = Math.Abs(stat);
            if (abs < 100f)   return stat.ToString("0.##", invariant);
            if (abs < 1000f)  return stat.ToString("0.#", invariant);
            if (abs < 10000f) return stat.ToString("#", invariant);

            float k = stat / 1000f;
            float absK = Math.Abs(k);
            if (absK < 100f && !compact) return k.ToString("0.##", invariant) + "k";
            if (absK < 1000f)            return k.ToString("0.#",  invariant) + "k";

            float m = stat / 1_000_000f;
            float absM = Math.Abs(m);
            if (absM < 100f && !compact) return m.ToString("0.##", invariant) + "M";
            if (absM < 1000f)            return m.ToString("0.#",  invariant) + "M";
            return m.ToString("#", invariant) + "M";
        }

        static string HostilesText(in ThreatMatrix.HostilePresence hostiles, bool withLabel)
        {
            string text = hostiles.NumShips > 0
                        ? $"{hostiles.NumShips} {Localizer.Token(hostiles.NumShips == 1 ? GameText.Ship : GameText.Ships)}, "
                        : "";

            text += $"{hostiles.Strength.GetNumberString()} {Localizer.Token(GameText.Str)}";
            return withLabel ? $"{Localizer.Token(GameText.Hostiles)}: {text}" : text;
        }

        /// <summary>
        /// Draws the System column of the planet and exotic systems lists: the star, its name, and
        /// under the name what our threat map knows of the hostiles there, with a flag per empire.
        /// </summary>
        public static void AddSystemNameAndHostiles(this UIElementContainer item, in Rectangle rect,
                                                    SolarSystem system, in ThreatMatrix.HostilePresence hostiles)
        {
            const int pad = 5;
            int iconSize = rect.Height - 10;
            var starRect = new Rectangle(rect.X + pad, rect.Y + 5, iconSize, iconSize);
            item.Panel(starRect, system.Sun.Icon);

            int left = starRect.Right + pad;
            int usable = rect.Right - pad - left;
            string systemName = system.Name;
            Graphics.Font nameFont = Fonts.Arial20Bold.MeasureString(systemName).X <= usable
                                   ? Fonts.Arial20Bold : Fonts.Arial12Bold;
            Graphics.Font tinyFont = Fonts.Arial8Bold;

            int textHeight = nameFont.LineSpacing + (hostiles.Any ? tinyFont.LineSpacing : 0);
            float nameY = 2 + rect.Y + rect.Height / 2 - textHeight / 2;
            item.Label(new Vector2(left, nameY), systemName, nameFont, Colors.Cream);

            if (!hostiles.Any)
                return;

            Empire[] empires = hostiles.Empires ?? Empty<Empire>.Array;
            int flagSize     = tinyFont.LineSpacing;
            float openW      = tinyFont.MeasureString(" (").X;
            float flagsW     = empires.Length > 0 ? openW + empires.Length*(flagSize + 1) + tinyFont.MeasureString(")").X : 0;

            string text = HostilesText(hostiles, withLabel: true);
            if (tinyFont.MeasureString(text).X + flagsW > usable) // no room for the label in this column
                text = HostilesText(hostiles, withLabel: false);

            float x = left;
            float y = (nameY + nameFont.LineSpacing).UpperBound(rect.Bottom - tinyFont.LineSpacing);
            Color color = Color.IndianRed;
            item.Label(new Vector2(x, y), text, tinyFont, color);
            x += tinyFont.MeasureString(text).X;

            if (empires.Length > 0)
            {
                item.Label(new Vector2(x, y), " (", tinyFont, color);
                x += openW;
                foreach (Empire e in empires)
                {
                    var box = new Rectangle((int)x, (int)y, flagSize, flagSize);
                    var flag = ResourceManager.Flag(e);
                    if (flag != null) item.Panel(box, e.EmpireColor, flag);
                    else              item.Panel(box, e.EmpireColor);
                    x += flagSize + 1;
                }
                item.Label(new Vector2(x, y), ")", tinyFont, color);
            }
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

        static public int GetMiddlePosForTitle(string title, Font font, float width, int x)
        {
            return (int)(x+ width*0.5f - font.MeasureString(title).X*0.5f);
        }

        static public Vector2 GetRightAlignedPosForTitle(string title, Font font, float right, float y, int offest = 5)
        {
            return new Vector2(right - font.MeasureString(title).X - offest, y);
        }

        static public Vector2 GetCorrectedMovePosWithAudio(Array<Ship> ships, Ship[] enemyShips, Vector2 pos)
        {
            float minimumDistance = (ships.Count * 100).LowerBound(5000);
            float minimumDistanceInBattle = (minimumDistance * 2).LowerBound(5000);
            var enemyShipsTooClose = enemyShips.Filter(s => s.Position.Distance(pos) <= minimumDistance);
            if (ships.All(s => s.InFrustum && !s.IsInWarp && s.Position.Distance(pos) < minimumDistanceInBattle) || enemyShipsTooClose.Length == 0)
            {
                GameAudio.AffirmativeClick();
                return pos;
            }

            GameAudio.SmallServo(); // Notify player that order was not exactly followed 
            Vector2 closestEnemySPos = enemyShipsTooClose.FindMin(s => s.Position.SqDist(pos)).Position;
            float distanceNeeded = minimumDistance - closestEnemySPos.Distance(pos);
            Vector2 directionToProjectedPos = closestEnemySPos.DirectionToTarget(pos);
            Vector2 corrected = pos + directionToProjectedPos * distanceNeeded;
            return corrected;
        }

        static public Vector2 GetWarpOvershootMovePosWithAudio(Ship ship, Ship[] enemyShips, Vector2 pos)
        {
            float minimumDistance = 7500;
            var enemyShipsTooClose = enemyShips.Filter(s => s.Position.Distance(ship.Position) <= minimumDistance);
            if (enemyShipsTooClose.Length == 0)
            {
                GameAudio.AffirmativeClick();
                return pos;
            }

            GameAudio.SmallServo();
            Vector2 FarthestEnemyPos = enemyShipsTooClose.FindMax(s => s.Position.SqDist(ship.Position)).Position;
            float distanceNeeded = minimumDistance + FarthestEnemyPos.Distance(ship.Position);
            Vector2 corrected = ship.Position + ship.Direction * distanceNeeded;
            return corrected;
        }

        static public bool CanExitWarpForChangingDirectionByCommand(Ship[] ships, Ship[] enemyShips)
        {
            if (enemyShips.Length == 0)
                return true;

            float minimumDistance = 5000f;
            if (ships.Any(s => s.IsInWarp && enemyShips.Any(enemy => enemy.Position.Distance(s.Position) < minimumDistance)))
                return false;

            return true;
        }

        static public Ship[] GetAllPotentialTargetsIfInWarp(Array<Ship> ships)
        {
            Array<Ship> potentialTargets = new();
            if (!ships.Any(s => s.IsInWarp))
                return [];

            for (int i = 0; i < ships.Count; ++i)
            {
                Ship ship = ships[i];
                for (int j = 0; j < ship.AI.PotentialTargets.Length; j++)
                {
                    Ship target = ship.AI.PotentialTargets[j];
                    potentialTargets.AddUniqueRef(target);
                }
            }

            return potentialTargets.ToArray();
        }
    }
}