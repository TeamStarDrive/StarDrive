#include <spatial/Search.h>
#include <spatial/ObjectCollection.h>
#include <src/rpp/tests.h>

using spatial::Rect;
using spatial::Point;
using spatial::FoundCells;
using spatial::SpatialObject;
using spatial::ObjectCollection;

TestImpl(FindNearby)
{
    TestInit(FindNearby)
    {
    }

    static constexpr int FRIEND = 1;
    static constexpr int ENEMY = 3;
    static constexpr int ID0 = 0;
    static constexpr int ID1 = 1;
    static constexpr int ID2 = 2;
    static constexpr int ID3 = 3;

    struct Cell
    {
        Rect rect;
        int radius;
        std::vector<int> objectIds;
        std::vector<SpatialObject*> objects;
        Cell(Point c, int r) : rect{Rect::fromPointRadius(c.x, c.y, r)}, radius{r} {}
        bool tryAdd(const Rect& r, int id)
        {
            if (!rect.overlaps(r))
                return false;
            objectIds.push_back(id);
            return true;
        }
        void appendTo(FoundCells& found, ObjectCollection& objlist)
        {
            objects.clear();
            for (int id : objectIds)
                objects.push_back( (SpatialObject*)&objlist.get(id) );
            found.add(objects.data(), objects.size(), rect.center(), radius);
        }
    };

    struct World
    {
        std::shared_ptr<ObjectCollection> objects = std::make_shared<ObjectCollection>();
        std::vector<std::shared_ptr<Cell>> cells;
        World() = default;
        World(Point center, int radius) { addCell(center, radius); }
        void addCell(Point center, int radius)
        {
            cells.push_back(std::make_shared<Cell>(center, radius));
        }
        int addObject(uint8_t loyalty, uint8_t type, Point center, int radius)
        {
            Rect r = Rect::fromPointRadius(center.x, center.y, radius);
            int objectId = objects->insert(SpatialObject{loyalty, type, 0xff, 0, r});
            int added = 0;
            for (auto& cell : cells) if (cell->tryAdd(r, objectId)) ++added;
            if (added == 0) LogError("World::addShip failed: no cell overlap");
            return objectId;
        }
        int addShip(uint8_t loyalty, Point center, int radius) { return addObject(loyalty, /*ship:*/1, center, radius); }
        int addFriend(Point center, int radius) { return addShip(FRIEND, center, radius); }
        int addEnemy(Point center, int radius)  { return addShip(ENEMY, center, radius); }
        FoundCells createFoundCells()
        {
            objects->submitPending();
            FoundCells found;
            for (auto& cell : cells) cell->appendTo(found, *objects);
            return found;
        }

        std::vector<SpatialObject*> filter(FoundCells& found, const spatial::SearchOptions& opt) const
        {
            std::vector<SpatialObject*> spats;
            std::vector<int> results(opt.MaxResults);
            int* outResults = results.data();
            int count = found.filterResults(outResults, *objects, opt);
            for (int i = 0; i < count; ++i)
                spats.push_back( (SpatialObject*)&objects->get(results[i]) );
            return spats;
        }
    };

    #define AssertObjectIdAt(at, expected) do { \
        if (at < results.size()) { AssertThat(results[at]->objectId, expected); }\
        else { Assert(at < results.size()); }\
    } while (0)

    TestCase(rectangleFilter)
    {
        World w { {0,0}, 10'000 };
        // in rectangle filter, object radius is added to the overlap check
        int id0 = w.addFriend({0, 0}, 64); // inside the radius
        int id1 = w.addEnemy({0, 256}, 64); // inside the radius
        int id2 = w.addEnemy({-256, -256}, 64); // diagonal inside rect
        int id3 = w.addFriend({-256, 256}, 64); // diagonal inside rect
        int id4 = w.addFriend({-256-64, 256+64}, 64); // exactly at boundary of the overlap (+o.radius)
        int id5 = w.addFriend({-256-65, 256+65}, 64); // exactly OUTSIDE of the overlap (+o.radius)
        int id6 = w.addFriend({-512, 256}, 64); // totally outside of rect
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt;
        opt.SearchRect = Rect::fromPointRadius(0, 0, 256);
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 5u);
        AssertObjectIdAt(0, id0);
        AssertObjectIdAt(1, id1);
        AssertObjectIdAt(2, id2);
        AssertObjectIdAt(3, id3);
        AssertObjectIdAt(4, id4);
    }

    TestCase(radialFilter)
    {
        World w { {0,0}, 10'000 };
        int id0 = w.addFriend({0, 0}, 64); // inside the radius
        int id1 = w.addEnemy({0, 256}, 64); // inside the radius
        int id2 = w.addFriend({-256-64, 0}, 64); // exactly at boundary of the overlap (+o.radius)
        int id3 = w.addFriend({-256-65, 0}, 64); // exactly OUTSIDE of the overlap (+o.radius)
        int id4 = w.addEnemy({-256, -256}, 64); // diagonally OUTSIDE of radius
        int id5 = w.addFriend({-256, 256}, 64); // diagonally OUTSIDE of radius
        int id6 = w.addFriend({-512, 256}, 64); // totally OUTSIDE of radius
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt;
        opt.SearchRect = Rect::fromPointRadius(0, 0, 256);
        opt.RadialFilter = spatial::Circle{0, 0, 256};
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 3u);
        AssertObjectIdAt(0, id0);
        AssertObjectIdAt(1, id1);
        AssertObjectIdAt(2, id2);
    }

    World genericWorldWith4Ships()
    {
        World w { {0,0}, 10'000 };
        w.addFriend({0, 0}, 64);
        w.addEnemy({0, 256}, 64);
        w.addEnemy({-256, -256}, 64);
        w.addFriend({-256, 256}, 64);
        return w;
    }

    spatial::SearchOptions makeOpt(int radius)
    {
        spatial::SearchOptions opt;
        opt.SearchRect = Rect::fromPointRadius(0, 0, radius);
        opt.RadialFilter = spatial::Circle{0,0,radius};
        return opt;
    }

    TestCase(maxResults)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.MaxResults = 2;
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID0);
        AssertObjectIdAt(1, ID1);
    }

    TestCase(filterByType)
    {
        World w { {0,0}, 10'000 };
        int id0 = w.addObject(FRIEND, 1, {0, 0}, 64);
        int id1 = w.addObject(FRIEND, 4, {0, 256}, 64);
        int id2 = w.addObject(ENEMY, 1, {-256, -256}, 64);
        int id3 = w.addObject(ENEMY, 4, {-256, 256}, 64);
        FoundCells found = w.createFoundCells();
        
        spatial::SearchOptions opt = makeOpt(1000);
        opt.Type = 4;
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, id1);
        AssertObjectIdAt(1, id3);
    }

    TestCase(excludeObject)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.Exclude = ID0;
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 3u);
        AssertObjectIdAt(0, ID1);
        AssertObjectIdAt(1, ID2);
        AssertObjectIdAt(2, ID3);
    }

    TestCase(excludeLoyalty)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.ExcludeLoyalty = FRIEND;
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID1);
        AssertObjectIdAt(1, ID2);

        opt.ExcludeLoyalty = ENEMY;
        results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID0);
        AssertObjectIdAt(1, ID3);
    }

    TestCase(onlyLoyalty)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.OnlyLoyalty = FRIEND;
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID0);
        AssertObjectIdAt(1, ID3);

        opt.OnlyLoyalty = ENEMY;
        results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID1);
        AssertObjectIdAt(1, ID2);
    }

    TestCase(onlyFriendsExcludingSelf)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.Exclude = ID0;
        opt.OnlyLoyalty = FRIEND;
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 1u);
        AssertObjectIdAt(0, ID3);
    }

    TestCase(onlyEnemiesExcludingSelf)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.Exclude = ID0;
        opt.ExcludeLoyalty = FRIEND;
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID1);
        AssertObjectIdAt(1, ID2);
    }

    TestCase(onlySpecificEnemyExcludingSelf)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.Exclude = ID0;
        opt.ExcludeLoyalty = FRIEND;
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 2u);
        AssertObjectIdAt(0, ID1);
        AssertObjectIdAt(1, ID2);
    }

    TestCase(onlyPassingFilterFunc)
    {
        World w = genericWorldWith4Ships();
        FoundCells found = w.createFoundCells();

        spatial::SearchOptions opt = makeOpt(1000);
        opt.FilterFunction = [](int id) -> int { return id == ID3; };
        
        std::vector<SpatialObject*> results = w.filter(found, opt);
        AssertThat(results.size(), 1u);
        AssertObjectIdAt(0, ID3);
    }
};
