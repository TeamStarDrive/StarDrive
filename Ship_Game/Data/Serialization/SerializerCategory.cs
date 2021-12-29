using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Serialization
{
    public enum SerializerCategory
    {
        None, // fundamental types: int, string, Vector2, ...
        UserClass,  // [StarDataType] classes
        RawArray,   // T[]
        Collection, // generic collections Array<T> and Map<K,V>
    }
}
