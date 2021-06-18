#include "Simulation.h"

///////////////////////////////////////////////////////////////////////////////

Simulation::Simulation(SimParams p) : params{p}
{
    createObjectsIfNeeded();
    opt.SearchRect = {};
    opt.MaxResults = 2048;
    setLoyaltyFilterType(opt, filter_type, params.loyaltyA, params.loyaltyB);
    opt.DebugId = 1;
    searchResults.resize(opt.MaxResults);
}

///////////////////////////////////////////////////////////////////////////////

void Simulation::getDrawList()
{
    DrawList = ImGui::GetWindowDrawList();
    //DrawList->Flags &= ~ImDrawListFlags_AntiAliasedLines;
    DrawList->Flags |= ImDrawListFlags_AntiAliasedLinesUseTex;
}

void Simulation::updateWindow()
{
    ImVec2 winPos = ImGui::GetWindowPos();
    window_size = ImGui::GetWindowSize();
    window_center = ImVec2{ winPos.x + window_size.x * 0.5f,
        winPos.y + window_size.y * 0.5f };
}

void Simulation::updateCameraFrustum()
{
    ImVec2 topLeft = screenToWorld(0, 0);
    ImVec2 botRight = screenToWorld(window_size.x, window_size.y);
    camera_frustum.x1 = (int)topLeft.x;
    camera_frustum.y1 = (int)topLeft.y;
    camera_frustum.x2 = (int)botRight.x;
    camera_frustum.y2 = (int)botRight.y;
}

void Simulation::moveCamera(ImVec2 delta)
{
    camera_world.x += delta.x / camera_zoom;
    camera_world.y += delta.y / camera_zoom;
}

///////////////////////////////////////////////////////////////////////////////

void Simulation::drawRect(spatial::Rect r, Color c)
{
    ImVec2 tl = worldToScreen(r.x1, r.y1);
    ImVec2 br = worldToScreen(r.x2, r.y2);
    ImVec2 points[4] = { tl, ImVec2{ br.x, tl.y }, br, ImVec2{ tl.x, br.y } };
    DrawList->AddPolyline(points, 4, getColor(c), true, 1.0f);
}

void Simulation::drawCircle(spatial::Circle ci, Color c)
{
    DrawList->AddCircle(worldToScreen(ci.x, ci.y), worldToScreen(ci.radius), getColor(c));
}

void Simulation::drawLine(spatial::Point a, spatial::Point b, Color c)
{
    ImVec2 points[2] = { worldToScreen(a.x, a.y), worldToScreen(b.x, b.y) };
    DrawList->AddPolyline(points, 2, getColor(c), false, 1.0f);
}

inline void Simulation::drawText(spatial::Point p, int size, const char* text, Color c)
{
    float screenSize = worldToScreen(size);
    if (screenSize > 200)
        DrawList->AddText(worldToScreen(p.x, p.y), getColor(c), text);
}

///////////////////////////////////////////////////////////////////////////////

void Simulation::createObjectsIfNeeded()
{
    int expectedSize = params.numObjects;
    if (params.spawnProjectiles)
        expectedSize += params.numObjects;

    if (spat == nullptr || spat->type() != params.type || objects.size() != expectedSize)
    {
        SpatialWithObjects swo = createSpatialWithObjects(params.type, params);
        spat = swo.spatial;
        objects = std::move(swo.objects);
    }
}

void Simulation::updateObjectPositions(float timeStep)
{
    float universeLo = spat->worldSize() * -0.5f;
    float universeHi = spat->worldSize() * +0.5f;
    for (MyGameObject& o : objects)
    {
        if (o.pos.x < universeLo || o.pos.x > universeHi)
            o.vel.x = -o.vel.x;

        if (o.pos.y < universeLo || o.pos.y > universeHi)
            o.vel.y = -o.vel.y;

        o.pos += o.vel * timeStep;

        auto rect = spatial::Rect::fromPointRadius((int)o.pos.x, (int)o.pos.y, (int)o.radius);
        spat->update(o.spatialId, rect);
    }
}

