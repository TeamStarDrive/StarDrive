using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Color = Microsoft.Xna.Framework.Color;
using DirectionalLight = SynapseGaming.LightingSystem.Lights.DirectionalLight;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.GameScreens.NewGame;

// a cruiser of the race whose color the picker shows, wearing the picked color as that race's ships would
public sealed class ShipColorPreview : UIElementV2
{
    const float TurnPerSecond = 0.3f;
    const float TurnPerPixel = 0.01f;
    const float Elevation = 30f * RadMath.Deg1AsRads;
    const float Margin = 1.1f;
    const int MinShipAreaHeight = 80; // a shorter area leaves the panel out

    // the hull lies lengthwise with its nose to the right, seen from a little above its side
    static readonly XnaMatrix Pose = XnaMatrix.CreateRotationX(RadMath.PI)
                                   * XnaMatrix.CreateRotationZ(-RadMath.HalfPI)
                                   * XnaMatrix.CreateRotationX(Elevation - RadMath.HalfPI);

    readonly UIColorPicker Picker;
    readonly GameContentManager Content;
    readonly Func<IEmpireData> Race;
    readonly Func<bool> ColorWorn;
    readonly Menu1 Frame;
    readonly string Hint;
    readonly SceneState State = new();
    SceneInterface Scene;
    IEmpireData ShownRace;
    SceneObject Ship;
    XnaVector3 ShipCenter;
    float Radius;
    float CoverX, CoverY, HalfWidth, HalfHeight; // what the hull covers on screen over a whole turn, in model units
    float Turn;
    bool Dragging;
    float DragX;

    public ShipColorPreview(in Rectangle rect, UIColorPicker picker, GameContentManager content,
                            Func<IEmpireData> race, Func<bool> colorWorn) : base(rect)
    {
        Picker = picker;
        Content = content;
        Race = race;
        ColorWorn = colorWorn;
        Frame = new Menu1(rect);
        Hint = Localizer.Token(GameText.DragToTurn);
        Scene = new SceneInterface(ScreenManager.Instance.Graphics);
        AddLight("Preview Key", new(0.45f, -0.55f, -0.70f), new(1f, 1f, 1f), 1.75f);
        AddLight("Preview Fill", new(-0.60f, 0.30f, -0.75f), new(0.85f, 0.88f, 0.92f), 0.8f);
        AddLight("Preview Back", new(0.20f, -0.50f, 0.85f), new(0.32f, 0.36f, 0.39f), 0.5f);
    }

    void AddLight(string name, XnaVector3 direction, XnaVector3 color, float intensity)
    {
        Scene.LightManager.Submit(new DirectionalLight
        {
            Name = name, Direction = direction, DiffuseColor = color, Intensity = intensity, Enabled = true,
        });
    }

    public override void OnRemoved()
    {
        Ship = null;
        Mem.Dispose(ref Scene);
    }

    // a race's biggest cruiser hull, or its biggest warship hull when it has no cruiser
    public static ShipHull HullOf(IEmpireData race)
    {
        ShipHull best = null;
        foreach (ShipHull hull in ResourceManager.Hulls)
        {
            if (hull.Style == race.ShipType && hull.IsValidForCurrentMod
                && hull.Role >= RoleName.corvette && hull.Role <= RoleName.capital
                && (best == null || Outranks(hull, best)))
                best = hull;
        }
        return best;
    }

    static bool Outranks(ShipHull a, ShipHull b)
    {
        bool aCruiser = a.Role == RoleName.cruiser, bCruiser = b.Role == RoleName.cruiser;
        if (aCruiser != bCruiser)
            return aCruiser;
        int aArea = a.Size.X * a.Size.Y, bArea = b.Size.X * b.Size.Y;
        return aArea != bArea ? aArea > bArea : string.CompareOrdinal(a.HullName, b.HullName) < 0;
    }

    bool Shown => Picker.Visible && ShipArea.Height >= MinShipAreaHeight;

    public override void Update(float fixedDeltaTime)
    {
        if (Shown && Scene != null)
        {
            Show(Race());
            if (!Dragging)
                Turn += TurnPerSecond * fixedDeltaTime;
            try
            {
                Ship?.UpdateAnimation(fixedDeltaTime);
            }
            catch (Exception e)
            {
                Log.Error(e, $"ShipColorPreview could not animate the ship of {ShownRace?.Name}");
                DropShip();
            }
        }
        base.Update(fixedDeltaTime);
    }

    void Show(IEmpireData race)
    {
        if (race == ShownRace)
            return;

        ShownRace = race;
        Turn = 0f;
        DropShip();

        ShipHull hull = null;
        try
        {
            hull = race != null ? HullOf(race) : null;
            StaticMesh mesh = hull != null ? StaticMesh.LoadMesh(Content, hull.ModelPath, hull.Animated) : null;
            Ship = mesh?.CreateSceneObject();
            if (Ship == null)
                return;

            Ship.Visibility = ObjectVisibility.Rendered;
            ShipCenter = (mesh.Bounds.Min + mesh.Bounds.Max) * 0.5f;
            Radius = mesh.Radius;
            Scene.ObjectManager.Submit(Ship);
            FitTurns();
        }
        catch (Exception e)
        {
            Log.Error(e, $"ShipColorPreview could not show {hull?.HullName} for {race?.Name}");
            DropShip();
        }
    }

