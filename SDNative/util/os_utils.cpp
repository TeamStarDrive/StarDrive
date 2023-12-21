#define WIN32_LEAN_AND_MEAN 1
#include <Windows.h>
#include <rpp/thread_pool.h>

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT int __stdcall GetPhysicalCPUCoreCount()
{
    static int num_cores = []
    {
        DWORD bytes = 0;
        GetLogicalProcessorInformation(nullptr, &bytes);
        std::vector<SYSTEM_LOGICAL_PROCESSOR_INFORMATION> coreInfo(bytes / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
        GetLogicalProcessorInformation(coreInfo.data(), &bytes);

        int cores = 0;
        for (auto& info : coreInfo)
        {
            if (info.Relationship == RelationProcessorCore)
                ++cores;
        }
        return cores > 0 ? cores : 1;
    }();
    return num_cores;
}
