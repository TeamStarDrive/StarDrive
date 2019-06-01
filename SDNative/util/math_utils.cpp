#include <rpp/vec.h>
#include <cmath>
using rpp::Vector2;

#define DLLEXPORT extern "C" __declspec(dllexport)

DLLEXPORT Vector2 __stdcall RadiansToDirection(float radians)
{
    // @note This should invoke x86 FSINCOS instruction
    float s = sin(radians);
    float c = cos(radians);
    return { s, -c };
}

DLLEXPORT Vector2 __stdcall RotateAroundPoint(const Vector2& self, const Vector2& center, float radians)
{
    float s = sin(radians);
    float c = cos(radians);
    float dx = (self.x - center.x);
    float dy = (self.y - center.y);
    return { center.x + c*dx - s*dy, 
             center.y + s*dx + c*dy };
}

DLLEXPORT Vector2 __stdcall RotatePoint(Vector2 self, float radians)
{
    float s = sin(radians);
    float c = cos(radians);
    return { c*self.x - s*self.y, 
             s*self.x + c*self.y };
}

DLLEXPORT Vector2 __stdcall OrbitalOffsetRotate(Vector2 offset, float orbitRadius, float radians)
{
    // get the delta rotation sin/cos
    float s = sin(radians);
    float c = cos(radians);

    // new offset vector
    float dx = c*offset.x - s*offset.y;
    float dy = s*offset.x + c*offset.y;

    // extra orbital precision, this is needed to prevent orbital decay
    float invLen = orbitRadius / sqrt(dx*dx + dy*dy);
    return { dx*invLen, dy*invLen };
}
