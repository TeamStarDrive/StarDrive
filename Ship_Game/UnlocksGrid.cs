using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public enum UnlockType
    {
        ShipModule,
        Troop,
        Building,
        Hull,
        Advance, // BONUS
    }

    public sealed class UnlockItem
    {
        public UnlockType Type;
        public ShipModule module;
        public Troop troop;
        public Building building;
        public ShipHull hull;

        public string Title;
        public string Description;
        public SubTexture Icon;

        public UnlockItem(string tech, in Technology.UnlockedMod unlockedMod)
        {
            // Allow NULL, so we can display error icon
            if (!ResourceManager.GetModuleTemplate(unlockedMod.ModuleUID, out ShipModule template))
                Log.Warning(ConsoleColor.Red, $"Tech={tech} unlock module={unlockedMod.ModuleUID} missing");

            Type = UnlockType.ShipModule;
            module = template;
            Title = template?.NameText.Text ?? unlockedMod.ModuleUID;
            Description = template?.DescriptionText.Text ?? "Module: " + unlockedMod.ModuleUID;
            Icon = template?.ModuleTexture ?? ResourceManager.InvalidTexture;
        }

        public UnlockItem(string tech, in Technology.UnlockedTroop unlockedTroop)
        {
            // Allow NULL, so we can display error icon
            if (!ResourceManager.GetTroopTemplate(unlockedTroop.Name, out Troop troopTemplate))
                Log.Warning(ConsoleColor.Red, $"Tech={tech} unlock troop={unlockedTroop.Name} missing");

            Type = UnlockType.Troop;
            troop = troopTemplate;
            Title = unlockedTroop.Name;
            Description = unlockedTroop.Name;
            Icon = troopTemplate?.IconTexture ?? ResourceManager.InvalidTexture;
        }

        public UnlockItem(string tech, Empire player, in Technology.UnlockedHull unlockedHull)
        {
            // Allow NULL, so we can display error icon
            if (!ResourceManager.Hull(unlockedHull.Name, out ShipHull hullData))
                Log.Warning(ConsoleColor.Red, $"Tech={tech} unlock hull={unlockedHull.Name} missing");
            
            Type = UnlockType.Hull;
            hull = hullData;
            Title = hullData?.VisibleName ?? unlockedHull.Name;
            Description = Localizer.Token(GameText.UnlocksANewHullType) + " " +
                          (hullData != null ? Localizer.GetRole(hullData.Role, player) + $" ({hullData.HullSlots.Length} slots)"
                                            : "Hull: " + unlockedHull.Name); 
            Icon = hullData?.Icon ?? ResourceManager.InvalidTexture;
        }

        public UnlockItem(string tech, in Technology.UnlockedBuilding unlockedBuilding)
        {
            // Allow NULL, so we can display error icon
            if (!ResourceManager.GetBuilding(unlockedBuilding.Name, out Building b))
                Log.Warning(ConsoleColor.Red, $"Tech={tech} unlock building={unlockedBuilding.Name} missing");

            Type = UnlockType.Building;
            building = b;
            Title = b?.TranslatedName.Text ?? unlockedBuilding.Name;
            Description = b?.DescriptionText.Text ?? "Building: " + unlockedBuilding.Name;
            Icon = b?.IconTex64 ?? ResourceManager.InvalidTexture;
        }

        public UnlockItem(string tech, in Technology.UnlockedBonus unlockedBonus)
        {
            Type = UnlockType.Advance;
            Title = unlockedBonus.Name;
            Description = Localizer.Token(unlockedBonus.BonusIndex);
            Icon = ResourceManager.Texture("TechIcons/star");
        }

        public int ModW => module?.XSize ?? 1;
        public int ModH => module?.YSize ?? 1;

        public RectF GetModuleRect(float x, float y, float fillW, float fillH)
        {
            float modW = ModW;
            float modH = ModH;
            float w = fillW;
            float h = fillH;

            if (modH > modW)
                w = (float)Math.Floor(modW / modH * fillH);
            else if (modW > modH)
                h = (float)Math.Floor(modH / modW * fillW);

            RectF r;
            r.X = (float)Math.Floor(x + (fillW / 2) - (w / 2));
            r.Y = (float)Math.Floor(y + (fillH / 2) - (h / 2));
            r.W = w;
            r.H = h;
            return r;
        }

        public static Array<UnlockItem> CreateUnlocksList(Technology tech, Empire player, int maxUnlocks = int.MaxValue)
        {
            bool IsValidTypeForPlayer(string type)
            {
                return type == null
                    || type == "ALL"
                    || type == player.data.Traits.ShipType
                    || player.GetTechEntry(tech.UID).WasAcquiredFrom.Contains(type);
            }

            var unlocks = new Array<UnlockItem>();
            
            // NOTE: The order here is important
            foreach (Technology.UnlockedHull hull in tech.HullsUnlocked)
            {
                if (unlocks.Count >= maxUnlocks) break;
                if (IsValidTypeForPlayer(hull.ShipType))
                    unlocks.Add(new UnlockItem(tech.UID, player, hull));
            }
            foreach (Technology.UnlockedMod module in tech.ModulesUnlocked)
            {
                if (unlocks.Count >= maxUnlocks) break;
                if (IsValidTypeForPlayer(module.Type))
                    unlocks.Add(new UnlockItem(tech.UID, module));
            }
            foreach (Technology.UnlockedTroop troop in tech.TroopsUnlocked)
            {
                if (unlocks.Count >= maxUnlocks) break;
                if (IsValidTypeForPlayer(troop.Type))
                    unlocks.Add(new UnlockItem(tech.UID, troop));
            }
            foreach (Technology.UnlockedBuilding building in tech.BuildingsUnlocked)
            {
                if (unlocks.Count >= maxUnlocks) break;
                if (IsValidTypeForPlayer(building.Type))
                    unlocks.Add(new UnlockItem(tech.UID, building));
            }
            foreach (Technology.UnlockedBonus bonus in tech.BonusUnlocked)
            {
                if (unlocks.Count >= maxUnlocks) break;
                if (IsValidTypeForPlayer(bonus.Type))
                    unlocks.Add(new UnlockItem(tech.UID, bonus));
            }

            return unlocks;
        }
    }

    /// <summary>
    /// This is the tiny grid right next to a technology
    /// to preview 
    /// </summary>
    public sealed class UnlocksGrid
    {
        public struct GridItem
        {
            public UnlockItem Item;
            public RectF Rect;
            public Vector2 Pos => Rect.Pos;
        }

        public Array<GridItem> GridOfUnlocks = new Array<GridItem>();

        public UnlocksGrid(IEnumerable<UnlockItem> unlocks, RectF r)
        {
            int x = 0;
            int y = 0;
            foreach (UnlockItem item in unlocks)
            {
                GridOfUnlocks.Add(new GridItem
                {
                    Item = item, Rect = new(r.X + 32 * x, r.Y + 32 * y, 32, 32),
                });
                if (++y == 2)
                {
                    y = 0;
                    x++;
                }
            }
        }

        public void Draw(SpriteBatch batch)
        {
            foreach (GridItem gi in GridOfUnlocks)
            {
                UnlockItem unlock = gi.Item;
                Rectangle iconRect = gi.Rect;

                if (unlock.Type == UnlockType.ShipModule)
                {
                    iconRect = unlock.GetModuleRect(gi.Rect.X, gi.Rect.Y, gi.Rect.W, gi.Rect.H);
                }

                batch.Draw(unlock.Icon, iconRect, Color.White);
            }
        }
    }
}