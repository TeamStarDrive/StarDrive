using System.Collections.Generic;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.Ships;
#pragma warning disable CA2213

namespace Ship_Game;

public partial class UniverseScreen
{
    // TODO: refactor all of this into a single SelectedItem class
    public SolarSystem SelectedSystem;
    public Planet SelectedPlanet;
    public Fleet SelectedFleet;
    public Ship SelectedShip;
    public Ship PrevSelectedShip;
    Array<Ship> SelectedShipList = new();
    public ClickableSpaceBuildGoal SelectedItem;

    public IReadOnlyList<Ship> SelectedShips => SelectedShipList;

    public bool HasSelectedItem => SelectedFleet != null
                                || SelectedItem != null
                                || SelectedShip != null
                                || SelectedPlanet != null
                                || SelectedShipList.Count != 0;

    public void ClearSelectedItems(bool clearFlags = true,
                                   bool clearShipList = true,
                                   bool updatePrevSelectedShip = true,
                                   Fleet fleet = null)
    {
        if (updatePrevSelectedShip)
            UpdatePrevSelectedShip(newShip: null);

        SelectedSystem = null;
        SelectedPlanet = null;
        SelectedFleet = fleet;
        SelectedItem = null;
        SelectedShip = null;

        if (clearShipList)
        {
            SelectedShipList?.Clear();
            shipListInfoUI?.ClearShipList();
        }

        if (clearFlags)
        {
            ViewingShip = false;
            snappingToShip = false;
            returnToShip = false;
        }
    }

    Ship FollowedShipToSelect;

    public void UpdateSelectedShips()
    {
        if (SelectedShip is { IsLanding: true })
        {
            if (ViewingShip && ShipToView == SelectedShip)
                FollowedShipToSelect = SelectedShip;
            ClearSelectedItems(clearFlags: false, updatePrevSelectedShip: false, fleet: SelectedFleet);
        }

        if (TakeFollowedShipThatTookOff() is Ship tookOff)
            SetSelectedShip(tookOff, clearFlags: false);

        int num = SelectedShipList.Count();
        SelectedShipList.RemoveAll(s => !s.Active || s.IsLanding);
        if (SelectedShip != null)
            SetSelectedShip(SelectedShip, SelectedFleet, clearFlags: false); // same ship, UI refresh only
        else if (num != SelectedShipList.Count())
            SetSelectedShipList(SelectedShipList, SelectedFleet);
    }

    internal Ship TakeFollowedShipThatTookOff()
    {
        Ship ship = FollowedShipToSelect;
        if (ship == null)
            return null;

        if (!ship.Active || !ViewingShip || ShipToView != ship)
        {
            FollowedShipToSelect = null;
            return null;
        }

        if (ship.IsLanding)
            return null;

        FollowedShipToSelect = null;
        return ship;
    }

    void UpdatePrevSelectedShip(Ship newShip)
    {
        // previously selected ship is not null, and we selected a new ship, and new ship is not previous ship
        if (SelectedShip != null && PrevSelectedShip != SelectedShip && SelectedShip != newShip)
        {
            PrevSelectedShip = SelectedShip;
        }
    }

    bool HandlePrevSelectedShipChange(InputState input)
    {
        // CG: previous target code.
        if (PrevSelectedShip != null && input.PreviousTarget)
        {
            if (PrevSelectedShip is { Active: true, IsLanding: false })
                SetSelectedShip(PrevSelectedShip);
            else
                PrevSelectedShip = null;  //fbedard: remove inactive ship
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets the currently selected ship and clears selected ships list
    /// </summary>
    /// <param name="clearFlags">FALSE keeps the camera chase flags, for refreshing the same ship</param>
    public void SetSelectedShip(Ship selectedShip, Fleet fleet = null, bool clearFlags = true)
    {
        SelectedSomethingTimer = 3f;

        // manually update prev selected ship
        UpdatePrevSelectedShip(selectedShip);
        ClearSelectedItems(clearFlags: clearFlags, updatePrevSelectedShip: false, fleet: fleet);

        SelectedShip = selectedShip;

        if (selectedShip != null)
        {
            SelectedShipList.Add(selectedShip);
            ShipInfoUIElement.SetShip(selectedShip);
        }
    }

    public void SetSelectedShipList(IReadOnlyList<Ship> ships, Fleet fleet)
    {
        ClearSelectedItems(clearShipList: false, fleet: fleet);
        // if SetSelectedShipList(SelectedShipList) then this is a no-op
        // but otherwise always clone the ship list
        SelectedShipList = ReferenceEquals(ships, SelectedShipList) ? SelectedShipList : new(ships);
        if (SelectedShipList.Count == 1 && (SelectedShipList[0].Fleet == null || SelectedShipList[0].Fleet.Ships.Count > 1))
            SetSelectedShip(SelectedShipList[0]);
        else
            shipListInfoUI.SetShipList(SelectedShipList, fleet != null);
    }
    

    public void SetSelectedFleet(Fleet fleet, Array<Ship> selectedShips = null)
    {
        SelectedSomethingTimer = 3f;
        Array<Ship> ships = selectedShips ?? fleet.Ships;

        if (ships.Count > 0)
        {
            SetSelectedShipList(ships, fleet: fleet);
        }
        else // nothing actually selected
        {
            ClearSelectedItems();
        }
    }

    public void SetSelectedPlanet(Planet planet)
    {
        SelectedSomethingTimer = 3f;
        ClearSelectedItems();
        SelectedPlanet = planet;
        pInfoUI.SetPlanet(planet);
    }

    public void SetSelectedSystem(SolarSystem system, Planet p = null)
    {
        SelectedSomethingTimer = 3f;
        ClearSelectedItems();

        SelectedSystem = system;
        SystemInfoOverlay.SetSystem(SelectedSystem);

        if (p != null)
        {
            SelectedPlanet = p;
            pInfoUI.SetPlanet(p);
        }
    }

    void SetSelectedItem(ClickableSpaceBuildGoal item)
    {
        SelectedSomethingTimer = 3f;
        ClearSelectedItems();
        SelectedItem = item;
    }
}
