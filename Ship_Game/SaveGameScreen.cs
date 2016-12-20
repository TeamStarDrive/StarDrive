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
	public sealed class SaveGameScreen : GenericLoadSaveScreen, IDisposable
	{
        private UniverseScreen screen;

        public SaveGameScreen(UniverseScreen screen) : base(SLMode.Save, string.Concat(screen.PlayerLoyalty, ", Star Date ", screen.StarDate.ToString(screen.StarDateFmt)), "Save Game", "Saved Games", "Saved Game already exists.  Overwrite?")
		{
			this.screen = screen;
            this.Path = string.Concat(Dir.ApplicationData, "/StarDrive/Saved Games/");
            //this.selectedFile = new FileData();
        }

		public override void DoSave()
		{
			SavedGame savedGame = new SavedGame(this.screen, this.EnterNameArea.Text);
			this.ExitScreen();
        }

        protected override void DeleteFile(object sender, EventArgs e)
        {
            try
            {
                FileInfo headerToDel = new FileInfo(string.Concat(this.Path, "Headers/", this.fileToDel.Name.Substring(0, this.fileToDel.Name.LastIndexOf('.'))));       // find header of save file
                //Log.Info(headerToDel.FullName);
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile(sender, e);
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            var saves = new List<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path + "Headers", "xml"))
            {
                try
                {
                    HeaderData data = ResourceManager.HeaderSerializer.Deserialize<HeaderData>(fileInfo);

                    if (string.IsNullOrEmpty(data.SaveName))
                    {
                        data.SaveName = fileInfo.NameNoExt(); // set name before it's used
                        data.Version = 0;
                    }

                    data.FI = new FileInfo(Path + data.SaveName + SavedGame.OldZipExt);
                    if (!data.FI.Exists)
                        data.FI = new FileInfo(Path + data.SaveName + SavedGame.NewZipExt);
                    if (!data.FI.Exists)
                    {
                        Log.Warning("Missing save payload {0}", data.FI.FullName);
                        continue;
                    }

                    string info = data.PlayerName + " StarDate " + data.StarDate;
                    string extraInfo = data.RealDate;
                    saves.Add(new FileData(data.FI, data, data.SaveName, info, extraInfo));
                }
                catch
                {
                }
            }
            var sortedList = from header in saves orderby (header.Data as HeaderData)?.Time descending select header;
            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
        }
    }
}