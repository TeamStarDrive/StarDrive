using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.GameScreens.Espionage;

namespace Ship_Game
{
    public sealed class EmpireUIOverlay
    {
        public Empire Player;
        Rectangle res1;
        Rectangle res2;
        Rectangle res3;
        Rectangle res4;
        Rectangle res5;
        Array<Button> Buttons = new Array<Button>();
        bool LowRes;
        UniverseScreen Screen;

        public EmpireUIOverlay(Empire playerEmpire, GraphicsDevice device, UniverseScreen screen)
        {
            Player = playerEmpire;
            Screen = screen;

            var iRes1 = ResourceManager.Texture("EmpireTopBar/empiretopbar_res1");
            var iRes2 = ResourceManager.Texture("EmpireTopBar/empiretopbar_res2");
            var iRes3 = ResourceManager.Texture("EmpireTopBar/empiretopbar_res3");
            var iRes4 = ResourceManager.Texture("EmpireTopBar/empiretopbar_res4");
            var iRes5 = ResourceManager.Texture("EmpireTopBar/empiretopbar_res5");

            Vector2 Cursor = Vector2.Zero;
            res1 = new Rectangle((int)Cursor.X, 2, iRes1.Width, iRes1.Height);
            Cursor.X = Cursor.X + iRes1.Width;

            res2 = new Rectangle((int)Cursor.X, 2, iRes2.Width, iRes2.Height);
            Cursor.X = Cursor.X + iRes2.Width;

            res3 = new Rectangle((int)Cursor.X, 2, iRes3.Width, iRes3.Height);
            Cursor.X = Cursor.X + iRes3.Width;

            res4 = new Rectangle((int)Cursor.X, 2, iRes4.Width, iRes4.Height);
            Cursor.X = Cursor.X + iRes4.Width;

            Cursor.X = Empire.Universe.ScreenWidth - iRes5.Width;
            res5 = new Rectangle((int)Cursor.X, 2, iRes5.Width, iRes5.Height);

            Button r1 = new Button();
            r1.Rect = res1;
            r1.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_res1");
            r1.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_res1_hover");
            r1.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_res1_press");
            r1.launches = "Research";
            Buttons.Add(r1);

            Button r2 = new Button();
            r2.Rect = res2;
            r2.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_res2");
            r2.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_res2");
            r2.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_res2");
            r2.launches = "Research";
            Buttons.Add(r2);

            Button r3 = new Button();
            r3.Rect = res3;
            r3.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_res3");
            r3.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_res3_hover");
            r3.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_res3_press");
            r3.launches = "Budget";
            Buttons.Add(r3);

            Button r4 = new Button();
            r4.Rect = res4;
            r4.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_res4");
            r4.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_res4");
            r4.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_res4");
            r4.launches = "Budget";
            Buttons.Add(r4);

            Button r5 = new Button();
            r5.Rect = res5;
            r5.NormalTexture  = ResourceManager.Texture("EmpireTopBar/empiretopbar_res5");
            r5.HoverTexture   = ResourceManager.Texture("EmpireTopBar/empiretopbar_res5");
            r5.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_res5");
            Buttons.Add(r5);

            float rangeforbuttons = r5.Rect.X - (r4.Rect.X + r4.Rect.Width);
            float roomoneitherside = (rangeforbuttons - 734f) / 2f;
            //Added by McShooterz: Shifted buttons to add new ones, added dummy espionage button
            Cursor.X = r4.Rect.X + r4.Rect.Width + roomoneitherside;

            if (Empire.Universe.ScreenWidth >= 1920)
            {
                float saveY = Cursor.Y;
                Cursor.X -= 220f;

                Button ShipList = new Button();
                ShipList.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
                ShipList.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military");
                ShipList.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_hover");
                ShipList.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_pressed");
                ShipList.Text = Localizer.Token(GameText.ShipsArray);
                ShipList.launches = "ShipList";
                Buttons.Add(ShipList);
                Cursor.X = Cursor.X + ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 5;

                Button Fleets = new Button();
                Fleets.Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
                Fleets.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military");
                Fleets.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_hover");
                Fleets.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_pressed");
                Fleets.Text = Localizer.Token(GameText.Fleets);
                Fleets.launches = "Fleets";
                Buttons.Add(Fleets);
                Cursor.X = Cursor.X + ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 5;
                Cursor.Y = saveY;
            }
            else
            {
                Cursor.X -= 50f;                    
            }

            Button Shipyard = new Button();
            Shipyard.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            Shipyard.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military");
            Shipyard.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_hover");
            Shipyard.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_military_pressed");
            Shipyard.Text = Localizer.Token(GameText.Shipyard);
            Shipyard.launches = "Shipyard";
            Buttons.Add(Shipyard);
            Cursor.X = Cursor.X + ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 40;

            Button empire = new Button();
            empire.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            empire.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px");
            empire.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover");
            empire.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_pressed");
            empire.launches = "Empire";
            empire.Text = Localizer.Token(GameText.Empire);
            Buttons.Add(empire);
            Cursor.X = Cursor.X + ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 40;

            Button Espionage = new Button();
            Espionage.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            Espionage.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip");
            Espionage.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip_hover");
            Espionage.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip_pressed");
            Espionage.Text = Localizer.Token(GameText.Espionage2);
            Espionage.launches = "Espionage";
            Buttons.Add(Espionage);
            Cursor.X = Cursor.X + ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 5;

            Button Diplomacy = new Button();
            Diplomacy.Rect = new Rectangle((int)Cursor.X, 2, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px").Height);
            Diplomacy.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip");
            Diplomacy.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip_hover");
            Diplomacy.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_dip_pressed");
            Diplomacy.launches = "Diplomacy";
            Diplomacy.Text = Localizer.Token(GameText.Diplomacy);
            Buttons.Add(Diplomacy);
            Cursor.X = Cursor.X + (ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_168px_hover").Width + 7);

            Button MainMenu = new Button();
            MainMenu.Rect = new Rectangle(res5.X + 52, 39, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height);
            MainMenu.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu");
            MainMenu.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_hover");
            MainMenu.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_menu_pressed");
            MainMenu.launches = "Main Menu";
            MainMenu.Text = Localizer.Token(GameText.MainMenu);
            Buttons.Add(MainMenu);
            Cursor.X = Cursor.X + (ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px_hover").Width + 5);

            Button Help = new Button();
            Help.Rect = new Rectangle(res5.X + 72, 64, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px").Width, ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_132px").Height);
            Help.NormalTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px_menu");
            Help.HoverTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px_menu_hover");
            Help.PressedTexture = ResourceManager.Texture("EmpireTopBar/empiretopbar_btn_68px_menu_pressed");
            Help.Text = "Help";
            Help.launches = "?";
            Buttons.Add(Help);
        }

        public void Draw(SpriteBatch batch)
        {
            Vector2 textCursor = new Vector2();
            foreach (Button b in Buttons)
            {
                if (!string.IsNullOrEmpty(b.Text))
                {
                    textCursor.X = b.Rect.X + b.Rect.Width / 2 - Fonts.Arial12Bold.MeasureString(b.Text).X / 2f;
                    textCursor.Y = b.Rect.Y + b.Rect.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2 - (LowRes ? 1 : 0);
                }
                if (b.State == PressState.Normal)
                {
                    batch.Draw(b.NormalTexture, b.Rect, Color.White);
                    if (string.IsNullOrEmpty(b.Text))
                    {
                        continue;
                    }
                    batch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
                }
                else if (b.State != PressState.Hover)
                {
                    if (b.State != PressState.Pressed)
                    {
                        continue;
                    }
                    batch.Draw(b.PressedTexture, b.Rect, Color.White);
                    if (string.IsNullOrEmpty(b.Text))
                    {
                        continue;
                    }
                    textCursor.Y = textCursor.Y + 1f;
                    batch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
                }
                else
                {
                    batch.Draw(b.HoverTexture, b.Rect, Color.White);
                    if (string.IsNullOrEmpty(b.Text))
                    {
                        continue;
                    }
                    batch.DrawString(Fonts.Arial12Bold, b.Text, textCursor, new Color(255, 240, 189));
                }
            }

            int money = (int)Player.Money;
            float damoney = Empire.Universe.player.EstimateNetIncomeAtTaxRate(Empire.Universe.player.data.TaxRate);
            if (damoney <= 0f)
            {
                textCursor.X = res4.X + res2.Width - 30 - Fonts.Arial12Bold.MeasureString(string.Concat(money.ToString(), " (", damoney.ToString("#.0"), ")")).X;
                textCursor.Y = res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2;
                batch.DrawString(Fonts.Arial12Bold, string.Concat(money.ToString(), " (", damoney.String(), ")"), textCursor, new Color(255, 240, 189));
            }
            else
            {
                textCursor.X = res4.X + res2.Width - 30 - Fonts.Arial12Bold.MeasureString(string.Concat(money.ToString(), " (+", damoney.ToString("#.0"), ")")).X;
                textCursor.Y = res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2;
                batch.DrawString(Fonts.Arial12Bold, string.Concat(money.ToString(), " (+", damoney.String(), ")"), textCursor, new Color(255, 240, 189));
            }

            var starDatePos = new Vector2(res5.X + 75, textCursor.Y);
            string starDateText = LowRes ? Empire.Universe.StarDateString : "StarDate: " + Empire.Universe.StarDateString;
            batch.DrawString(Fonts.Arial12Bold, starDateText, starDatePos, new Color(255, 240, 189));

            if (Player.Research.NoTopic)
            {
                textCursor.X = res2.X + res2.Width - 30 - Fonts.Arial12Bold.MeasureString(Localizer.Token(GameText.Choose)+"...").X;
                textCursor.Y = res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2;
                batch.DrawString(Fonts.Arial12Bold, Localizer.Token(GameText.Choose)+"...", textCursor, new Color(255, 240, 189));
            }
            else
            {
                int xOffset = (int)(Player.Research.Current.PercentResearched * res2.Width);
                Rectangle gradientSourceRect = res2;
                gradientSourceRect.X = 159 - xOffset;
                Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("EmpireTopBar/empiretopbar_res2_gradient"), new Rectangle(res2.X, res2.Y, res2.Width, res2.Height), gradientSourceRect, Color.White);
                Empire.Universe.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("EmpireTopBar/empiretopbar_res2_over"), res2, Color.White);
                int research = (int)Player.Research.Current.Progress;
                int resCost = (int)Player.Research.Current.TechCost;
                float plusRes = Player.Research.NetResearch;
                float x = res2.X + res2.Width - 30;
                Graphics.Font arial12Bold = Fonts.Arial12Bold;
                object[] str = { research, "/", resCost, " (+", plusRes.String(1), ")" };
                textCursor.X = x - arial12Bold.MeasureString(string.Concat(str)).X;
                textCursor.Y = res2.Height / 2 - Fonts.Arial12Bold.LineSpacing / 2;
                Graphics.Font spriteFont = Fonts.Arial12Bold;
                object[] objArray = { research, "/", resCost, " (+", plusRes.String(1), ")" };
                batch.DrawString(spriteFont, string.Concat(objArray), textCursor, new Color(255, 240, 189));
            }
        }

