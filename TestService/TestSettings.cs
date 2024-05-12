namespace IsaTestAgent.TestService
{
    public record TestSettings
    {
        public readonly TargetSensors TargetSensors;

        public readonly float DeltaRot;
        public readonly float DeltaZ;
        public readonly float MaxRot;
        public readonly float MinRot;
        public readonly float MaxZ;
        public readonly float MinZ;
        public readonly int PersonsCount;

        public TestSettings(float deltaRot, float deltaZ, float maxRot, float minRot, float maxZ, float minZ, int personsCount)
        {
            DeltaRot = deltaRot;
            DeltaZ = deltaZ;
            MaxRot = maxRot;
            MinRot = minRot;
            MaxZ = maxZ;
            MinZ = minZ;
            PersonsCount = personsCount;
        }
    }

    public enum TargetSensors { all, haarCascade, keyPoints, keyPointsRect };
}