#include <rpp/timer.h>
#include <rpp/tests.h>
#include "SpatialSim.h"

TestImpl(TestSimulation)
{
    TestInitNoAutorun(TestSimulation)
    {
    }
    TestCase(simulate)
    {
        SimParams p;
        //p.solarSystems = 1;
        //p.singleSystemPos = {0,0};
        //p.solarRadius = 100;
        //p.numObjects = 70;
        //p.type = spatial::SpatialType::QuadTree;

        Simulation sim { p };
        sim.run();
    }
};
