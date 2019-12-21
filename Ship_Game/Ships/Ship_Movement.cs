using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public void RotateToFacing(float elapsedTime, float angleDiff, float rotationDir)
        {
            isTurning = true;
            float rotAmount = rotationDir * elapsedTime * rotationRadiansPerSecond;
            if (Math.Abs(rotAmount) > angleDiff)
            {
                rotAmount = rotAmount <= 0f ? -angleDiff : angleDiff;
            }

            if (rotAmount > 0f) // Y-bank:
            {
                if (yRotation > -MaxBank)
                    yRotation -= yBankAmount;
            }
            else if (rotAmount < 0f)
            {
                if (yRotation <  MaxBank)
                    yRotation += yBankAmount;
            }

            Rotation += rotAmount;
            Rotation = Rotation.AsNormalizedRadians();
        }

        public void RestoreYBankRotation()
        {
            if (yRotation > 0f)
            {
                yRotation -= yBankAmount;
                if (yRotation < 0f)
                    yRotation = 0f;
            }
            else if (yRotation < 0f)
            {
                yRotation += yBankAmount;
                if (yRotation > 0f)
                    yRotation = 0f;
            }
        }
    }
}
