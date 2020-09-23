#include <rpp/tests.h>
#include "SpatialSim.h"

bool hasArgument(int argc, char** argv, const char* text)
{
    for (int i = 0; i < argc; ++i)
        if (strstr(argv[i], text) != nullptr)
            return true;
    return false;
}

int main(int argc, char** argv)
{
    int ret = rpp::test::run_tests(argc, argv);

    if (hasArgument(argc, argv, "Simulation"))
    {
        SimParams p;
        Simulation sim { p };
        sim.run();
    }
    return ret;
}
