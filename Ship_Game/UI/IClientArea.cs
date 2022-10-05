using System;
using SDGraphics;

namespace Ship_Game.UI;

/// <summary>
/// Allows for UI components to bind to their Parent's ClientArea property
/// </summary>
public interface IClientArea
{
    /// <summary>
    /// ClientArea: the usable working area in an UIPanel/Submenu/UIElementContainer
    /// </summary>
    RectF ClientArea { get; }
}
