#include <quadtree/QuadTree.h>
#include <rpp/timer.h>
#include <rpp/tests.h>
#include "DebugGfxWindow.h"

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    static std::vector<tree::SpatialObj> createObjects(int numObjects, float universeSize)
    {
        std::vector<tree::SpatialObj> objects;
        float spacing = universeSize / std::sqrtf((float)numObjects);

        // universe is centered at [0,0], so Root node goes from [-half, +half)
        float half = universeSize / 2;
        float start = -half + spacing/2;
        float x = start;
        float y = start;

        for (int i = 0; i < numObjects; ++i)
        {
            tree::SpatialObj& o = objects.emplace_back(x, y, 64.0f);
            o.Loyalty = (i % 2) == 0 ? 1 : 2;
            o.Type = 1;
            o.ObjectId = i;

            x += spacing;
            if (x >= half)
            {
                x = start;
                y += spacing;
            }
        }
        return objects;
    }

    static std::vector<tree::SpatialObj> createTestSpace(tree::QuadTree& tree, int numObjects)
    {
        std::vector<tree::SpatialObj> objects = createObjects(numObjects, tree.universeSize());
        tree.updateAll(objects);
        return objects;
    }

    template<class Func>
    static void measureEachObj(const char* what, int iterations,
                               const std::vector<tree::SpatialObj>& objects, Func&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x)
        {
            for (const tree::SpatialObj& o : objects)
            {
                func(o);
            }
        }
        double e = t.elapsed_ms();
        int total_operations = objects.size() * iterations;
        printf("QuadTree %s total: %.2fms  avg: %.2fus\n", what, e, (e / total_operations)*1000);
    }

    template<class VoidFunc>
    static void measureIterations(const char* what, int iterations, VoidFunc&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x)
        {
            func();
        }
        double e = t.elapsed_ms();
        printf("QuadTree %s total: %.2fms  avg: %.2fus\n", what, e, (e / iterations)*1000);
    }

    struct ImGuiQtreeVis : tree::QtreeVisualizer
    {
        float camera_zoom = 1.0f;
        ImVec2 camera_world = { 0.0f, 0.0f };
        ImVec2 window_center = { 400.0f, 400.0f };
        ImVec2 window_size = { 800.0f, 800.0f };

        float getSize(float v) const { return v * camera_zoom; }
        ImVec2 getPoint(float x, float y) const
        {
            return ImVec2{window_center.x + (camera_world.x + x)*camera_zoom,
                          window_center.y + (camera_world.y + y)*camera_zoom};
        }
        void move_camera(ImVec2 delta)
        {
            camera_world.x += delta.x / camera_zoom;
            camera_world.y += delta.y / camera_zoom;
        }
        static ImU32 getColor(const float color[4])
        {
            ImVec4 c { color[0] / 255.0f, color[1] / 255.0f, color[2] / 255.0f, color[3] / 255.0f };
            return ImGui::ColorConvertFloat4ToU32(c);
        }
        void drawRect(float x1, float y1, float x2, float y2, const float color[4]) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddRect(getPoint(x1, y1), getPoint(x2, y2), getColor(color));
        }
        void drawCircle(float x, float y, float radius, const float color[4]) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddCircle(getPoint(x, y), getSize(radius), getColor(color));
        }
        void drawLine(float x1, float y1, float x2, float y2, const float color[4]) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddLine(getPoint(x1, y1), getPoint(x2, y2), getColor(color));
        }
        void drawText(float x, float y, const char* text, const float color[4]) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddText(getPoint(x, y), getColor(color), text);
        }
        bool isVisible(float x1, float y1, float x2, float y2) const override
        {
            ImVec2 p1 = getPoint(x1, y1);
            ImVec2 p2 = getPoint(x2, y2);
            // does rectangle [p1, p2] overlap the screen?
            return p1.x <= window_size.x && p2.x > 0.0f
                && p1.y <= window_size.y && p2.y > 0.0f;
        }
    };

    static void visualizeTree(std::function<void(ImGuiQtreeVis&)>&& draw)
    {
        ImGuiQtreeVis vis;
        vis.camera_zoom = 0.001f;

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
                vis.move_camera(ImGui::GetIO().MouseDelta);
            }

            float wheel = ImGui::GetIO().MouseWheel;
            if (wheel != 0)
            {
                vis.camera_zoom = wheel < 0 ? vis.camera_zoom * 0.5f : vis.camera_zoom * 2.0f;
            }

            draw(vis);
            ImGui::Text("Qtree avg %.3f ms/frame (%.1f FPS)",
                        1000.0f / ImGui::GetIO().Framerate, ImGui::GetIO().Framerate);
            ImGui::End();
            return true;
        });
    }

    static void visualizeTree(tree::QuadTree& tree)
    {
        visualizeTree([&](ImGuiQtreeVis& vis) { tree.debugVisualize(vis); });
    }

    const float UNIVERSE_SIZE = 500'000;
    const float SMALLEST_SIZE = 32;
    const int NUM_OBJECTS = 10'000;

    TestCase(update_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::SpatialObj> objects = createTestSpace(tree, NUM_OBJECTS);

        measureIterations("Qtree.updateAll", 1000, [&]()
        {
            tree.updateAll(objects);
        });
    }

    TestCase(search_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::SpatialObj> objects = createTestSpace(tree, NUM_OBJECTS);

        //visualizeTree(tree);

        const float defaultSensorRange = 30000;
        std::vector<int> results(1024, 0);

        measureEachObj("findNearby", 200, objects, [&](const tree::SpatialObj& o)
        {
            tree::SearchOptions opt;
            opt.OriginX = o.CX;
            opt.OriginY = o.CY;
            opt.SearchRadius = defaultSensorRange;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.ObjectId;
            opt.FilterExcludeByLoyalty = o.Loyalty;
            int n = tree.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::SpatialObj> objects = createTestSpace(tree, NUM_OBJECTS);

        const float defaultSensorRange = 30000;
        const int iterations = 200;
        std::vector<int> results(1024, 0);

        measureIterations("collideAll", 100, [&]()
        {
            tree.collideAll(1.0f/60.0f, [](int objectA, int objectB)->int
            {
                return 0;
            });
        });
    }
};
