#include "ImGuiQtreeVis.h"
#include "DebugGfxWindow.h"
#include <algorithm>
#include <rpp/timer.h>

namespace tree::vis
{
    static ImU32 getColor(QtreeColor c)
    {
        ImVec4 cv { c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, c.a / 255.0f };
        return ImGui::ColorConvertFloat4ToU32(cv);
    }

    static const ImU32 Yellow = getColor({ 255, 255,   0, 255 });
    static const ImU32 Cyan   = getColor({   0, 255, 255, 255 });

    struct VisualizationState final : QtreeVisualizer
    {
        SimContext& context;
        QuadTree& tree;
        SearchOptions opt;
        std::vector<int> searchResults;
        int numSearchResults = 0;
        float timeSink = 0.0f;
        int leafSplitThreshold = QuadDefaultLeafSplitThreshold;

        int KEY_SPACE = 0;
        int KEY_UP_ARROW = 0;
        int KEY_DOWN_ARROW = 0;
        int KEY_LEFT_ARROW = 0;
        int KEY_RIGHT_ARROW = 0;

        explicit VisualizationState(SimContext& context) : context{context}, tree{context.tree}
        {
            camera_zoom = context.zoom;
            opt.SearchRadius = 0;
            opt.MaxResults = 2048;
            searchResults.resize(opt.MaxResults);
        }

        float camera_zoom = 1.0f;
        ImVec2 camera_world = { 0.0f, 0.0f };
        ImVec2 window_center = { 400.0f, 400.0f };
        ImVec2 window_size = { 800.0f, 800.0f };
        QtreeRect camera_frustum { 0, 0, 0, 0 };

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

        static bool isKeyPressed(int key) { return ImGui::IsKeyPressed(key); }

        void updateCameraFrustum()
        {
            ImVec2 topLeft = screenToWorld(0, 0);
            ImVec2 botRight = screenToWorld(window_size.x, window_size.y);
            camera_frustum.left = (int)topLeft.x;
            camera_frustum.top  = (int)topLeft.y;
            camera_frustum.right  = (int)botRight.x;
            camera_frustum.bottom = (int)botRight.y;
        }
        void moveCamera(ImVec2 delta)
        {
            camera_world.x += delta.x / camera_zoom;
            camera_world.y += delta.y / camera_zoom;
        }

        void drawRect(int x1, int y1, int x2, int y2, QtreeColor c) override
        {
            DrawList->AddRect(worldToScreen(x1, y1), worldToScreen(x2, y2), getColor(c));
        }
        void drawCircle(int x, int y, int radius, QtreeColor c) override
        {
            DrawList->AddCircle(worldToScreen(x, y), worldToScreen(radius), getColor(c));
        }
        void drawLine(int x1, int y1, int x2, int y2, QtreeColor c) override
        {
            DrawList->AddLine(worldToScreen(x1, y1), worldToScreen(x2, y2), getColor(c));
        }
        void drawText(int x, int y, int size, const char* text, QtreeColor c) override
        {
            float screenSize = worldToScreen(size);
            if (screenSize > 200)
                DrawList->AddText(worldToScreen(x, y), getColor(c), text);
        }

        void updateWindow()
        {
            if (KEY_SPACE == 0)
            {
                KEY_SPACE = ImGui::GetKeyIndex(ImGuiKey_Space);
                KEY_UP_ARROW = ImGui::GetKeyIndex(ImGuiKey_UpArrow);
                KEY_DOWN_ARROW = ImGui::GetKeyIndex(ImGuiKey_DownArrow);
                KEY_LEFT_ARROW = ImGui::GetKeyIndex(ImGuiKey_LeftArrow);
                KEY_RIGHT_ARROW = ImGui::GetKeyIndex(ImGuiKey_RightArrow);
            }

            ImVec2 winPos = ImGui::GetWindowPos();
            window_size = ImGui::GetWindowSize();
            window_center = ImVec2{ winPos.x + window_size.x * 0.5f,
                                    winPos.y + window_size.y * 0.5f };
        }

