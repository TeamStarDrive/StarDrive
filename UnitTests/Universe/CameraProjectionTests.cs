using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using Ship_Game;

namespace UnitTests.Universe;

[TestClass]
public class CameraProjectionTests : StarDriveTest
{
    public CameraProjectionTests()
    {
        CreateUniverseAndPlayerEmpire(playerArchetype: "Human");
    }

    [TestMethod]
    public void CameraZoomDoesNotCauseNAN()
    {
        Vector3d camPos = new(100, 100, 10);
        Universe.UpdateViewport();
        Universe.SetViewPerspective(Matrices.CreateLookAtDown(camPos.X, camPos.Y, -camPos.Z), maxDistance: 30000);

        Vector3d centerPos = Universe.GetNewCameraPos(camPos, targetScreenPos:Universe.ScreenCenter, 10.0);
        AssertFalse(centerPos.IsNaN(), $"GetNewCameraPos pos={Universe.ScreenCenter} cannot be NaN: {centerPos}");
    }

    [TestMethod]
    public void UnprojectToWorldPosition3D()
    {
        Vector3d camPos = new(0,0, 10_000);
        Universe.SetViewPerspective(Matrices.CreateLookAtDown(camPos.X, camPos.Y, -camPos.Z), maxDistance: UniverseScreen.CAM_MAX);

        Vector3d zeroPos = Universe.UnprojectToWorldPosition3D(Vector2.Zero);
        AssertFalse(zeroPos.IsNaN(), $"Unprojected pos={Vector2.Zero} cannot be NaN: {zeroPos}");

        Vector3d centerPos = Universe.UnprojectToWorldPosition3D(Universe.ScreenCenter);
        AssertFalse(centerPos.IsNaN(), $"Unprojected pos={Universe.ScreenCenter} cannot be NaN={centerPos}");

        Vector3d bottomRightPos = Universe.UnprojectToWorldPosition3D(Universe.ScreenArea);
        AssertFalse(bottomRightPos.IsNaN(), $"Unprojected pos={Universe.ScreenArea} cannot be NaN={bottomRightPos}");
    }
}
