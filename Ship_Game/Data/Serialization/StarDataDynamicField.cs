using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Serialization
{
    /// <summary>
    /// Allows for dynamically setting field values
    /// during the [StarDataSerialize] event:
    ///
    /// [StarDataSerialize]
    /// StarDataDynamicField[] OnSerialize()
    /// {
    ///    return new StarDataDynamicField[]
    ///    {
    ///        new("Save", new Save())
    ///    };
    /// }
    /// </summary>
    public class StarDataDynamicField
    {
        public string Name; // field name
        public object Value; // field value

        public StarDataDynamicField() {}
        public StarDataDynamicField(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
