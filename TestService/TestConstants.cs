using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsaTestAgent.TestService
{
    public static class TestConstants
    {
        public const float CalibrationDistance = 0.5f;

        public static readonly TestSettings FullTest = new TestSettings(
            deltaRot: 10f,
            deltaZ: 0.05f,
            maxRot: 30f,
            minRot: -30f,
            maxZ: 1f,
            minZ: 0.3f,
            personsCount: 5
            );

        public static readonly TestSettings ShortTest = new TestSettings(
            deltaRot: 10f,
            deltaZ: 0.1f,
            maxRot: 10f,
            minRot: 0,
            maxZ: 1f,
            minZ: 0.3f,
            personsCount: 1
            );
    }
}
