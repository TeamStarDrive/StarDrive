#pragma once
#include "SimParams.h"
#include "SpatialSimUtils.h"
#include "DebugGfxWindow.h"
#include <spatial/SpatialDebug.h>
#include <rpp/timer.h>
#include <algorithm> // std::max
#include "LoyaltyFilterType.h"

using spatial::Color;

static ImU32 getColor(Color c)
{
    ImVec4 cv { c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, c.a / 255.0f };
    return ImGui::ColorConvertFloat4ToU32(cv);
}


struct Simulation final : spatial::Visualizer
{
    SimParams params;
    std::vector<MyGameObject> objects;
    std::shared_ptr<spatial::Spatial> spat;

    double rebuildMs = 0.0; // time spent in Qtree::rebuild()
    double collideMs = 0.0; // time spent in Qtree::collideAll()
    double findNearbyMs = 0.0; // time spent in Qtree::findNearby()
    int numCollisions = 0;

    bool isPaused = true;
    bool isExiting = false;
    bool useRadialFilter = true;
    bool wasMouseDragging = false;
    ImVec2 mouseDragStart {};

    spatial::SearchOptions opt;
    std::vector<int> searchResults;
    int numSearchResults = 0;

    // if TRUE, we wait for simulation to catch up if it's lagging
    bool waitForSimulation = false;
    float timeSink = 0.0f;

    float camera_zoom = 0.0001f;
    ImVec2 camera_world = { 0.0f, 0.0f };
    ImVec2 window_center = { 400.0f, 400.0f };
    ImVec2 window_size = { 800.0f, 800.0f };
    spatial::Rect camera_frustum { 0, 0, 0, 0 };

    LoyaltyFilterType filter_type = LoyaltyFilterType::All;

    ///////////////////////////////////////////////////////////////////////////////

    explicit Simulation(SimParams p);

    ///////////////////////////////////////////////////////////////////////////////

    ImDrawList* DrawList = nullptr;
    void getDrawList();
    void updateWindow();

    static bool isPressed(int key) { return ImGui::IsKeyPressed(ImGui::GetKeyIndex(key)); }
    static bool isPressed(char key) { return ImGui::IsKeyPressed(key); }

    ///////////////////////////////////////////////////////////////////////////////

    float worldToScreen(int worldSize)    const { return worldSize * camera_zoom; }
    float screenToWorld(float screenSize) const { return screenSize/camera_zoom; }
    ImVec2 worldToScreen(int worldX, int worldY) const // get screen point from world point
    {
        return ImVec2{window_center.x + (camera_world.x + worldX)*camera_zoom,
                      window_center.y + (camera_world.y + worldY)*camera_zoom};
    }
    ImVec2 screenToWorld(float screenX, float screenY) const
    {
        return { (screenX - window_center.x)/camera_zoom - camera_world.x,
                 (screenY - window_center.y)/camera_zoom - camera_world.y };
    }
    ImVec2 screenToWorld(ImVec2 screenXY) const { return screenToWorld(screenXY.x, screenXY.y); }
    void updateCameraFrustum();
    void moveCamera(ImVec2 delta);

    ///////////////////////////////////////////////////////////////////////////////

    void drawRect(spatial::Rect r, Color c) override;
    void drawCircle(spatial::Circle ci, Color c) override;
    void drawLine(spatial::Point a, spatial::Point b, Color c) override;
    void drawText(spatial::Point p, int size, const char* text, Color c) override;

    ///////////////////////////////////////////////////////////////////////////////
    
    void createObjectsIfNeeded();
    void updateObjectPositions(float timeStep);
    void collide(int objectA, int objectB);

    ///////////////////////////////////////////////////////////////////////////////
    
    void update(float timeStep);
    void findObjects();
    void handleInput();
    void draw();

    ///////////////////////////////////////////////////////////////////////////////

    void run();
};
