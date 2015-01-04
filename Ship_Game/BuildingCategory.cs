using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    public enum BuildingCategory
    {
        General,
        Defense,
        Food,
        Production,
        Science,
        Terraforming,
        Finance, //Added 'Finance', need an identifier for buildings such as the Bank that exist to boost revenue
        Military, // Added 'Military' as a separate category from 'Defense' - there should be a distinction between e.g. Barracks as a military production building, and a Shield Generator which is a Defensive building
        Storage, // Added 'Storage', need an identifier for AI to recognise buildings that enlarge planetary capacity
        Sensor, //Added 'Sensor', need an identifier for things like Sensor Arrays, Telescopes
        Population, //Added 'Population', need an identifier for buildings that specific increase maximum planetary population
        Growth, //Added 'Growth', need an identifier for buildings that boost population growth rate
        Shipyard, //Added 'Shipyard', need an identifier for buildings that enable ship construction
        Victory, //Added 'Victory', need an identifier for buildings that cause or lead to quest-based game victory
        Biosphere //Added 'Biosphere', need an identifier for buildings that can be built on uninhabitable tiles to make them habitable: in mods there could be new such buildings
    }
}