void Simulation::collide(int objectA, int objectB)
{
    MyGameObject& a = objects[objectA];
    MyGameObject& b = objects[objectB];

    // impulse calculation
    // https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331
    rpp::Vector2 collisionNormal = (b.pos - a.pos).normalized();
    rpp::Vector2 relativeVelocity = b.vel - a.vel;
    float velAlongNormal = relativeVelocity.dot(collisionNormal);
    if (velAlongNormal < 0)
    {
        float restitution = 1.0f; // perfect rigidity, all energy conserved

                                  // calculate impulse scalar
        float invMassA = 1.0f / a.mass;
        float invMassB = 1.0f / b.mass;
        float j = -(1 + restitution) * velAlongNormal;
        j /= invMassA + invMassB;

        // apply impulse
        rpp::Vector2 impulse = j * collisionNormal;
        a.vel -= invMassA * impulse;
        b.vel += invMassB * impulse;
    }
}

///////////////////////////////////////////////////////////////////////////////

void Simulation::update(float timeStep)
{
    createObjectsIfNeeded();

    if (isPaused)
        return;

    updateObjectPositions(timeStep);

    rpp::Timer t1;
    spat->rebuild();
    rebuildMs = t1.elapsed_ms();

    rpp::Timer t2;

    spatial::CollisionParams cp;
    cp.showCollisions = true;
    cp.ignoreSameLoyalty = true;

    spatial::Array<spatial::CollisionPair> collisions = spat->collideAll(spat->root(), cp);
    for (spatial::CollisionPair collision : collisions)
    {
        collide(collision.a, collision.b);
    }

    collideMs = t2.elapsed_ms();
}

