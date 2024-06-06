using System;
using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using ISA_2;
using ISA_2.ImageProcessing.ImageProcessors;

namespace IsaTestAgent
{
    public class SensorTestData
    {
        public string ImageProcessorName { get; private set; }

        public IEnumerable<(float realDistance, float measuredDistance)> Distances => distances.Select(x => x.ToCarriage());
        public IEnumerable<float> Errors => distances.Select(x => x.Error);

        public float CalibrateCoefficient { get; private set; }


        public int FatalErrorsCount { get; private set; } = 0;


        List<Distance> distances = new List<Distance>();
        DistanceSensorCamera distanceSensor;

        public SensorTestData(IImageProcessor imageProcessor, string imageProcessorName)
        {
            distanceSensor = new DistanceSensorCamera(imageProcessor, null);
            ImageProcessorName = imageProcessorName;
        }

        public void AddData(float realDistance, float measuredDistance)
        {
            if (!float.IsNormal(measuredDistance))
            {
                FatalErrorsCount++;
                return;
            }

            distances.Add(new Distance(realDistance, measuredDistance));
        }

        public void AddData(float realDistance, Mat image)
        {
            using var imageCopy = image.Clone();
            float measuredDistance = distanceSensor.GetDistanceFromImageAndDrawBorders(imageCopy);
            AddData(realDistance, measuredDistance);
        }

        public double AvgAbsError()
        {
            double sum = 0;
            foreach (var error in Errors)
            {
                sum += MathF.Abs(error);
            }
            return sum / Errors.Count();
        }

        public double AvgRealativeAbsError()
        {
            double sum = 0;
            foreach (var data in distances)
            {
                sum += MathF.Abs((data.measured - data.real)/data.real);
            }
            return sum / distances.Count();
        }

        public float MaxError()
        {
            return Errors.Max(x => MathF.Abs(x));
        }

        public void Calibrate(float realDistance, Mat image)
        {
            distanceSensor.Calibrate(image, realDistance, out var calibrateCoefficient);
            CalibrateCoefficient = calibrateCoefficient;
        }
        public float GetDistanceFromImageAndDrawBorders(Mat imageMat)
        {
            return distanceSensor.GetDistanceFromImageAndDrawBorders(imageMat);
        }
        public struct Distance
        {
            public float real;
            public float measured;

            public float Error => measured - real;

            public Distance(float real, float measured)
            {
                this.real = real;
                this.measured = measured;
            }

            public (float realDistance, float measuredDistance) ToCarriage()
            {
                return (real, measured);
            }
        }
    }
}