        public void HandleInput(InputState input)
        {
            if (!GlobalStats.TakingInput)
            {
                if (input.KeyPressed(Keys.R))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
                }
                if (input.KeyPressed(Keys.T))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
                if (input.KeyPressed(Keys.Y))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
                }
                if (input.KeyPressed(Keys.U))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new EmpireManagementScreen(Empire.Universe, this));
                }
                if (input.KeyPressed(Keys.I))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                }
                if (input.KeyPressed(Keys.O))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                if (input.KeyPressed(Keys.E))
                {
                    GameAudio.EchoAffirmative();
                    Empire.Universe.ScreenManager.AddScreen(new EspionageScreen(Empire.Universe));
                }
                if (input.KeyPressed(Keys.P))
                {
                    GameAudio.TacticalPause();
                    Empire.Universe.ScreenManager.AddScreen(new InGameWiki(Empire.Universe));
                }
                if (input.KeyPressed(Keys.Home))
                {
                    if (EmpireManager.Player.GetCurrentCapital(out Planet capital))
                    {
                        GameAudio.SubBassWhoosh();
                        Screen.SelectedPlanet = capital;
                        Screen.CamDestination = new Vector3(capital.Center.X, capital.Center.Y + 400f, 9000);
                    }
                    else
                    {
                        GameAudio.NegativeClick();
                    }
                }
            }

            foreach (Button b in Buttons)
            {
                if (!b.Rect.HitTest(input.CursorPosition))
                {
                    b.State = PressState.Normal;
                }
                else
                {
                    string str = b.launches;
                    string str1 = str;
                    if (str != null)
                    {
                        switch (str1)
                        {
                            case "Research":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.ResearchScreen)+"\n\n"+Localizer.Token(GameText.CurrentResearch)+": "+Player.Research.TopicLocText, "R");
                                break;
                            }
                            case "Budget":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.EconomicOverview2), "T");
                                break;
                            }
                            case "Main Menu":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheMainMenu), "O");
                                break;
                            }
                            case "Shipyard":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheShipyard), "Y");
                                break;
                            }
                            case "Empire":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheEmpireOverviewScreen), "U");
                                break;
                            }
                            case "Diplomacy":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheDiplomacyOverviewScreen), "I");
                                break;
                            }
                            case "Espionage":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheEspionageManagementScreen), "E");
                                break;
                            }
                            case "ShipList":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheShipRoster), "K");
                                break;
                            }
                            case "Fleets":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheFleetManager), "J");
                                break;
                            }
                            case "?":
                            {
                                ToolTip.CreateTooltip(Localizer.Token(GameText.OpensTheHelpMenu), "P");
                                break;
                            }
                        }
                    }
                    if (b.State != PressState.Hover && b.State != PressState.Pressed)
                    {
                        GameAudio.MouseOver();
                    }
                    b.State = PressState.Hover;
                    if (input.LeftMouseHeldDown)
                    {
                        b.State = PressState.Pressed;
                    }
                    if (!input.InGameSelect)
                    {
                        continue;
                    }
                    string str2 = b.launches;
                    string str3 = str2;
                    if (str2 != null)
                    {
                        if (str3 == "Research")
                        {
                            GameAudio.EchoAffirmative();
                            Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
                        }
                        else if (str3 == "Budget")
                        {
                            GameAudio.EchoAffirmative();
                            Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                        }
                    }
                    string str4 = b.launches;
                    string str5 = str4;
                    if (str4 == null)
                    {
                        continue;
                    }
                    if (str5 == "Main Menu")
                    {
                        GameAudio.EchoAffirmative();
                        Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                    }
                    else if (str5 == "Shipyard")
                    {
                        GameAudio.EchoAffirmative();
                        Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
                    }
                    else if (str5 == "Fleets")
                    {
                        GameAudio.EchoAffirmative();
                        Empire.Universe.ScreenManager.AddScreen(new FleetDesignScreen(Empire.Universe, this));
                    }
                    else if (str5 == "ShipList")
                    {
                        GameAudio.EchoAffirmative();
                        Empire.Universe.ScreenManager.AddScreen(new ShipListScreen(Empire.Universe, this));
                    }
                    else if (str5 == "Empire")
                    {
                        Empire.Universe.ScreenManager.AddScreen(new EmpireManagementScreen(Empire.Universe, this));
                        GameAudio.EchoAffirmative();
                    }
                    else if (str5 == "Diplomacy")
                    {
                        Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                        GameAudio.EchoAffirmative();
                    }
                    else if (str5 == "Espionage")
                    {
                        Empire.Universe.ScreenManager.AddScreen(new EspionageScreen(Empire.Universe));
                        GameAudio.EchoAffirmative();
                    }
                    else if (str5 == "?")
                    {
                        GameAudio.TacticalPause();
                        Empire.Universe.ScreenManager.AddScreen(new InGameWiki(Empire.Universe));
                    }
                }
            }
        }

        public void HandleInput(InputState input, GameScreen caller)
        {
            foreach (Button b in Buttons)
            {
                if (!b.Rect.HitTest(input.CursorPosition))
                {
                    b.State = PressState.Normal;
                }
                else
                {

                    if (b.State != PressState.Hover && b.State != PressState.Pressed)
                    {
                        GameAudio.MouseOver();
                    }
                    b.State = input.LeftMouseHeldDown ? PressState.Pressed : PressState.Hover;

                    if (input.LeftMouseClick)
                    {
                        if (!(caller is ShipDesignScreen) && !(caller is FleetDesignScreen))
                        {
                            caller.ExitScreen();
                        }
                        else if (b.launches != "Shipyard" && b.launches != "Fleets")
                        {
                            if (caller is ShipDesignScreen)
                            {
                                (caller as ShipDesignScreen)//.ExitScreen();
                                    .ExitToMenu(b.launches);
                            }
                            else if (caller is FleetDesignScreen)
                            {
                                (caller as FleetDesignScreen).ExitScreen();
                            }
                            return;
                        }
                        else if (caller is FleetDesignScreen && b.launches != "Fleets")
                        {
                            (caller as FleetDesignScreen).ExitScreen();
                        }
                        string str = b.launches;
                        string str1 = str;
                        if (str != null)
                        {
                            if (str1 == "Research")
                            {
                                GameAudio.EchoAffirmative();
                                if (!(caller is ResearchScreenNew))
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new ResearchScreenNew(Empire.Universe, this));
                                }
                            }
                            else if (str1 == "Budget")
                            {
                                GameAudio.EchoAffirmative();
                                if (!(caller is BudgetScreen))
                                {
                                    Empire.Universe.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                                }
                            }
                        }
                        string str2 = b.launches;
                        string str3 = str2;
                        if (str2 == null)
                        {
                            continue;
                        }
                        if (str3 == "Main Menu")
                        {
                            GameAudio.EchoAffirmative();
                            Empire.Universe.ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe, caller));
                        }
                        else if (str3 == "Shipyard")
                        {
                            if (caller is ShipDesignScreen)
                            {
                                continue;
                            }
                            GameAudio.EchoAffirmative();
                            Empire.Universe.ScreenManager.AddScreen(new ShipDesignScreen(Empire.Universe, this));
                        }
                        else if (str3 == "Fleets")
                        {
                            if (caller is FleetDesignScreen)
                            {
                                continue;
                            }
                            GameAudio.EchoAffirmative();
                            Empire.Universe.ScreenManager.AddScreen(new FleetDesignScreen(Empire.Universe, this));
                        }
                        else if (str3 == "Empire")
                        {
                            Empire.Universe.ScreenManager.AddScreen(new EmpireManagementScreen(Empire.Universe, this));
                            GameAudio.EchoAffirmative();
                        }
                        else if (str3 == "Diplomacy")
                        {
                            Empire.Universe.ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                            GameAudio.EchoAffirmative();
                        }
                        else if (str3 == "?")
                        {
                            GameAudio.TacticalPause();
                            Empire.Universe.ScreenManager.AddScreen(new InGameWiki(Empire.Universe));
                        }
                    }
                }
            }
        }

        public void Update(float elapsedTime)
        {
        }

        public class Button
        {
            public Rectangle Rect;
            public PressState State;
            public SubTexture NormalTexture;
            public SubTexture HoverTexture;
            public SubTexture PressedTexture;
            public string Text = "";
            public string launches;
        }

        public enum PressState
        {
            Normal,
            Hover,
            Pressed
        }
    }
}
