using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Ship_Game.Data.YamlSerializer;

namespace UnitTests.Serialization
{
    [TestClass]
    public class YamlSerializerTests : StarDriveTest
    {
        [TestMethod]
        public void SerializeParticleSettings()
        {
            var ps = new ParticleSettings
            {
                Name = "Flame",
                TextureName = "texture1",
                MaxParticles = 10,
                Duration = TimeSpan.FromSeconds(123.5),
                DurationRandomness = 1,
                EmitterVelocitySensitivity = 2,
                MinHorizontalVelocity = 3,
                MaxHorizontalVelocity = 4.2f,
                MinVerticalVelocity = 5.4f,
                MaxVerticalVelocity = 5.8f,
                Gravity = new Vector3(1,2,3),
                EndVelocity = 6,
                MinColor = new Color(10,20,30,255),
                MaxColor = new Color(40,50,60,255),
                MinRotateSpeed = 7,
                MaxRotateSpeed = 8,
                MinStartSize = 9,
                MaxStartSize = 10,
                MinEndSize = 11,
                MaxEndSize = 12,
                SourceBlend = Blend.BlendFactor,
                DestinationBlend = Blend.InverseBlendFactor,
            };

            var sw = new StringWriter();
            var ser = new YamlSerializer(typeof(ParticleSettings));
            ser.Serialize(sw, ps);

            string text = sw.ToString();
            string yaml =
              @"ParticleSettings:
                  Name: ""Flame""
                  TextureName: ""texture1""
                  MaxParticles: 10
                  Duration: 123.5
                  DurationRandomness: 1
                  EmitterVelocitySensitivity: 2
                  MinHorizontalVelocity: 3
                  MaxHorizontalVelocity: 4.2
                  MinVerticalVelocity: 5.4
                  MaxVerticalVelocity: 5.8
                  Gravity: [1,2,3]
                  EndVelocity: 6
                  MinColor: [10,20,30,255]
                  MaxColor: [40,50,60,255]
                  MinRotateSpeed: 7
                  MaxRotateSpeed: 8
                  MinStartSize: 9
                  MaxStartSize: 10
                  MinEndSize: 11
                  MaxEndSize: 12
                  SourceBlend: BlendFactor
                  DestinationBlend: InverseBlendFactor
                ".Replace("\r\n", "\n").Replace("                ", "");

            Assert.That.Equal(yaml, text);
        }
    }

}
