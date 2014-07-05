using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class FleetDesignScreen : GameScreen, IDisposable
	{
		public static bool Open;

		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		public Camera2d camera;

		public ShipData ActiveHull;

		private Background bg = new Background();

		private Starfield starfield;

		public EmpireUIOverlay EmpireUI;

		private Menu2 TitleBar;

		private Menu2 ShipDesigns;

		private Vector2 TitlePos;

		private Vector2 ShipDesignsTitlePos;

		private Menu1 LeftMenu;

		private Menu1 RightMenu;

		public Fleet fleet;

		private ScrollList FleetSL;

		private ScrollList ShipSL;

		private CloseButton close;

		private BlueButton RequisitionForces;

		private BlueButton SaveDesign;

		private BlueButton LoadDesign;

		private Rectangle SelectedStuffRect;

		private Rectangle OperationsRect;

		private Rectangle PrioritiesRect;

		/*private BlueButton Orders1;

		private BlueButton Orders2;

		private BlueButton Orders3;

		private BlueButton Orders4;

		private BlueButton Orders5;

		private BlueButton Orders6; */

		private WeightSlider Slider_Assist;

		private WeightSlider Slider_Vulture;

		private WeightSlider Slider_Defend;

		private WeightSlider Slider_DPS;

		private WeightSlider Slider_Armor;

		private WeightSlider Slider_Shield;

		private List<ToggleButton> OrdersButtons = new List<ToggleButton>();

		private FloatSlider OperationalRadius;

		private SizeSlider Slider_Size;

		private Submenu sub_ships;

		private BatchRemovalCollection<Ship> AvailableShips = new BatchRemovalCollection<Ship>();

		private Vector3 CamPos = new Vector3(0f, 0f, 14000f);

		private Dictionary<int, Rectangle> FleetsRects = new Dictionary<int, Rectangle>();

		private float dragTimer;

		private List<FleetDesignScreen.ClickableSquad> ClickableSquads = new List<FleetDesignScreen.ClickableSquad>();

		private Vector2 CamVelocity = Vector2.Zero;

		private float DesiredCamHeight = 14000f;

		private Ship ActiveShipDesign;

		public int FleetToEdit = -1;

		private Vector2 startDrag;

		private Vector2 endDrag;

		private MouseState current;

		private MouseState previous;

		private UITextEntry FleetNameEntry = new UITextEntry();

		private Selector stuffSelector;

		private Selector operationsSelector;

		private Selector priorityselector;

		private List<FleetDesignScreen.ClickableNode> ClickableNodes = new List<FleetDesignScreen.ClickableNode>();

		private Fleet.Squad SelectedSquad;

		private Fleet.Squad HoveredSquad;

		private bool StartSelectionBox;

		private Rectangle SelectionBox;

		public static UniverseScreen screen;

		private List<FleetDataNode> SelectedNodeList = new List<FleetDataNode>();

		private List<FleetDataNode> HoveredNodeList = new List<FleetDataNode>();

		private Vector2 starfieldPos = Vector2.Zero;

		static FleetDesignScreen()
		{
			FleetDesignScreen.Open = false;
		}

		public FleetDesignScreen(EmpireUIOverlay EmpireUI, Fleet f)
		{
			this.fleet = f;
			this.EmpireUI = EmpireUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.75);
		}

		public FleetDesignScreen(EmpireUIOverlay EmpireUI)
		{
			this.fleet = new Fleet();
			this.EmpireUI = EmpireUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.75);
		}

		private void AdjustCamera()
		{
			this.CamPos.Z = MathHelper.SmoothStep(this.CamPos.Z, this.DesiredCamHeight, 0.2f);
		}

		public void ChangeFleet(int which)
		{
			this.SelectedNodeList.Clear();
			if (this.FleetToEdit != -1)
			{
				foreach (KeyValuePair<int, Ship_Game.Gameplay.Fleet> Fleet in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict())
				{
					foreach (Ship ship in Fleet.Value.Ships)
					{
						ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
					}
				}
			}
			this.FleetToEdit = which;
			List<FleetDataNode> ToRemove = new List<FleetDataNode>();
			foreach (FleetDataNode node in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].DataNodes)
			{
				if ((Ship_Game.ResourceManager.ShipsDict.ContainsKey(node.ShipName) || node.GetShip() != null) && (node.GetShip() != null || EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).WeCanBuildThis(node.ShipName)))
				{
					continue;
				}
				ToRemove.Add(node);
			}
			List<Ship_Game.Gameplay.Fleet.Squad> SquadsToRemove = new List<Ship_Game.Gameplay.Fleet.Squad>();
			foreach (FleetDataNode node in ToRemove)
			{
				EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].DataNodes.Remove(node);
				foreach (List<Ship_Game.Gameplay.Fleet.Squad> flanks in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].AllFlanks)
				{
					foreach (Ship_Game.Gameplay.Fleet.Squad Squad in flanks)
					{
						if (Squad.DataNodes.Contains(node))
						{
							Squad.DataNodes.Remove(node);
						}
						if (Squad.DataNodes.Count != 0)
						{
							continue;
						}
						SquadsToRemove.Add(Squad);
					}
				}
			}
			foreach (List<Ship_Game.Gameplay.Fleet.Squad> flanks in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].AllFlanks)
			{
				foreach (Ship_Game.Gameplay.Fleet.Squad Squad in SquadsToRemove)
				{
					if (!flanks.Contains(Squad))
					{
						continue;
					}
					flanks.Remove(Squad);
				}
			}
			this.fleet = EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[which];
			foreach (Ship ship in this.fleet.Ships)
			{
				ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
				ship.GetSO().Visibility = ObjectVisibility.Rendered;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			Viewport viewport;
			Rectangle? nullable;
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
				base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
				base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
			}
			base.ScreenManager.GraphicsDevice.Clear(Color.Black);
			FleetDesignScreen.screen.bg.Draw(FleetDesignScreen.screen, FleetDesignScreen.screen.starfield);
			base.ScreenManager.SpriteBatch.Begin();
			this.DrawGrid();
			if (this.SelectedNodeList.Count == 1)
			{
				viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 screenSpacePosition = viewport.Project(new Vector3(this.SelectedNodeList[0].FleetOffset.X, this.SelectedNodeList[0].FleetOffset.Y, 0f), this.projection, this.view, Matrix.Identity);
				Vector2 screenPos = new Vector2(screenSpacePosition.X, screenSpacePosition.Y);
				Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, this.SelectedNodeList[0].FleetOffset, 10000f * this.OperationalRadius.amount);
				viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
				Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
				float SSRadius = (float)Math.Abs(insetRadialSS.X - screenPos.X);
				Rectangle nodeRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, (int)SSRadius * 2, (int)SSRadius * 2);
				Vector2 Origin = new Vector2((float)(Ship_Game.ResourceManager.TextureDict["UI/node"].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict["UI/node"].Height / 2));
				nullable = null;
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/node1"], nodeRect, nullable, new Color(0, 255, 0, 75), 0f, Origin, SpriteEffects.None, 1f);
			}
			this.ClickableNodes.Clear();
			foreach (FleetDataNode node in this.fleet.DataNodes)
			{
				if (node.GetShip() == null)
				{
					float radius = 150f;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, node.FleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
					FleetDesignScreen.ClickableNode cs = new FleetDesignScreen.ClickableNode()
					{
						Radius = Radius,
						ScreenPos = pPos,
						nodeToClick = node
					};
					this.ClickableNodes.Add(cs);
				}
				else
				{
					Ship ship = node.GetShip();
					ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
					float radius = ship.GetSO().WorldBoundingSphere.Radius;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, ship.RelativeFleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
					FleetDesignScreen.ClickableNode cs = new FleetDesignScreen.ClickableNode()
					{
						Radius = Radius,
						ScreenPos = pPos,
						nodeToClick = node
					};
					this.ClickableNodes.Add(cs);
				}
			}
			foreach (FleetDataNode node in this.HoveredNodeList)
			{
				if (node.GetShip() == null)
				{
					if (node.GetShip() != null)
					{
						continue;
					}
					float radius = 150f;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, node.FleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
					{
						if (!squad.squad.DataNodes.Contains(node))
						{
							continue;
						}
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, squad.screenPos, pPos, new Color(0, 255, 0, 70), 2f);
					}
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, pPos, Radius, 250, new Color(255, 255, 255, 70), 2f);
				}
				else
				{
					Ship ship = node.GetShip();
					float radius = ship.GetSO().WorldBoundingSphere.Radius;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, ship.RelativeFleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
					{
						if (!squad.squad.DataNodes.Contains(node))
						{
							continue;
						}
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, squad.screenPos, pPos, new Color(0, 255, 0, 70), 2f);
					}
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, pPos, Radius, 250, new Color(255, 255, 255, 70), 2f);
				}
			}
			foreach (FleetDataNode node in this.SelectedNodeList)
			{
				if (node.GetShip() == null)
				{
					if (node.GetShip() != null)
					{
						continue;
					}
					float radius = 150f;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, node.FleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
					{
						if (!squad.squad.DataNodes.Contains(node))
						{
							continue;
						}
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, squad.screenPos, pPos, new Color(0, 255, 0, 70), 2f);
					}
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, pPos, Radius, 250, Color.White, 2f);
				}
				else
				{
					Ship ship = node.GetShip();
					float radius = ship.GetSO().WorldBoundingSphere.Radius;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, ship.RelativeFleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
					{
						if (!squad.squad.DataNodes.Contains(node))
						{
							continue;
						}
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, squad.screenPos, pPos, new Color(0, 255, 0, 70), 2f);
					}
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, pPos, Radius, 250, Color.White, 2f);
				}
			}
			this.DrawFleetManagementIndicators();
			Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, this.SelectionBox, Color.Green, 1f);
			base.ScreenManager.SpriteBatch.End();
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.RenderManager.Render();
			}
			base.ScreenManager.SpriteBatch.Begin();
			this.TitleBar.Draw();
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, "Fleet Hotkeys", this.TitlePos, new Color(255, 239, 208));
			int numEntries = 9;
			int k = 9;
			int m = 0;
			foreach (KeyValuePair<int, Rectangle> rect in this.FleetsRects)
			{
				if (m == 9)
				{
					break;
				}
				Rectangle r = rect.Value;
				float transitionOffset = MathHelper.Clamp((base.TransitionPosition - 0.5f * (float)k / (float)numEntries) / 0.5f, 0f, 1f);
				k--;
				if (base.ScreenState != Ship_Game.ScreenState.TransitionOn)
				{
					r.X = r.X + (int)transitionOffset * 512;
				}
				else
				{
					r.X = r.X - (int)(transitionOffset * 256f);
					if (transitionOffset == 0f)
					{
						AudioManager.PlayCue("blip_click");
					}
				}
				Selector sel = new Selector(base.ScreenManager, r, Color.TransparentBlack);
				if (rect.Key != this.FleetToEdit)
				{
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/rounded_square"], r, Color.Black);
				}
				else
				{
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/rounded_square"], r, new Color(0, 0, 255, 80));
				}
				sel.Draw();
				Fleet f = EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[rect.Key];
				if (f.DataNodes.Count > 0)
				{
					Rectangle firect = new Rectangle(rect.Value.X + 6, rect.Value.Y + 6, rect.Value.Width - 12, rect.Value.Width - 12);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("FleetIcons/", f.FleetIconIndex.ToString())], firect, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).EmpireColor);
				}
				Vector2 num = new Vector2((float)(rect.Value.X + 4), (float)(rect.Value.Y + 4));
				SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
				SpriteFont pirulen12 = Fonts.Pirulen12;
				int key = rect.Key;
				spriteBatch.DrawString(pirulen12, key.ToString(), num, Color.Orange);
				num.X = num.X + (float)(rect.Value.Width + 5);
				if (rect.Key != this.FleetToEdit)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, f.Name, num, Color.Gray);
				}
				else
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, f.Name, num, Color.White);
				}
				m++;
			}
			if (this.FleetToEdit != -1)
			{
				this.ShipDesigns.Draw();
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Laserian14, "Ship Designs", this.ShipDesignsTitlePos, new Color(255, 239, 208));
				Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, this.sub_ships.Menu, new Color(0, 0, 0, 130));
				this.sub_ships.Draw();
				this.ShipSL.Draw(base.ScreenManager.SpriteBatch);
				Vector2 bCursor = new Vector2((float)(this.RightMenu.Menu.X + 5), (float)(this.RightMenu.Menu.Y + 25));
				for (int i = this.ShipSL.indexAtTop; i < this.ShipSL.Copied.Count && i < this.ShipSL.indexAtTop + this.ShipSL.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.ShipSL.Copied[i];
					bCursor.Y = (float)e.clickRect.Y;
					if (e.item is ModuleHeader)
					{
						(e.item as ModuleHeader).DrawWidth(base.ScreenManager, bCursor, 265);
					}
					else if (e.clickRectHover != 0)
					{
						bCursor.Y = (float)e.clickRect.Y;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Icons/icon_ship_02"], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
						Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Name, tCursor, Color.White);
						tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (e.item as Ship).Role, tCursor, Color.Orange);
						if (e.Plus != 0)
						{
							if (e.PlusHover != 0)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
							}
							else
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_add_hover1"], e.addRect, Color.White);
							}
						}
						if (e.Edit != 0)
						{
							if (e.EditHover != 0)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
							}
							else
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_edit_hover1"], e.editRect, Color.White);
							}
						}
						if (e.clickRect.Y == 0)
						{
						}
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
						Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, ((e.item as Ship).VanityName != "" ? (e.item as Ship).VanityName : (e.item as Ship).Name), tCursor, Color.White);
						tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
						if (this.sub_ships.Tabs[0].Selected)
						{
							base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as Ship).Role, tCursor, Color.Orange);
						}
						else if ((e.item as Ship).GetSystem() == null)
						{
							base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Deep Space", tCursor, Color.Orange);
						}
						else
						{
							base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat((e.item as Ship).GetSystem().Name, " system"), tCursor, Color.Orange);
						}
						if (e.Plus != 0)
						{
							if (e.PlusHover != 0)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_add_hover2"], e.addRect, Color.White);
							}
							else
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_add"], e.addRect, Color.White);
							}
						}
						if (e.Edit != 0)
						{
							if (e.EditHover != 0)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_edit_hover2"], e.editRect, Color.White);
							}
							else
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["NewUI/icon_build_edit"], e.editRect, Color.White);
							}
						}
						if (e.clickRect.Y == 0)
						{
						}
					}
				}
			}
			this.EmpireUI.Draw(base.ScreenManager.SpriteBatch);
			foreach (FleetDataNode node in this.fleet.DataNodes)
			{
				Vector2 vector2 = new Vector2((float)(Ship_Game.ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
				if (node.GetShip() == null || this.CamPos.Z <= 15000f)
				{
					if (node.GetShip() != null || node.ShipName == "Troop Shuttle")
					{
						continue;
					}
					Ship ship = Ship_Game.ResourceManager.ShipsDict[node.ShipName];
					float radius = 150f;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(node.FleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, node.FleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					Rectangle r = new Rectangle((int)pPos.X - (int)Radius, (int)pPos.Y - (int)Radius, (int)Radius * 2, (int)Radius * 2);
					Guid goalGUID = node.GoalGUID;
					if (node.GoalGUID == Guid.Empty)
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("TacticalIcons/symbol_", ship.Role)], r, (this.HoveredNodeList.Contains(node) || this.SelectedNodeList.Contains(node) ? Color.White : Color.Red));
					}
					else
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("TacticalIcons/symbol_", ship.Role)], r, (this.HoveredNodeList.Contains(node) || this.SelectedNodeList.Contains(node) ? Color.White : Color.Yellow));
						string buildingat = "";
						foreach (Goal g in this.fleet.Owner.GetGSAI().Goals)
						{
							if (!(g.guid == node.GoalGUID) || g.GetPlanetWhereBuilding() == null)
							{
								continue;
							}
							buildingat = g.GetPlanetWhereBuilding().Name;
						}
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, (buildingat != "" ? string.Concat("Building at:\n", buildingat) : "Need spaceport"), pPos + new Vector2(5f, -5f), Color.White);
					}
				}
				else
				{
					Ship ship = node.GetShip();
					float radius = ship.GetSO().WorldBoundingSphere.Radius;
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(ship.RelativeFleetOffset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Vector2 radialPos = HelperFunctions.GeneratePointOnCircle(90f, ship.RelativeFleetOffset, radius);
					viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 insetRadialPos = viewport.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					float Radius = Vector2.Distance(insetRadialSS, pPos);
					if (Radius < 10f)
					{
						Radius = 10f;
					}
					Rectangle r = new Rectangle((int)pPos.X - (int)Radius, (int)pPos.Y - (int)Radius, (int)Radius * 2, (int)Radius * 2);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("TacticalIcons/symbol_", ship.Role)], r, (this.HoveredNodeList.Contains(node) || this.SelectedNodeList.Contains(node) ? Color.White : Color.Green));
				}
			}
			if (this.ActiveShipDesign != null)
			{
				Ship ship = this.ActiveShipDesign;
				int x = Mouse.GetState().X;
				MouseState state = Mouse.GetState();
				Rectangle rectangle = new Rectangle(x, state.Y, ship.Size / 2, ship.Size / 2);
				float scale = (float)((float)ship.Size) / (float)(30 + Ship_Game.ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width);
				Vector2 IconOrigin = new Vector2((float)(Ship_Game.ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict["TacticalIcons/symbol_fighter"].Width / 2));
				scale = scale * 4000f / this.CamPos.Z;
				if (scale > 1f)
				{
					scale = 1f;
				}
				if (scale < 0.15f)
				{
					scale = 0.15f;
				}
				SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
				Texture2D item = Ship_Game.ResourceManager.TextureDict[string.Concat("TacticalIcons/symbol_", ship.Role)];
				float single = (float)Mouse.GetState().X;
				state = Mouse.GetState();
				nullable = null;
				spriteBatch1.Draw(item, new Vector2(single, (float)state.Y), nullable, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).EmpireColor, 0f, IconOrigin, scale, SpriteEffects.None, 1f);
			}
			this.DrawSelectedData(gameTime);
			this.close.Draw(base.ScreenManager);
			ToolTip.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.EndFrameRendering();
				base.ScreenManager.editor.EndFrameRendering();
				base.ScreenManager.sceneState.EndFrameRendering();
			}
		}

		private void DrawFleetManagementIndicators()
		{
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 pScreenSpace = viewport.Project(new Vector3(0f, 0f, 0f), this.projection, this.view, Matrix.Identity);
			Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
			Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, new Rectangle((int)pPos.X - 3, (int)pPos.Y - 3, 6, 6), new Color(255, 255, 255, 80));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Fleet Center", new Vector2(pPos.X - Fonts.Arial12Bold.MeasureString("Fleet Center").X / 2f, pPos.Y + 5f), new Color(255, 255, 255, 70));
			foreach (List<Fleet.Squad> flank in this.fleet.AllFlanks)
			{
				foreach (Fleet.Squad squad in flank)
				{
					Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
					pScreenSpace = viewport1.Project(new Vector3(squad.Offset, 0f), this.projection, this.view, Matrix.Identity);
					pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, new Rectangle((int)pPos.X - 2, (int)pPos.Y - 2, 4, 4), new Color(0, 255, 0, 110));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, "Squad", new Vector2(pPos.X - Fonts.Arial8Bold.MeasureString("Squad").X / 2f, pPos.Y + 5f), new Color(0, 255, 0, 70));
				}
			}
		}

		private void DrawGrid()
		{
			int size = 20000;
			for (int x = 0; x < 21; x++)
			{
				Vector3 Origin = new Vector3((float)(x * size / 20 - size / 2), (float)(-(size / 2)), 0f);
				Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 OriginScreenSpace = viewport.Project(Origin, this.projection, this.view, Matrix.Identity);
				Vector3 End = new Vector3((float)(x * size / 20 - size / 2), (float)(size - size / 2), 0f);
				Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 EndScreenSpace = viewport1.Project(End, this.projection, this.view, Matrix.Identity);
				Vector2 origin = new Vector2(OriginScreenSpace.X, OriginScreenSpace.Y);
				Vector2 end = new Vector2(EndScreenSpace.X, EndScreenSpace.Y);
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, origin, end, new Color(211, 211, 211, 70));
			}
			for (int y = 0; y < 21; y++)
			{
				Vector3 Origin = new Vector3((float)(-(size / 2)), (float)(y * size / 20 - size / 2), 0f);
				Viewport viewport2 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 OriginScreenSpace = viewport2.Project(Origin, this.projection, this.view, Matrix.Identity);
				Vector3 End = new Vector3((float)(size - size / 2), (float)(y * size / 20 - size / 2), 0f);
				Viewport viewport3 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 EndScreenSpace = viewport3.Project(End, this.projection, this.view, Matrix.Identity);
				Vector2 origin = new Vector2(OriginScreenSpace.X, OriginScreenSpace.Y);
				Vector2 end = new Vector2(EndScreenSpace.X, EndScreenSpace.Y);
				Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, origin, end, new Color(211, 211, 211, 70));
			}
		}

		private void DrawSelectedData(GameTime gameTime)
		{
			if (this.SelectedNodeList.Count == 1)
			{
				this.stuffSelector = new Selector(base.ScreenManager, this.SelectedStuffRect, new Color(0, 0, 0, 180));
				this.stuffSelector.Draw();
				Vector2 Cursor = new Vector2((float)(this.SelectedStuffRect.X + 20), (float)(this.SelectedStuffRect.Y + 10));
				if (this.SelectedNodeList[0].GetShip() == null)
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat("(", this.SelectedNodeList[0].ShipName, ")"), Cursor, new Color(255, 239, 208));
				}
				else
				{
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, (this.SelectedNodeList[0].GetShip().VanityName != "" ? this.SelectedNodeList[0].GetShip().VanityName : string.Concat(this.SelectedNodeList[0].GetShip().Name, " (", this.SelectedNodeList[0].GetShip().Role, ")")), Cursor, new Color(255, 239, 208));
				}
				Cursor.Y = (float)(this.OperationsRect.Y + 10);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Movement Orders", Cursor, new Color(255, 239, 208));
				foreach (ToggleButton button in this.OrdersButtons)
				{
					button.Draw(base.ScreenManager);
				}
				this.operationsSelector = new Selector(base.ScreenManager, this.OperationsRect, new Color(0, 0, 0, 180));
				this.operationsSelector.Draw();
				Cursor = new Vector2((float)(this.OperationsRect.X + 20), (float)(this.OperationsRect.Y + 10));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Target Selection", Cursor, new Color(255, 239, 208));
				this.Slider_Armor.Draw(base.ScreenManager);
				this.Slider_Assist.Draw(base.ScreenManager);
				this.Slider_Defend.Draw(base.ScreenManager);
				this.Slider_DPS.Draw(base.ScreenManager);
				this.Slider_Shield.Draw(base.ScreenManager);
				this.Slider_Vulture.Draw(base.ScreenManager);
				this.priorityselector = new Selector(base.ScreenManager, this.PrioritiesRect, new Color(0, 0, 0, 180));
				this.priorityselector.Draw();
				Cursor = new Vector2((float)(this.PrioritiesRect.X + 20), (float)(this.PrioritiesRect.Y + 10));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Priorities", Cursor, new Color(255, 239, 208));
				this.OperationalRadius.Draw(base.ScreenManager);
				this.Slider_Size.Draw(base.ScreenManager);
				return;
			}
			if (this.SelectedNodeList.Count > 1)
			{
				this.stuffSelector = new Selector(base.ScreenManager, this.SelectedStuffRect, new Color(0, 0, 0, 180));
				this.stuffSelector.Draw();
				Vector2 Cursor = new Vector2((float)(this.SelectedStuffRect.X + 20), (float)(this.SelectedStuffRect.Y + 10));
				if (this.SelectedNodeList[0].GetShip() == null)
				{
					SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
					SpriteFont arial20Bold = Fonts.Arial20Bold;
					int count = this.SelectedNodeList.Count;
					spriteBatch.DrawString(arial20Bold, string.Concat("Group of ", count.ToString(), " ships selected"), Cursor, new Color(255, 239, 208));
				}
				else
				{
					SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
					SpriteFont spriteFont = Fonts.Arial20Bold;
					int num = this.SelectedNodeList.Count;
					spriteBatch1.DrawString(spriteFont, string.Concat("Group of ", num.ToString(), " ships selected"), Cursor, new Color(255, 239, 208));
				}
				Cursor.Y = (float)(this.OperationsRect.Y + 10);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Group Movement Orders", Cursor, new Color(255, 239, 208));
				foreach (ToggleButton button in this.OrdersButtons)
				{
					button.Draw(base.ScreenManager);
				}
				this.operationsSelector = new Selector(base.ScreenManager, this.OperationsRect, new Color(0, 0, 0, 180));
				this.operationsSelector.Draw();
				Cursor = new Vector2((float)(this.OperationsRect.X + 20), (float)(this.OperationsRect.Y + 10));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Group Target Selection", Cursor, new Color(255, 239, 208));
				this.Slider_Armor.Draw(base.ScreenManager);
				this.Slider_Assist.Draw(base.ScreenManager);
				this.Slider_Defend.Draw(base.ScreenManager);
				this.Slider_DPS.Draw(base.ScreenManager);
				this.Slider_Shield.Draw(base.ScreenManager);
				this.Slider_Vulture.Draw(base.ScreenManager);
				this.priorityselector = new Selector(base.ScreenManager, this.PrioritiesRect, new Color(0, 0, 0, 180));
				this.priorityselector.Draw();
				Cursor = new Vector2((float)(this.PrioritiesRect.X + 20), (float)(this.PrioritiesRect.Y + 10));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Group Priorities", Cursor, new Color(255, 239, 208));
				this.OperationalRadius.Draw(base.ScreenManager);
				this.Slider_Size.Draw(base.ScreenManager);
				return;
			}
			if (this.FleetToEdit == -1)
			{
				float transitionOffset = (float)Math.Pow((double)base.TransitionPosition, 2);
				Rectangle r = this.SelectedStuffRect;
				if (base.ScreenState == Ship_Game.ScreenState.TransitionOn)
				{
					r.Y = r.Y + (int)(transitionOffset * 256f);
				}
				this.stuffSelector = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 180));
				this.stuffSelector.Draw();
				Vector2 Cursor = new Vector2((float)(r.X + 20), (float)(r.Y + 10));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, "No Fleet Selected", Cursor, new Color(255, 239, 208));
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial20Bold.LineSpacing + 2);
				string txt = "You are not currently editing a fleet. Click a hotkey on the left side of the screen to begin creating or editing the corresponding fleet. \n\nWhen you are finished editing, you can save your fleet design to disk for quick access in the future.";
				txt = HelperFunctions.parseText(Fonts.Arial12Bold, txt, (float)(this.SelectedStuffRect.Width - 40));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, txt, Cursor, new Color(255, 239, 208));
				return;
			}
			this.stuffSelector = new Selector(base.ScreenManager, this.SelectedStuffRect, new Color(0, 0, 0, 180));
			this.stuffSelector.Draw();
			Fleet f = EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit];
			Vector2 Cursor1 = new Vector2((float)(this.SelectedStuffRect.X + 20), (float)(this.SelectedStuffRect.Y + 10));
			this.FleetNameEntry.Text = f.Name;
			this.FleetNameEntry.ClickableArea = new Rectangle((int)Cursor1.X, (int)Cursor1.Y, (int)Fonts.Arial20Bold.MeasureString(f.Name).X, Fonts.Arial20Bold.LineSpacing);
			this.FleetNameEntry.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, Cursor1, gameTime, (this.FleetNameEntry.Hover ? Color.Orange : new Color(255, 239, 208)));
			Cursor1.Y = Cursor1.Y + (float)(Fonts.Arial20Bold.LineSpacing + 10);
			Cursor1 = Cursor1 + new Vector2(50f, 30f);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Fleet Icon", Cursor1, new Color(255, 239, 208));
			Rectangle ficonrect = new Rectangle((int)Cursor1.X + 12, (int)Cursor1.Y + Fonts.Pirulen12.LineSpacing + 5, 64, 64);
			base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("FleetIcons/", f.FleetIconIndex.ToString())], ficonrect, f.Owner.EmpireColor);
			this.RequisitionForces.Draw(base.ScreenManager);
			this.SaveDesign.Draw(base.ScreenManager);
			this.LoadDesign.Draw(base.ScreenManager);
			this.priorityselector = new Selector(base.ScreenManager, this.PrioritiesRect, new Color(0, 0, 0, 180));
			this.priorityselector.Draw();
			Cursor1 = new Vector2((float)(this.PrioritiesRect.X + 20), (float)(this.PrioritiesRect.Y + 10));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, "Fleet Design Overview", Cursor1, new Color(255, 239, 208));
			Cursor1.Y = Cursor1.Y + (float)(Fonts.Pirulen12.LineSpacing + 2);
			string txt0 = Localizer.Token(4043);
			txt0 = HelperFunctions.parseText(Fonts.Arial12Bold, txt0, (float)(this.PrioritiesRect.Width - 40));
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, txt0, Cursor1, new Color(255, 239, 208));
		}

		public override void ExitScreen()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
			}
			this.EmpireUI.screen.RecomputeFleetButtons(true);
			this.starfield.UnloadContent();
			base.ExitScreen();
		}

		/*protected override void Finalize()
		{
			try
			{
				this.Dispose(false);
			}
			finally
			{
				base.Finalize();
			}
		}*/
        ~FleetDesignScreen() {
            //should implicitly do the same thing as the original bad finalize
        }

		private Vector2 GetWorldSpaceFromScreenSpace(Vector2 screenSpace)
		{
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 nearPoint = viewport.Unproject(new Vector3(screenSpace, 0f), this.projection, this.view, Matrix.Identity);
			Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 farPoint = viewport1.Unproject(new Vector3(screenSpace, 1f), this.projection, this.view, Matrix.Identity);
			Vector3 direction = farPoint - nearPoint;
			direction.Normalize();
			Ray pickRay = new Ray(nearPoint, direction);
			float k = -pickRay.Position.Z / pickRay.Direction.Z;
			Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
			return new Vector2(pickedPosition.X, pickedPosition.Y);
		}

		private void HandleEdgeDetection(InputState input)
		{
			this.EmpireUI.HandleInput(input, this);
			if (this.FleetNameEntry.HandlingInput)
			{
				return;
			}
			Vector2 MousePos = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			PresentationParameters pp = base.ScreenManager.GraphicsDevice.PresentationParameters;
			Vector2 upperLeftWorldSpace = this.GetWorldSpaceFromScreenSpace(new Vector2(0f, 0f));
			Vector2 lowerRightWorldSpace = this.GetWorldSpaceFromScreenSpace(new Vector2((float)pp.BackBufferWidth, (float)pp.BackBufferHeight));
			float xDist = lowerRightWorldSpace.X - upperLeftWorldSpace.X;
			if (MousePos.X == 0f || input.CurrentKeyboardState.IsKeyDown(Keys.Left) || input.CurrentKeyboardState.IsKeyDown(Keys.A))
			{
				this.CamPos.X = this.CamPos.X - 0.008f * xDist;
			}
			if (MousePos.X == (float)(pp.BackBufferWidth - 1) || input.CurrentKeyboardState.IsKeyDown(Keys.Right) || input.CurrentKeyboardState.IsKeyDown(Keys.D))
			{
				this.CamPos.X = this.CamPos.X + 0.008f * xDist;
			}
			if (MousePos.Y == 0f || input.CurrentKeyboardState.IsKeyDown(Keys.Up) || input.CurrentKeyboardState.IsKeyDown(Keys.W))
			{
				this.CamPos.Y = this.CamPos.Y - 0.008f * xDist;
			}
			if (MousePos.Y == (float)(pp.BackBufferHeight - 1) || input.CurrentKeyboardState.IsKeyDown(Keys.Down) || input.CurrentKeyboardState.IsKeyDown(Keys.S))
			{
				this.CamPos.Y = this.CamPos.Y + 0.008f * xDist;
			}
		}

		public override void HandleInput(InputState input)
		{
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
			this.current = Mouse.GetState();
			Vector2 MousePos = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			if (this.SelectedNodeList.Count != 1 && this.FleetToEdit != -1)
			{
				if (!HelperFunctions.CheckIntersection(this.FleetNameEntry.ClickableArea, MousePos))
				{
					this.FleetNameEntry.Hover = false;
				}
				else
				{
					this.FleetNameEntry.Hover = true;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.FleetNameEntry.HandlingInput = true;
						return;
					}
				}
			}
			if (!this.FleetNameEntry.HandlingInput)
			{
				GlobalStats.TakingInput = false;
			}
			else
			{
				GlobalStats.TakingInput = true;
				this.FleetNameEntry.HandleTextInput(ref EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].Name);
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.D1) && input.LastKeyboardState.IsKeyUp(Keys.D1))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(1);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D2) && input.LastKeyboardState.IsKeyUp(Keys.D2))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(2);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D3) && input.LastKeyboardState.IsKeyUp(Keys.D3))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(3);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D4) && input.LastKeyboardState.IsKeyUp(Keys.D4))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(4);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D5) && input.LastKeyboardState.IsKeyUp(Keys.D5))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(5);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D6) && input.LastKeyboardState.IsKeyUp(Keys.D6))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(6);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D7) && input.LastKeyboardState.IsKeyUp(Keys.D7))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(7);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D8) && input.LastKeyboardState.IsKeyUp(Keys.D8))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(8);
			}
			else if (input.CurrentKeyboardState.IsKeyDown(Keys.D9) && input.LastKeyboardState.IsKeyUp(Keys.D9))
			{
				AudioManager.PlayCue("echo_affirm");
				this.ChangeFleet(9);
			}
			foreach (KeyValuePair<int, Rectangle> rect in this.FleetsRects)
			{
				if (!HelperFunctions.CheckIntersection(rect.Value, MousePos) || input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
				{
					continue;
				}
				AudioManager.PlayCue("echo_affirm");
				this.FleetToEdit = rect.Key;
				this.ChangeFleet(this.FleetToEdit);
			}
			if (this.FleetToEdit != -1)
			{
				this.sub_ships.HandleInput(this);
				if (this.ShipSL.HandleInput(input))
				{
					return;
				}
			}
			if (this.SelectedNodeList.Count == 1)
			{
				this.SelectedNodeList[0].AttackShieldedWeight = this.Slider_Shield.HandleInput(input);
				this.SelectedNodeList[0].DPSWeight = this.Slider_DPS.HandleInput(input);
				this.SelectedNodeList[0].VultureWeight = this.Slider_Vulture.HandleInput(input);
				this.SelectedNodeList[0].ArmoredWeight = this.Slider_Armor.HandleInput(input);
				this.SelectedNodeList[0].DefenderWeight = this.Slider_Defend.HandleInput(input);
				this.SelectedNodeList[0].AssistWeight = this.Slider_Assist.HandleInput(input);
				this.SelectedNodeList[0].SizeWeight = this.Slider_Size.HandleInput(input);
				if (HelperFunctions.CheckIntersection(this.OperationsRect, MousePos))
				{
					this.dragTimer = 0f;
					return;
				}
				if (HelperFunctions.CheckIntersection(this.PrioritiesRect, MousePos))
				{
					this.dragTimer = 0f;
					this.SelectedNodeList[0].OrdersRadius = this.OperationalRadius.HandleInput(input);
					return;
				}
				if (HelperFunctions.CheckIntersection(this.SelectedStuffRect, MousePos))
				{
					foreach (ToggleButton button in this.OrdersButtons)
					{
						if (!HelperFunctions.CheckIntersection(button.r, MousePos))
						{
							button.Hover = false;
						}
						else
						{
							button.Hover = true;
							if (input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
							{
								continue;
							}
							foreach (ToggleButton b in this.OrdersButtons)
							{
								b.Active = false;
							}
							string action = button.Action;
							string str = action;
							if (action != null)
							{
								if (str == "attack")
								{
									this.SelectedNodeList[0].CombatState = CombatState.AttackRuns;
								}
								else if (str == "arty")
								{
									this.SelectedNodeList[0].CombatState = CombatState.Artillery;
								}
								else if (str == "hold")
								{
									this.SelectedNodeList[0].CombatState = CombatState.HoldPosition;
								}
								else if (str == "orbit_left")
								{
									this.SelectedNodeList[0].CombatState = CombatState.OrbitLeft;
								}
                                else if (str == "broadside_left")
                                {
                                    this.SelectedNodeList[0].CombatState = CombatState.BroadsideLeft;
                                }
                                else if (str == "orbit_right")
                                {
                                    this.SelectedNodeList[0].CombatState = CombatState.OrbitRight;
                                }
                                else if (str == "broadside_right")
                                {
                                    this.SelectedNodeList[0].CombatState = CombatState.BroadsideRight;
                                }
                                else if (str == "evade")
                                {
                                    this.SelectedNodeList[0].CombatState = CombatState.Evade;
                                }
							}
							if (this.SelectedNodeList[0].GetShip() == null)
							{
								continue;
							}
							this.SelectedNodeList[0].GetShip().GetAI().CombatState = this.SelectedNodeList[0].CombatState;
							button.Active = true;
							AudioManager.PlayCue("echo_affirm");
							break;
						}
					}
					return;
				}
			}
			else if (this.SelectedNodeList.Count > 1)
			{
				FleetDataNode fleetDataNode = new FleetDataNode();
				this.Slider_DPS.HandleInput(input);
				this.Slider_Vulture.HandleInput(input);
				this.Slider_Armor.HandleInput(input);
				this.Slider_Defend.HandleInput(input);
				this.Slider_Assist.HandleInput(input);
				this.Slider_Size.HandleInput(input);
				foreach (FleetDataNode node in this.SelectedNodeList)
				{
					node.DPSWeight = this.Slider_DPS.amount;
					node.VultureWeight = this.Slider_Vulture.amount;
					node.ArmoredWeight = this.Slider_Armor.amount;
					node.DefenderWeight = this.Slider_Defend.amount;
					node.AssistWeight = this.Slider_Assist.amount;
					node.SizeWeight = this.Slider_Size.amount;
				}
				if (HelperFunctions.CheckIntersection(this.OperationsRect, MousePos))
				{
					this.dragTimer = 0f;
					return;
				}
				if (HelperFunctions.CheckIntersection(this.PrioritiesRect, MousePos))
				{
					this.dragTimer = 0f;
					this.SelectedNodeList[0].OrdersRadius = this.OperationalRadius.HandleInput(input);
					return;
				}
				if (HelperFunctions.CheckIntersection(this.SelectedStuffRect, MousePos))
				{
					foreach (ToggleButton button in this.OrdersButtons)
					{
						if (!HelperFunctions.CheckIntersection(button.r, MousePos))
						{
							button.Hover = false;
						}
						else
						{
							button.Hover = true;
							if (input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
							{
								continue;
							}
							foreach (ToggleButton b in this.OrdersButtons)
							{
								b.Active = false;
							}
							AudioManager.PlayCue("echo_affirm");
							button.Active = true;
							foreach (FleetDataNode node in this.SelectedNodeList)
							{
								string action1 = button.Action;
								string str1 = action1;
								if (action1 != null)
								{
									if (str1 == "attack")
									{
										node.CombatState = CombatState.AttackRuns;
									}
									else if (str1 == "arty")
									{
										node.CombatState = CombatState.Artillery;
									}
									else if (str1 == "hold")
									{
										node.CombatState = CombatState.HoldPosition;
									}
									else if (str1 == "orbit_left")
									{
										node.CombatState = CombatState.OrbitLeft;
									}
                                    else if (str1 == "broadside_left")
                                    {
                                        node.CombatState = CombatState.BroadsideLeft;
                                    }
                                    else if (str1 == "orbit_right")
                                    {
                                        node.CombatState = CombatState.OrbitRight;
                                    }
                                    else if (str1 == "broadside_right")
                                    {
                                        node.CombatState = CombatState.BroadsideRight;
                                    }
                                    else if (str1 == "evade")
                                    {
                                        node.CombatState = CombatState.Evade;
                                    }
								}
								if (node.GetShip() == null)
								{
									continue;
								}
								node.GetShip().GetAI().CombatState = node.CombatState;
							}
						}
					}
					return;
				}
			}
			else if (this.FleetToEdit != -1 && this.SelectedNodeList.Count == 0 && HelperFunctions.CheckIntersection(this.SelectedStuffRect, MousePos))
			{
				if (this.RequisitionForces.HandleInput(input))
				{
					base.ScreenManager.AddScreen(new RequisitionScreen(this));
				}
				if (this.SaveDesign.HandleInput(input))
				{
					base.ScreenManager.AddScreen(new SaveFleetDesignScreen(this.fleet));
				}
				if (this.LoadDesign.HandleInput(input))
				{
					base.ScreenManager.AddScreen(new LoadSavedFleetDesignScreen(this));
				}
			}
			if (this.ActiveShipDesign != null)
			{
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 nearPoint = viewport.Unproject(new Vector3(MousePos.X, MousePos.Y, 0f), this.projection, this.view, Matrix.Identity);
					Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 farPoint = viewport1.Unproject(new Vector3(MousePos.X, MousePos.Y, 1f), this.projection, this.view, Matrix.Identity);
					Vector3 direction = farPoint - nearPoint;
					direction.Normalize();
					Ray pickRay = new Ray(nearPoint, direction);
					float k = -pickRay.Position.Z / pickRay.Direction.Z;
					Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
					FleetDataNode node = new FleetDataNode()
					{
						FleetOffset = new Vector2(pickedPosition.X, pickedPosition.Y),
						ShipName = this.ActiveShipDesign.Name
					};
					this.fleet.DataNodes.Add(node);
					if (this.AvailableShips.Contains(this.ActiveShipDesign))
					{
						if (this.fleet.Ships.Count == 0)
						{
							this.fleet.Position = this.ActiveShipDesign.Position;
						}
						node.SetShip(this.ActiveShipDesign);
						node.GetShip().GetSO().World = Matrix.CreateTranslation(new Vector3(node.FleetOffset, 0f));
						node.GetShip().RelativeFleetOffset = node.FleetOffset;
						this.AvailableShips.Remove(this.ActiveShipDesign);
						node.GetShip().fleet = this.fleet;
						this.fleet.AddShip(node.GetShip());
						if (this.sub_ships.Tabs[1].Selected)
						{
							ScrollList.Entry toremove = null;
							foreach (ScrollList.Entry e in this.ShipSL.Copied)
							{
								if (!(e.item is Ship) || e.item as Ship != this.ActiveShipDesign)
								{
									continue;
								}
								toremove = e;
								break;
							}
							if (toremove != null)
							{
								foreach (ScrollList.Entry e in this.ShipSL.Copied)
								{
									e.SubEntries.Remove(toremove);
								}
								this.ShipSL.Entries.Remove(toremove);
								this.ShipSL.Copied.Remove(toremove);
								this.ShipSL.Update();
							}
						}
						this.ActiveShipDesign = null;
					}
					if (!input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
					{
						this.ActiveShipDesign = null;
					}
				}
				if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
				{
					this.ActiveShipDesign = null;
				}
			}
			if (this.FleetToEdit != -1)
			{
				foreach (ScrollList.Entry e in this.ShipSL.Copied)
				{
					if (!(e.item is ModuleHeader))
					{
						if (!HelperFunctions.CheckIntersection(e.clickRect, MousePos) || input.CurrentMouseState.LeftButton != ButtonState.Pressed || input.LastMouseState.LeftButton != ButtonState.Released)
						{
							continue;
						}
						this.ActiveShipDesign = e.item as Ship;
						this.SelectedNodeList.Clear();
						this.SelectedSquad = null;
					}
					else
					{
						(e.item as ModuleHeader).HandleInput(input, e);
					}
				}
			}
			this.HandleEdgeDetection(input);
			this.HandleSelectionBox(input);
			if (input.ScrollIn)
			{
				FleetDesignScreen desiredCamHeight = this;
				desiredCamHeight.DesiredCamHeight = desiredCamHeight.DesiredCamHeight - 1500f;
			}
			if (input.ScrollOut)
			{
				FleetDesignScreen fleetDesignScreen = this;
				fleetDesignScreen.DesiredCamHeight = fleetDesignScreen.DesiredCamHeight + 1500f;
			}
			if (this.DesiredCamHeight < 3000f)
			{
				this.DesiredCamHeight = 3000f;
			}
			else if (this.DesiredCamHeight > 100000f)
			{
				this.DesiredCamHeight = 100000f;
			}
			bool dragging = false;
			if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
			{
				dragging = true;
				this.startDrag = MousePos;
			}
			if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Pressed)
			{
				dragging = true;
				this.endDrag = MousePos;
				if (Vector2.Distance(this.startDrag, this.endDrag) > 10f)
				{
					this.CamVelocity = HelperFunctions.FindVectorToTarget(this.endDrag, this.startDrag);
					this.CamVelocity = Vector2.Normalize(this.CamVelocity) * Vector2.Distance(this.startDrag, this.endDrag);
				}
			}
			if (!dragging)
			{
				this.CamVelocity = Vector2.Zero;
			}
			if (this.CamVelocity.Length() > 150f)
			{
				this.CamVelocity = Vector2.Normalize(this.CamVelocity) * 150f;
			}
			if (float.IsNaN(this.CamVelocity.X) || float.IsNaN(this.CamVelocity.Y))
			{
				this.CamVelocity = Vector2.Zero;
			}
			if (input.CurrentKeyboardState.IsKeyDown(Keys.Back) || input.CurrentKeyboardState.IsKeyDown(Keys.Delete))
			{
				if (this.SelectedSquad != null)
				{
					this.fleet.CenterFlank.Remove(this.SelectedSquad);
					this.fleet.LeftFlank.Remove(this.SelectedSquad);
					this.fleet.RearFlank.Remove(this.SelectedSquad);
					this.fleet.RightFlank.Remove(this.SelectedSquad);
					this.fleet.ScreenFlank.Remove(this.SelectedSquad);
					this.SelectedSquad = null;
					this.SelectedNodeList.Clear();
				}
				if (this.SelectedNodeList.Count > 0)
				{
					foreach (List<Fleet.Squad> flanks in this.fleet.AllFlanks)
					{
						foreach (Fleet.Squad squad in flanks)
						{
							foreach (FleetDataNode node in this.SelectedNodeList)
							{
								if (!squad.DataNodes.Contains(node))
								{
									continue;
								}
								squad.DataNodes.QueuePendingRemoval(node);
								if (node.GetShip() == null)
								{
									continue;
								}
								squad.Ships.QueuePendingRemoval(node.GetShip());
							}
							squad.DataNodes.ApplyPendingRemovals();
							squad.Ships.ApplyPendingRemovals();
						}
					}
					foreach (FleetDataNode node in this.SelectedNodeList)
					{
						this.fleet.DataNodes.Remove(node);
						if (node.GetShip() == null)
						{
							continue;
						}
						node.GetShip().GetSO().World = Matrix.CreateTranslation(new Vector3(node.GetShip().RelativeFleetOffset, -500000f));
						this.fleet.Ships.Remove(node.GetShip());
						node.GetShip().fleet = null;
					}
					this.SelectedNodeList.Clear();
					this.PopulateShipSL();
				}
			}
			if (input.Escaped)
			{
				FleetDesignScreen.Open = false;
				this.ExitScreen();
			}
			this.previous = this.current;
		}

		protected void HandleSelectionBox(InputState input)
		{
			if (HelperFunctions.CheckIntersection(this.LeftMenu.Menu, input.CursorPosition) || HelperFunctions.CheckIntersection(this.RightMenu.Menu, input.CursorPosition))
			{
				this.SelectionBox = new Rectangle(0, 0, -1, -1);
				this.StartSelectionBox = false;
				return;
			}
			Vector2 MousePosition = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			this.HoveredNodeList.Clear();
			bool hovering = false;
			foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
			{
				if (Vector2.Distance(input.CursorPosition, squad.screenPos) > 8f)
				{
					continue;
				}
				this.HoveredSquad = squad.squad;
				hovering = true;
				List<FleetDataNode>.Enumerator enumerator = this.HoveredSquad.DataNodes.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						FleetDataNode node = enumerator.Current;
						this.HoveredNodeList.Add(node);
					}
					break;
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
			}
			if (!hovering)
			{
				foreach (FleetDesignScreen.ClickableNode node in this.ClickableNodes)
				{
					if (Vector2.Distance(input.CursorPosition, node.ScreenPos) > node.Radius)
					{
						continue;
					}
					this.HoveredNodeList.Add(node.nodeToClick);
					hovering = true;
				}
			}
			if (!hovering)
			{
				this.HoveredNodeList.Clear();
			}
			bool hitsomething = false;
			if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
			{
				this.SelectedSquad = null;
				foreach (FleetDesignScreen.ClickableNode node in this.ClickableNodes)
				{
					if (Vector2.Distance(input.CursorPosition, node.ScreenPos) > node.Radius)
					{
						continue;
					}
					if (this.SelectedNodeList.Count > 0 && !input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
					{
						this.SelectedNodeList.Clear();
					}
					AudioManager.GetCue("techy_affirm1").Play();
					hitsomething = true;
					if (!this.SelectedNodeList.Contains(node.nodeToClick))
					{
						this.SelectedNodeList.Add(node.nodeToClick);
					}
					foreach (ToggleButton button in this.OrdersButtons)
					{
						button.Active = false;
						CombatState toset = CombatState.Artillery;
						string action = button.Action;
						string str = action;
						if (action != null)
						{
							if (str == "attack")
							{
								toset = CombatState.AttackRuns;
							}
							else if (str == "arty")
							{
								toset = CombatState.Artillery;
							}
							else if (str == "hold")
							{
								toset = CombatState.HoldPosition;
							}
							else if (str == "orbit_left")
							{
								toset = CombatState.OrbitLeft;
							}
                            else if (str == "broadside_left")
                            {
                                toset = CombatState.BroadsideLeft;
                            }
                            else if (str == "orbit_right")
                            {
                                toset = CombatState.OrbitRight;
                            }
                            else if (str == "broadside_right")
                            {
                                toset = CombatState.BroadsideRight;
                            }
                            else if (str == "evade")
                            {
                                toset = CombatState.Evade;
                            }
						}
						if (node.nodeToClick.CombatState != toset)
						{
							continue;
						}
						button.Active = true;
					}
					this.Slider_Armor.SetAmount(node.nodeToClick.ArmoredWeight);
					this.Slider_Assist.SetAmount(node.nodeToClick.AssistWeight);
					this.Slider_Defend.SetAmount(node.nodeToClick.DefenderWeight);
					this.Slider_DPS.SetAmount(node.nodeToClick.DPSWeight);
					this.Slider_Shield.SetAmount(node.nodeToClick.AttackShieldedWeight);
					this.Slider_Vulture.SetAmount(node.nodeToClick.VultureWeight);
					this.OperationalRadius.SetAmount(node.nodeToClick.OrdersRadius);
					this.Slider_Size.SetAmount(node.nodeToClick.SizeWeight);
					break;
				}
				foreach (FleetDesignScreen.ClickableSquad squad in this.ClickableSquads)
				{
					if (Vector2.Distance(input.CursorPosition, squad.screenPos) > 4f)
					{
						continue;
					}
					this.SelectedSquad = squad.squad;
					if (this.SelectedNodeList.Count > 0 && !input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
					{
						this.SelectedNodeList.Clear();
					}
					hitsomething = true;
					AudioManager.GetCue("techy_affirm1").Play();
					this.SelectedNodeList.Clear();
					foreach (FleetDataNode node in this.SelectedSquad.DataNodes)
					{
						this.SelectedNodeList.Add(node);
					}
					this.Slider_Armor.SetAmount(this.SelectedSquad.MasterDataNode.ArmoredWeight);
					this.Slider_Assist.SetAmount(this.SelectedSquad.MasterDataNode.AssistWeight);
					this.Slider_Defend.SetAmount(this.SelectedSquad.MasterDataNode.DefenderWeight);
					this.Slider_DPS.SetAmount(this.SelectedSquad.MasterDataNode.DPSWeight);
					this.Slider_Shield.SetAmount(this.SelectedSquad.MasterDataNode.AttackShieldedWeight);
					this.Slider_Vulture.SetAmount(this.SelectedSquad.MasterDataNode.VultureWeight);
					this.OperationalRadius.SetAmount(this.SelectedSquad.MasterDataNode.OrdersRadius);
					this.Slider_Size.SetAmount(this.SelectedSquad.MasterDataNode.SizeWeight);
					break;
				}
				if (!hitsomething)
				{
					this.SelectedSquad = null;
					this.SelectedNodeList.Clear();
				}
			}
			if (this.SelectedSquad != null)
			{
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragTimer > 0.1f)
				{
					Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 nearPoint = viewport.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 0f), this.projection, this.view, Matrix.Identity);
					Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 farPoint = viewport1.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f), this.projection, this.view, Matrix.Identity);
					Vector3 direction = farPoint - nearPoint;
					direction.Normalize();
					Ray pickRay = new Ray(nearPoint, direction);
					float k = -pickRay.Position.Z / pickRay.Direction.Z;
					Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
					Vector2 newspot = new Vector2((float)((int)pickedPosition.X), (float)((int)pickedPosition.Y));
					Vector2 difference = newspot - this.SelectedSquad.Offset;
					if (difference.Length() > 30f)
					{
						Fleet.Squad selectedSquad = this.SelectedSquad;
						selectedSquad.Offset = selectedSquad.Offset + difference;
						foreach (FleetDataNode node in this.SelectedSquad.DataNodes)
						{
							FleetDataNode fleetOffset = node;
							fleetOffset.FleetOffset = fleetOffset.FleetOffset + difference;
							if (node.GetShip() == null)
							{
								continue;
							}
							Ship ship = node.GetShip();
							ship.RelativeFleetOffset = ship.RelativeFleetOffset + difference;
						}
					}
				}
			}
			else if (this.SelectedNodeList.Count != 1)
			{
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
				{
					this.SelectionBox = new Rectangle(input.CurrentMouseState.X, input.CurrentMouseState.Y, 0, 0);
					this.StartSelectionBox = true;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && this.StartSelectionBox)
				{
					this.SelectionBox = new Rectangle(this.SelectionBox.X, this.SelectionBox.Y, input.CurrentMouseState.X - this.SelectionBox.X, input.CurrentMouseState.Y - this.SelectionBox.Y);
					return;
				}
				if (input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) && input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed && this.StartSelectionBox)
				{
					if (input.CurrentMouseState.X < this.SelectionBox.X)
					{
						this.SelectionBox.X = input.CurrentMouseState.X;
					}
					if (input.CurrentMouseState.Y < this.SelectionBox.Y)
					{
						this.SelectionBox.Y = input.CurrentMouseState.Y;
					}
					this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
					this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
					foreach (FleetDesignScreen.ClickableNode node in this.ClickableNodes)
					{
						if (!this.SelectionBox.Contains(new Point((int)node.ScreenPos.X, (int)node.ScreenPos.Y)))
						{
							continue;
						}
						this.SelectedNodeList.Add(node.nodeToClick);
					}
					this.SelectionBox = new Rectangle(0, 0, -1, -1);
					return;
				}
				if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
				{
					if (input.CurrentMouseState.X < this.SelectionBox.X)
					{
						this.SelectionBox.X = input.CurrentMouseState.X;
					}
					if (input.CurrentMouseState.Y < this.SelectionBox.Y)
					{
						this.SelectionBox.Y = input.CurrentMouseState.Y;
					}
					this.SelectionBox.Width = Math.Abs(this.SelectionBox.Width);
					this.SelectionBox.Height = Math.Abs(this.SelectionBox.Height);
					foreach (FleetDesignScreen.ClickableNode node in this.ClickableNodes)
					{
						if (!this.SelectionBox.Contains(new Point((int)node.ScreenPos.X, (int)node.ScreenPos.Y)))
						{
							continue;
						}
						this.SelectedNodeList.Add(node.nodeToClick);
					}
					this.SelectionBox = new Rectangle(0, 0, -1, -1);
				}
			}
			else if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragTimer > 0.1f)
			{
				Viewport viewport2 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 nearPoint = viewport2.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 0f), this.projection, this.view, Matrix.Identity);
				Viewport viewport3 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 farPoint = viewport3.Unproject(new Vector3(MousePosition.X, MousePosition.Y, 1f), this.projection, this.view, Matrix.Identity);
				Vector3 direction = farPoint - nearPoint;
				direction.Normalize();
				Ray pickRay = new Ray(nearPoint, direction);
				float k = -pickRay.Position.Z / pickRay.Direction.Z;
				Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
				Vector2 newspot = new Vector2((float)((int)pickedPosition.X), (float)((int)pickedPosition.Y));
				if (Vector2.Distance(newspot, this.SelectedNodeList[0].FleetOffset) > 1000f)
				{
					return;
				}
				Vector2 difference = newspot - this.SelectedNodeList[0].FleetOffset;
				if (difference.Length() > 30f)
				{
					FleetDataNode item = this.SelectedNodeList[0];
					item.FleetOffset = item.FleetOffset + difference;
					if (this.SelectedNodeList[0].GetShip() != null)
					{
						this.SelectedNodeList[0].GetShip().RelativeFleetOffset = this.SelectedNodeList[0].FleetOffset;
					}
				}
				foreach (FleetDesignScreen.ClickableSquad cs in this.ClickableSquads)
				{
					if (Vector2.Distance(cs.screenPos, MousePosition) >= 5f || cs.squad.DataNodes.Contains(this.SelectedNodeList[0]))
					{
						continue;
					}
					foreach (List<Fleet.Squad> flank in this.fleet.AllFlanks)
					{
						foreach (Fleet.Squad squad in flank)
						{
							squad.DataNodes.Remove(this.SelectedNodeList[0]);
							if (this.SelectedNodeList[0].GetShip() == null)
							{
								continue;
							}
							squad.Ships.Remove(this.SelectedNodeList[0].GetShip());
						}
					}
					cs.squad.DataNodes.Add(this.SelectedNodeList[0]);
					if (this.SelectedNodeList[0].GetShip() == null)
					{
						continue;
					}
					cs.squad.Ships.Add(this.SelectedNodeList[0].GetShip());
				}
			}
		}

		public override void LoadContent()
		{
			this.close = new CloseButton(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 38, 40, 20, 20));
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/ShipyardLightrig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
			}
			this.starfield = new Starfield(Vector2.Zero, base.ScreenManager.GraphicsDevice, base.ScreenManager.Content);
			this.starfield.LoadContent();
			Rectangle titleRect = new Rectangle(2, 44, 250, 80);
			this.TitleBar = new Menu2(base.ScreenManager, titleRect);
			this.TitlePos = new Vector2((float)(titleRect.X + titleRect.Width / 2) - Fonts.Laserian14.MeasureString("Fleet Hotkeys").X / 2f, (float)(titleRect.Y + titleRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, titleRect.Width, 500);
			this.LeftMenu = new Menu1(base.ScreenManager, leftRect, true);
			this.FleetSL = new ScrollList(this.LeftMenu.subMenu, 40);
			int i = 0;
			foreach (KeyValuePair<int, Ship_Game.Gameplay.Fleet> Fleet in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict())
			{
				this.FleetsRects.Add(Fleet.Key, new Rectangle(leftRect.X + 2, leftRect.Y + i * 53, 52, 48));
				i++;
			}
			Rectangle shipRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 282, 44, 280, 80);
			this.ShipDesigns = new Menu2(base.ScreenManager, shipRect);
			this.ShipDesignsTitlePos = new Vector2((float)(shipRect.X + shipRect.Width / 2) - Fonts.Laserian14.MeasureString("Ship Designs").X / 2f, (float)(shipRect.Y + shipRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2));
			Rectangle shipDesignsRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - shipRect.Width - 2, shipRect.Y + shipRect.Height + 5, shipRect.Width, 500);
			this.RightMenu = new Menu1(base.ScreenManager, shipDesignsRect);
			this.sub_ships = new Submenu(base.ScreenManager, shipDesignsRect);
			this.ShipSL = new ScrollList(this.sub_ships, 40);
			this.sub_ships.AddTab("Designs");
			this.sub_ships.AddTab("Owned");
			foreach (Ship ship in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetShips())
			{
				if (ship.fleet != null || !ship.Active)
				{
					continue;
				}
				this.AvailableShips.Add(ship);
			}
			this.PopulateShipSL();
			this.SelectedStuffRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 220, -13 + base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 200, 440, 210);
			Vector2 OrdersBarPos = new Vector2((float)(this.SelectedStuffRect.X + 20), (float)(this.SelectedStuffRect.Y + 65));
			ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon")
			{
				Action = "attack",
				HasToolTip = true,
				WhichToolTip = 1
			};
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Artillery = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_aft")
			{
				Action = "arty",
				HasToolTip = true,
				WhichToolTip = 2
			};
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton HoldPos = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_x")
			{
				Action = "hold",
				HasToolTip = true,
				WhichToolTip = 65
			};
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_left")
			{
				Action = "orbit_left",
				HasToolTip = true,
				WhichToolTip = 3
			};
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;
            ToggleButton BroadsideLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bleft")
            {
                Action = "broadside_left",
                HasToolTip = true,
                WhichToolTip = 159
            };
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_right")
			{
				Action = "orbit_right",
				HasToolTip = true,
				WhichToolTip = 4
			};
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;
            ToggleButton BroadsideRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bright")
            {
                Action = "broadside_right",
                HasToolTip = true,
                WhichToolTip = 160
            };
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
            OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Evade = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_stop")
			{
				Action = "evade",
				HasToolTip = true,
				WhichToolTip = 6
			};
			this.OrdersButtons.Add(Artillery);
			this.OrdersButtons.Add(HoldPos);
			this.OrdersButtons.Add(OrbitLeft);
            this.OrdersButtons.Add(BroadsideLeft);
			this.OrdersButtons.Add(OrbitRight);
            this.OrdersButtons.Add(BroadsideRight);
			this.OrdersButtons.Add(Evade);
			this.OrdersButtons.Add(AttackRuns);
			this.RequisitionForces = new BlueButton(new Vector2((float)(this.SelectedStuffRect.X + 240), (float)(this.SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20)), "Requisition...");
			this.SaveDesign = new BlueButton(new Vector2((float)(this.SelectedStuffRect.X + 240), (float)(this.SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 50)), "Save Design...");
			this.LoadDesign = new BlueButton(new Vector2((float)(this.SelectedStuffRect.X + 240), (float)(this.SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 100)), "Load Design...");
			this.RequisitionForces.ToggleOn = true;
			this.SaveDesign.ToggleOn = true;
			this.LoadDesign.ToggleOn = true;
			this.OperationsRect = new Rectangle(this.SelectedStuffRect.X + this.SelectedStuffRect.Width + 2, this.SelectedStuffRect.Y + 30, 360, this.SelectedStuffRect.Height - 30);
			Rectangle AssistRect = new Rectangle(this.OperationsRect.X + 15, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
			this.Slider_Assist = new WeightSlider(AssistRect, "Assist Nearby Weight")
			{
				Tip_ID = 7
			};
			Rectangle DefenderRect = new Rectangle(this.OperationsRect.X + 15, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
			this.Slider_Defend = new WeightSlider(DefenderRect, "Defend Nearby Weight")
			{
				Tip_ID = 8
			};
			Rectangle VultureRect = new Rectangle(this.OperationsRect.X + 15, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
			this.Slider_Vulture = new WeightSlider(VultureRect, "Target Damaged Weight")
			{
				Tip_ID = 9
			};
			Rectangle ArmoredRect = new Rectangle(this.OperationsRect.X + 15 + 180, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
			this.Slider_Armor = new WeightSlider(ArmoredRect, "Target Armored Weight")
			{
				Tip_ID = 10
			};
			Rectangle ShieldedRect = new Rectangle(this.OperationsRect.X + 15 + 180, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
			this.Slider_Shield = new WeightSlider(ShieldedRect, "Target Shielded Weight")
			{
				Tip_ID = 11
			};
			Rectangle DPSRect = new Rectangle(this.OperationsRect.X + 15 + 180, this.OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
			this.Slider_DPS = new WeightSlider(DPSRect, "Target DPS Weight")
			{
				Tip_ID = 12
			};
			this.PrioritiesRect = new Rectangle(this.SelectedStuffRect.X - this.OperationsRect.Width - 2, this.OperationsRect.Y, this.OperationsRect.Width, this.OperationsRect.Height);
			Rectangle oprect = new Rectangle(this.PrioritiesRect.X + 15, this.PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 300, 40);
			this.OperationalRadius = new FloatSlider(oprect, "Operational Radius");
			this.OperationalRadius.SetAmount(0.2f);
			this.OperationalRadius.Tip_ID = 13;
			Rectangle sizerect = new Rectangle(this.PrioritiesRect.X + 15, this.PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 300, 40);
			this.Slider_Size = new SizeSlider(sizerect, "Target Size Preference");
			this.Slider_Size.SetAmount(0.5f);
			this.Slider_Size.Tip_ID = 14;
			this.starfield = new Starfield(Vector2.Zero, base.ScreenManager.GraphicsDevice, base.ScreenManager.Content);
			this.starfield.LoadContent();
			this.bg = new Background();
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 100f, 15000f);
			foreach (Ship ship in this.fleet.Ships)
			{
				ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, 0f));
			}
			base.LoadContent();
		}

		public void LoadData(FleetDesign data)
		{
			foreach (Ship ship in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetFleetsDict()[this.FleetToEdit].Ships)
			{
				ship.GetSO().World = Matrix.CreateTranslation(new Vector3(ship.RelativeFleetOffset, -1000000f));
				ship.fleet = null;
			}
			this.fleet.DataNodes.Clear();
			this.fleet.Ships.Clear();
			foreach (List<Fleet.Squad> Flank in this.fleet.AllFlanks)
			{
				Flank.Clear();
			}
			this.fleet.Name = data.Name;
			foreach (FleetDataNode node in data.Data)
			{
				this.fleet.DataNodes.Add(node);
			}
			this.fleet.FleetIconIndex = data.FleetIconIndex;
		}

		public void PopulateShipSL()
		{
			this.AvailableShips.Clear();
			foreach (Ship ship in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetShips())
			{
				if (ship.fleet != null)
				{
					continue;
				}
				this.AvailableShips.Add(ship);
			}
			this.ShipSL.Entries.Clear();
			this.ShipSL.indexAtTop = 0;
			if (this.sub_ships.Tabs[0].Selected)
			{
				List<string> Roles = new List<string>();
				foreach (string shipname in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).ShipsWeCanBuild)
				{
					if (Roles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].Role))
					{
						continue;
					}
					Roles.Add(Ship_Game.ResourceManager.ShipsDict[shipname].Role);
					ModuleHeader mh = new ModuleHeader(Ship_Game.ResourceManager.ShipsDict[shipname].Role, 295f);
					this.ShipSL.AddItem(mh);
				}
				foreach (ScrollList.Entry e in this.ShipSL.Entries)
				{
					foreach (string shipname in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).ShipsWeCanBuild)
					{
						Ship ship = Ship_Game.ResourceManager.ShipsDict[shipname];
						if (ship.Role != (e.item as ModuleHeader).Text)
						{
							continue;
						}
						e.AddItem(ship);
					}
				}
			}
			else if (this.sub_ships.Tabs[1].Selected)
			{
				List<string> Roles = new List<string>();
				foreach (Ship ship in this.AvailableShips)
				{
					if (Roles.Contains(ship.Role) || ship.Role == "Troop")
					{
						continue;
					}
					Roles.Add(ship.Role);
					ModuleHeader mh = new ModuleHeader(ship.Role, 295f);
					this.ShipSL.AddItem(mh);
				}
				foreach (ScrollList.Entry e in this.ShipSL.Entries)
				{
					foreach (Ship ship in this.AvailableShips)
					{
						if (ship.Role == "troop" || !(ship.Role == (e.item as ModuleHeader).Text))
						{
							continue;
						}
						e.AddItem(ship);
					}
				}
			}
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (this.current.LeftButton == ButtonState.Pressed && this.previous.LeftButton == ButtonState.Released)
			{
				FleetDesignScreen fleetDesignScreen = this;
				fleetDesignScreen.dragTimer = fleetDesignScreen.dragTimer + elapsedTime;
			}
			else if (this.current.LeftButton != ButtonState.Pressed || this.previous.LeftButton != ButtonState.Pressed)
			{
				this.dragTimer = 0f;
			}
			else
			{
				FleetDesignScreen fleetDesignScreen1 = this;
				fleetDesignScreen1.dragTimer = fleetDesignScreen1.dragTimer + elapsedTime;
			}
			this.AdjustCamera();
			this.CamPos.X = this.CamPos.X + this.CamVelocity.X;
			this.CamPos.Y = this.CamPos.Y + this.CamVelocity.Y;
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(new Vector3(-this.CamPos.X, this.CamPos.Y, this.CamPos.Z), new Vector3(-this.CamPos.X, this.CamPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.ClickableSquads.Clear();
			foreach (List<Fleet.Squad> flank in this.fleet.AllFlanks)
			{
				foreach (Fleet.Squad squad in flank)
				{
					Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
					Vector3 pScreenSpace = viewport.Project(new Vector3(squad.Offset, 0f), this.projection, this.view, Matrix.Identity);
					Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					FleetDesignScreen.ClickableSquad cs = new FleetDesignScreen.ClickableSquad()
					{
						screenPos = pPos,
						squad = squad
					};
					this.ClickableSquads.Add(cs);
				}
			}
			Vector2 p = HelperFunctions.findPointFromAngleAndDistanceUsingRadians(this.fleet.Position, this.fleet.facing, 1f);
			Vector2 fvec = HelperFunctions.FindVectorToTarget(this.fleet.Position, p);
			this.fleet.AssembleFleet(this.fleet.facing, fvec);
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public struct ClickableNode
		{
			public Vector2 ScreenPos;

			public float Radius;

			public FleetDataNode nodeToClick;
		}

		private struct ClickableSquad
		{
			public Fleet.Squad squad;

			public Vector2 screenPos;
		}
	}
}