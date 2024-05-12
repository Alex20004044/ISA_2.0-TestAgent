namespace IsaTestAgent
{
    public record TestSettings
    {
        public readonly float deltaRot = 10f;
        public readonly float deltaZ = 0.05f;
        public readonly float maxRot = 30f;
        public readonly float minRot = -30f;
        public readonly float maxZ = 1f;
        public readonly float minZ = 0.3f;
        public readonly int personsCount = 5;

        public TestSettings(float deltaRot, float deltaZ, float maxRot, float minRot, float maxZ, float minZ, int personsCount)
        {
            this.deltaRot = deltaRot;
            this.deltaZ = deltaZ;
            this.maxRot = maxRot;
            this.minRot = minRot;
            this.maxZ = maxZ;
            this.minZ = minZ;
            this.personsCount = personsCount;
        }
    }
}