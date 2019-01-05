using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Ship_Game
{
    public sealed class SaveRaceScreen : GenericLoadSaveScreen
    {
        private readonly RaceDesignScreen Screen;
        private readonly RaceSave RaceSave;

        public SaveRaceScreen(RaceDesignScreen screen, RacialTrait data) 
            : base(screen, SLMode.Save, data.Name, "Save Race", "Saved Races", "Saved Race already exists.  Overwrite?")
        {
            Screen = screen;
            Path = Dir.StarDriveAppData + "/Saved Races/";
            RaceSave = new RaceSave(data);
        }

        public override void DoSave()
        {
            RaceSave.Name = EnterNameArea.Text;
            using (TextWriter writeStream = new StreamWriter(Path + EnterNameArea.Text + ".xml"))
                new XmlSerializer(typeof(RaceSave)).Serialize(writeStream, RaceSave);
            ExitScreen();
        }

        protected override void InitSaveList()        // Set list of files to show
        {
            var saves = new Array<FileData>();
            foreach (FileInfo fileInfo in Dir.GetFiles(Path))
            {
                try
                {
                    RaceSave data = fileInfo.Deserialize<RaceSave>();

                    if (string.IsNullOrEmpty(data.Name))
                    {
                        data.Name = fileInfo.NameNoExt();
                        data.Version = 0;
                    }

                    string info;
                    string extraInfo;
                    if (data.Version < 308)     // Version checking
                    {
                        info = "Invalid Race File";
                        extraInfo = "";
                    } else {
                        info ="Race Name: " + data.Traits.Name;
                        extraInfo = data.ModName != "" ? "Mod: " + data.ModName : "Default";
                    }
                    saves.Add(new FileData(fileInfo, data, data.Name, info, extraInfo));
                }
                catch
                {
                }
            }

            var sortedList = from data in saves orderby data.FileName select data;
            foreach (FileData data in sortedList)
                SavesSL.AddItem(data).AddSubItem(data.FileLink);
        }
    }
}