void Simulation::findObjects()
{
    spatial::Spatial& spat = *this->spat;

    if (!wasMouseDragging)
    {
        mouseDragStart = screenToWorld(ImGui::GetMousePos());
    }

    ImVec2 end = screenToWorld(ImGui::GetMousePos());
    opt.SearchRect = { (int)mouseDragStart.x, (int)mouseDragStart.y, (int)end.x, (int)end.y };
    opt.SearchRect = opt.SearchRect.normalized();
    if (useRadialFilter)
    {
        int radius = std::max(opt.SearchRect.width(), opt.SearchRect.height()) / 2;
        opt.RadialFilter = { opt.SearchRect.centerX(), opt.SearchRect.centerY(), radius };
        //opt.RadialFilter = { opt.SearchRect.left, opt.SearchRect.centerY(), radius };
    }
    else
    {
        opt.RadialFilter = spatial::Circle::Zero();
    }

    if (opt.SearchRect.width() > 200'000)
        printf("break");
    
    rpp::Timer t;
    numSearchResults = spat.findNearby(spat.root(), searchResults.data(), opt);
    findNearbyMs = t.elapsed_ms();
}

void Simulation::handleInput()
{
    if (isPressed(ImGuiKey_Escape))
        isExiting = true;

    if (isPressed(ImGuiKey_V))
    {
        params.type = static_cast<spatial::SpatialType>((int)params.type + 1);
        if (params.type >= spatial::SpatialType::MAX)
            params.type = spatial::SpatialType::Grid;
    }

    if (isPressed('R'))
        useRadialFilter = !useRadialFilter;

    if (isPressed('T'))
    {
        if (opt.Type == ObjectType_Any)  opt.Type = ObjectType_Ship;
        else if (opt.Type == ObjectType_Ship) opt.Type = ObjectType_Proj;
        else if (opt.Type == ObjectType_Proj) opt.Type = ObjectType_Any;
    }

    if (isPressed('L'))
    {
        toggleLoyaltyFilterType(filter_type);
        setLoyaltyFilterType(opt, filter_type, params.loyaltyA, params.loyaltyB);
    }

    if (ImGui::IsMouseDown(ImGuiMouseButton_Left))
        moveCamera(ImGui::GetIO().MouseDelta);

    if (isPressed(ImGuiKey_Space))
        isPaused = !isPaused;

    if (isPressed(ImGuiKey_UpArrow))
    {
        if (params.numObjects < 10000)
            params.numObjects += 2000;
        else
            params.numObjects += 10'000;
    }
    else if (isPressed(ImGuiKey_DownArrow) && params.numObjects > 2000)
    {
        if (params.numObjects <= 10000)
            params.numObjects -= 2000;
        else
            params.numObjects -= 10'000;
    }

    bool isDragging = ImGui::IsMouseDragging(ImGuiMouseButton_Right);
    if (isDragging)
        findObjects();
    wasMouseDragging = isDragging;

    spatial::Spatial& spat = *this->spat;

    if (isPressed(ImGuiKey_LeftArrow))  spat.nodeCapacity(std::max(spat.nodeCapacity() / 2, 2));
    else if (isPressed(ImGuiKey_RightArrow)) spat.nodeCapacity(std::min(spat.nodeCapacity() * 2, 256));

    if (isPressed(ImGuiKey_PageUp))   spat.smallestCellSize(std::max(spat.smallestCellSize() / 2, 256));
    else if (isPressed(ImGuiKey_PageDown)) spat.smallestCellSize(std::min(spat.smallestCellSize() * 2, 256 * 1024));

    float wheel = ImGui::GetIO().MouseWheel;
    if (wheel != 0)
        camera_zoom = wheel < 0 ? camera_zoom * 0.5f : camera_zoom * 2.0f;
}

void Simulation::draw()
{
    getDrawList();
    spatial::Spatial& spat = *this->spat;

    spatial::VisualizerOptions vo;
    vo.visibleWorldRect = camera_frustum;
    vo.nodeText = false;
    vo.objectToLeaf = false;

    rpp::Timer t1;
    spat.debugVisualize(spat.root(), vo, *this);
    double elapsedDrawMs = t1.elapsed_ms();

    if (isPaused)
    {
        ImGui::Text("Simulation Paused, press SPACE to resume");
        ImGui::Text("  Change # of Objects:   Up/Down Arrow");
        ImGui::Text("  Change Node Capacity:  Left/Right Arrow");
        ImGui::Text("  Change Cell Size:      PgUp/Down");
        ImGui::Text("  Toggle Spatial Type:   V ");
        ImGui::Text("  Toggle Radius Filter:  R");
        ImGui::Text("  Toggle Filter Type:    T  %s", toString((ObjectType)opt.Type));
        ImGui::Text("  Toggle Filter Loyalty: L  %s", toString(filter_type));
        ImGui::Text("  Use FindNearby:        RightMouse ");
    }

    const char* name = spat.name();
    int n = spat.numActive();
    ImGui::Text("%s avg %.3f ms/frame (%.1f FPS)", name, 1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);
    ImGui::Text("%s::nodeCapacity: %d", name, spat.nodeCapacity());
    ImGui::Text("%s::smallestCellSize: %d", name, spat.smallestCellSize());
    ImGui::Text("%s::memory:  %.1fKB", name, spat.totalMemory() / 1024.0f);
    ImGui::Text("%s::rebuild(%d) elapsed: %.1fms", name, n, rebuildMs);
    ImGui::Text("%s::collideAll(%d) elapsed: %.1fms  %d collisions", name, n, collideMs, numCollisions);
    ImGui::Text("%s::findNearby(%dx%d) elapsed: %.3fms  %d results",
        name, opt.SearchRect.width(), opt.SearchRect.height(), findNearbyMs, numSearchResults);
    ImGui::Text("%s::draw() elapsed: %.1fms", name, elapsedDrawMs);
    ImGui::Text("%s::total(%d) elapsed: %.1fms", name, n, collideMs + rebuildMs + elapsedDrawMs);
    ImGui::NewLine();

    for (int i = 0; i < numSearchResults && i < 30; ++i)
    {
        const MyGameObject& o = objects[searchResults[i]];
        ImGui::Text("  object %4d %s loy=%d(%s)", o.spatialId, toString(o.type), o.loyalty, 
                                                 o.loyalty == params.loyaltyA ? "us":"them");
    }
}

///////////////////////////////////////////////////////////////////////////////

void Simulation::run()
{
    DebugGfxWindow window;
    window.Run([this, &window]()
    {
        if (isExiting)
            return false;

        ImGui::SetNextWindowSize(ImVec2((float)window.width(), (float)window.height()));
        ImGui::SetNextWindowPos(ImVec2(0, 0));

        ImGui::Begin("Main", nullptr,
            ImGuiWindowFlags_NoMove | ImGuiWindowFlags_NoResize |
            ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoScrollbar |
            ImGuiWindowFlags_NoCollapse);

        updateWindow();
        handleInput();
        float fixedTimeStep = 1.0f / 60.0f;
        updateCameraFrustum();
        if (waitForSimulation)
        {
            timeSink += ImGui::GetIO().DeltaTime;
            while (timeSink >= fixedTimeStep)
            {
                timeSink -= fixedTimeStep;
                update(fixedTimeStep);
            }
        }
        else
        {
            update(fixedTimeStep);
        }
        draw();

        ImGui::End();
        return true;
    });
}









