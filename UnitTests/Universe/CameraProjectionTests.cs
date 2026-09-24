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

    void LookDownFrom(Vector3d camPos)
    {
        Universe.CamPos = camPos;
        Universe.SetViewPerspective(Matrices.CreateLookAtDown(camPos.X, camPos.Y, -camPos.Z),
                                    maxDistance: UniverseScreen.CAM_MAX);
    }

    // Near the origin the unproject is still accurate, so it is a fair reference.
    [TestMethod]
    public void VisibleWorldSizeMatchesTheUnprojectedCornersNearTheOrigin()
    {
        LookDownFrom(new Vector3d(0, 0, 60_000));

        Vector2 topLeft = Universe.UnprojectToWorldPosition(Vector2.Zero);
        Vector2 botRight = Universe.UnprojectToWorldPosition(Universe.ScreenArea);
        AABoundingBox2Dd r = Universe.ExactVisibleWorldRect;

        AssertEqual(r.Width * 1e-4, botRight.X - topLeft.X, r.Width, "visible width");
        AssertEqual(r.Height * 1e-4, botRight.Y - topLeft.Y, r.Height, "visible height");
    }

    // The size depends only on camera height, so the same height far out must give the same size.
    [TestMethod]
    public void VisibleWorldSizeDoesNotDriftWithDistanceFromTheOrigin()
    {
        LookDownFrom(new Vector3d(0, 0, 500_000));
        AABoundingBox2Dd atOrigin = Universe.ExactVisibleWorldRect;

        LookDownFrom(new Vector3d(8_000_000, -6_000_000, 500_000));
        AABoundingBox2Dd farOut = Universe.ExactVisibleWorldRect;

        AssertEqual(1e-6, atOrigin.Width, farOut.Width, "visible width must not depend on where the camera is");
        AssertEqual(1e-6, atOrigin.Height, farOut.Height, "visible height must not depend on where the camera is");
    }

    [TestMethod]
    public void VisibleWorldSizeScalesWithCameraHeight()
    {
        LookDownFrom(new Vector3d(0, 0, 100_000));
        AABoundingBox2Dd low = Universe.ExactVisibleWorldRect;

        LookDownFrom(new Vector3d(0, 0, 300_000));
        AABoundingBox2Dd high = Universe.ExactVisibleWorldRect;

        AssertEqual(1.0, low.Width * 3.0, high.Width, "width is linear in camera height");
        AssertEqual(1.0, low.Height * 3.0, high.Height, "height is linear in camera height");
    }

    // Pan smoothly one way and the minimap frame must march one way. Built from Unproject round
    // trips it reverses instead, which is the tremble that was reported.
    [TestMethod]
    public void TheViewportFrameDoesNotTrembleFarFromTheOrigin()
    {
        const double camZ = 4_000_000, startX = 9_000_000, y = -7_000_000, step = 2000.0;
        float scale = 200f / (float)(Universe.UState.Size * 2.1f);

        var frameX = new List<int>();
        for (int i = 0; i < 300; ++i)
        {
            LookDownFrom(new Vector3d(startX + i * step, y, camZ));
            double left = Universe.ExactVisibleWorldRect.X1;
            frameX.Add((int)Math.Round(left * scale));
        }

        int reversals = 0;
        for (int i = 2; i < frameX.Count; ++i)
            if (Math.Sign(frameX[i] - frameX[i-1]) * Math.Sign(frameX[i-1] - frameX[i-2]) < 0)
                ++reversals;

        Assert.IsTrue(frameX[0] < frameX[frameX.Count-1], "the frame must follow the camera");
        AssertEqual(0, reversals, $"the frame reversed {reversals} times while the camera panned one way");
    }

    // viewState rebuilds the projection with a different far plane; only M33/M43 carry it, so the
    // visible rect must not move. That was the other thing that made the old frame jump.
    [TestMethod]
    public void TheFarPlaneDoesNotMoveTheVisibleRect()
    {
        var camPos = new Vector3d(1_000_000, 2_000_000, 300_000);
        LookDownFrom(camPos);
        AABoundingBox2Dd atMaxRange = Universe.ExactVisibleWorldRect;

        Universe.SetViewPerspective(Matrices.CreateLookAtDown(camPos.X, camPos.Y, -camPos.Z),
                                    maxDistance: 15_035_000);
        AABoundingBox2Dd nearer = Universe.ExactVisibleWorldRect;

        AssertEqual(1e-9, atMaxRange.X1, nearer.X1, "the far plane must not move the rect");
        AssertEqual(1e-9, atMaxRange.Width, nearer.Width, "the far plane must not resize the rect");
    }
}
