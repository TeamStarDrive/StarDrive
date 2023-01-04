using System.Xml.Serialization;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Building
    {
        [StarData] public string Name;
        [StarData] public bool IsSensor;
        [StarData] public bool NoRandomSpawn;
        [StarData] public bool AllowShipBuilding;
        [StarData] public int NameTranslationIndex;
        [StarData] public int DescriptionIndex;
        [StarData] public int ShortDescriptionIndex;
        [StarData] public string ResourceCreated;
        [StarData] public string ResourceConsumed;
        [StarData] public float ConsumptionPerTurn;
        [StarData] public float OutputPerTurn;
        [StarData] public string CommodityRequired;
        [StarData] public bool IsCommodity;
        [StarData] public bool WinsGame;
        [StarData] public bool BuildOnlyOnce;
        [StarData] public string EventOnBuild;
        [StarData(DefaultValue="")] public string EventTriggerUID = "";
        [StarData(DefaultValue=15)] public byte EventSpawnChance = 15;
        [StarData] public bool CanBuildAnywhere;
        [StarData] public float PlusTerraformPoints;
        [StarData] public float PlusProdPerRichness;
        [StarData] public float PlanetaryShieldStrengthAdded;
        [StarData] public float PlusFlatPopulation;
        [StarData] public float MaxFertilityOnBuild;
        [StarData] public string Icon;
        [StarData(DefaultValue=5)] public int Strength = 5;
        [StarData(DefaultValue=true)] public bool Scrappable = true;
        [StarData(DefaultValue=true)] public bool Unique = true;
        [StarData] public bool isWeapon;
        [StarData] public string Weapon = "";
        [StarData] public float WeaponTimer;
        [StarData] public float AttackTimer;
        [StarData] public int AvailableAttackActions; // FB - use UpdateAttackActions
        [StarData] public int CombatStrength;
        [StarData] public int SoftAttack;
        [StarData] public int HardAttack;
        [StarData] public int Defense; // Defense vs bombardment
        [StarData] public float PlusTaxPercentage;
        [StarData] public bool AllowInfantry;
        [StarData] public float Maintenance;
        [StarData] public float Income;
        [StarData] public float Cost;
        [StarData] public int StorageAdded;
        [StarData] public float PlusResearchPerColonist;
        [StarData] public string ExcludesPlanetType = "";
        [StarData] public float PlusFlatResearchAmount;
        [StarData] public float CreditsPerColonist;
        [StarData] public float PlusFlatFoodAmount;
        [StarData] public float PlusFoodPerColonist;
        [StarData] public float MaxPopIncrease;
        [StarData] public float PlusProdPerColonist;
        [StarData] public float PlusFlatProductionAmount;
        [StarData] public float SensorRange;
        [StarData] public bool IsProjector;
        [StarData] public float ProjectorRange;
        [StarData] public float ShipRepair; // ship repair multiplier, intended to be 1.0 or 2.0 etc
        [StarData] public BuildingCategory Category;
        [StarData] public bool IsPlayerAdded;
        [StarData] public int InvadeInjurePoints;
        [StarData] public int DefenseShipsCapacity;
        [StarData] public RoleName DefenseShipsRole;
        [StarData] public float Infrastructure;
        [StarData] public bool DetectsRemnantFleet;
        [StarData] public bool CannotBeBombed;
        [StarData] public float IncreaseRichness;
        [StarData] public float FoodCache; // Works with Flat food only
        [StarData] public float ProdCache; // Works with Prod per colonist only
        [StarData] public bool CanBeCreatedFromLava; // Can be created when lava is solidified
        [StarData] public bool CanBeTerraformed; // By level 1

        // XML Ignore because we load these from XML templates
        [XmlIgnore] public Weapon TheWeapon { get; private set; }
        [XmlIgnore] [StarData] public float Offense { get; private set; }
        [XmlIgnore] [StarData] public float MilitaryStrength { get; private set; }
        [XmlIgnore] public int CurrentNumDefenseShips { get; private set; }
        [XmlIgnore] public float ActualCost => Cost * UniverseState.DummyProductionPacePlaceholder;
        [XmlIgnore] public bool IsBadCacheResourceBuilding => 
            FoodCache.Greater(0) && PlusFlatFoodAmount.AlmostZero() 
            || ProdCache.Greater(0) && PlusProdPerColonist.AlmostZero();

        public override string ToString()
            => $"BID:{BID} Name:{Name} ActualCost:{ActualCost} +Tax:{PlusTaxPercentage}  Short:{GetShortDescrText()}";

        [XmlIgnore] public LocalizedText TranslatedName  => new(NameTranslationIndex);
        [XmlIgnore] public LocalizedText DescriptionText => new(DescriptionIndex);

        // Each Building templates has a unique ID:
        [XmlIgnore] public int BID { get; private set; }
        public void AssignBuildingId(int bid) => BID = bid;

        public static int CapitalId, OutpostId, BiospheresId, SpacePortId, TerraformerId;
        public static int VolcanoId, ActiveVolcanoId, EruptingVolcanoId, Lava1Id, Lava2Id, Lava3Id;
        public static int Crater1Id, Crater2Id, Crater3Id, Crater4Id;
        [XmlIgnore] public bool IsCapital          => BID == CapitalId;
        [XmlIgnore] public bool IsOutpost          => BID == OutpostId;
        [XmlIgnore] public bool IsCapitalOrOutpost => BID == CapitalId || BID == OutpostId;
        [XmlIgnore] public bool IsBiospheres       => BID == BiospheresId;
        [XmlIgnore] public bool IsSpacePort        => BID == SpacePortId;
        [XmlIgnore] public bool IsTerraformer      => BID == TerraformerId;
        [XmlIgnore] public bool IsVolcano          => BID == VolcanoId || BID == ActiveVolcanoId || BID == EruptingVolcanoId;
        [XmlIgnore] public bool IsLava             => BID == Lava1Id || BID == Lava2Id || BID == Lava3Id;
        [XmlIgnore] public bool IsCrater           => BID == Crater1Id || BID == Crater2Id || BID == Crater3Id || BID == Crater4Id;
        [XmlIgnore] public bool IsDynamicUpdate    => IsLava | IsVolcano || IsCrater;
        [XmlIgnore] public SubTexture IconTex      => ResourceManager.Texture($"Buildings/icon_{Icon}_48x48");
        [XmlIgnore] public SubTexture IconTex64    => ResourceManager.Texture($"Buildings/icon_{Icon}_64x64");
        [XmlIgnore] public string IconPath64       => $"Buildings/icon_{Icon}_64x64";
        [XmlIgnore] public float CostEffectiveness => MilitaryStrength / Cost.LowerBound(0.1f);
        [XmlIgnore] public bool HasLaunchedAllDefenseShips => CurrentNumDefenseShips <= 0;
        [XmlIgnore] private float DefenseShipStrength;
        [XmlIgnore] public float SpaceRange = 10000f;

        // these appear in Hardcore Ruleset
        public static int FissionablesId, MineFissionablesId, FuelRefineryId;

        [XmlIgnore]
        public float StrengthMax => ResourceManager.GetBuildingTemplate(BID).Strength;

        [StarDataDeserialized]
        public void OnDeserialized()
        {
            Building template = ResourceManager.GetBuildingTemplate(Name);
            if (template == null)
                return;

            BID = template.BID;

            // Patching: because of some game data changes, these values need to be patched
            //           from latest building templates
            ShipRepair = template.ShipRepair;
        }

        public Building Clone()
        {
            var b = (Building)MemberwiseClone();
            b.TheWeapon = null;
            return b;
        }

        public void CreateWeapon(Planet p)
        {
            if (!isWeapon || TheWeapon != null)
                return;

            TheWeapon = ResourceManager.CreateWeapon(Weapon);
            SpaceRange = TheWeapon.BaseRange;
            UpdateOffense(p);
        }

        public void UpdateAttackActions(int amount)
        {
            AvailableAttackActions = (AvailableAttackActions + amount).Clamped(0, 1);
        }

        public void UpdateAttackTimer(float amount)
        {
            if (!CanAttack) AttackTimer += amount;
        }

        public void ResetAttackTimer()
        {
            AttackTimer = 10;
        }

        public float ActualFireDelay(Planet p) => TheWeapon != null ? TheWeapon.FireDelay / (p.Level.LowerBound(1)) : 1;

        bool CanLaunchDefenseShips(Empire empire) => !HasLaunchedAllDefenseShips && empire.Money > 100;

        static IShipDesign GetDefenseShipName(RoleName roleName, Empire empire) 
                    => ShipBuilder.PickCostEffectiveShipToBuild(roleName, empire, 
                        empire.Money - 90, empire.Money/10);

        void LaunchDefenseShips(Planet p, Ship target, Empire empire)
        {
            if (CurrentNumDefenseShips <= 0 || target == null)
                return;

            IShipDesign selectedShip = GetDefenseShipName(DefenseShipsRole, empire);
            if (selectedShip == null) // the empire does not have any ship of this role to launch
                return;

            Vector2 launchVector = NewMathExt.RandomOffsetAndDistance(p.Position, 1000);
            Ship defenseShip = Ship.CreateDefenseShip(p.Universe, selectedShip.Name, empire, launchVector, p);
            if (defenseShip == null)
            {
                Log.Warning($"Could not create defense ship, ship name = {selectedShip}");
            }
            else
            {
                defenseShip.Level = 3;
                defenseShip.Velocity = UniverseRandom.RandomDirection() * defenseShip.MaxSTLSpeed;
                UpdateCurrentDefenseShips(-1);
                empire.ChargeCreditsHomeDefense(defenseShip);
            }
        }

        public void UpdateOffense(Planet p)
        {
            if (isWeapon)
            {
                if (TheWeapon == null)
                    CreateWeapon(p);
                Offense = TheWeapon.CalculateOffense(null) * p.Level; // fire delay is shorter when planet level is higher
            }
        }

        public void UpdateDefenseShipBuildingOffense(Empire empire, Planet p)
        {
            if (DefenseShipsCapacity <= 0)
                return;

            Offense = 0;
            UpdateOffense(p);
            IShipDesign pickedShip = ShipBuilder.PickFromCandidates(DefenseShipsRole, empire);

            if (pickedShip != null && ResourceManager.GetShipTemplate(pickedShip.Name, out Ship ship))
                DefenseShipStrength = ship.CalculateShipStrength() * DefenseShipsCapacity;

            Offense += DefenseShipStrength;
        }

        public void UpdateCurrentDefenseShips(int num)
        {
            if (DefenseShipsCapacity > 0)
                CurrentNumDefenseShips = (CurrentNumDefenseShips + num).Clamped(0, DefenseShipsCapacity);
        }

        public bool UpdateSpaceCombatActions(FixedSimTime timeStep, Planet p)
        {
            if (!isWeapon && DefenseShipsCapacity == 0)
                return false;

            bool canFireWeapon = false;
            if (TheWeapon != null)
            {
                WeaponTimer -= timeStep.FixedTime;
                canFireWeapon = WeaponTimer < 0f;
            }

            bool canLaunchShips = CanLaunchDefenseShips(p.Owner);

            if (canFireWeapon || canLaunchShips)
            {
                // this scan is pretty expensive
                Ship target = p.ScanForSpaceCombatTargets(TheWeapon, SpaceRange);
                if (target != null)
                {
                    if (canFireWeapon)
                    {
                        TheWeapon.FireFromPlanet(p, target);
                        WeaponTimer = ActualFireDelay(p);
                    }
                    if (canLaunchShips)
                    {
                        LaunchDefenseShips(p, target, p.Owner);
                    }
                    return true;
                }
            }
            return false;
        }

        public bool TryLandOnBuilding(Ship ship)
        {
            RoleName roleName = ship.DesignRole;
            if (DefenseShipsRole == roleName && CurrentNumDefenseShips < DefenseShipsCapacity)
            {
                UpdateCurrentDefenseShips(1);
                return true;
            }

            return false;
        }

        public float MaxFertilityOnBuildFor(Empire empire, PlanetCategory category) 
            => Empire.RacialEnvModifer(category, empire) * MaxFertilityOnBuild;

        public float ActualMaintenance(Planet p) => Maintenance * p.Owner.data.Traits.MaintMultiplier;
        
        [XmlIgnore]
        public bool EventHere          => !string.IsNullOrEmpty(EventTriggerUID) || Name == "Dynamic Crash Site";
        [XmlIgnore]
        public bool IsAttackable       => CombatStrength > 0;
        [XmlIgnore]
        public bool CanAttack          => CombatStrength > 0 && AvailableAttackActions > 0;
        [XmlIgnore]
        public bool IsMoneyBuilding    => CreditsPerColonist > 0 || PlusTaxPercentage > 0 || Income > 0;
        [XmlIgnore]
        public bool ProducesProduction => PlusFlatProductionAmount > 0 || PlusProdPerColonist > 0 || PlusProdPerRichness > 0;
        [XmlIgnore]
        public bool ProducesResearch   => PlusResearchPerColonist > 0 || PlusFlatResearchAmount > 0;
        [XmlIgnore]
        public bool ProducesFood       => PlusFlatFoodAmount > 0 || PlusFoodPerColonist > 0;
        [XmlIgnore]
        public bool ProducesPopulation => PlusFlatPopulation > 0;
        [XmlIgnore]
        public bool IsHarmfulToEnv     => MaxFertilityOnBuild < 0;
        [XmlIgnore]
        public bool IsMilitary         => CombatStrength > 0 
                                        && !IsCapitalOrOutpost
                                        && MaxPopIncrease.AlmostZero(); // FB - pop relevant because of CA

        [XmlIgnore] 
        public bool IsEventTerraformer => IsCommodity && PlusTerraformPoints > 0;

        public bool GoodFlatProduction(Planet p) =>
            PlusFlatProductionAmount > 0 || PlusProdPerRichness > 0 && p.MineralRichness > 0.2f;

        public bool GoodFlatFood() => PlusFlatFoodAmount > 0;

        static float Production(Planet planet, float flatBonus, float perColonistBonus, float adjust = 1)
        {
            return flatBonus + perColonistBonus * planet.PopulationBillion * adjust;
        }

        public float CreditsProduced(Planet planet)
        {
            return Production(planet, 0, CreditsPerColonist);            
        }

        public float FoodProduced(Planet planet)
        {
            if (planet.NonCybernetic)
                return Production(planet, PlusFlatFoodAmount, PlusFoodPerColonist, planet.Fertility);

            return ProductionProduced(planet);
        }

        public float ProductionProduced(Planet planet)
        {
            return Production(planet, PlusFlatProductionAmount, PlusProdPerColonist, planet.MineralRichness);
        }

        public bool AssignBuildingToTilePlanetCreation(Planet p, out PlanetGridSquare tile)
        {
            tile = AssignBuildingToRandomTile(p);
            if (tile != null)
                return true;


            if (EventHere && !CanBuildAnywhere) // set a random tile habitable for the event
            {
                PlanetGridSquare targetTile = p.TilesList.RandItem();
                targetTile.Habitable        = true;
                tile = AssignBuildingToRandomTile(p);

                return tile != null;
            }

            return false;
        }

        public PlanetGridSquare AssignBuildingToRandomTile(Planet planet)
        {
            var list = planet.TilesList.Filter(pgs => pgs.NoBuildingOnTile && (CanBuildAnywhere 
                                                                                ? !pgs.Habitable 
                                                                                : pgs.Habitable));

            if (list.Length == 0 && CanBuildAnywhere && !IsBiospheres) // try any tile available
                list = planet.TilesList.Filter(pgs => pgs.Building == null);

            if (list.Length == 0)
                return null;

            PlanetGridSquare target = RandomMath.RandItem(list);
            target.PlaceBuilding(this, planet);
            return target;
        }

        public bool AssignBuildingToTileOnColonize(Planet planet)
        {
            return AssignBuildingToRandomTile(planet) != null;
        }

        public bool AssignBuildingToTile(Building b, ref PlanetGridSquare where, Planet planet)
        {
            // only validate the location
            if (where != null)
                return where.CanBuildHere(b);

            PlanetGridSquare[] freeSpots = planet.TilesList.Filter(pgs => pgs.CanBuildHere(b));
            if (freeSpots.Length > 0)
                where = RandomMath.RandItem(freeSpots);
            return where != null;
        }

        // Event when a building is built at planet p
        public void OnBuildingBuiltAt(Planet p)
        {
            p.AddBuildingsFertility(MaxFertilityOnBuild);
            p.MineralRichness += IncreaseRichness.LowerBound(0); // This must be positive. since richness cannot go below 0.
            p.BuildingList.Add(this);

            p.HasSpacePort |= IsSpacePort || AllowShipBuilding;

            if (ProdCache.Greater(0) && PlusProdPerColonist.Greater(0))
                p.SetHasLimitedResourceBuilding(true);

            if (FoodCache.Greater(0) && PlusFlatFoodAmount.Greater(0))
                p.SetHasLimitedResourceBuilding(true);

            if (EventOnBuild != null && p.OwnerIsPlayer)
            {
                UniverseScreen u = p.Universe.Screen;
                ExplorationEvent e = ResourceManager.Event(EventOnBuild);
                u.ScreenManager.AddScreen(new EventPopup(u, u.Player, e, e.PotentialOutcomes[0], true, p));
            }

            if (IsCapital)
                p.RemoveOutpost();

            if (SensorRange > 0)
                p.OnSensorBuildingChange();
            
            UpdateOffense(p);
        }

        public void CalcMilitaryStrength(Planet p)
        {
            float score = 0;
            if (CanAttack)
            {
                score += Strength + Defense/2 + CombatStrength + SoftAttack + HardAttack;
                score += PlanetaryShieldStrengthAdded;
                score += InvadeInjurePoints * 20;
                if (AllowInfantry)
                    score += 50;

                score += CalcDefenseShipScore();
            }

            if (isWeapon && Offense == 0f)
                UpdateOffense(p);

            MilitaryStrength = score + Offense * 0.1f;
        }

        float CalcDefenseShipScore()
        {
            if (DefenseShipsCapacity <= 0 || DefenseShipsRole == RoleName.disabled)
                return 0;

            float defenseShipScore;
            switch (DefenseShipsRole)
            {
                case RoleName.drone:    defenseShipScore = 3f;  break;
                case RoleName.fighter:  defenseShipScore = 5f;  break;
                case RoleName.corvette: defenseShipScore = 10f; break;
                case RoleName.frigate:  defenseShipScore = 20f; break;
                default:                defenseShipScore = 50f; break;
            }

            return defenseShipScore * DefenseShipsCapacity;
        }
        
        // Calculate the actual ShipRepair of this building after applying bonuses
        public float ActualShipRepair(Planet p)
        {
            int level = (p?.Owner != null ? p.Level : 0);
            float baseRepairRate = ShipRepair * GlobalStats.Defaults.BaseShipyardRepair;
            float levelBonus = 1f + level * GlobalStats.Defaults.BonusRepairPerColonyLevel;
            return baseRepairRate * levelBonus;
        }

        public bool MoneyBuildingAndProfitable(float maintenance, float populationBillion)
        {
            if (!IsMoneyBuilding)
                return false;

            // we use gross profit since we dont want tax rate change to affect this often
            float grossProfit = PlusTaxPercentage * populationBillion + CreditsPerColonist * populationBillion;
            return maintenance < grossProfit;
        }

        public string GetShortDescrText(Planet p = null)
        {
            if (ShortDescriptionIndex > 0)
                return new LocalizedText(ShortDescriptionIndex).Text;

            bool comma = false;
            string text = "";
            if (MaxFertilityOnBuild.NotZero())
            {
                text += p?.Owner == null 
                    ? $"{BuildShortDescString(MaxFertilityOnBuild, GameText.MaxFerfilityOnBuild, ref comma)}" 
                    : $"{BuildShortDescString(MaxFertilityOnBuildFor(p.Owner, p.Category), GameText.MaxFerfilityOnBuild, ref comma)}";
            }

            if (PlusFoodPerColonist.NotZero())
            {
                text += p?.Owner == null 
                    ? $"{BuildShortDescString(PlusFoodPerColonist, GameText.FoodPerColonist, ref comma)}" 
                    : $"{BuildShortDescString(p.Food.FoodYieldFormula(p.Fertility, PlusFoodPerColonist-1), GameText.FoodPerColonist, ref comma)}";
            }

            if (PlusFlatFoodAmount.NotZero())
                text += $"{BuildShortDescString(PlusFlatFoodAmount, GameText.FoodPerTurn, ref comma)}";

            if (IncreaseRichness.NotZero())
                text += $"{BuildShortDescString(IncreaseRichness, GameText.MineralRichness, ref comma)}";

            if (PlusProdPerColonist.NotZero())
            {
                text += p?.Owner == null
                    ? $"{BuildShortDescString(PlusProdPerColonist, GameText.ProdPerColonist, ref comma)}"
                    : $"{BuildShortDescString(p.Prod.ProdYieldFormula(p.MineralRichness, PlusProdPerColonist - 1), GameText.ProdPerColonist, ref comma)}";
            }

            if (PlusFlatProductionAmount.NotZero())
                text += $"{BuildShortDescString(PlusFlatProductionAmount, GameText.ProdPerTurn, ref comma)}";

            if (PlusProdPerRichness.NotZero())
                text += $"{BuildShortDescString(PlusProdPerRichness, GameText.ProductionPerRichness, ref comma)}";

            if (PlusResearchPerColonist.NotZero())
                text += $"{BuildShortDescString(PlusResearchPerColonist, GameText.ResearchPerTurnPerAssigned, ref comma)}";

            if (PlusFlatResearchAmount.NotZero())
                text += $"{BuildShortDescString(PlusFlatResearchAmount, GameText.ResearchPerTurn, ref comma)}";

            if (CreditsPerColonist.NotZero())
                text += $"{BuildShortDescString(CreditsPerColonist, GameText.CreditsAddedPerColonist, ref comma)}";

            if (PlusTaxPercentage.NotZero())
                text += $"{BuildShortDescString(PlusTaxPercentage*100, GameText.IncreaseToTaxIncomes, ref comma, percent: true)}";

            if (MaxPopIncrease.NotZero())
                text += $"{BuildShortDescString(MaxPopIncrease/1000, GameText.PopMax, ref comma)}";

            if (PlusFlatPopulation.NotZero())
                text += $"{BuildShortDescString(PlusFlatPopulation, GameText.PlusFlatPop, ref comma)}";

            if (Infrastructure.NotZero())
                text += $"{BuildShortDescString(Infrastructure, GameText.Infrastructure, ref comma)}";

            if (ShipRepair.NotZero())
            {
                text += $"{BuildShortDescString(ActualShipRepair(p), GameText.ShipRepair, ref comma)}";
            }

            if (StorageAdded != 0)
                text += $"{BuildShortDescString(StorageAdded, GameText.MaxStorage, ref comma)}";

            if (PlanetaryShieldStrengthAdded.NotZero())
                text += $"{BuildShortDescString(PlanetaryShieldStrengthAdded, GameText.PlanetaryShieldStrengthAdded, ref comma)}";

            return text;
        }

        string BuildShortDescString(float value, GameText text, ref bool comma, bool percent = false)
        {
            string shortDesc = "";
            if (comma)
                shortDesc = ", ";

            shortDesc = value > 0 ? $"{shortDesc}+" : $"{shortDesc}";
            string percentage = percent ? "%" : "";
            shortDesc = $"{shortDesc}{value.RoundToFractionOf100()}{percentage} {new LocalizedText(text).Text}";
            comma = true;
            return shortDesc;
        }
    }
}
