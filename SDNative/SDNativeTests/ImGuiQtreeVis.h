#pragma once
#include <quadtree/QuadTree.h>
#include "DebugGfxWindow.h"

namespace tree::vis
{
    struct ImGuiQtreeVis final : tree::QtreeVisualizer
    {
        float camera_zoom = 1.0f;
        ImVec2 camera_world = { 0.0f, 0.0f };
        ImVec2 window_center = { 400.0f, 400.0f };
        ImVec2 window_size = { 800.0f, 800.0f };

        tree::QtreeRect camera_frustum { 0, 0, 0, 0 };

        float worldToScreen(int worldSize) const { return worldSize * camera_zoom; }
        ImVec2 worldToScreen(int worldX, int worldY) const // get screen point from world point
        {
            return ImVec2{window_center.x + (camera_world.x + worldX)*camera_zoom,
                          window_center.y + (camera_world.y + worldY)*camera_zoom};
        }

        float screenToWorld(float screenSize) const
        {
            return screenSize/camera_zoom;
        }
        ImVec2 screenToWorld(float screenX, float screenY) const
        {
            return { (screenX - window_center.x)/camera_zoom - camera_world.x,
                     (screenY - window_center.y)/camera_zoom - camera_world.y };
        }

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
        static ImU32 getColor(tree::QtreeColor c)
        {
            ImVec4 cv { c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, c.a / 255.0f };
            return ImGui::ColorConvertFloat4ToU32(cv);
        }
        void drawRect(int x1, int y1, int x2, int y2, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddRect(worldToScreen(x1, y1), worldToScreen(x2, y2), getColor(c));
        }
        void drawCircle(int x, int y, int radius, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddCircle(worldToScreen(x, y), worldToScreen(radius), getColor(c));
        }
        void drawLine(int x1, int y1, int x2, int y2, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddLine(worldToScreen(x1, y1), worldToScreen(x2, y2), getColor(c));
        }
        void drawText(int x, int y, int size, const char* text, tree::QtreeColor c) override
        {
            float screenSize = worldToScreen(size);
            if (screenSize > 200)
            {
                ImDrawList* draw = ImGui::GetWindowDrawList();
                draw->AddText(worldToScreen(x, y), getColor(c), text);
            }
        }
    };

    inline void show(float zoom, tree::QuadTree& tree)
    {
        ImGuiQtreeVis vis;
        vis.camera_zoom = zoom;

        tree::SearchOptions opt;
        opt.OriginX = 0;
        opt.OriginY = 0;
        opt.SearchRadius = 0;
        opt.MaxResults = 2048;
        std::vector<int> searchResults(opt.MaxResults);
        int numResults = 0;
        double find_elapsed_ms = 0.0;
        float timeSink = 0.0f;
        float timeStep = 1.0f / 60.0f;

        float universeLo = tree.universeSize() * -0.5f;
        float universeHi = tree.universeSize() * 0.5f;

        for (int i = 0; i < tree.count(); ++i)
        {
            tree::QtreeObject& o = const_cast<tree::QtreeObject&>( tree.get(i) );
            o.vx = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 200000.0f;
            o.vy = ((rand() / (float)RAND_MAX) - 0.5f) * 2.0f * 200000.0f;
        }

        DebugGfxWindow window;
        window.Run([&]()
        {
            ImGui::SetNextWindowSize(ImVec2((float)window.width(), (float)window.height()));
            ImGui::SetNextWindowPos(ImVec2(0, 0));

            ImGui::Begin("Main", nullptr, 
                ImGuiWindowFlags_NoMove|ImGuiWindowFlags_NoResize|
                ImGuiWindowFlags_NoTitleBar|ImGuiWindowFlags_NoScrollbar|
                ImGuiWindowFlags_NoCollapse);

            ImVec2 winPos = ImGui::GetWindowPos();
            vis.window_size = ImGui::GetWindowSize();
            vis.window_center = ImVec2{winPos.x + vis.window_size.x*0.5f,
                                       winPos.y + vis.window_size.y*0.5f};

            if (ImGui::IsMouseDown(ImGuiMouseButton_Left))
            {
                vis.moveCamera(ImGui::GetIO().MouseDelta);
            }

            if (ImGui::IsMouseDragging( ImGuiMouseButton_Right ))
            {
                ImVec2 delta = ImGui::GetMouseDragDelta(ImGuiMouseButton_Right);
                ImVec2 start = ImGui::GetMousePos();
                ImVec2 world = vis.screenToWorld(start.x - delta.x, start.y - delta.y);
                opt.OriginX = (int)world.x;
                opt.OriginY = (int)world.y;
                opt.SearchRadius = (int)vis.screenToWorld( sqrtf(delta.x*delta.x + delta.y*delta.y) );
                rpp::Timer t;
                numResults = tree.findNearby(searchResults.data(), opt);
                find_elapsed_ms = t.elapsed_ms();
            }

            float wheel = ImGui::GetIO().MouseWheel;
            if (wheel != 0)
            {
                vis.camera_zoom = wheel < 0 ? vis.camera_zoom * 0.5f : vis.camera_zoom * 2.0f;
            }

            vis.updateCameraFrustum();

            timeSink += ImGui::GetIO().DeltaTime;
            while (timeSink >= timeStep)
            {
                timeSink -= timeStep;
                for (int i = 0; i < tree.count(); ++i)
                {
                    tree::QtreeObject& o = const_cast<tree::QtreeObject&>( tree.get(i) );

                    if (o.x < universeLo || o.x > universeHi)
                        o.vx = -o.vx;

                    if (o.y < universeLo || o.y > universeHi)
                        o.vy = -o.vy;

                    o.x += o.vx * timeStep;
                    o.y += o.vy * timeStep;


                }
            }
            tree.rebuild();
            tree.debugVisualize(vis.camera_frustum, vis);

            if (opt.SearchRadius > 1)
            {
                static const QtreeColor Yellow = { 255, 255,  0, 255 };
                ImDrawList* draw = ImGui::GetWindowDrawList();
                draw->AddCircle(vis.worldToScreen(opt.OriginX, opt.OriginY), vis.worldToScreen(opt.SearchRadius), vis.getColor(Yellow), 0, 2.0f);

                for (int i = 0; i < numResults; ++i)
                {
                    const tree::QtreeObject& o = tree.get(searchResults[i]);
                    draw->AddCircle(vis.worldToScreen(o.x, o.y), vis.worldToScreen(o.rx), vis.getColor(Yellow), 8);
                }
            }

            ImGui::Text("Qtree avg %.3f ms/frame (%.1f FPS)",
                        1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);

            ImGui::Text("Qtree::findNearby(radius=%d) elapsed: %.3fms", opt.SearchRadius, find_elapsed_ms);
            ImGui::Text("     Results: %d", numResults);
            ImGui::End();
            return true;
        });
    }
}
