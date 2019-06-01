#include <rpp/vec.h>
#include <cmath>

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT rpp::Vector2 __stdcall RadiansToDirection(float radians)
{
    // @note This should invoke x86 FSINCOS instruction
    float s = sin(radians);
    float c = cos(radians);
    return { s, -c };
}

DLLEXPORT rpp::Vector2 __stdcall OrbitalRotate(
    const rpp::Vector2& center, const rpp::Vector2& orbitPos,
    float orbitRadius, float radians)
{
    // current orbital normal
    float normalX = (orbitPos.x - center.x);
    float normalY = (orbitPos.y - center.y);
    
    // get the delta rotation sin/cos
    // @note This should invoke x86 FSINCOS instruction
    float s = sin(radians);
    float c = cos(radians);

    // new delta vector
    float dx = c*normalX - s*normalY;
    float dy = s*normalX + c*normalY;

    // extra orbital precision, this is needed to prevent orbital decay
    float invLen = orbitRadius / sqrt(dx*dx + dy*dy);

    return { center.x + dx*invLen, 
             center.y + dy*invLen };
}

DLLEXPORT rpp::Vector2 __stdcall OrbitalOffsetRotate(
    rpp::Vector2 offset, float orbitRadius, float radians)
{
    // get the delta rotation sin/cos
    // @note This should invoke x86 FSINCOS instruction
    float s = sin(radians);
    float c = cos(radians);

    // new offset vector
    float dx = c*offset.x - s*offset.y;
    float dy = s*offset.x + c*offset.y;

    // extra orbital precision, this is needed to prevent orbital decay
    float invLen = orbitRadius / sqrt(dx*dx + dy*dy);
    return { dx*invLen, dy*invLen };
}
