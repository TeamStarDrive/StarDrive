using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.YamlSerializer
{
    // type mapping cache for converters
    class YamlSerializerMap : TypeSerializerMap
    {
        public YamlSerializerMap()
        {
            Add(typeof(object), new ObjectSerializer());
        }

        protected override TypeSerializer AddUserTypeSerializer(Type type)
        {
            return new YamlSerializer(type);
        }
    }
}