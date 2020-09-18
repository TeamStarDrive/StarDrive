#include <quadtree/QuadTree.h>
#include <rpp/timer.h>
#include <rpp/tests.h>
#include "DebugGfxWindow.h"

TestImpl(QuadTree)
{
    TestInit(QuadTree)
    {
    }

    static std::vector<tree::QtreeObject> createObjects(int numObjects, int universeSize)
    {
        std::vector<tree::QtreeObject> objects;
        int spacing = universeSize / (int)std::sqrtf((float)numObjects);

        // universe is centered at [0,0], so Root node goes from [-half, +half)
        int half = universeSize / 2;
        int start = -half + spacing/2;
        int x = start;
        int y = start;

        for (int i = 0; i < numObjects; ++i)
        {
            tree::QtreeObject& o = objects.emplace_back(x, y, 64);
            o.Loyalty = (i % 2) == 0 ? 1 : 2;
            o.type = tree::ObjectType_Ship;
            o.objectId = i;

            x += spacing;
            if (x >= half)
            {
                x = start;
                y += spacing;
            }
        }
        return objects;
    }

    static std::vector<tree::QtreeObject> createTestSpace(tree::QuadTree& tree, int numObjects)
    {
        std::vector<tree::QtreeObject> objects = createObjects(numObjects, tree.universeSize());
        tree.insert(objects);
        tree.rebuild();
        return objects;
    }

    template<class Func>
    static void measureEachObj(const char* what, int iterations,
                               const std::vector<tree::QtreeObject>& objects, Func&& func)
    {
        rpp::Timer t;
        for (int x = 0; x < iterations; ++x)
        {
            for (const tree::QtreeObject& o : objects)
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
        printf("QuadTree %s total: %.2fms  avg: %.2fus\n", what, e, (e*1000)/iterations);
    }

    struct ImGuiQtreeVis : tree::QtreeVisualizer
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
        void drawText(int x, int y, const char* text, tree::QtreeColor c) override
        {
            ImDrawList* draw = ImGui::GetWindowDrawList();
            draw->AddText(getPoint(x, y), getColor(c), text);
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

    static void visualizeTree(tree::QuadTree& tree)
    {
        visualizeTree([&](ImGuiQtreeVis& vis)
        {
            tree.debugVisualize(vis.camera_frustum, vis);
        });
    }

    const int UNIVERSE_SIZE = 500'000;
    const int SMALLEST_SIZE = 32;
    const int NUM_OBJECTS = 10'000;
    const int DEFAULT_SENSOR_RANGE = 30000;

    TestCase(update_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::QtreeObject> objects = createTestSpace(tree, NUM_OBJECTS);

        measureIterations("Qtree.updateAll", 1000, [&]()
        {
            tree.rebuild();
        });
    }

    TestCase(search_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::QtreeObject> objects = createTestSpace(tree, NUM_OBJECTS);

        visualizeTree(tree);

        std::vector<int> results(1024, 0);

        measureEachObj("findNearby", 200, objects, [&](const tree::QtreeObject& o)
        {
            tree::SearchOptions opt;
            opt.OriginX = o.x;
            opt.OriginY = o.y;
            opt.SearchRadius = DEFAULT_SENSOR_RANGE;
            opt.MaxResults = 1024;
            opt.FilterExcludeObjectId = o.objectId;
            opt.FilterExcludeByLoyalty = o.Loyalty;
            int n = tree.findNearby(results.data(), opt);
        });
    }

    TestCase(collision_perf)
    {
        tree::QuadTree tree { UNIVERSE_SIZE, SMALLEST_SIZE };
        std::vector<tree::QtreeObject> objects = createTestSpace(tree, NUM_OBJECTS);

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
