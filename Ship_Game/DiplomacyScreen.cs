using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ship_Game
{
	public class DiplomacyScreen : GameScreen, IDisposable
	{
		private Empire them;

		private Empire playerEmpire;

		private string whichDialogue;

		private bool WarDeclared;

		public GenericButton SendOffer;

		private Rectangle Portrait;

		private Vector2 TextCursor;

		private SolarSystem sysToDiscuss;

		private Planet pToDiscuss;

		private Cue music;

		private Video video;

		private VideoPlayer player;

		private Texture2D videoTexture;

		private DiplomacyScreen.DialogState dState;

		private Menu2 Player;

		private List<GenericButton> Buttons = new List<GenericButton>();

		private GenericButton DeclareWar;

		private GenericButton Negotiate;

		private GenericButton Discuss;

		private GenericButton Exit;

		private Rectangle DialogRect;

		private Rectangle UsRect;

		private Rectangle ThemRect;

		private Rectangle BigTradeRect;

		private ScrollList StatementsSL;

		private ScrollList OurItemsSL;

		private ScrollList TheirItemsSL;

		private GenericButton Accept;

		private GenericButton Reject;

		private GenericButton Trust;

		private GenericButton Anger;

		private GenericButton Fear;

		private List<GenericButton> TAFButtons = new List<GenericButton>();

		private Rectangle Attitude_Pleading_Rect;

		private Rectangle Attitude_Respectful_Rect;

		private Rectangle Attitude_Threaten_Rect;

		private GenericButton ap;

		private GenericButton ar;

		private GenericButton at;

		private Vector2 EmpireNamePos;

		private Rectangle R;

		private Rectangle BridgeRect;

		private ScrollList OfferTextSL;

		private Rectangle Negotiate_Right;

		private Rectangle Negotiate_Left;

		private Rectangle ToneContainerRect;

		private Rectangle AccRejRect;

		private Rectangle TrustRect;

		private Rectangle AngerRect;

		private Rectangle FearRect;

		private Offer.Attitude Attitude = Offer.Attitude.Respectful;

		public Offer OurOffer = new Offer();

		public Offer TheirOffer = new Offer();

		public Empire empToDiscuss;

		//private int cNum;

		private string TheirText;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;


		public DiplomacyScreen(Empire e, Empire us, string which)
		{
			float TheirOpinionOfUs;
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.them = e;
			this.playerEmpire = us;
			this.whichDialogue = which;
			base.IsPopup = true;
			string str = which;
			string str1 = str;
			if (str != null)
			{
				switch (str1)
				{
					case "Conquered_Player":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Declare War Imperialism":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War Imperialism Break NA":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War Defense":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War Defense BrokenNA":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Compliment Military":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Compliment Military Better":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Insult Military":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Declare War BC":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					default:
					{
						TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
						if (TheirOpinionOfUs < 0f)
						{
							TheirOpinionOfUs = 0f;
						}
						this.TheirText = this.GetDialogue(TheirOpinionOfUs);
						base.TransitionOnTime = TimeSpan.FromSeconds(1);
						return;
					}
				}
			}
			else
			{
				TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
				if (TheirOpinionOfUs < 0f)
				{
					TheirOpinionOfUs = 0f;
				}
				this.TheirText = this.GetDialogue(TheirOpinionOfUs);
				base.TransitionOnTime = TimeSpan.FromSeconds(1);
				return;
			}
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

		public DiplomacyScreen(Empire e, Empire us, string which, bool EndOnly)
		{
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.them = e;
			this.playerEmpire = us;
			this.whichDialogue = which;
			base.IsPopup = true;
			this.TheirText = this.GetDialogueByName(which);
			this.dState = DiplomacyScreen.DialogState.End;
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

		public DiplomacyScreen(Empire e, Empire us, string which, Offer OurOffer, Offer TheirOffer)
		{
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.them = e;
			this.playerEmpire = us;
			this.whichDialogue = which;
			base.IsPopup = true;
			this.dState = DiplomacyScreen.DialogState.TheirOffer;
			this.TheirText = this.GetDialogueByName(which);
			this.OurOffer = OurOffer;
			this.TheirOffer = TheirOffer;
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

		public DiplomacyScreen(Empire e, Empire us, string which, Offer OurOffer, Offer TheirOffer, Empire taremp)
		{
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.them = e;
			this.playerEmpire = us;
			this.empToDiscuss = taremp;
			this.whichDialogue = which;
			base.IsPopup = true;
			this.dState = DiplomacyScreen.DialogState.TheirOffer;
			this.TheirText = this.GetDialogueByName(which);
			this.OurOffer = OurOffer;
			this.TheirOffer = TheirOffer;
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

		public DiplomacyScreen(Empire e, Empire us, string which, Planet p)
		{
			float TheirOpinionOfUs;
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.pToDiscuss = p;
			this.sysToDiscuss = p.system;
			this.them = e;
			this.playerEmpire = us;
			this.whichDialogue = which;
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			string str = which;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Declare War Defense")
				{
					this.TheirText = this.GetDialogueByName(which);
					this.dState = DiplomacyScreen.DialogState.End;
					this.WarDeclared = true;
					base.TransitionOnTime = TimeSpan.FromSeconds(1);
					return;
				}
				else if (str1 == "Declare War BC")
				{
					this.TheirText = this.GetDialogueByName(which);
					this.dState = DiplomacyScreen.DialogState.End;
					this.WarDeclared = true;
					base.TransitionOnTime = TimeSpan.FromSeconds(1);
					return;
				}
				else
				{
					if (str1 != "Declare War BC TarSys")
					{
						TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
						if (TheirOpinionOfUs < 0f)
						{
							TheirOpinionOfUs = 0f;
						}
						this.TheirText = this.GetDialogue(TheirOpinionOfUs);
						base.TransitionOnTime = TimeSpan.FromSeconds(1);
						return;
					}
					this.TheirText = this.GetDialogueByName(which);
					this.dState = DiplomacyScreen.DialogState.End;
					this.WarDeclared = true;
					base.TransitionOnTime = TimeSpan.FromSeconds(1);
					return;
				}
			}
			TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
			if (TheirOpinionOfUs < 0f)
			{
				TheirOpinionOfUs = 0f;
			}
			this.TheirText = this.GetDialogue(TheirOpinionOfUs);
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

		public DiplomacyScreen(Empire e, Empire us, string which, SolarSystem s)
		{
			float TheirOpinionOfUs;
			e.GetRelations()[us].turnsSinceLastContact = 0;
			this.sysToDiscuss = s;
			this.them = e;
			this.playerEmpire = us;
			this.whichDialogue = which;
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			string str = which;
			string str1 = str;
			if (str != null)
			{
				switch (str1)
				{
					case "Invaded NA Pact":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Invaded Start War":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War Defense":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War BC":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Declare War BC TarSys":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						this.WarDeclared = true;
						break;
					}
					case "Stole Claim":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Stole Claim 2":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					case "Stole Claim 3":
					{
						this.TheirText = this.GetDialogueByName(which);
						this.dState = DiplomacyScreen.DialogState.End;
						break;
					}
					default:
					{
						TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
						if (TheirOpinionOfUs < 0f)
						{
							TheirOpinionOfUs = 0f;
						}
						this.TheirText = this.GetDialogue(TheirOpinionOfUs);
						base.TransitionOnTime = TimeSpan.FromSeconds(1);
						return;
					}
				}
			}
			else
			{
				TheirOpinionOfUs = this.them.GetRelations()[this.playerEmpire].GetStrength();
				if (TheirOpinionOfUs < 0f)
				{
					TheirOpinionOfUs = 0f;
				}
				this.TheirText = this.GetDialogue(TheirOpinionOfUs);
				base.TransitionOnTime = TimeSpan.FromSeconds(1);
				return;
			}
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.OurItemsSL != null)
                        this.OurItemsSL.Dispose();
                    if (this.TheirItemsSL != null)
                        this.TheirItemsSL.Dispose();
                    if (this.StatementsSL != null)
                        this.StatementsSL.Dispose();
                    if (this.OfferTextSL != null)
                        this.OfferTextSL.Dispose();

                }
                this.OurItemsSL = null;
                this.TheirItemsSL = null;
                this.StatementsSL = null;
                this.OfferTextSL = null;
                this.disposed = true;
            }
		}

		private void DoNegotiationResponse(string answer)
		{
			this.StatementsSL.Reset();
			this.TheirText = "";
			if (this.TheirOffer.NAPact && this.them.GetRelations()[this.playerEmpire].HaveRejected_NAPACT)
			{
				this.TheirText = string.Concat(this.GetDialogueByName("ComeAround_NAPACT"), "\n\n");
			}
			else if (this.TheirOffer.TradeTreaty && this.them.GetRelations()[this.playerEmpire].HaveRejected_TRADE)
			{
				this.TheirText = string.Concat(this.GetDialogueByName("ComeAround_TRADE"), "\n\n");
			}
			DiplomacyScreen diplomacyScreen = this;
			diplomacyScreen.TheirText = string.Concat(diplomacyScreen.TheirText, this.GetDialogueByName(answer));
			this.dState = DiplomacyScreen.DialogState.Them;
		}

		public override void Draw(GameTime gameTime)
		{
            
            string text;
			Vector2 Position;
			Vector2 drawCurs;
			if (!base.IsActive)
			{
				return;
			}
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 4 / 5);
			base.ScreenManager.SpriteBatch.Begin();
			if (!string.IsNullOrEmpty(this.them.data.Traits.VideoPath))
			{
				if (this.player.State != MediaState.Stopped)
				{
					this.videoTexture = this.player.GetTexture();
				}
				if (this.videoTexture != null)
				{
					base.ScreenManager.SpriteBatch.Draw(this.videoTexture, this.Portrait, Color.White);
				}
			}
			else
			{
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Portraits/", this.them.PortraitName)], this.Portrait, Color.White);
			}
			HelperFunctions.DrawDropShadowText(base.ScreenManager, this.them.data.Traits.Name, this.EmpireNamePos, Fonts.Pirulen20);
			if (this.dState == DiplomacyScreen.DialogState.Negotiate)
			{
				Rectangle stripe = new Rectangle(0, this.R.Y, 1920, this.R.Height);
				Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, stripe, new Color(0, 0, 0, 150));
			}
			else
			{
				Rectangle stripe = new Rectangle(0, this.DialogRect.Y, 1920, this.R.Height);
				Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, stripe, new Color(0, 0, 0, 150));
			}
			base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Bridge"], this.BridgeRect, Color.White);
			foreach (GenericButton taf in this.TAFButtons)
			{
				taf.DrawWithShadowCaps(base.ScreenManager);
				this.TrustRect.Width = (int)this.them.GetRelations()[this.playerEmpire].Trust;
				if (this.TrustRect.Width < 1)
				{
					this.TrustRect.Width = 1;
				}
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], this.TrustRect, Color.Green);
				this.AngerRect.Width = (int)this.them.GetRelations()[this.playerEmpire].TotalAnger;
				if (this.AngerRect.Width > 100)
				{
					this.AngerRect.Width = 100;
				}
				if (this.AngerRect.Width < 1)
				{
					this.AngerRect.Width = 1;
				}
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], this.AngerRect, Color.Yellow);
				this.FearRect.Width = (int)this.them.GetRelations()[this.playerEmpire].Threat;
				if (this.FearRect.Width < 1)
				{
					this.FearRect.Width = 1;
				}
				base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/bw_bargradient_2"], this.FearRect, Color.Red);
			}
			switch (this.dState)
			{
				case DiplomacyScreen.DialogState.Them:
				{
					Selector selector = new Selector(base.ScreenManager, this.DialogRect, new Color(0, 0, 0, 220));
					text = this.parseText(this.TheirText, (float)(this.DialogRect.Width - 25), Fonts.Consolas18);
					Position = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, this.TextCursor.Y);
					HelperFunctions.ClampVectorToInt(ref Position);
					this.DrawDropShadowText(text, Position, Fonts.Consolas18);
					goto case DiplomacyScreen.DialogState.Choosing;
				}
				case DiplomacyScreen.DialogState.Choosing:
				{
					if (this.dState == DiplomacyScreen.DialogState.End || this.dState == DiplomacyScreen.DialogState.TheirOffer)
					{
						this.Exit.DrawWithShadowCaps(base.ScreenManager);
					}
					else
					{
						int numEntries = 4;
						int k = 4;
						foreach (GenericButton b in this.Buttons)
						{
							Rectangle r = b.R;
							float transitionOffset = MathHelper.Clamp((base.TransitionPosition - 0.5f * (float)k / (float)numEntries) / 0.5f, 0f, 1f);
							k--;
							if (base.ScreenState != Ship_Game.ScreenState.TransitionOn)
							{
								r.X = r.X + (int)transitionOffset * 512;
							}
							else
							{
								r.X = r.X + (int)(transitionOffset * 512f);
							}
							b.TransitionCaps(r);
							b.DrawWithShadowCaps(base.ScreenManager);
						}
					}
                    Vector2 pos = new Vector2((float)(this.Portrait.X + 200), (float)(this.Portrait.Y + 200));
					//{ no idea how this managed to compile in the first place
						pos.Y = pos.Y + (float)(Fonts.Pirulen16.LineSpacing + 15);
                        pos.X = pos.X - 8f;
					//};*/
                   
					pos.Y = pos.Y + (float)(Fonts.Pirulen16.LineSpacing + 15);
					pos.X = pos.X - 8f;
					pos.Y = pos.Y + (float)(Fonts.Pirulen16.LineSpacing + 15);
					pos.X = pos.X - 8f;
					ToolTip.Draw(base.ScreenManager);
					base.ScreenManager.SpriteBatch.End();
					return;
				}
				case DiplomacyScreen.DialogState.Discuss:
				{
					Selector selector1 = new Selector(base.ScreenManager, this.DialogRect, new Color(0, 0, 0, 220));
					this.StatementsSL.Draw(base.ScreenManager.SpriteBatch);
					drawCurs = this.TextCursor;
					int i = this.StatementsSL.indexAtTop;
					while (i < this.StatementsSL.Entries.Count)
					{
						if (i < this.StatementsSL.indexAtTop + this.StatementsSL.entriesToDisplay)
						{
							ScrollList.Entry e = this.StatementsSL.Entries[i];
							if (e.clickRectHover == 0)
							{
								(e.item as DialogOption).Update(drawCurs);
								(e.item as DialogOption).Draw(base.ScreenManager, Fonts.Consolas18);
								drawCurs.Y = drawCurs.Y + (float)(Fonts.Consolas18.LineSpacing + 5);
							}
							i++;
						}
						else
						{
							goto case DiplomacyScreen.DialogState.Choosing;
						}
					}
					goto case DiplomacyScreen.DialogState.Choosing;
				}
				case DiplomacyScreen.DialogState.Negotiate:
				{
					drawCurs = new Vector2((float)(this.R.X + 15), (float)(this.R.Y + 10));
					this.TheirOffer.Them = this.them;
					string txt = this.OurOffer.FormulateOfferText(this.Attitude, this.TheirOffer);
					this.OfferTextSL.Entries.Clear();
					HelperFunctions.parseTextToSL(txt, (float)(this.DialogRect.Width - 30), Fonts.Consolas18, ref this.OfferTextSL);
					for (int i = this.OfferTextSL.indexAtTop; i < this.OfferTextSL.Entries.Count && i < this.OfferTextSL.indexAtTop + this.OfferTextSL.entriesToDisplay; i++)
					{
						ScrollList.Entry e = this.OfferTextSL.Entries[i];
						drawCurs.Y = (float)(e.clickRect.Y - 33);
						this.DrawDropShadowText(e.item as string, drawCurs, Fonts.Consolas18);
					}
					if (!this.TheirOffer.IsBlank() || !this.OurOffer.IsBlank() || this.OurOffer.Alliance)
					{
						this.SendOffer.DrawWithShadow(base.ScreenManager);
					}
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Right"], this.Negotiate_Right, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Left"], this.Negotiate_Left, Color.White);
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["GameScreens/Negotiate_Tone"], this.ToneContainerRect, Color.White);
					this.DrawOurItems();
					this.DrawTheirItems();
					this.OfferTextSL.Draw(base.ScreenManager.SpriteBatch);
					this.ap.Transition(this.Attitude_Pleading_Rect);
					this.ap.Draw(base.ScreenManager);
					this.at.Transition(this.Attitude_Threaten_Rect);
					this.at.Draw(base.ScreenManager);
					this.ar.Transition(this.Attitude_Respectful_Rect);
					this.ar.Draw(base.ScreenManager);
					drawCurs = new Vector2((float)(this.UsRect.X + 10), (float)(this.UsRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 9));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Localizer.Token(1221), drawCurs, Color.White);
					drawCurs = new Vector2((float)(this.ThemRect.X + 10), (float)(this.ThemRect.Y - Fonts.Pirulen12.LineSpacing * 2 + 9));
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, Localizer.Token(1222), drawCurs, Color.White);
					goto case DiplomacyScreen.DialogState.Choosing;
				}
				case DiplomacyScreen.DialogState.TheirOffer:
				{
					base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["UI/AcceptReject"], this.AccRejRect, Color.White);
					text = this.parseText(this.TheirText, (float)(this.DialogRect.Width - 20), Fonts.Consolas18);
					Position = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, this.TextCursor.Y);
					this.DrawDropShadowText(text, Position, Fonts.Consolas18);
					this.Accept.DrawWithShadow(base.ScreenManager);
					this.Reject.DrawWithShadow(base.ScreenManager);
					goto case DiplomacyScreen.DialogState.Choosing;
				}
				case DiplomacyScreen.DialogState.End:
				{
					Selector selector2 = new Selector(base.ScreenManager, this.DialogRect, new Color(0, 0, 0, 220));
					text = this.parseText(this.TheirText, (float)(this.DialogRect.Width - 20), Fonts.Consolas18);
					Position = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Consolas18.MeasureString(text).X / 2f, this.TextCursor.Y);
					HelperFunctions.ClampVectorToInt(ref Position);
					this.DrawDropShadowText(text, Position, Fonts.Consolas18);
					goto case DiplomacyScreen.DialogState.Choosing;
				}
				default:
				{
					goto case DiplomacyScreen.DialogState.Choosing;
				}
			}
		}

		private void DrawDropShadowText(string Text, Vector2 Pos, SpriteFont Font)
		{
			Vector2 offset = new Vector2(2f, 2f);
			base.ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
			base.ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, Color.White);
		}

		private void DrawOurItems()
		{
			this.OurItemsSL.Draw(base.ScreenManager.SpriteBatch);
			Vector2 drawCurs = new Vector2((float)(this.UsRect.X + 10), (float)(this.UsRect.Y + Fonts.Pirulen12.LineSpacing + 10));
			for (int i = this.OurItemsSL.indexAtTop; i < this.OurItemsSL.Copied.Count && i < this.OurItemsSL.indexAtTop + this.OurItemsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.OurItemsSL.Copied[i];
				if (e.clickRectHover == 0)
				{
					(e.item as ItemToOffer).Update(drawCurs);
					(e.item as ItemToOffer).Draw(base.ScreenManager.SpriteBatch, Fonts.Arial12Bold);
					drawCurs.Y = drawCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
			}
		}

		private void DrawSpecialText1612(string Text, Vector2 Pos)
		{
			Vector2 offset = new Vector2(2f, 2f);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos + offset, Color.Black);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen16, Text, Pos, Color.White);
		}

		private void DrawTheirItems()
		{
			this.TheirItemsSL.Draw(base.ScreenManager.SpriteBatch);
			Vector2 drawCurs = new Vector2((float)(this.ThemRect.X + 10), (float)(this.ThemRect.Y + Fonts.Pirulen12.LineSpacing + 10));
			for (int i = this.TheirItemsSL.indexAtTop; i < this.TheirItemsSL.Copied.Count && i < this.TheirItemsSL.indexAtTop + this.TheirItemsSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.TheirItemsSL.Copied[i];
				if (e.clickRectHover == 0)
				{
					(e.item as ItemToOffer).Update(drawCurs);
					(e.item as ItemToOffer).Draw(base.ScreenManager.SpriteBatch, Fonts.Arial12Bold);
					drawCurs.Y = drawCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
			}
		}

		public override void ExitScreen()
		{
			if (!this.them.data.ModRace)
			{
				this.music.Stop(AudioStopOptions.Immediate);
			}
			base.ScreenManager.musicCategory.Resume();
			base.ScreenManager.racialMusic.Stop(AudioStopOptions.Immediate);
			if (this.video != null)
			{
				this.player.Stop();
			}
			if (this.player != null)
			{
				this.video = null;
				while (!this.player.IsDisposed)
				{
					this.player.Dispose();
				}
			}
			this.player = null;
			this.Dispose();
			GC.Collect(1, GCCollectionMode.Optimized);
			base.ScreenManager.RemoveScreen(this);
		}

		private void FillOurItems()
		{
			ItemToOffer item;
			this.OurItemsSL.Reset();
			Vector2 newCurs = new Vector2((float)(this.UsRect.X + 10), (float)(this.UsRect.Y + Fonts.Pirulen12.LineSpacing + 2));
			if (!this.playerEmpire.GetRelations()[this.them].AtWar)
			{
				if (!this.playerEmpire.GetRelations()[this.them].Treaty_NAPact)
				{
					item = new ItemToOffer(Localizer.Token(1214), newCurs, Fonts.Arial12Bold)
					{
						Response = "NAPact"
					};
					this.OurItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (!this.playerEmpire.GetRelations()[this.them].Treaty_Trade)
				{
					item = new ItemToOffer(Localizer.Token(1215), newCurs, Fonts.Arial12Bold);
					this.OurItemsSL.AddItem(item);
					item.Response = "TradeTreaty";
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (!this.them.GetRelations()[this.playerEmpire].Treaty_OpenBorders)
				{
					item = new ItemToOffer(Localizer.Token(1216), newCurs, Fonts.Arial12Bold)
					{
						Response = "OpenBorders"
					};
					this.OurItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (this.playerEmpire.GetRelations()[this.them].Treaty_Trade && this.playerEmpire.GetRelations()[this.them].Treaty_NAPact && !this.playerEmpire.GetRelations()[this.them].Treaty_Alliance)
				{
					item = new ItemToOffer(Localizer.Token(2045), newCurs, Fonts.Arial12Bold)
					{
						Response = "OfferAlliance"
					};
					this.OurItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
			}
			else
			{
				ItemToOffer item1 = new ItemToOffer(Localizer.Token(1213), newCurs, Fonts.Arial12Bold)
				{
					Response = "Peace Treaty"
				};
				this.OurItemsSL.AddItem(item1);
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			item = new ItemToOffer(Localizer.Token(1217), newCurs, Fonts.Arial12Bold);
			ScrollList.Entry e = this.OurItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (KeyValuePair<string, TechEntry> Technology in this.playerEmpire.GetTDict())
			{
                //Added by McShooterz: prevent root nodes from being traded
				if (!Technology.Value.Unlocked || this.them.GetTDict()[Technology.Key].Unlocked || !this.them.HavePreReq(Technology.Key) || Technology.Value.GetTech().RootNode == 1)
				{
					continue;
				}
				ItemToOffer item1 = new ItemToOffer(Localizer.Token(ResourceManager.TechTree[Technology.Key].NameIndex), newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Tech";
                item1.SpecialInquiry = Technology.Key;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
			item = new ItemToOffer(Localizer.Token(1218), newCurs, Fonts.Arial12Bold);
			e = this.OurItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (Ship_Game.Artifact Artifact in this.playerEmpire.data.OwnedArtifacts)
			{
				ItemToOffer item1 = new ItemToOffer(Localizer.Token(Artifact.NameIndex), newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Artifacts";
                item1.SpecialInquiry = Artifact.Name;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
			item = new ItemToOffer(Localizer.Token(1219), newCurs, Fonts.Arial12Bold);
			e = this.OurItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (Planet p in this.playerEmpire.GetPlanets())
			{
				ItemToOffer item1 = new ItemToOffer(p.Name, newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Colony";
				item1.SpecialInquiry = p.Name;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
		}

		private void FillTheirItems()
		{
			ItemToOffer item;
			this.TheirItemsSL.Reset();
			Vector2 newCurs = new Vector2((float)(this.ThemRect.X + 10), (float)(this.ThemRect.Y + Fonts.Pirulen12.LineSpacing + 2));
			if (!this.playerEmpire.GetRelations()[this.them].AtWar)
			{
				if (!this.playerEmpire.GetRelations()[this.them].Treaty_NAPact)
				{
					item = new ItemToOffer(Localizer.Token(1214), newCurs, Fonts.Arial12Bold)
					{
						Response = "NAPact"
					};
					this.TheirItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (!this.playerEmpire.GetRelations()[this.them].Treaty_Trade)
				{
					item = new ItemToOffer(Localizer.Token(1215), newCurs, Fonts.Arial12Bold);
					this.TheirItemsSL.AddItem(item);
					item.Response = "TradeTreaty";
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (!this.playerEmpire.GetRelations()[this.them].Treaty_OpenBorders)
				{
					item = new ItemToOffer(Localizer.Token(1216), newCurs, Fonts.Arial12Bold)
					{
						Response = "OpenBorders"
					};
					this.TheirItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
				if (this.playerEmpire.GetRelations()[this.them].Treaty_Trade && this.playerEmpire.GetRelations()[this.them].Treaty_NAPact && !this.playerEmpire.GetRelations()[this.them].Treaty_Alliance)
				{
					item = new ItemToOffer(Localizer.Token(2045), newCurs, Fonts.Arial12Bold)
					{
						Response = "OfferAlliance"
					};
					this.TheirItemsSL.AddItem(item);
					newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
				}
			}
			else
			{
				ItemToOffer item1 = new ItemToOffer(Localizer.Token(1213), newCurs, Fonts.Arial12Bold);
				this.TheirItemsSL.AddItem(item1);
				item1.Response = "Peace Treaty";
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			item = new ItemToOffer(Localizer.Token(1217), newCurs, Fonts.Arial12Bold);
			ScrollList.Entry e = this.TheirItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (KeyValuePair<string, TechEntry> Technology in this.them.GetTDict())
			{
                //added by McShooterz: Prevents Racial techs from being traded
                if (!Technology.Value.Unlocked || this.playerEmpire.GetTDict()[Technology.Key].Unlocked || !this.playerEmpire.HavePreReq(Technology.Key) || Technology.Value.GetTech().RootNode == 1)
				{
					continue;
				}
				ItemToOffer item1 = new ItemToOffer(Localizer.Token(ResourceManager.TechTree[Technology.Key].NameIndex), newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Tech";
                item1.SpecialInquiry = Technology.Key;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
			item = new ItemToOffer(Localizer.Token(1218), newCurs, Fonts.Arial12Bold);
			e = this.TheirItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (Ship_Game.Artifact Artifact in this.them.data.OwnedArtifacts)
			{
                ItemToOffer item1 = new ItemToOffer(Localizer.Token(Artifact.NameIndex), newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Artifacts";
                item1.SpecialInquiry = Artifact.Name;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
			item = new ItemToOffer(Localizer.Token(1219), newCurs, Fonts.Arial12Bold);
			e = this.TheirItemsSL.AddItem(item);
			newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			newCurs.X = newCurs.X + 10f;
			foreach (Planet p in this.them.GetPlanets())
			{
				ItemToOffer item1 = new ItemToOffer(p.Name, newCurs, Fonts.Arial12Bold);
				e.AddItem(item1);
				item1.Response = "Colony";
				item1.SpecialInquiry = p.Name;
				newCurs.Y = newCurs.Y + (float)(Fonts.Arial12Bold.LineSpacing + 5);
			}
			newCurs.X = newCurs.X - 10f;
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

        ~DiplomacyScreen() {
            //should implicitly do the same thing as the original bad finalize
            this.Dispose(false);
        }

		public string GetDialogue(float Attitude)
		{
			//string neutral;
			if (this.playerEmpire.GetRelations()[this.them].AtWar)
			{
				switch (this.them.GetRelations()[this.playerEmpire].ActiveWar.GetWarScoreState())
				{
					case WarState.ColdWar:
					{
						return this.GetDialogueByName("Greeting_AtWar");
					}
					case WarState.LosingBadly:
					{
						return this.GetDialogueByName("AtWar_Losing");
					}
					case WarState.LosingSlightly:
					{
						return this.GetDialogueByName("AtWar_Losing");
					}
					case WarState.EvenlyMatched:
					{
						return this.GetDialogueByName("Greeting_AtWar");
					}
					case WarState.WinningSlightly:
					{
						return this.GetDialogueByName("AtWar_Winning");
					}
					case WarState.Dominating:
					{
						return this.GetDialogueByName("AtWar_Winning");
					}
				}
				return this.GetDialogueByName("Greeting_AtWar");
			}
			else
            {
                foreach (DialogLine dialogLine in this.them.dd.Dialogs)
                {
                    if (dialogLine.DialogType == this.whichDialogue)
                    {
                        if ((double)Attitude >= 40.0 && (double)Attitude < 60.0)
                            return dialogLine.Neutral;
                        if ((double)Attitude >= 60.0)
                            return dialogLine.Friendly;
                        else
                            return dialogLine.Hostile;
                    }
                }
                return "";
            }
		}

		public string GetDialogueByName(string Name)
		{
			string resp = "";
			foreach (DialogLine dl in this.them.dd.Dialogs)
			{
				if (dl.DialogType != Name)
				{
					continue;
				}
				if (!string.IsNullOrEmpty(dl.Default))
				{
					resp = string.Concat(resp, dl.Default);
				}
				string name = this.them.data.DiplomaticPersonality.Name;
				string str = name;
				if (name != null)
				{
					if (str == "Aggressive")
					{
						resp = string.Concat(resp, dl.DL_Agg);
					}
					else if (str == "Ruthless")
					{
						resp = string.Concat(resp, dl.DL_Ruth);
					}
					else if (str == "Honorable")
					{
						resp = string.Concat(resp, dl.DL_Hon);
					}
					else if (str == "Xenophobic")
					{
						resp = string.Concat(resp, dl.DL_Xeno);
					}
					else if (str == "Pacifist")
					{
						resp = string.Concat(resp, dl.DL_Pac);
					}
					else if (str == "Cunning")
					{
						resp = string.Concat(resp, dl.DL_Cunn);
					}
				}
				string name1 = this.them.data.EconomicPersonality.Name;
				string str1 = name1;
				if (name1 == null)
				{
					continue;
				}
				if (str1 == "Expansionists")
				{
					resp = string.Concat(resp, dl.DL_Exp);
				}
				else if (str1 == "Technologists")
				{
					resp = string.Concat(resp, dl.DL_Tech);
				}
				else if (str1 == "Militarists")
				{
					resp = string.Concat(resp, dl.DL_Mil);
				}
				else if (str1 == "Industrialists")
				{
					resp = string.Concat(resp, dl.DL_Ind);
				}
				else if (str1 == "Generalists")
				{
					resp = string.Concat(resp, dl.DL_Gen);
				}
			}
			return resp;
		}

        public override void HandleInput(InputState input)
        {
            if (HelperFunctions.CheckIntersection(new Rectangle(this.TrustRect.X - (int)Fonts.Pirulen16.MeasureString("Trust").X, this.TrustRect.Y, (int)Fonts.Pirulen16.MeasureString("Trust").X + this.TrustRect.Width, 14), input.CursorPosition))
                ToolTip.CreateTooltip(47, this.ScreenManager);
            if (HelperFunctions.CheckIntersection(new Rectangle(this.AngerRect.X - (int)Fonts.Pirulen16.MeasureString("Anger").X, this.AngerRect.Y, (int)Fonts.Pirulen16.MeasureString("Anger").X + this.AngerRect.Width, 14), input.CursorPosition))
                ToolTip.CreateTooltip(48, this.ScreenManager);
            if (HelperFunctions.CheckIntersection(new Rectangle(this.FearRect.X - (int)Fonts.Pirulen16.MeasureString("Fear").X, this.FearRect.Y, (int)Fonts.Pirulen16.MeasureString("Fear").X + this.FearRect.Width, 14), input.CursorPosition))
                ToolTip.CreateTooltip(49, this.ScreenManager);
            if (this.Exit.HandleInput(input) && this.dState != DiplomacyScreen.DialogState.TheirOffer)
                this.ExitScreen();
            if (this.dState == DiplomacyScreen.DialogState.End)
                return;
            if (this.dState != DiplomacyScreen.DialogState.TheirOffer)
            {
                if (!this.playerEmpire.GetRelations()[this.them].Treaty_Peace)
                {
                    if (this.DeclareWar != null && this.DeclareWar.HandleInput(input))
                    {
                        this.StatementsSL.Reset();
                        this.dState = DiplomacyScreen.DialogState.End;
                        if (this.playerEmpire.GetRelations()[this.them].Treaty_NAPact)
                        {
                            this.TheirText = this.GetDialogueByName("WarDeclared_FeelsBetrayed");
                            this.playerEmpire.GetGSAI().DeclareWarOn(this.them, WarType.ImperialistWar);
                            this.them.GetGSAI().GetWarDeclaredOnUs(this.playerEmpire, WarType.ImperialistWar);
                        }
                        else
                        {
                            this.TheirText = this.GetDialogueByName("WarDeclared_Generic");
                            this.playerEmpire.GetGSAI().DeclareWarOn(this.them, WarType.ImperialistWar);
                            this.them.GetGSAI().GetWarDeclaredOnUs(this.playerEmpire, WarType.ImperialistWar);
                        }
                        this.playerEmpire.GetGSAI().DeclareWarOn(this.them, WarType.ImperialistWar);
                    }
                }
                else if (this.DeclareWar != null && HelperFunctions.CheckIntersection(this.DeclareWar.R, input.CursorPosition))
                    ToolTip.CreateTooltip(128, this.ScreenManager);
                if (this.Discuss != null && this.Discuss.HandleInput(input))
                {
                    this.StatementsSL.Entries.Clear();
                    this.StatementsSL.indexAtTop = 0;
                    this.dState = DiplomacyScreen.DialogState.Discuss;
                    foreach (StatementSet statementSet in ResourceManager.DDDict["SharedDiplomacy"].StatementSets)
                    {
                        if (statementSet.Name == "Ordinary Discussion")
                        {
                            int n = 1;
                            Vector2 Cursor = this.TextCursor;
                            foreach (DialogOption dialogOption1 in statementSet.DialogOptions)
                            {
                                string str = dialogOption1.words;
                                if (!string.IsNullOrEmpty(dialogOption1.SpecialInquiry))
                                    str = this.GetDialogueByName(dialogOption1.SpecialInquiry);
                                DialogOption dialogOption2 = new DialogOption(n, str, Cursor, Fonts.Consolas18);
                                dialogOption2.words = this.parseText(str, (float)(this.DialogRect.Width - 20), Fonts.Consolas18);
                                this.StatementsSL.AddItem((object)dialogOption2);
                                dialogOption2.Response = dialogOption1.Response;
                                Cursor.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
                                ++n;
                            }
                        }
                    }
                }
                if (this.dState == DiplomacyScreen.DialogState.Discuss)
                {
                    this.StatementsSL.HandleInput(input);
                    foreach (ScrollList.Entry entry in (List<ScrollList.Entry>)this.StatementsSL.Entries)
                    {
                        if ((entry.item as DialogOption).HandleInput(input) != null)
                        {
                            this.Respond(entry.item as DialogOption);
                            break;
                        }
                    }
                }
                if (this.dState == DiplomacyScreen.DialogState.Negotiate)
                {
                    if ((!this.TheirOffer.IsBlank() || !this.OurOffer.IsBlank() || this.TheirOffer.Alliance) && this.SendOffer.HandleInput(input))
                    {
                        this.DoNegotiationResponse(this.them.GetGSAI().AnalyzeOffer(this.OurOffer, this.TheirOffer, this.playerEmpire, this.Attitude));
                        this.OurOffer = new Offer();
                        this.TheirOffer = new Offer();
                        this.TheirOffer.Them = this.them;
                    }
                    this.OfferTextSL.HandleInput(input);
                    this.OurItemsSL.HandleInput(input);
                    //string str = (string)null;
                    foreach (ScrollList.Entry e in (List<ScrollList.Entry>)this.OurItemsSL.Copied)
                    {
                        switch ((e.item as ItemToOffer).HandleInput(input, e))
                        {
                            case "NAPact":
                                this.OurOffer.NAPact = !this.OurOffer.NAPact;
                                this.TheirOffer.NAPact = this.OurOffer.NAPact;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.TheirItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "NAPact")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "We Declare War":
                                this.OurOffer.NAPact = !this.OurOffer.NAPact;
                                this.TheirOffer.NAPact = this.OurOffer.NAPact;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.TheirItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "NAPact")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "Peace Treaty":
                                this.OurOffer.PeaceTreaty = !this.OurOffer.PeaceTreaty;
                                this.TheirOffer.PeaceTreaty = this.OurOffer.PeaceTreaty;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.TheirItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "Peace Treaty")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "OfferAlliance":
                                this.OurOffer.Alliance = !this.OurOffer.Alliance;
                                this.TheirOffer.Alliance = this.OurOffer.Alliance;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.TheirItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "OfferAlliance")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "OpenBorders":
                                this.OurOffer.OpenBorders = !this.OurOffer.OpenBorders;
                                continue;
                            case "Tech":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.OurOffer.TechnologiesOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.OurOffer.TechnologiesOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "Artifacts":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.OurOffer.ArtifactsOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.OurOffer.ArtifactsOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "Colony":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.OurOffer.ColoniesOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.OurOffer.ColoniesOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "TradeTreaty":
                                this.OurOffer.TradeTreaty = !this.OurOffer.TradeTreaty;
                                this.TheirOffer.TradeTreaty = this.OurOffer.TradeTreaty;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.TheirItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "TradeTreaty")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            default:
                                continue;
                        }
                    }
                    this.OurItemsSL.Update();
                    this.TheirItemsSL.HandleInput(input);
                    //str = (string)null;
                    foreach (ScrollList.Entry e in (List<ScrollList.Entry>)this.TheirItemsSL.Copied)
                    {
                        switch ((e.item as ItemToOffer).HandleInput(input, e))
                        {
                            case "NAPact":
                                this.TheirOffer.NAPact = !this.TheirOffer.NAPact;
                                this.OurOffer.NAPact = this.TheirOffer.NAPact;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.OurItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "NAPact")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "Declare War":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.TheirOffer.EmpiresToWarOn.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.TheirOffer.EmpiresToWarOn.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "Peace Treaty":
                                this.TheirOffer.PeaceTreaty = !this.TheirOffer.PeaceTreaty;
                                this.OurOffer.PeaceTreaty = this.TheirOffer.PeaceTreaty;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.OurItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "Peace Treaty")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "OfferAlliance":
                                this.TheirOffer.Alliance = !this.TheirOffer.Alliance;
                                this.OurOffer.Alliance = this.TheirOffer.Alliance;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.OurItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "OfferAlliance")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            case "Colony":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.TheirOffer.ColoniesOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.TheirOffer.ColoniesOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "Tech":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.TheirOffer.TechnologiesOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.TheirOffer.TechnologiesOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "Artifacts":
                                if ((e.item as ItemToOffer).Selected)
                                {
                                    this.TheirOffer.ArtifactsOffered.Add((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                                else
                                {
                                    this.TheirOffer.ArtifactsOffered.Remove((e.item as ItemToOffer).SpecialInquiry);
                                    continue;
                                }
                            case "OpenBorders":
                                this.TheirOffer.OpenBorders = !this.TheirOffer.OpenBorders;
                                continue;
                            case "TradeTreaty":
                                this.TheirOffer.TradeTreaty = !this.TheirOffer.TradeTreaty;
                                this.OurOffer.TradeTreaty = this.TheirOffer.TradeTreaty;
                                using (List<ScrollList.Entry>.Enumerator enumerator = this.OurItemsSL.Copied.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        ScrollList.Entry current = enumerator.Current;
                                        if ((current.item as ItemToOffer).Response == "TradeTreaty")
                                            (current.item as ItemToOffer).Selected = (e.item as ItemToOffer).Selected;
                                    }
                                    continue;
                                }
                            default:
                                continue;
                        }
                    }
                    this.TheirItemsSL.Update();
                    if (this.ap.HandleInput(input))
                    {
                        this.ap.ToggleOn = true;
                        this.ar.ToggleOn = false;
                        this.at.ToggleOn = false;
                        this.Attitude = Offer.Attitude.Pleading;
                    }
                    if (this.ar.HandleInput(input))
                    {
                        this.ar.ToggleOn = true;
                        this.ap.ToggleOn = false;
                        this.at.ToggleOn = false;
                        this.Attitude = Offer.Attitude.Respectful;
                    }
                    if (this.at.HandleInput(input))
                    {
                        this.at.ToggleOn = true;
                        this.ap.ToggleOn = false;
                        this.ar.ToggleOn = false;
                        this.Attitude = Offer.Attitude.Threaten;
                    }
                }
                if (this.Negotiate.HandleInput(input))
                {
                    this.dState = DiplomacyScreen.DialogState.Negotiate;
                    this.OurOffer = new Offer();
                    this.TheirOffer = new Offer();
                    this.TheirOffer.Them = this.them;
                    this.FillOurItems();
                    this.FillTheirItems();
                }
            }
            if (this.dState == DiplomacyScreen.DialogState.TheirOffer)
            {
                if (this.Accept.HandleInput(input))
                {
                    if (this.TheirOffer.ValueToModify != null)
                        this.TheirOffer.ValueToModify.Value = false;
                    if (this.OurOffer.ValueToModify != null)
                        this.OurOffer.ValueToModify.Value = true;
                    this.dState = DiplomacyScreen.DialogState.End;
                    this.TheirText = this.GetDialogueByName(this.TheirOffer.AcceptDL);
                    this.playerEmpire.GetGSAI().AcceptOffer(this.TheirOffer, this.OurOffer, this.playerEmpire, this.them);
                }
                if (this.Reject.HandleInput(input))
                {
                    if (this.TheirOffer.ValueToModify != null)
                        this.TheirOffer.ValueToModify.Value = true;
                    if (this.OurOffer.ValueToModify != null)
                        this.OurOffer.ValueToModify.Value = false;
                    this.dState = DiplomacyScreen.DialogState.End;
                    this.TheirText = this.GetDialogueByName(this.TheirOffer.RejectDL);
                }
            }
            if (input.Escaped)
                this.ExitScreen();
            base.HandleInput(input);
        }

		public override void LoadContent()
		{
			Rectangle prect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 659, 0, 1318, 757);
			this.BridgeRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
			this.Player = new Menu2(base.ScreenManager, prect);
			this.Portrait = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
			Vector2 Cursor = new Vector2((float)(this.Portrait.X + this.Portrait.Width - 85), (float)(this.Portrait.Y + 140));
			this.EmpireNamePos = new Vector2(Cursor.X - Fonts.Pirulen20.MeasureString(this.them.data.Traits.Name).X, (float)(this.Portrait.Y + 40));
			if (!this.playerEmpire.GetRelations()[this.them].AtWar)
			{
				this.DeclareWar = new GenericButton(Cursor, Localizer.Token(1200), Fonts.Pirulen20, Fonts.Pirulen16);
				this.Buttons.Add(this.DeclareWar);
				Cursor.Y = Cursor.Y + 25f;
				this.Discuss = new GenericButton(Cursor, Localizer.Token(1201), Fonts.Pirulen20, Fonts.Pirulen16);
				this.Buttons.Add(this.Discuss);
				Cursor.Y = Cursor.Y + 25f;
			}
			this.Negotiate = new GenericButton(Cursor, Localizer.Token(1202), Fonts.Pirulen20, Fonts.Pirulen16);
			this.Buttons.Add(this.Negotiate);
			Cursor.Y = Cursor.Y + 25f;
			this.Exit = new GenericButton(Cursor, Localizer.Token(1203), Fonts.Pirulen20, Fonts.Pirulen16);
			this.Buttons.Add(this.Exit);
			Cursor = new Vector2((float)(this.Portrait.X + 115), (float)(this.Portrait.Y + 160));
			this.Trust = new GenericButton(Cursor, Localizer.Token(1204), Fonts.Pirulen16, Fonts.Pirulen12)
			{
				ToggleOn = true
			};
			this.TAFButtons.Add(this.Trust);
			this.TrustRect = new Rectangle(this.Portrait.X + 125, this.Trust.R.Y + 2, 100, this.Trust.R.Height);
			Cursor.Y = Cursor.Y + 25f;
			this.Anger = new GenericButton(Cursor, Localizer.Token(1205), Fonts.Pirulen16, Fonts.Pirulen12)
			{
				ToggleOn = true
			};
			this.AngerRect = new Rectangle(this.Portrait.X + 125, this.Anger.R.Y + 2, 100, this.Anger.R.Height);
			this.TAFButtons.Add(this.Anger);
			Cursor.Y = Cursor.Y + 25f;
			this.Fear = new GenericButton(Cursor, Localizer.Token(1206), Fonts.Pirulen16, Fonts.Pirulen12)
			{
				ToggleOn = true
			};
			this.TAFButtons.Add(this.Fear);
			this.FearRect = new Rectangle(this.Portrait.X + 125, this.Fear.R.Y + 2, 100, this.Fear.R.Height);
			Cursor.Y = Cursor.Y + 25f;
			this.DialogRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 350, this.Portrait.Y + this.Portrait.Height - 110, 700, 55);
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight < 820)
			{
				this.DialogRect.Y = this.Portrait.Y + this.Portrait.Height - 100;
			}
			this.R = this.DialogRect;
			this.R.Height = this.R.Height + 75;
			if (this.R.Y + this.R.Height > base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				this.R.Y = this.R.Y - (this.R.Y + this.R.Height - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight + 2);
			}
			Rectangle blerdybloo = this.R;
			blerdybloo.Height = blerdybloo.Height - 40;
			Submenu ot = new Submenu(base.ScreenManager, blerdybloo);
			this.OfferTextSL = new ScrollList(ot, Fonts.Consolas18.LineSpacing + 2, true);
			this.Attitude_Pleading_Rect = new Rectangle(this.R.X + 45, this.R.Y + this.R.Height - 48, 180, 48);
			this.Attitude_Respectful_Rect = new Rectangle(this.R.X + 250 + 5, this.R.Y + this.R.Height - 48, 180, 48);
			this.Attitude_Threaten_Rect = new Rectangle(this.R.X + 450 + 15, this.R.Y + this.R.Height - 48, 180, 48);
			this.ToneContainerRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 324, this.Attitude_Pleading_Rect.Y, 648, 48);
			this.ap = new GenericButton(this.Attitude_Pleading_Rect, Localizer.Token(1207), Fonts.Pirulen12);
			this.ar = new GenericButton(this.Attitude_Respectful_Rect, Localizer.Token(1209), Fonts.Pirulen12)
			{
				ToggleOn = true
			};
			this.at = new GenericButton(this.Attitude_Threaten_Rect, Localizer.Token(1208), Fonts.Pirulen12);
			this.AccRejRect = new Rectangle(this.R.X + this.R.Width / 2 - 220, this.R.Y + this.R.Height - 48, 440, 48);
			this.Accept = new GenericButton(new Rectangle(this.AccRejRect.X, this.AccRejRect.Y, 220, 48), Localizer.Token(1210), Fonts.Pirulen12);
			this.Reject = new GenericButton(new Rectangle(this.AccRejRect.X + 220, this.AccRejRect.Y, 220, 48), Localizer.Token(1211), Fonts.Pirulen12);
			this.Negotiate_Right = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 192, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
			this.Negotiate_Left = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 280, 192, 280);
			this.BigTradeRect = new Rectangle(this.DialogRect.X + 75, this.DialogRect.Y - 202, this.DialogRect.Width - 150, 200);
			this.UsRect = new Rectangle(this.Negotiate_Right.X + 20, this.Negotiate_Right.Y + 35, this.BigTradeRect.Width / 2 - 9, 300);
			this.ThemRect = new Rectangle(this.Negotiate_Left.X + 15, this.Negotiate_Left.Y + 35, this.BigTradeRect.Width / 2 - 10, 300);
			this.SendOffer = new GenericButton(new Rectangle(this.R.X + this.R.Width / 2 - 90, this.R.Y - 40, 180, 33), Localizer.Token(1212), Fonts.Pirulen20);
			Submenu themsub = new Submenu(base.ScreenManager, this.ThemRect);
			this.TheirItemsSL = new ScrollList(themsub, Fonts.Consolas18.LineSpacing + 5, true);
			Submenu ussub = new Submenu(base.ScreenManager, this.UsRect);
			this.OurItemsSL = new ScrollList(ussub, Fonts.Consolas18.LineSpacing + 5, true);
			Submenu sub = new Submenu(base.ScreenManager, blerdybloo);
			this.StatementsSL = new ScrollList(sub, Fonts.Consolas18.LineSpacing + 2, true);
			if (!string.IsNullOrEmpty(this.them.data.Traits.VideoPath))
			{
				try
				{
					this.video = base.ScreenManager.Content.Load<Video>(string.Concat("Video/", this.them.data.Traits.VideoPath));
					this.player = new VideoPlayer()
					{
						Volume = 0.7f,
						IsLooped = true
					};
					this.player.Play(this.video);
				}
				catch
				{
					this.video = base.ScreenManager.Content.Load<Video>(string.Concat("ModVideo/", this.them.data.Traits.VideoPath));
					this.player = new VideoPlayer()
					{
						Volume = 0.7f,
						IsLooped = true
					};
					this.player.Play(this.video);
				}
			}
			base.ScreenManager.musicCategory.Pause();
			if (!this.them.data.ModRace)
			{
				base.ScreenManager.racialMusic.SetVolume(1f);
				if (this.them.data.MusicCue != null)
				{
					if (this.WarDeclared)
					{
						this.music = AudioManager.GetCue("Stardrive_Combat 1c_114BPM");
						this.music.Play();
					}
					else
					{
						this.music = AudioManager.GetCue(this.them.data.MusicCue);
						this.music.Play();
					}
				}
			}
			this.TextCursor = new Vector2((float)(this.DialogRect.X + 5), (float)(this.DialogRect.Y + 5));
		}

		private string parseText(string text, float Width, SpriteFont font)
		{
			Width = Width - 5f;
			if (text == null)
			{
				return string.Concat("Debug info: Error. Expected ", this.whichDialogue);
			}
			string line = string.Empty;
			string returnString = string.Empty;
			string[] wordArray = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)wordArray.Length; i++)
			{
				if (wordArray[i] == "SING")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Singular;
				}
				else if (wordArray[i] == "SING.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ".");
				}
				else if (wordArray[i] == "SING,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ",");
				}
				else if (wordArray[i] == "SING, ")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ", ");
				}
				else if (wordArray[i] == "SING?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, "?");
				}
				else if (wordArray[i] == "SING!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, "!");
				}
				if (wordArray[i] == "PLURAL")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Plural;
				}
				else if (wordArray[i] == "PLURAL.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ".");
				}
				else if (wordArray[i] == "PLURAL,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ",");
				}
				else if (wordArray[i] == "PLURAL?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, "?");
				}
				else if (wordArray[i] == "PLURAL!")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, "!");
				}
				if (wordArray[i] == "TARSYS")
				{
					wordArray[i] = this.sysToDiscuss.Name;
				}
				else if (wordArray[i] == "TARSYS.")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, ".");
				}
				else if (wordArray[i] == "TARSYS,")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, ",");
				}
				else if (wordArray[i] == "TARSYS?")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, "?");
				}
				else if (wordArray[i] == "TARSYS!")
				{
					wordArray[i] = string.Concat(this.sysToDiscuss.Name, "!");
				}
				if (wordArray[i] == "TAREMP")
				{
					wordArray[i] = this.empToDiscuss.data.Traits.Name;
				}
				else if (wordArray[i] == "TAREMP.")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ".");
				}
				else if (wordArray[i] == "TAREMP,")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ",");
				}
				else if (wordArray[i] == "TAREMP?")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, "?");
				}
				else if (wordArray[i] == "TAREMP!")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, "!");
				}
				if (wordArray[i] == "TECH_DEMAND")
				{
					wordArray[i] = Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex);
				}
				else if (wordArray[i] == "TECH_DEMAND.")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), ".");
				}
				else if (wordArray[i] == "TECH_DEMAND,")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), ",");
				}
				else if (wordArray[i] == "TECH_DEMAND?")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), "?");
				}
				else if (wordArray[i] == "TECH_DEMAND!")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), "!");
				}
			}
			string[] strArrays = wordArray;
			for (int j = 0; j < (int)strArrays.Length; j++)
			{
				string word = strArrays[j];
				if (font.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			return string.Concat(returnString, line);
		}

		private string parseTextToSL(string text, float Width, SpriteFont font, ref ScrollList List)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] wordArray = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)wordArray.Length; i++)
			{
				if (wordArray[i] == "SING")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Singular;
				}
				else if (wordArray[i] == "SING.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ".");
				}
				else if (wordArray[i] == "SING,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, ",");
				}
				else if (wordArray[i] == "SING?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Singular, "?");
				}
				if (wordArray[i] == "PLURAL")
				{
					wordArray[i] = this.playerEmpire.data.Traits.Plural;
				}
				else if (wordArray[i] == "PLURAL.")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ".");
				}
				else if (wordArray[i] == "PLURAL,")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, ",");
				}
				else if (wordArray[i] == "PLURAL?")
				{
					wordArray[i] = string.Concat(this.playerEmpire.data.Traits.Plural, "?");
				}
				if (wordArray[i] == "TARSYS")
				{
					wordArray[i] = this.empToDiscuss.data.Traits.Name;
				}
				else if (wordArray[i] == "TARSYS.")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ".");
				}
				else if (wordArray[i] == "TARSYS,")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ",");
				}
				if (wordArray[i] == "TAREMP")
				{
					wordArray[i] = this.empToDiscuss.data.Traits.Name;
				}
				else if (wordArray[i] == "TAREMP.")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ".");
				}
				else if (wordArray[i] == "TAREMP,")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, ",");
				}
				else if (wordArray[i] == "TAREMP?")
				{
					wordArray[i] = string.Concat(this.empToDiscuss.data.Traits.Name, "?");
				}
				if (wordArray[i] == "TECH_DEMAND")
				{
					wordArray[i] = Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex);
				}
				else if (wordArray[i] == "TECH_DEMAND.")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), ".");
				}
				else if (wordArray[i] == "TECH_DEMAND,")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), ",");
				}
				else if (wordArray[i] == "TECH_DEMAND?")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), "?");
				}
				else if (wordArray[i] == "TECH_DEMAND!")
				{
					wordArray[i] = string.Concat(Localizer.Token(ResourceManager.TechTree[this.OurOffer.TechnologiesOffered[0]].NameIndex), "!");
				}
			}
			string[] strArrays = wordArray;
			for (int j = 0; j < (int)strArrays.Length; j++)
			{
				string word = strArrays[j];
				if (font.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				line = string.Concat(line, word, ' ');
			}
			string[] lineArray = returnString.Split(new char[] { '\n' });
			for (int i = 0; i < (int)lineArray.Length; i++)
			{
				string sent = lineArray[i];
				if (sent.Length > 0)
				{
					this.OfferTextSL.AddItem(sent);
				}
				else if (string.IsNullOrEmpty(sent) && (int)lineArray.Length > i + 1 && !string.IsNullOrEmpty(lineArray[i + 1]))
				{
					this.OfferTextSL.AddItem("\n");
				}
			}
			this.OfferTextSL.AddItem(line);
			return string.Concat(returnString, line);
		}

        private void Respond(DialogOption resp)
        {
            string Name = resp.Response;
            if (resp.Target is Empire)
                this.empToDiscuss = resp.Target as Empire;
            switch (Name)
            {
                case "Target_Opinion":
                    if (this.empToDiscuss == null)
                        break;
                    this.StatementsSL.Reset();
                    float strength = this.them.GetRelations()[this.empToDiscuss].GetStrength();
                    if ((double)strength >= 65.0)
                        this.TheirText = this.GetDialogueByName("Opinion_Positive_" + this.empToDiscuss.data.Traits.ShipType);
                    else if ((double)strength < 65.0 && (double)strength >= 40.0)
                        this.TheirText = this.GetDialogueByName("Opinion_Neutral_" + this.empToDiscuss.data.Traits.ShipType);
                    else if ((double)strength < 40.0)
                        this.TheirText = this.GetDialogueByName("Opinion_Negative_" + this.empToDiscuss.data.Traits.ShipType);
                    this.dState = DiplomacyScreen.DialogState.Them;
                    break;
                case "EmpireDiscuss":
                    using (List<StatementSet>.Enumerator enumerator = ResourceManager.DDDict["SharedDiplomacy"].StatementSets.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            StatementSet current = enumerator.Current;
                            if (current.Name == "EmpireDiscuss")
                            {
                                this.StatementsSL.Reset();
                                int n = 1;
                                Vector2 Cursor = this.TextCursor;
                                foreach (DialogOption dialogOption1 in current.DialogOptions)
                                {
                                    DialogOption dialogOption2 = new DialogOption(n, dialogOption1.words, Cursor, Fonts.Consolas18);
                                    dialogOption2.words = this.parseText(dialogOption1.words, (float)(this.DialogRect.Width - 20), Fonts.Consolas18);
                                    this.StatementsSL.AddItem((object)dialogOption2);
                                    dialogOption2.Response = dialogOption1.Response;
                                    dialogOption2.Target = (object)this.empToDiscuss;
                                    Cursor.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
                                    ++n;
                                }
                            }
                        }
                        break;
                    }
                case "Hardcoded_EmpireChoose":
                    this.StatementsSL.Entries.Clear();
                    Vector2 Cursor1 = this.TextCursor;
                    int n1 = 1;
                    foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.them.GetRelations())
                    {
                        if (keyValuePair.Value.Known && !keyValuePair.Key.isFaction && (keyValuePair.Key != this.playerEmpire && !keyValuePair.Key.data.Defeated) && this.playerEmpire.GetRelations()[keyValuePair.Key].Known)
                        {
                            DialogOption dialogOption = new DialogOption(n1, Localizer.Token(2220) + " " + keyValuePair.Key.data.Traits.Name, Cursor1, Fonts.Consolas18);
                            dialogOption.Target = (object)keyValuePair.Key;
                            dialogOption.words = this.parseText(dialogOption.words, (float)(this.DialogRect.Width - 20), Fonts.Consolas18);
                            dialogOption.Response = "EmpireDiscuss";
                            Cursor1.Y += (float)(Fonts.Consolas18.LineSpacing + 5);
                            this.StatementsSL.AddItem((object)dialogOption);
                            ++n1;
                        }
                    }
                    if (this.StatementsSL.Entries.Count != 0)
                        break;
                    this.StatementsSL.Reset();
                    this.TheirText = this.GetDialogueByName("Dunno_Anybody");
                    this.dState = DiplomacyScreen.DialogState.Them;
                    break;
                case "Hardcoded_War_Analysis":
                    this.TheirText = "";
                    this.dState = DiplomacyScreen.DialogState.Them;
                    if (this.empToDiscuss == null)
                        break;
                    if (!this.playerEmpire.GetRelations()[this.empToDiscuss].AtWar)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_YouAreNotAtWar");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (this.them.GetRelations()[this.empToDiscuss].AtWar)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_WeAreAtWar");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (this.them.GetRelations()[this.playerEmpire].Treaty_Alliance)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_Allied_OK");
                        diplomacyScreen.TheirText = str;
                        this.them.GetGSAI().DeclareWarOn(this.empToDiscuss, WarType.ImperialistWar);
                        this.empToDiscuss.GetGSAI().GetWarDeclaredOnUs(this.them, WarType.ImperialistWar);
                        break;
                    }
                    else if ((double)this.them.GetRelations()[this.playerEmpire].GetStrength() < 30.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_Reject_PoorRelations");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if (this.them.data.DiplomaticPersonality.Name == "Pacifist" || this.them.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_Reject_Pacifist");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if ((double)this.them.GetRelations()[this.empToDiscuss].GetStrength() > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_Allied_DECLINE");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else if ((double)this.them.GetRelations()[this.playerEmpire].GetStrength() > 60.0 && (double)this.empToDiscuss.MilitaryScore < (double)this.them.MilitaryScore)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_OK");
                        diplomacyScreen.TheirText = str;
                        this.them.GetGSAI().DeclareWarOn(this.empToDiscuss, WarType.ImperialistWar);
                        this.empToDiscuss.GetGSAI().GetWarDeclaredOnUs(this.them, WarType.ImperialistWar);
                        break;
                    }
                    else
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("JoinWar_Reject_TooDangerous");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                case "Hardcoded_Federation_Analysis":
                    this.StatementsSL.Entries.Clear();
                    this.TheirText = "";
                    this.dState = DiplomacyScreen.DialogState.Them;
                    if (!this.them.GetRelations()[this.playerEmpire].Treaty_Alliance)
                    {
                        if (this.them.GetRelations()[this.playerEmpire].TurnsKnown < 50)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_JustMet");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else if ((double)this.them.GetRelations()[this.playerEmpire].GetStrength() >= 75.0)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_NoAlliance");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_RelationsPoor");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                    }
                    else if (this.them.GetRelations()[this.playerEmpire].TurnsAllied < 100)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_AllianceTooYoung");
                        diplomacyScreen.TheirText = str;
                        break;
                    }
                    else
                    {
                        if (this.them.GetRelations()[this.playerEmpire].TurnsAllied < 100)
                            break;
                        if ((double)this.them.TotalScore > (double)this.playerEmpire.TotalScore * 0.800000011920929 && (double)this.them.GetRelations()[this.playerEmpire].Threat < 0.0)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_WeAreTooStrong");
                            diplomacyScreen.TheirText = str;
                            break;
                        }
                        else
                        {
                            List<Empire> warTargets = new List<Empire>();
                            List<Empire> list2 = new List<Empire>();
                            foreach (KeyValuePair<Empire, Relationship> keyValuePair in this.them.GetRelations())
                            {
                                if (!keyValuePair.Key.isFaction && keyValuePair.Value.AtWar)
                                    warTargets.Add(keyValuePair.Key);
                                if (this.playerEmpire.GetRelations().ContainsKey(keyValuePair.Key) && !keyValuePair.Key.isFaction && ((double)keyValuePair.Value.GetStrength() > 75.0 && this.playerEmpire.GetRelations()[keyValuePair.Key].AtWar))
                                    list2.Add(keyValuePair.Key);
                            }
                            if (warTargets.Count > 0)
                            {
                                IOrderedEnumerable<Empire> orderedEnumerable = Enumerable.OrderByDescending<Empire, int>((IEnumerable<Empire>)warTargets, (Func<Empire, int>)(emp => emp.TotalScore));
                                if (Enumerable.Count<Empire>((IEnumerable<Empire>)orderedEnumerable) <= 0)
                                    break;
                                this.empToDiscuss = Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable);
                                DiplomacyScreen diplomacyScreen = this;
                                string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_Quest_DestroyEnemy");
                                diplomacyScreen.TheirText = str;
                                this.them.GetRelations()[this.playerEmpire].FedQuest = new FederationQuest()
                                {
                                    EnemyName = this.empToDiscuss.data.Traits.Name
                                };
                                break;
                            }
                            else if (list2.Count > 0)
                            {
                                IOrderedEnumerable<Empire> orderedEnumerable = Enumerable.OrderByDescending<Empire, float>((IEnumerable<Empire>)list2, (Func<Empire, float>)(emp => this.them.GetRelations()[emp].GetStrength()));
                                if (Enumerable.Count<Empire>((IEnumerable<Empire>)orderedEnumerable) <= 0)
                                    break;
                                this.empToDiscuss = Enumerable.First<Empire>((IEnumerable<Empire>)orderedEnumerable);
                                DiplomacyScreen diplomacyScreen = this;
                                string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_Quest_AllyFriend");
                                diplomacyScreen.TheirText = str;
                                this.them.GetRelations()[this.playerEmpire].FedQuest = new FederationQuest()
                                {
                                    type = QuestType.AllyFriend,
                                    EnemyName = this.empToDiscuss.data.Traits.Name
                                };
                                break;
                            }
                            else
                            {
                                DiplomacyScreen diplomacyScreen = this;
                                string str = diplomacyScreen.TheirText + this.GetDialogueByName("Federation_Accept");
                                diplomacyScreen.TheirText = str;
                                this.playerEmpire.AbsorbEmpire(this.them);
                                break;
                            }
                        }
                    }
                case "Hardcoded_Grievances":
                    this.StatementsSL.Entries.Clear();
                    this.TheirText = "";
                    float num = this.them.GetRelations()[this.playerEmpire].GetStrength();
                    if ((double)num < 0.0)
                        num = 0.0f;
                    if (this.them.GetRelations()[this.playerEmpire].TurnsKnown < 20)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("Opinion_JustMetUs");
                        diplomacyScreen.TheirText = str;
                    }
                    else if ((double)num > 60.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("Opinion_NoProblems");
                        diplomacyScreen.TheirText = str;
                    }
                    else if (this.them.GetRelations()[this.playerEmpire].WarHistory.Count > 0 && (double)this.them.GetRelations()[this.playerEmpire].WarHistory[this.them.GetRelations()[this.playerEmpire].WarHistory.Count - 1].EndStarDate - (double)this.them.GetRelations()[this.playerEmpire].WarHistory[this.them.GetRelations()[this.playerEmpire].WarHistory.Count - 1].StartDate < 50.0)
                    {
                        DiplomacyScreen diplomacyScreen = this;
                        string str = diplomacyScreen.TheirText + this.GetDialogueByName("PROBLEM_RECENTWAR");
                        diplomacyScreen.TheirText = str;
                    }
                    else if ((double)num >= 0.0)
                    {
                        bool flag = false;
                        if ((double)this.them.GetRelations()[this.playerEmpire].Anger_TerritorialConflict + (double)this.them.GetRelations()[this.playerEmpire].Anger_FromShipsInOurBorders > (double)(this.them.data.DiplomaticPersonality.Territorialism / 2))
                        {
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + this.GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            flag = true;
                            if ((double)this.them.GetRelations()[this.playerEmpire].Threat > 75.0)
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + this.GetDialogueByName("Problem_Territorial");
                                diplomacyScreen2.TheirText = str2;
                                DiplomacyScreen diplomacyScreen3 = this;
                                string str3 = diplomacyScreen3.TheirText + this.GetDialogueByName("Problem_AlsoMilitary");
                                diplomacyScreen3.TheirText = str3;
                            }
                            else if ((double)this.them.GetRelations()[this.playerEmpire].Threat < -20.0 && (this.them.data.DiplomaticPersonality.Name == "Ruthless" || this.them.data.DiplomaticPersonality.Name == "Aggressive"))
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + this.GetDialogueByName("Problem_Territorial");
                                diplomacyScreen2.TheirText = str2;
                                DiplomacyScreen diplomacyScreen3 = this;
                                string str3 = diplomacyScreen3.TheirText + this.GetDialogueByName("Problem_AlsoMilitaryWeak");
                                diplomacyScreen3.TheirText = str3;
                            }
                            else
                            {
                                DiplomacyScreen diplomacyScreen2 = this;
                                string str2 = diplomacyScreen2.TheirText + this.GetDialogueByName("Problem_JustTerritorial");
                                diplomacyScreen2.TheirText = str2;
                            }
                        }
                        else if ((double)this.them.GetRelations()[this.playerEmpire].Threat > 75.0)
                        {
                            flag = true;
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + this.GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            DiplomacyScreen diplomacyScreen2 = this;
                            string str2 = diplomacyScreen2.TheirText + this.GetDialogueByName("Problem_PrimaryMilitary");
                            diplomacyScreen2.TheirText = str2;
                        }
                        else if ((double)this.them.GetRelations()[this.playerEmpire].Threat < -20.0 && (this.them.data.DiplomaticPersonality.Name == "Ruthless" || this.them.data.DiplomaticPersonality.Name == "Aggressive"))
                        {
                            DiplomacyScreen diplomacyScreen1 = this;
                            string str1 = diplomacyScreen1.TheirText + this.GetDialogueByName("Opinion_Problems");
                            diplomacyScreen1.TheirText = str1;
                            DiplomacyScreen diplomacyScreen2 = this;
                            string str2 = diplomacyScreen2.TheirText + this.GetDialogueByName("Problem_MilitaryWeak");
                            diplomacyScreen2.TheirText = str2;
                        }
                        if (!flag)
                        {
                            DiplomacyScreen diplomacyScreen = this;
                            string str = diplomacyScreen.TheirText + this.GetDialogueByName("Opinion_NothingMajor");
                            diplomacyScreen.TheirText = str;
                        }
                    }
                    this.dState = DiplomacyScreen.DialogState.Them;
                    break;
                default:
                    this.StatementsSL.Reset();
                    this.TheirText = this.GetDialogueByName(Name);
                    this.dState = DiplomacyScreen.DialogState.Them;
                    break;
            }
        }

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{

            
            if (base.IsActive)
            {
                if (this.player.State == MediaState.Paused)
                {
                    this.player.Resume();
                }
                if (!this.them.data.ModRace)
                {
                    if (this.music.IsStopped)
                    {
                        if (this.them.data.MusicCue != null)
                        {
                            if (this.WarDeclared)
                            {
                                this.music = AudioManager.GetCue("Stardrive_Combat 1c_114BPM");
                                this.music.Play();
                            }
                            else
                            {
                                this.music = AudioManager.GetCue(this.them.data.MusicCue);
                                this.music.Play();
                            }
                        }
                    }
                    else if (this.music.IsPaused)
                    {
                        this.music.Resume();
                    }
                }
            }
            else
            {
                if (this.player.State == MediaState.Playing)
                {
                    this.player.Pause();
                }
                if (!this.them.data.ModRace && this.music.IsPlaying)
                {
                    this.music.Pause();
                }
            }
			if (this.Discuss != null)
			{
				if (this.dState != DiplomacyScreen.DialogState.Discuss)
				{
					this.Discuss.ToggleOn = false;
				}
				else
				{
					this.Discuss.ToggleOn = true;
				}
			}
			if (this.dState != DiplomacyScreen.DialogState.Negotiate)
			{
				this.Negotiate.ToggleOn = false;
			}
			else
			{
				this.Negotiate.ToggleOn = true;
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		private enum DialogState
		{
			Them,
			Choosing,
			Discuss,
			Negotiate,
			TheirOffer,
			End
		}

		private enum TradeProposals
		{
			TradePact,
			NAPact,
			Peace,
			OpenBorders
		}
	}
}