using System.Xml.Serialization;
using Newtonsoft.Json;
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
        [StarData] public CommodityBonusType CommodityBonusType;
        [StarData] public float CommodityBonusAmount;
        [StarData] public bool IsCommodity;
        [StarData] public bool WinsGame;
        [StarData] public bool BuildOnlyOnce;
        [StarData] public string EventOnBuild;
        [StarData] public string EventTriggerUID = "";
        [StarData] public bool EventWasTriggered;
        [StarData] public bool CanBuildAnywhere;
        [StarData] public float PlusTerraformPoints;
        [StarData] public int Strength = 5;
        [StarData] public float PlusProdPerRichness;
        [StarData] public float PlanetaryShieldStrengthAdded;
        [StarData] public float PlusFlatPopulation;
        [StarData] public float MaxFertilityOnBuild;
        [StarData] public string Icon;
        [StarData] public bool Scrappable = true;
        [StarData] public bool Unique = true;
        [StarData] public bool isWeapon;
        [StarData] public string Weapon = "";
        [StarData] public float WeaponTimer;
        [StarData] public float AttackTimer;
        [StarData] public int AvailableAttackActions = 1; // FB - use UpdateAttackActions
        [StarData] public int CombatStrength;
        [StarData] public int SoftAttack;
        [StarData] public int HardAttack;
        [StarData] public int Defense; // Defense vs bombardment
        [StarData] public float PlusTaxPercentage;
        [StarData] public bool AllowInfantry;
        [StarData] public float Maintenance;
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
        [StarData] public float ShipRepair;
        [StarData] public BuildingCategory Category = BuildingCategory.General;
        [StarData] public bool IsPlayerAdded = false;
        [StarData] public int InvadeInjurePoints;
        [StarData] public int DefenseShipsCapacity;
        [StarData] public RoleName DefenseShipsRole;
        [StarData] public float Infrastructure;
        [StarData] public bool DetectsRemnantFleet;
        [StarData] public bool CannotBeBombed;
        [StarData] public float IncreaseRichness;
        [StarData] public byte EventSpawnChance = 15;
        [StarData] public float FoodCache; // Works with Flat food only
        [StarData] public float ProdCache; // Works with Prod per colonist only
        [StarData] public bool CanBeCreatedFromLava; // Can be created when lava is solidified


        // XML Ignore because we load these from XML templates
        [XmlIgnore][JsonIgnore] public Weapon TheWeapon { get; private set; }
        [XmlIgnore][JsonIgnore] public float Offense { get; private set; }
        [XmlIgnore][JsonIgnore] public int CurrentNumDefenseShips { get; private set; }
        [XmlIgnore][JsonIgnore] public float MilitaryStrength { get; private set; }
        [XmlIgnore][JsonIgnore] public float ActualCost => Cost * CurrentGame.ProductionPace;
        [XmlIgnore][JsonIgnore] public bool IsBadCacheResourceBuilding => 
            FoodCache.Greater(0) && PlusFlatFoodAmount.AlmostZero() 
            || ProdCache.Greater(0) && PlusProdPerColonist.AlmostZero();

        public override string ToString()
            => $"BID:{BID} Name:{Name} ActualCost:{ActualCost} +Tax:{PlusTaxPercentage}  Short:{ShortDescrText}";

        [XmlIgnore][JsonIgnore] public LocalizedText TranslatedName  => new LocalizedText(NameTranslationIndex);
        [XmlIgnore][JsonIgnore] public LocalizedText DescriptionText => new LocalizedText(DescriptionIndex);
        [XmlIgnore][JsonIgnore] public LocalizedText ShortDescrText  => new LocalizedText(ShortDescriptionIndex);

        // Each Building templates has a unique ID: 
        [XmlIgnore][JsonIgnore] public int BID { get; private set; }
        public void AssignBuildingId(int bid) => BID = bid;

        public static int CapitalId, OutpostId, BiospheresId, SpacePortId, TerraformerId;
        public static int VolcanoId, ActiveVolcanoId, EruptingVolcanoId, Lava1Id, Lava2Id, Lava3Id;
        public static int Crater1Id, Crater2Id, Crater3Id, Crater4Id;
        [XmlIgnore][JsonIgnore] public bool IsCapital          => BID == CapitalId;
        [XmlIgnore][JsonIgnore] public bool IsOutpost          => BID == OutpostId;
        [XmlIgnore][JsonIgnore] public bool IsCapitalOrOutpost => BID == CapitalId || BID == OutpostId;
        [XmlIgnore][JsonIgnore] public bool IsBiospheres       => BID == BiospheresId;
        [XmlIgnore][JsonIgnore] public bool IsSpacePort        => BID == SpacePortId;
        [XmlIgnore][JsonIgnore] public bool IsTerraformer      => BID == TerraformerId;
        [XmlIgnore][JsonIgnore] public bool IsVolcano          => BID == VolcanoId || BID == ActiveVolcanoId || BID == EruptingVolcanoId;
        [XmlIgnore][JsonIgnore] public bool IsLava             => BID == Lava1Id || BID == Lava2Id || BID == Lava3Id;
        [XmlIgnore][JsonIgnore] public bool IsCrater           => BID == Crater1Id || BID == Crater2Id || BID == Crater3Id || BID == Crater4Id;
        [XmlIgnore][JsonIgnore] public bool IsDynamicUpdate    => IsLava | IsVolcano || IsCrater;
        [XmlIgnore][JsonIgnore] public SubTexture IconTex      => ResourceManager.Texture($"Buildings/icon_{Icon}_48x48");
        [XmlIgnore][JsonIgnore] public SubTexture IconTex64    => ResourceManager.Texture($"Buildings/icon_{Icon}_64x64");
        [XmlIgnore][JsonIgnore] public string IconPath64       => $"Buildings/icon_{Icon}_64x64";
        [XmlIgnore][JsonIgnore] public float CostEffectiveness => MilitaryStrength / Cost.LowerBound(0.1f);
        [XmlIgnore][JsonIgnore] public bool HasLaunchedAllDefenseShips => CurrentNumDefenseShips <= 0;
        [XmlIgnore][JsonIgnore] private float DefenseShipStrength;
        [XmlIgnore][JsonIgnore] public float SpaceRange = 10000f;

        // these appear in Hardcore Ruleset
        public static int FissionablesId, MineFissionablesId, FuelRefineryId;

        [XmlIgnore][JsonIgnore]
        public float StrengthMax => ResourceManager.GetBuildingTemplate(BID).Strength;

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

        static Ship GetDefenseShipName(RoleName roleName, Empire empire) 
                    => ShipBuilder.PickCostEffectiveShipToBuild(roleName, empire, 
                        empire.Money - 90, empire.Money/10);

        void LaunchDefenseShips(Planet p, Ship target, Empire empire)
        {
            if (CurrentNumDefenseShips <= 0 || target == null)
                return;

            Ship selectedShip = GetDefenseShipName(DefenseShipsRole, empire);
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
                defenseShip.Velocity = UniverseRandom.RandomDirection() * defenseShip.SpeedLimit;
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
            Ship pickedShip = ShipBuilder.PickFromCandidates(DefenseShipsRole, empire);

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
        
        [XmlIgnore][JsonIgnore]
        public bool EventHere          => !string.IsNullOrEmpty(EventTriggerUID) || Name == "Dynamic Crash Site";
        [XmlIgnore][JsonIgnore]
        public bool IsAttackable       => CombatStrength > 0;
        [XmlIgnore][JsonIgnore]
        public bool CanAttack          => CombatStrength > 0 && AvailableAttackActions > 0;
        [XmlIgnore][JsonIgnore]
        public bool IsMoneyBuilding    => CreditsPerColonist > 0 || PlusTaxPercentage > 0;
        [XmlIgnore][JsonIgnore]
        public bool ProducesProduction => PlusFlatProductionAmount > 0 || PlusProdPerColonist > 0 || PlusProdPerRichness > 0;
        [XmlIgnore][JsonIgnore]
        public bool ProducesResearch   => PlusResearchPerColonist > 0 || PlusFlatResearchAmount > 0;
        [XmlIgnore][JsonIgnore]
        public bool ProducesFood       => PlusFlatFoodAmount > 0 || PlusFoodPerColonist > 0;
        [XmlIgnore][JsonIgnore]
        public bool ProducesPopulation => PlusFlatPopulation > 0;
        [XmlIgnore][JsonIgnore]
        public bool IsHarmfulToEnv     => MaxFertilityOnBuild < 0;
        [XmlIgnore][JsonIgnore]
        public bool IsMilitary         => CombatStrength > 0 
                                        && !IsCapitalOrOutpost
                                        && MaxPopIncrease.AlmostZero(); // FB - pop relevant because of CA

        [XmlIgnore] [JsonIgnore]
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
            if (DefenseShipsCapacity <= 0 || DefenseShipsRole == 0)
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

        public bool MoneyBuildingAndProfitable(float maintenance, float populationBillion)
        {
            if (!IsMoneyBuilding)
                return false;

            // we use gross profit since we dont want tax rate change to affect this often
            float grossProfit = PlusTaxPercentage * populationBillion + CreditsPerColonist * populationBillion;
            return maintenance < grossProfit;
        }
    }
}