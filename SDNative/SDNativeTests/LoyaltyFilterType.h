#pragma once
#include <spatial/Search.h>

enum class LoyaltyFilterType
{
    All,
    OnlyUs,
    OnlyThem,
    ExcludeUs,
    ExcludeThem,
};

inline const char* toString(LoyaltyFilterType type)
{
    switch (type)
    {
        case LoyaltyFilterType::All: return "All";
        case LoyaltyFilterType::OnlyUs: return "OnlyUs";
        case LoyaltyFilterType::OnlyThem: return "OnlyThem";
        case LoyaltyFilterType::ExcludeUs: return "ExcludeUs";
        case LoyaltyFilterType::ExcludeThem: return "ExcludeThem";
    }
    return "Unknown";
}

inline void setLoyaltyFilterType(spatial::SearchOptions& opt, LoyaltyFilterType type, uint8_t us, uint8_t them)
{
    switch (type)
    {
        case LoyaltyFilterType::All:      opt.OnlyLoyalty = 0;  opt.ExcludeLoyalty = 0; break;
        case LoyaltyFilterType::OnlyUs:   opt.OnlyLoyalty = us; opt.ExcludeLoyalty = 0; break;
        case LoyaltyFilterType::OnlyThem: opt.OnlyLoyalty = them; opt.ExcludeLoyalty = 0; break;
        case LoyaltyFilterType::ExcludeUs:   opt.OnlyLoyalty = 0; opt.ExcludeLoyalty = us; break;
        case LoyaltyFilterType::ExcludeThem: opt.OnlyLoyalty = 0; opt.ExcludeLoyalty = them; break;
    }
}

inline void toggleLoyaltyFilterType(LoyaltyFilterType& type)
{
    switch (type)
    {
        case LoyaltyFilterType::All:      type = LoyaltyFilterType::OnlyUs; break;
        case LoyaltyFilterType::OnlyUs:   type = LoyaltyFilterType::OnlyThem; break;
        case LoyaltyFilterType::OnlyThem: type = LoyaltyFilterType::ExcludeUs; break;
        case LoyaltyFilterType::ExcludeUs:   type = LoyaltyFilterType::ExcludeThem; break;
        case LoyaltyFilterType::ExcludeThem: type = LoyaltyFilterType::All; break;
    }
}