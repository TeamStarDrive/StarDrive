using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Ship_Game
{
	public sealed class LoadSaveScreen : GenericLoadSaveScreen
	{
        private UniverseScreen screen;
		private MainMenuScreen mmscreen;

        public LoadSaveScreen(UniverseScreen screen) : base(screen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
		{
			this.screen = screen;
            Path = Dir.ApplicationData +  "/StarDrive/Saved Games/";
        }
		public LoadSaveScreen(MainMenuScreen mmscreen) : base(mmscreen, SLMode.Load, "", Localizer.Token(6), "Saved Games")
        {
            this.mmscreen = mmscreen;
            Path = Dir.ApplicationData + "/StarDrive/Saved Games/";
        }

        protected override void DeleteFile(object sender, EventArgs e)
        {
            try
            {
                FileInfo headerToDel = new FileInfo(Path + "Headers/" + fileToDel.Name.Substring(0, fileToDel.Name.LastIndexOf('.')));       // find header of save file
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile(sender, e);
        }

        protected override void Load()
        {
            if (selectedFile != null)
            {
                screen?.ExitScreen();
                ScreenManager.AddScreen(new LoadUniverseScreen(selectedFile.FileLink));
                mmscreen?.ExitScreen();
            }
            else
            {
                AudioManager.PlayCue("UI_Misc20");
            }
            ExitScreen();
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo saveHeaderFile in Dir.GetFiles(Path + "Headers", "xml"))
            {
                try
                {
                    HeaderData data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(saveHeaderFile);

                    bool newFormat = false;
                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.OldZipExt);
                    if (!data.FI.Exists)
                    {
                        data.FI = new FileInfo(Path + data.SaveName + SavedGame.NewZipExt);
                        newFormat = true;
                    }
                    if (!data.FI.Exists)
                    {
                        Log.Info("Savegame missing payload: {0}", data.FI.FullName);
                        continue;
                    }

                    if (string.IsNullOrEmpty(data.SaveName))
                        continue;

                    if (GlobalStats.ActiveMod != null)
                    {
                        // check mod and check version of save file since format changed
                        if (data.Version  > 0 && data.ModPath != GlobalStats.ActiveMod.ModPath || 
                            data.Version == 0 && data.ModName != GlobalStats.ActiveMod.ModPath)   
                            continue;
                    }
                    else if (data.Version  > 0 && !string.IsNullOrEmpty(data.ModPath) || 
                             data.Version == 0 && !string.IsNullOrEmpty(data.ModName))
                        continue; // skip non-mod savegames

                    string info = data.PlayerName + " StarDate " + data.StarDate;
                    if (newFormat) info += " (sav)";

                    string extraInfo = data.RealDate;
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo));
                }
                catch
                {
                }
            }

            var sortedList = from header in saves
                             orderby (header.Data as HeaderData)?.Time 
                             descending select header;

            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
        }

    }
}