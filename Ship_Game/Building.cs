using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Building
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public bool IsSensor;
        [Serialize(2)] public bool NoRandomSpawn;
        [Serialize(3)] public bool AllowShipBuilding;
        [Serialize(4)] public int NameTranslationIndex;
        [Serialize(5)] public int DescriptionIndex;
        [Serialize(6)] public int ShortDescriptionIndex;
        [Serialize(7)] public string ResourceCreated;
        [Serialize(8)] public string ResourceConsumed;
        [Serialize(9)] public float ConsumptionPerTurn;
        [Serialize(10)] public float OutputPerTurn;
        [Serialize(11)] public string CommodityRequired;
        [Serialize(12)] public CommodityBonusType CommodityBonusType;
        [Serialize(13)] public float CommodityBonusAmount;
        [Serialize(14)] public bool IsCommodity;
        [Serialize(15)] public bool WinsGame;
        [Serialize(16)] public bool BuildOnlyOnce;
        [Serialize(17)] public string EventOnBuild;
        [Serialize(18)] public string EventTriggerUID = "";
        [Serialize(19)] public bool EventWasTriggered;
        [Serialize(20)] public bool CanBuildAnywhere;
        [Serialize(21)] public float PlusTerraformPoints;
        [Serialize(22)] public int Strength = 5;
        [Serialize(23)] public float PlusProdPerRichness;
        [Serialize(24)] public float PlanetaryShieldStrengthAdded;
        [Serialize(25)] public float PlusFlatPopulation;
        [Serialize(26)] public float MaxFertilityOnBuild;
        [Serialize(27)] public string Icon;
        [Serialize(28)] public bool Scrappable = true;
        [Serialize(29)] public bool Unique = true;
        [Serialize(30)] public bool isWeapon;
        [Serialize(31)] public string Weapon = "";
        [Serialize(33)] public float WeaponTimer;
        [Serialize(34)] public float AttackTimer;
        [Serialize(35)] public int AvailableAttackActions = 1; // FB - use UpdateAttackActions
        [Serialize(36)] public int CombatStrength;
        [Serialize(37)] public int SoftAttack;
        [Serialize(38)] public int HardAttack;
        [Serialize(39)] public int Defense;
        [Serialize(40)] public float PlusTaxPercentage;
        [Serialize(41)] public bool AllowInfantry;
        [Serialize(42)] public float Maintenance;
        [Serialize(43)] public float Cost;
        [Serialize(44)] public int StorageAdded;
        [Serialize(45)] public float PlusResearchPerColonist;
        [Serialize(46)] public string ExcludesPlanetType = "";
        [Serialize(47)] public float PlusFlatResearchAmount;
        [Serialize(48)] public float CreditsPerColonist;
        [Serialize(49)] public float PlusFlatFoodAmount;
        [Serialize(50)] public float PlusFoodPerColonist;
        [Serialize(51)] public float MaxPopIncrease;
        [Serialize(52)] public float PlusProdPerColonist;
        [Serialize(53)] public float PlusFlatProductionAmount;
        [Serialize(54)] public float SensorRange;
        [Serialize(55)] public bool IsProjector;
        [Serialize(56)] public float ProjectorRange;
        [Serialize(57)] public float ShipRepair;
        [Serialize(58)] public BuildingCategory Category = BuildingCategory.General;
        [Serialize(59)] public bool IsPlayerAdded = false;
        [Serialize(60)] public int InvadeInjurePoints;
        [Serialize(61)] public int DefenseShipsCapacity;
        [Serialize(62)] public ShipData.RoleName DefenseShipsRole;
        [Serialize(63)] public float Infrastructure;
        [Serialize(64)] public bool DetectsRemnantFleet;
        [Serialize(65)] public bool CannotBeBombed;
        [Serialize(66)] public float IncreaseRichness;
        [Serialize(67)] public byte EventSpawnChance = 15;
        [Serialize(68)] public float FoodCache; // Works with Flat food only
        [Serialize(69)] public float ProdCache; // Works with Prod per colonist only


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

        [XmlIgnore][JsonIgnore] public LocalizedText TranslatedName => NameTranslationIndex;
        [XmlIgnore][JsonIgnore] public LocalizedText DescriptionText => DescriptionIndex;
        [XmlIgnore][JsonIgnore] public LocalizedText ShortDescrText => ShortDescriptionIndex;

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
        [XmlIgnore][JsonIgnore] public string IconPath64       => $"Buildings/icon_{Icon}_64x64";
        [XmlIgnore][JsonIgnore] public float CostEffectiveness => MilitaryStrength / Cost.LowerBound(0.1f);
        [XmlIgnore][JsonIgnore] public bool HasLaunchedAllDefenseShips => CurrentNumDefenseShips <= 0;
        [XmlIgnore][JsonIgnore] private float DefenseShipStrength;
        [XmlIgnore][JsonIgnore] public float SpaceRange = 10000f;

        // these appear in Hardcore Ruleset
        public static int FissionablesId, MineFissionablesId, FuelRefineryId;

        public float StrengthMax => ResourceManager.GetBuildingTemplate(BID).Strength;

        public Building Clone()
        {
            var b       = (Building)MemberwiseClone();
            b.TheWeapon = null;
            return b;
        }

        private void CreateWeapon()
        {
            if (!isWeapon)
                return;

            TheWeapon  = ResourceManager.CreateWeapon(Weapon);
            SpaceRange = TheWeapon.BaseRange;
            UpdateOffense();
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

        void ResetSpaceWeaponTimer(Planet p)
        {
            WeaponTimer = ActualFireDelay(p);
        }

        public float ActualFireDelay(Planet p) => TheWeapon != null ? TheWeapon.fireDelay / (p.Level.LowerBound(1) / 2f) : 1;

        void UpdateSpaceWeaponTimer(FixedSimTime timeStep)
        {
            WeaponTimer -= timeStep.FixedTime;
        }

        void FireOnSpaceTarget(Planet planet, Ship target)
        {
            if (isWeapon && target != null)
            {
                TheWeapon.FireFromPlanet(planet, target);
                ResetSpaceWeaponTimer(planet);
            }
        }

        bool ReadyToFireOnSpaceTargets            => WeaponTimer.Less(0);
        bool CanLaunchDefenseShips(Empire empire) => !HasLaunchedAllDefenseShips && empire.Money > 100;

        static Ship GetDefenseShipName(ShipData.RoleName roleName, Empire empire) 
                    => ShipBuilder.PickCostEffectiveShipToBuild(roleName, empire, 
                        empire.Money - 90, empire.Money/10);

        void LaunchDefenseShips(Planet p, Ship target, Empire empire)
        {
            if (CurrentNumDefenseShips <= 0 || target == null)
                return;

            Ship selectedShip = GetDefenseShipName(DefenseShipsRole, empire);
            if (selectedShip == null) // the empire does not have any ship of this role to launch
                return;

            Vector2 launchVector = MathExt.RandomOffsetAndDistance(p.Center, 1000);
            Ship defenseShip = Ship.CreateDefenseShip(selectedShip.Name, empire, launchVector, p);
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

        private void UpdateOffense(int planetLevel = 1)
        {
            if (isWeapon)
                Offense = TheWeapon.CalculateOffense() * (planetLevel/2f); // fire delay is shorter when planet level is higher
        }

        public void UpdateOffense(Planet p) => UpdateOffense(p.Level);

        public void UpdateDefenseShipBuildingOffense(Empire empire, Planet p)
        {
            if (DefenseShipsCapacity <= 0)
                return;

            Offense = 0;
            UpdateOffense(p.Level);
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

        public void UpdateSpaceCombatActions(FixedSimTime timeStep, Planet p, out bool targetFound)
        {
            targetFound = false;
            if (!isWeapon && DefenseShipsCapacity == 0)
                return;

            UpdateSpaceWeaponTimer(timeStep);
            if (ReadyToFireOnSpaceTargets || CanLaunchDefenseShips(p.Owner))
            {
                Ship target = p.ScanForSpaceCombatTargets(SpaceRange);
                targetFound = target != null;
                FireOnSpaceTarget(p, target);
                LaunchDefenseShips(p, target, p.Owner);
            }
        }

        public bool TryLandOnBuilding(Ship ship)
        {
            ShipData.RoleName roleName = ship.DesignRole;
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

        public bool EventHere          => !string.IsNullOrEmpty(EventTriggerUID) || Name == "Dynamic Crash Site";
        public bool IsAttackable       => CombatStrength > 0;
        public bool CanAttack          => CombatStrength > 0 && AvailableAttackActions > 0;
        public bool IsMoneyBuilding    => CreditsPerColonist > 0 || PlusTaxPercentage > 0;
        public bool ProducesProduction => PlusFlatProductionAmount > 0 || PlusProdPerColonist > 0 || PlusProdPerRichness > 0;
        public bool ProducesResearch   => PlusResearchPerColonist > 0 || PlusFlatResearchAmount > 0;
        public bool ProducesFood       => PlusFlatFoodAmount > 0 || PlusFoodPerColonist > 0;
        public bool ProducesPopulation => PlusFlatPopulation > 0;
        public bool IsHarmfulToEnv     => MaxFertilityOnBuild < 0;
        public bool IsMilitary         => CombatStrength > 0 
                                        && !IsCapitalOrOutpost
                                        && MaxPopIncrease.AlmostZero(); // FB - pop relevant because of CA

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

        public float ResearchProduced(Planet planet)
        {
            return Production(planet, PlusFlatResearchAmount, PlusResearchPerColonist);
        }

        public bool AssignBuildingToTile(Planet solarSystemBody = null)
        {
            if (AssignBuildingToRandomTile(solarSystemBody, true) != null)
                return true;
            PlanetGridSquare targetPGS;
            if (EventHere)
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)                
                    return targetPGS.Habitable = true;                    
                
            }
            if (IsOutpost || EventHere)
            {
                targetPGS = AssignBuildingToRandomTile(solarSystemBody);
                if (targetPGS != null)
                    return targetPGS.Habitable = true;
            }
            if (IsBiospheres)
                return AssignBuildingToRandomTile(solarSystemBody) != null;                    

            return false;            
        }

        public PlanetGridSquare AssignBuildingToRandomTile(Planet planet, bool mustBeHabitableTile = false)
        {
            PlanetGridSquare[] list = mustBeHabitableTile 
                ? planet.TilesList.Filter(pgs => pgs.Building == null && pgs.Habitable) 
                : planet.TilesList.Filter(pgs => pgs.Building == null);

            if (list.Length == 0)
                return null;

            PlanetGridSquare target = RandomMath.RandItem(list);
            target.PlaceBuilding(this, planet);
            return target;
        }

        public bool AssignBuildingToTileOnColonize(Planet planet)
        {
            if (AssignBuildingToRandomTile(planet, mustBeHabitableTile: true) != null) return true;
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
            p.MineralRichness += IncreaseRichness.LowerBound(0); //This must be positive. since richness cannot go below 0.
            p.BuildingList.Add(this);
            if (IsSpacePort && Empire.Universe != null)
            {
                p.Station.Planet = p;
                p.Station.LoadContent(Empire.Universe.ScreenManager, p.Owner);
            }

            p.HasSpacePort |= IsSpacePort || AllowShipBuilding;

            if (ProdCache.Greater(0) && PlusProdPerColonist.Greater(0))
                p.SetHasLimitedResourceBuilding(true);

            if (FoodCache.Greater(0) && PlusFlatFoodAmount.Greater(0))
                p.SetHasLimitedResourceBuilding(true);

            if (EventOnBuild != null && p.Owner?.isPlayer == true)
            {
                UniverseScreen u = Empire.Universe;
                ExplorationEvent e = ResourceManager.Event(EventOnBuild);
                u.ScreenManager.AddScreen(new EventPopup(u, u.PlayerEmpire, e, e.PotentialOutcomes[0], true, p));
            }

            UpdateOffense(p.Level);
        }

        public void CalcMilitaryStrength()
        {
            CreateWeapon();
            float score = 0;
            if (CanAttack)
            {
                score += Strength + Defense + CombatStrength + SoftAttack + HardAttack;
                score += PlanetaryShieldStrengthAdded;
                score += InvadeInjurePoints * 20;
                if (AllowInfantry)
                    score += 50;

                score += CalcDefenseShipScore();
            }

            MilitaryStrength = score + Offense/10;
        }

        float CalcDefenseShipScore()
        {
            if (DefenseShipsCapacity <= 0 || DefenseShipsRole == 0)
                return 0;

            float defenseShipScore;
            switch (DefenseShipsRole)
            {
                case ShipData.RoleName.drone:    defenseShipScore = 3f;  break;
                case ShipData.RoleName.fighter:  defenseShipScore = 5f;  break;
                case ShipData.RoleName.corvette: defenseShipScore = 10f; break;
                case ShipData.RoleName.frigate:  defenseShipScore = 20f; break;
                default:                         defenseShipScore = 50f; break;
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