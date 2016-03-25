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
            this.Path = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/StarDrive/Saved Games/");
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
                //Console.WriteLine(headerToDel.FullName);
                headerToDel.Delete();
            }
            catch { }

            base.DeleteFile(sender, e);
        }

        protected override void SetSavesSL()        // Set list of files to show
        {
            List<FileData> saves = new List<FileData>();
            FileInfo[] filesFromDirectory = HelperFunctions.GetFilesFromDirectory(string.Concat(this.Path, "Headers"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                Stream file = filesFromDirectory[i].OpenRead();
                try
                {
                    HeaderData data = (HeaderData)ResourceManager.HeaderSerializer.Deserialize(file);

                    if (string.IsNullOrEmpty(data.SaveName))
                    {
                        data.SaveName = System.IO.Path.GetFileNameWithoutExtension(filesFromDirectory[i].Name);         // set name before it's used
                        data.Version = 0;
                    }

                    data.SetFileInfo(new FileInfo(string.Concat(this.Path, data.SaveName, ".xml.gz")));

                    if ((!string.IsNullOrEmpty(data.ModPath) && data.Version > 0) || (data.Version == 0 && !string.IsNullOrEmpty(data.ModName)))
                    {
                        file.Dispose();
                        continue;
                    }

                    string info = string.Concat(data.PlayerName, " StarDate ", data.StarDate);
                    string extraInfo = data.RealDate;
                    saves.Add(new FileData(data.GetFileInfo(), data, data.SaveName, info, extraInfo));
                    file.Dispose();
                }
                catch
                {
                    file.Dispose();
                }
            }
            IOrderedEnumerable<FileData> sortedList =
                from header in saves
                orderby (header.Data as HeaderData).Time descending
                select header;
            foreach (FileData data in sortedList)
            {
                this.SavesSL.AddItem(data).AddItemWithCancel(data.FileLink);
            }
        }
    }
}