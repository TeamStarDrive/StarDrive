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

        float getSize(int v) const { return v * camera_zoom; }
        ImVec2 getPoint(int x, int y) const // get screen point from world point
        {
            return ImVec2{window_center.x + (camera_world.x + x)*camera_zoom,
                          window_center.y + (camera_world.y + y)*camera_zoom};
        }
        void updateCameraFrustum()
        {
            camera_frustum.left = int( (0 - window_center.x)/camera_zoom - camera_world.x );
            camera_frustum.top  = int( (0 - window_center.y)/camera_zoom - camera_world.y );
            camera_frustum.right  = int( (window_size.x - window_center.x)/camera_zoom - camera_world.x );
            camera_frustum.bottom = int( (window_size.y - window_center.y)/camera_zoom - camera_world.y );
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
            draw->AddRect(getPoint(x1, y1), getPoint(x2, y2), getColor(c));
        }
        void drawCircle(int x, int y, int radius, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddCircle(getPoint(x, y), getSize(radius), getColor(c));
        }
        void drawLine(int x1, int y1, int x2, int y2, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddLine(getPoint(x1, y1), getPoint(x2, y2), getColor(c));
        }
        void drawText(int x, int y, int size, const char* text, tree::QtreeColor c) override
        {
            float screenSize = getSize(size);
            if (screenSize > 200)
            {
                ImDrawList* draw = ImGui::GetWindowDrawList();
                draw->AddText(getPoint(x, y), getColor(c), text);
            }
        }
    };

    inline void show(float zoom, std::function<void(ImGuiQtreeVis&)>&& draw)
    {
        ImGuiQtreeVis vis;
        vis.camera_zoom = zoom;

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

            float wheel = ImGui::GetIO().MouseWheel;
            if (wheel != 0)
            {
                vis.camera_zoom = wheel < 0 ? vis.camera_zoom * 0.5f : vis.camera_zoom * 2.0f;
            }

            vis.updateCameraFrustum();
            draw(vis);
            ImGui::Text("Qtree avg %.3f ms/frame (%.1f FPS)",
                        1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);
            ImGui::End();
            return true;
        });
    }

    inline void show(tree::QuadTree& tree, float zoom = 0.001f)
    {
        show(zoom, [&](ImGuiQtreeVis& vis)
        {
            tree.debugVisualize(vis.camera_frustum, vis);
        });
    }
}