        void handleInput()
        {
            if (ImGui::IsMouseDown(ImGuiMouseButton_Left))
                moveCamera(ImGui::GetIO().MouseDelta);

            if (isKeyPressed(KEY_SPACE))
                context.isPaused = !context.isPaused;

            if (isKeyPressed(KEY_UP_ARROW))
            {
                if (context.totalObjects < 10000)
                    context.totalObjects += 2000;
                else
                    context.totalObjects += 10'000;
            }
            else if (isKeyPressed(KEY_DOWN_ARROW) && context.totalObjects > 2000)
            {
                if (context.totalObjects <= 10000)
                    context.totalObjects -= 2000;
                else
                    context.totalObjects -= 10'000;
            }

            if (ImGui::IsMouseDragging(ImGuiMouseButton_Right))
            {
                ImVec2 delta = ImGui::GetMouseDragDelta(ImGuiMouseButton_Right);
                ImVec2 start = ImGui::GetMousePos();
                ImVec2 world = screenToWorld(start.x - delta.x, start.y - delta.y);
                opt.OriginX = (int)world.x;
                opt.OriginY = (int)world.y;
                opt.SearchRadius = (int)screenToWorld(sqrtf(delta.x * delta.x + delta.y * delta.y));
                rpp::Timer t;
                numSearchResults = tree.findNearby(searchResults.data(), opt);
                context.findNearbyMs = t.elapsed_ms();
            }

            if      (isKeyPressed(KEY_LEFT_ARROW))  leafSplitThreshold = std::max(leafSplitThreshold / 2, 2);
            else if (isKeyPressed(KEY_RIGHT_ARROW)) leafSplitThreshold = std::min(leafSplitThreshold * 2, 256);
            tree.setLeafSplitThreshold(leafSplitThreshold);

            float wheel = ImGui::GetIO().MouseWheel;
            if (wheel != 0)
                camera_zoom = wheel < 0 ? camera_zoom * 0.5f : camera_zoom * 2.0f;
        }

        void update()
        {
            float fixedTimeStep = 1.0f / 60.0f;
            updateCameraFrustum();

            timeSink += ImGui::GetIO().DeltaTime;
            while (timeSink >= fixedTimeStep)
            {
                timeSink -= fixedTimeStep;
                context.update(fixedTimeStep);
            }
        }

        ImDrawList* DrawList = nullptr;

        void draw()
        {
            DrawList = ImGui::GetWindowDrawList();

            QtreeVisualizerOptions vo;
            vo.visibleWorldRect = camera_frustum;
            vo.nodeText = false;
            vo.objectToLeafLines = false;
            tree.debugVisualize(vo, *this);
            
            if (opt.SearchRadius > 1)
            {
                DrawList = ImGui::GetWindowDrawList();
                DrawList->AddCircle(worldToScreen(opt.OriginX, opt.OriginY), worldToScreen(opt.SearchRadius), Yellow, 0, 2.0f);

                for (int i = 0; i < numSearchResults; ++i)
                {
                    const QtreeObject& o = tree.get(searchResults[i]);
                    DrawList->AddRect(worldToScreen(o.left(), o.top()), worldToScreen(o.right(), o.bottom()), Yellow);
                }
            }

            if (context.numCollisions > 0)
            {
                DrawList = ImGui::GetWindowDrawList();
                for (int objectId : context.collidedObjects)
                {
                    const QtreeObject& o = tree.get(objectId);
                    DrawList->AddRect(worldToScreen(o.left(), o.top()), worldToScreen(o.right(), o.bottom()), Cyan);
                }
            }

            if (context.isPaused)
                ImGui::Text("Simulation Paused, press Space to resume");

            ImGui::Text("Qtree avg %.3f ms/frame (%.1f FPS)", 1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);
            ImGui::Text("Qtree::leafSize: %d", leafSplitThreshold);
            ImGui::Text("Qtree::rebuild(%d) elapsed: %.1fms", tree.count(), context.rebuildMs);
            ImGui::Text("Qtree::collideAll(%d) elapsed: %.1fms  %d collisions", tree.count(), context.collideMs, context.numCollisions);
            ImGui::Text("Qtree::findNearby(radius=%d) elapsed: %.3fms  %d results", opt.SearchRadius, context.findNearbyMs, numSearchResults);
            ImGui::Text("Qtree::total(%d) elapsed: %.1fms", tree.count(), context.collideMs+context.rebuildMs);
        }
    };

    void show(SimContext& context)
    {
        DebugGfxWindow window;
        VisualizationState state { context };
        window.Run([&]()
        {
            ImGui::SetNextWindowSize(ImVec2((float)window.width(), (float)window.height()));
            ImGui::SetNextWindowPos(ImVec2(0, 0));

            ImGui::Begin("Main", nullptr,
                ImGuiWindowFlags_NoMove | ImGuiWindowFlags_NoResize |
                ImGuiWindowFlags_NoTitleBar | ImGuiWindowFlags_NoScrollbar |
                ImGuiWindowFlags_NoCollapse);

            state.updateWindow();
            state.handleInput();
            state.update();
            state.draw();

            ImGui::End();
            return true;
        });
    }
}