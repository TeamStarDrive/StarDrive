using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class ColoniesListItem : ScrollListItem<ColoniesListItem>
    {
        readonly EmpireManagementScreen Screen;
        public Planet P;
        public Rectangle SysNameRect;
        public Rectangle PlanetNameRect;
        public Rectangle SliderRect;
        public Rectangle StorageRect;
        public Rectangle QueueRect;
        public Rectangle PopRect;
        public Rectangle FoodRect;
        public Rectangle ProdRect;
        public Rectangle ResRect;
        public Rectangle MoneyRect;

        AssignLaborComponent AssignLabor;

        ProgressBar FoodStorage;
        ProgressBar ProdStorage;
        Rectangle ApplyProductionRect;
        Rectangle CancelProductionRect;
        DropDownMenu FoodDropDown;
        DropDownMenu ProdDropDown;
        Rectangle FoodStorageIcon;
        Rectangle ProdStorageIcon;
        int NumShipsInQueue;
        int NumBuildingsInQueue;
        int NumTroopsInQueue;
        int TotalProdNeeded;

        bool ApplyProdHover;
        bool CancelProdHover;
        readonly bool LowRes;

        public ColoniesListItem(EmpireManagementScreen screen, Planet planet)
        {
            Screen = screen;
            LowRes = Screen.LowRes;
            P = planet;

            //UIList columns = Add(new UIList());
            //foreach (int columnWidth in new [] { 200, 200, 30, 30, 30, 30, 30, 375, 375 } )
            //{
            //    columns.Add(new UIPanel(0, 0, columnWidth, 80)).Border = ;
            //}
            //columns.PerformLayout();
        }

        public override void PerformLayout()
        {
            int x = (int)X;
            int y = (int)Y;
            int sliderWidth = Screen.LowRes ? 250 : 375;

            P.UpdateIncomes(false);
            SysNameRect    = new Rectangle(x, y, (int)((Rect.Width - (sliderWidth + 150)) * 0.17f) - 30, Rect.Height);
            PlanetNameRect = new Rectangle(x + SysNameRect.Width, y, (int)((Rect.Width - (sliderWidth + 150)) * 0.17f), Rect.Height);
            PopRect     = new Rectangle(PlanetNameRect.Right,      y,  30, Rect.Height);
            FoodRect    = new Rectangle(PlanetNameRect.Right + 30, y, 30, Rect.Height);
            ProdRect    = new Rectangle(PlanetNameRect.Right + 60, y, 30, Rect.Height);
            ResRect     = new Rectangle(PlanetNameRect.Right + 90, y, 30, Rect.Height);
            MoneyRect   = new Rectangle(PlanetNameRect.Right + 120, y, 30, Rect.Height);
            SliderRect  = new Rectangle(PlanetNameRect.Right + 150, y - 30, sliderWidth, Rect.Height + 25);
            StorageRect = new Rectangle(PlanetNameRect.Right + sliderWidth + 150, y, (int)((Rect.Width - (sliderWidth + 120)) * 0.33f), Rect.Height);
            QueueRect   = new Rectangle(PlanetNameRect.Right + sliderWidth + StorageRect.Width + 150, y, (int)((Rect.Width - (sliderWidth + 150)) * 0.33f), Rect.Height);

            if (AssignLabor == null)
            {
                AssignLabor = Add(new AssignLaborComponent(P, new RectF(SliderRect), useTitleFrame: false));
            }
            else
                AssignLabor.Rect = SliderRect;

            FoodStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, StorageRect.Y + (int)(0.25 * StorageRect.Height), (int)(0.4f * StorageRect.Width), 18))
            {
                Max = P.Storage.Max,
                Progress = P.FoodHere,
                color = "green"
            };

            int ddwidth = (int)(0.2f * StorageRect.Width);
            FoodDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
            FoodDropDown.AddOption(Localizer.Token(GameText.Store));
            FoodDropDown.AddOption(Localizer.Token(GameText.Import));
            FoodDropDown.AddOption(Localizer.Token(GameText.Export));
            FoodDropDown.ActiveIndex = (int)P.FS;
            FoodStorageIcon = new Rectangle(StorageRect.X + 20, FoodStorage.pBar.Y + FoodStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_food").Height / 2, ResourceManager.Texture("NewUI/icon_food").Width, ResourceManager.Texture("NewUI/icon_food").Height);
            ProdStorage = new ProgressBar(new Rectangle(StorageRect.X + 50, FoodStorage.pBar.Y + FoodStorage.pBar.Height + 10, (int)(0.4f * StorageRect.Width), 18))
            {
                Max = P.Storage.Max,
                Progress = P.ProdHere
            };
            ProdStorageIcon = new Rectangle(StorageRect.X + 20, ProdStorage.pBar.Y + ProdStorage.pBar.Height / 2 - ResourceManager.Texture("NewUI/icon_production").Height / 2, ResourceManager.Texture("NewUI/icon_production").Width, ResourceManager.Texture("NewUI/icon_production").Height);
            ProdDropDown = new DropDownMenu(new Rectangle(StorageRect.X + 50 + (int)(0.4f * StorageRect.Width) + 20, ProdStorage.pBar.Y + FoodStorage.pBar.Height / 2 - 9, ddwidth, 18));
            ProdDropDown.AddOption(Localizer.Token(GameText.Store));
            ProdDropDown.AddOption(Localizer.Token(GameText.Import));
            ProdDropDown.AddOption(Localizer.Token(GameText.Export));
            ProdDropDown.ActiveIndex = (int)P.PS;
            ApplyProductionRect = new Rectangle(QueueRect.X + QueueRect.Width - 50, QueueRect.Y + 10, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Width, ResourceManager.Texture("NewUI/icon_queue_rushconstruction").Height);
            CancelProductionRect = new Rectangle(QueueRect.X + QueueRect.Width - 20, QueueRect.Y + 10, ResourceManager.Texture("NewUI/icon_queue_delete").Width, ResourceManager.Texture("NewUI/icon_queue_delete").Height);
            UpdateQueueItemsList();

            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            P.UpdateIncomes(false);

            ApplyProdHover  = ApplyProductionRect.HitTest(input.CursorPosition);
            CancelProdHover = CancelProductionRect.HitTest(input.CursorPosition);

            if (ApplyProductionRect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.ClickToRushProductionFrom);

            if (CancelProductionRect.HitTest(input.CursorPosition))
                ToolTip.CreateTooltip(GameText.CancelProductionAndRemoveThis);

            if (input.LeftMouseClick)
            {
                if (CancelProdHover && P.IsConstructing)
                {
                    RunOnEmpireThread(() =>
                    {
                        QueueItem item = P.Construction.GetConstructionQueue()[0];
                        if (!item.IsComplete)
                        {
                            P.Construction.Cancel(item);
                            GameAudio.AcceptClick();
                        }
                        else
                        {
                            GameAudio.NegativeClick();
                            Log.Warning($"Deferred Action: Cancel Queue Item: Failed at index 0");
                        }
                        GameAudio.AcceptClick();
                    });
                }

                if (ApplyProdHover && P.IsConstructing)
                {
                    float maxAmount = input.IsCtrlKeyDown ? 10000f : 10f;
                    RunOnEmpireThread(() =>
                    {
                        bool hasValidConstruction = P.Construction.NotEmpty && !P.ConstructionQueue[0].IsComplete;
                        if (hasValidConstruction && P.Construction.RushProduction(0, maxAmount, rush: true))
                        {
                            GameAudio.AcceptClick();
                            UpdateQueueItemsList();
                        }
                        else
                        {
                            if (!hasValidConstruction)
                                Log.Warning($"Deferred Action: ColonyListItem: Rush failed");
                            GameAudio.NegativeClick();
                        }
                    });

                    return true;
                }

                if (P.NonCybernetic && FoodDropDown.r.HitTest(input.CursorPosition))
                {
                    GameAudio.AcceptClick();
                    FoodDropDown.Toggle();
                    RunOnEmpireThread(() =>
                    {
                        P.FS = (Planet.GoodState)((int)P.FS + (int)Planet.GoodState.IMPORT);
                        if (P.FS > Planet.GoodState.EXPORT)
                            P.FS = Planet.GoodState.STORE;
                    });
                    return true;
                }

                if (ProdDropDown.r.HitTest(input.CursorPosition))
                {
                    GameAudio.AcceptClick();
                    ProdDropDown.Toggle();
                    RunOnEmpireThread(() =>
                    {
                        P.PS = (Planet.GoodState)((int)P.PS + (int)Planet.GoodState.IMPORT);
                        if (P.PS > Planet.GoodState.EXPORT)
                            P.PS = Planet.GoodState.STORE;
                    });
                    return true;
                }
            }
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ProdStorage.Progress = P.ProdHere;
            FoodStorage.Progress = P.FoodHere;
            var TextColor2 = new Color(118, 102, 67, 50);
            var smallHighlight = new Color(118, 102, 67, 25);
            if (ItemIndex % 2 == 0)
            {
                batch.FillRectangle(Rect, smallHighlight);
            }
            if (P == Screen.SelectedPlanet)
            {
                batch.FillRectangle(Rect, TextColor2);
            }

            Color TextColor = Colors.Cream;
            if (Fonts.Pirulen16.MeasureString(P.ParentSystem.Name).X <= SysNameRect.Width)
            {
                Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen16.MeasureString(P.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen16, P.ParentSystem.Name, SysNameCursor, TextColor);
            }
            else
            {
                Vector2 SysNameCursor = new Vector2(SysNameRect.X + SysNameRect.Width / 2 - Fonts.Pirulen12.MeasureString(P.ParentSystem.Name).X / 2f, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen12, P.ParentSystem.Name, SysNameCursor, TextColor);
            }
            Rectangle planetIconRect = new Rectangle(PlanetNameRect.X + 5, PlanetNameRect.Y + 25, PlanetNameRect.Height - 50, PlanetNameRect.Height - 50);
            batch.Draw(P.PlanetTexture, planetIconRect, Color.White);
            var cursor = new Vector2(PopRect.X + PopRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            float population = P.PopulationBillion;
            string popstring = population.String();
            cursor.X = cursor.X - Fonts.Arial12.MeasureString(popstring).X;
            HelperFunctions.ClampVectorToInt(ref cursor);
            batch.DrawString(Fonts.Arial12, popstring, cursor, Color.White);
            cursor = new Vector2(FoodRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);

            string fstring = P.Food.NetIncome.String();
            cursor.X -= Fonts.Arial12.MeasureString(fstring).X;
            HelperFunctions.ClampVectorToInt(ref cursor);
            batch.DrawString(Fonts.Arial12, fstring, cursor, (P.Food.NetIncome >= 0f ? Color.White : Color.LightPink));
            
            cursor = new Vector2(ProdRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            string pstring = P.Prod.NetIncome.String();
            cursor.X -= Fonts.Arial12.MeasureString(pstring).X;
            HelperFunctions.ClampVectorToInt(ref cursor);
            bool pink = P.Prod.NetIncome < 0f;
            batch.DrawString(Fonts.Arial12, pstring, cursor, (pink ? Color.LightPink : Color.White));
            
            cursor = new Vector2(ResRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            string rstring = P.Res.NetIncome.String();
            cursor.X = cursor.X - Fonts.Arial12.MeasureString(rstring).X;
            HelperFunctions.ClampVectorToInt(ref cursor);
            batch.DrawString(Fonts.Arial12, rstring, cursor, Color.White);
            
            cursor = new Vector2(MoneyRect.X + FoodRect.Width - 5, PlanetNameRect.Y + PlanetNameRect.Height / 2 - Fonts.Arial12.LineSpacing / 2);
            float money = P.Money.NetRevenue;
            string mstring = money.String();
            cursor.X = cursor.X - Fonts.Arial12.MeasureString(mstring).X;
            HelperFunctions.ClampVectorToInt(ref cursor);
            batch.DrawString(Fonts.Arial12, mstring, cursor, (money >= 0f ? Color.White : Color.LightPink));
            
            if (Fonts.Pirulen16.MeasureString(P.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
            {
                var a = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen16.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen16, P.Name, a, TextColor);
            }
            else if (Fonts.Pirulen12.MeasureString(P.Name).X + planetIconRect.Width + 10f <= PlanetNameRect.Width)
            {
                var b = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Pirulen12.LineSpacing / 2);
                batch.DrawString(Fonts.Pirulen12, P.Name, b, TextColor);
            }
            else
            {
                var c = new Vector2(planetIconRect.X + planetIconRect.Width + 10, SysNameRect.Y + SysNameRect.Height / 2 - Fonts.Arial8Bold.LineSpacing / 2);
                batch.DrawString(Fonts.Arial8Bold, P.Name, c, TextColor);
            }

            base.Draw(batch, elapsed);

            DrawStorage(batch);

            if (P.ConstructionQueue.Count > 0)
            {
                QueueItem qi = P.ConstructionQueue[0];
                qi.DrawAt(batch, new Vector2(QueueRect.X + 10, QueueRect.Y + QueueRect.Height / 2 - 30), LowRes);

                batch.Draw((ApplyProdHover ? ResourceManager.Texture("NewUI/icon_queue_rushconstruction_hover1") : ResourceManager.Texture("NewUI/icon_queue_rushconstruction")), ApplyProductionRect, Color.White);
                batch.Draw((CancelProdHover ? ResourceManager.Texture("NewUI/icon_queue_delete_hover1") : ResourceManager.Texture("NewUI/icon_queue_delete")), CancelProductionRect, Color.White);
                DrawQueueStats(batch);
            }

            batch.DrawRectangle(Rect, TextColor2);
        }

        void DrawQueueStats(SpriteBatch batch)
        {
            if (P.ConstructionQueue.Count < 2)
                return;

            string stats = $"In Queue ({P.ConstructionQueue.Count}):";
            if (NumShipsInQueue > 0)
                stats = $"{stats} ships ({NumShipsInQueue}),";

            if (NumBuildingsInQueue > 0)
                stats = $"{stats} buildings ({NumBuildingsInQueue}),";

            if (NumTroopsInQueue > 0)
                stats = $"{stats} Troops ({NumTroopsInQueue}),";

            stats   = stats.TrimEnd(',');
            stats   = $"{stats}. Total: {TotalProdNeeded}";
            var pos = new Vector2(QueueRect.X + 10, QueueRect.Y + QueueRect.Height / 2 + 15);

            SpriteFont font = LowRes ? Fonts.Arial8Bold : Fonts.Arial12;
            batch.DrawString(font, stats, pos, Color.Gray);
        }

        void DrawStorage(SpriteBatch batch)
        {
            if (P.Owner.data.Traits.Cybernetic != 0)
            {
                FoodStorage.DrawGrayed(batch);
                FoodDropDown.DrawGrayed(batch);
            }
            else
            {
                FoodStorage.Draw(batch);
                FoodDropDown.Draw(batch);
            }

            ProdStorage.Draw(batch);
            ProdDropDown.Draw(batch);
            batch.Draw(ResourceManager.Texture("NewUI/icon_food"), FoodStorageIcon,
                (P.Owner.NonCybernetic ? Color.White : new Color(110, 110, 110, 255)));
            batch.Draw(ResourceManager.Texture("NewUI/icon_production"), ProdStorageIcon, Color.White);

            if (FoodStorageIcon.HitTest(Screen.Input.CursorPosition))
            {
                ToolTip.CreateTooltip(P.Owner.IsCybernetic ? GameText.YourPeopleAreCyberneticAnd
                                                           : GameText.IndicatesTheAmountOfFood);
            }

            if (ProdStorageIcon.HitTest(Screen.Input.CursorPosition))
            {
                ToolTip.CreateTooltip(GameText.IndicatesTheAmountOfProduction);
            }
        }

        void UpdateQueueItemsList()
        {
            if (P.ConstructionQueue.Count < 2)
                return;

            NumShipsInQueue     = P.ConstructionQueue.Filter(q => q.isShip).Length;
            NumBuildingsInQueue = P.ConstructionQueue.Filter(q => q.isBuilding).Length;
            NumTroopsInQueue    = P.ConstructionQueue.Filter(q => q.isTroop).Length;
            TotalProdNeeded     = (int)(P.TotalProdNeededInQueue() - P.ConstructionQueue.ToArray().Sum(q => q.ProductionSpent));
        }
    }
}
