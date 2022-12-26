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
        Universe.SetPerspectiveProjection(maxDistance: 30000);
        Vector3d camPos = new(100, 100, 10);
        Universe.SetViewMatrix(Matrices.CreateLookAtDown(camPos.X, camPos.Y, -camPos.Z));

        Vector3d pos1 = Universe.GetNewCameraPos(camPos, targetScreenPos:new(1280/2,720/2), 10.0);
        AssertFalse(double.IsNaN(pos1.X));
        AssertFalse(double.IsNaN(pos1.Y));
        AssertFalse(double.IsNaN(pos1.Z));
    }
}