    // the race keeps no ship until another race is shown, so a failure is not retried every frame
    void DropShip()
    {
        if (Ship != null)
            Scene?.ObjectManager.Remove(Ship);
        Ship = null;
    }

    // the mesh bounds are boxes around bounding spheres, so the hull is drawn at every turn into one small target to see what it covers
    void FitTurns()
    {
        const int size = 256;
        float reach = Radius * 1.75f; // a corner of the bounds lies up to 1.73 radii out
        GraphicsDevice device = ScreenManager.Instance.GraphicsDevice;
        using var target = new RenderTarget2D(device, size, size, false, SurfaceFormat.Color, DepthFormat.Depth16);
        RenderTargetBinding[] previous = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(target);
            device.Clear(Color.Transparent);
            for (int step = 0; step < 36; ++step)
                Render(step * 10f * RadMath.Deg1AsRads, 0f, 0f, reach, reach);
        }
        finally
        {
            device.SetRenderTargets(previous);
        }

        var pixels = new Color[size * size];
        target.GetData(pixels);
        int minX = size, maxX = -1, minY = size, maxY = -1;
        for (int i = 0; i < pixels.Length; ++i)
        {
            if (pixels[i].PackedValue == 0)
                continue;
            int x = i % size, y = i / size;
            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
        }

        if (maxX < 0)
        {
            CoverX = CoverY = 0f;
            HalfWidth = HalfHeight = reach;
            return;
        }
        float unit = 2f * reach / size;
        CoverX = ((minX + maxX + 1) * 0.5f - size * 0.5f) * unit;
        CoverY = (size * 0.5f - (minY + maxY + 1) * 0.5f) * unit;
        HalfWidth = (maxX - minX + 1) * 0.5f * unit;
        HalfHeight = (maxY - minY + 1) * 0.5f * unit;
    }

    void Render(float turn, float centerX, float centerY, float halfWidth, float halfHeight)
    {
        Ship.World = ShipWorld(turn);
        float distance = Radius * 2f + 10f;
        XnaMatrix view = XnaMatrix.CreateLookAt(new XnaVector3(centerX, centerY, distance), new XnaVector3(centerX, centerY, 0f), XnaVector3.Up);
        XnaMatrix projection = XnaMatrix.CreateOrthographic(halfWidth * 2f, halfHeight * 2f, 1f, distance * 2f);
        State.BeginFrameRendering(ref view, ref projection, 0f, null, false);
        Scene.BeginFrameRendering(State);
        Scene.RenderScene();
    }

    // a positive turn swings the hull's near side to the right, as a drag to the right does
    XnaMatrix ShipWorld(float turn)
        => XnaMatrix.CreateTranslation(-ShipCenter) * XnaMatrix.CreateRotationZ(-turn) * Pose;

    public override bool HandleInput(InputState input)
    {
        if (!Shown)
        {
            Dragging = false;
            return false;
        }

        if (Dragging)
        {
            Dragging = input.LeftMouseDown;
            Turn += (input.CursorPosition.X - DragX) * TurnPerPixel;
            DragX = input.CursorPosition.X;
            return true;
        }

        // a right click falls through to the picker, which closes on it
        if (Ship == null || !HitTest(input.CursorPosition) || input.RightMouseClick)
            return false;

        if (input.LeftMouseClick)
        {
            Dragging = true;
            DragX = input.CursorPosition.X;
        }
        return true; // the picker closes on a click outside it, and the panel belongs to it
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Shown || Ship == null || Scene == null)
            return;

        Frame.Draw(batch, elapsed);
        batch.SafeEnd();
        try
        {
            DrawShip(ShipArea);
        }
        finally
        {
            batch.SafeBegin();
        }
        if (Ship == null)
            return; // the draw failed, and the panel goes from the next frame

        var hintPos = new Vector2(Rect.Right - 22 - Fonts.Arial12.TextWidth(Hint), Rect.Bottom - 30);
        batch.DrawString(Fonts.Arial12, Hint, hintPos, Color.DarkGray);
    }

    Rectangle ShipArea => Rect.Bevel(-14);

    // draws the hull into the area of the current render target, over what is already there
    void DrawShip(in Rectangle area)
    {
        GraphicsDevice device = ScreenManager.Instance.GraphicsDevice;
        Viewport screen = device.Viewport;
        int x = Math.Max(area.X, 0), y = Math.Max(area.Y, 0);
        int w = Math.Min(area.Right, screen.Width) - x, h = Math.Min(area.Bottom, screen.Height) - y;
        if (w < 16 || h < 16)
            return;

        try
        {
            device.Viewport = new Viewport(x, y, w, h);
            device.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0); // the menu's 3D scene left its depths behind

            Ship.HullTint = Empire.HullTintOf(ShownRace.HullColors, Picker.CurrentColor, ColorWorn());
            float halfHeight = Math.Max(HalfHeight, HalfWidth * h / w) * Margin;
            Render(Turn, CoverX, CoverY, halfHeight * w / h, halfHeight);
        }
        catch (Exception e)
        {
            Log.Error(e, $"ShipColorPreview could not draw the ship of {ShownRace?.Name}");
            DropShip();
        }
        finally
        {
            device.Viewport = screen;
        }
    }
}
