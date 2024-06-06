using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emgu.CV;
using ISA_2;
using ISA_2.ImageProcessing;
using ISA_2.ImageProcessing.ImageProcessors;

namespace IsaTestAgent
{
    class Program
    {
        const string realPhoto = @"D:\Data\Projects\ISA 2.0\TestSet\1";
        const string virtualFront = @"D:\Data\Projects\ISA 2.0\TestSet\3";
        const string virtualFrontTestTable = @"D:\Data\Projects\ISA 2.0\TestSet\4 TestTable";
        const string testTableRotate0 = @"D:\Data\Projects\ISA 2.0\TestSet\5 VRotate 0";
        const string testTableRotate30 = @"D:\Data\Projects\ISA 2.0\TestSet\6 VRotate 30";


        static string dataSetPath => realPhoto;
        const int СalibrateImageIndex = 4;


        static string ynnModelPath = @"D:\Data\Projects\ISA 2.0\ISA 2\Resources\face_detection_yunet_2023mar.onnx";
        static bool renderConfidence = true;

        const int columnWidth = 10;
        static void Main(string[] args)
        {
            new UnityTest().Start();

            //TestWebCam();
            //TestDistanceSensor();
            //TestTable();
        }

        private static void TestWebCam()
        {
            Console.WriteLine("Test Web Cam");

            VideoCapture videoCapture = new VideoCapture(0);
            videoCapture.Start();

            IImageProcessor imageProcessor;        
            //imageProcessor = new ImageProcessorHaarCascade();

            //imageProcessor = new ImageProcessorYNNFaceLandmarks(videoCapture.Width, videoCapture.Height);
            //imageProcessor = new ImageProcessorYNNFaceRect(videoCapture.Width, videoCapture.Height);
            imageProcessor = new ImageProcessorLandmarksHaarCascadeLBF();
            imageProcessor = new ImageProcessorLandmarksYNN_LBF(videoCapture.Width, videoCapture.Height);

            while (true)
            {
                Mat frame = videoCapture.QueryFrame();
                var measure = imageProcessor.MeasureFaceAndDrawBorders(frame);
                Console.WriteLine("Measure: " + measure);

                CvInvoke.Imshow("IsaTest", frame);
                //CvInvoke.Imshow("ISA", grayImage);
                int key = CvInvoke.WaitKey(1);

                if (key == (int)ConsoleKey.Escape)
                    break;
            }
        }

        private static void TestTable(int calibratePhotoIndex = СalibrateImageIndex)
        {
            Console.WriteLine("Test Distance Sensor");
            Console.WriteLine($"Calibrate Image Index: {calibratePhotoIndex}");

            var images = GetImageSet();
            List<TestImageProcessorData> testImageProcessorDatas = new List<TestImageProcessorData>();
            testImageProcessorDatas.Add(new TestImageProcessorData(new ImageProcessorHaarCascade(), "HaarCascade"));
            testImageProcessorDatas.Add(new TestImageProcessorData(new ImageProcessorYNNFaceLandmarks(1920, 1080), "YNN"));
            testImageProcessorDatas.Add(new TestImageProcessorData(new ImageProcessorYNNFaceRect(1920, 1080), "YNNRect"));
            testImageProcessorDatas.Add(new TestImageProcessorData(new ImageProcessorLandmarksHaarCascadeLBF(), "HaarCascade_LBF"));
            testImageProcessorDatas.Add(new TestImageProcessorData(new ImageProcessorLandmarksYNN_LBF(1920, 1080), "YNN_LBF"));

            testImageProcessorDatas.ForEach(x => x.ProcessImages(calibratePhotoIndex, images));
            
            WriteTableRow("Name", testImageProcessorDatas.Select(x => x.ImageProcessorName));
            for (int i = 0; i < images.Count; i++)
            {
                WriteTableRow(i + " " + new string(images[i].name.TakeLast(columnWidth).ToArray()),
                    testImageProcessorDatas.Select(x => $"{MathF.Round(x.Distances[i].realDist)}/{MathF.Round(x.Distances[i].measuredDist)}"));
            }
            WriteTableRow("MaxError", testImageProcessorDatas.Select(x => x.MaxError.ToString()));
            WriteTableRow("AvgError", testImageProcessorDatas.Select(x => x.AvgError.ToString()));
        }

        private static void WriteTableRow(string rowName, IEnumerable<string> values)
        {
            Console.Write(rowName.Substring(0, rowName.Length > columnWidth ? columnWidth : rowName.Length).PadRight(columnWidth) + "\t");
            foreach (var x in values)
            {
                Console.Write(x.Substring(0, x.Length > columnWidth ? columnWidth : x.Length).PadRight(columnWidth) + "\t");
            }
            Console.WriteLine();
        }

        private static void TestDistanceSensor(int calibratePhotoIndex = СalibrateImageIndex)
        {
            Console.WriteLine("Test Distance Sensor");

            var imageSet = GetImageSet();
            //IImageProcessor imageProcessor = new ImageProcessorHaarCascade();
            //IImageProcessor imageProcessor = new ImageProcessorKeyPoints(1920, 1080);
            IImageProcessor imageProcessor = new ImageProcessorLandmarksYNN_LBF(1920, 1080);
            var distanceSensor = new DistanceSensorCamera(imageProcessor, null);

            distanceSensor.Calibrate(imageSet[calibratePhotoIndex].image.Clone(), imageSet[calibratePhotoIndex].distance, out var calibrateCoefficient);

            Console.WriteLine($"Calibrate coefficient: {calibrateCoefficient}");

            List<(float realDist, float measuredDist)> dists = new List<(float realDist, float measuredDist)>();

            foreach (var x in imageSet)
            {
                float measuredDistance = distanceSensor.GetDistanceFromImageAndDrawBorders(x.image);
                dists.Add((x.distance, measuredDistance));
                Console.WriteLine($"ProcessImage {x.name}. Distance: {x.distance}. MeasuredDistance: {measuredDistance}");
                CvInvoke.Imshow("ISA", x.image);
                int key = CvInvoke.WaitKey();
            }
            var MaxError = dists.Max(x => MathF.Abs(x.measuredDist - x.realDist));
            var AvgError = dists.Average(x => MathF.Abs(x.measuredDist - x.realDist));
            Console.WriteLine($"MaxError: {MaxError}");
            Console.WriteLine($"AvgError: {AvgError}");
        }



        public static List<ImageDist> GetImageSet()
        {
            List<ImageDist> imageSet = new List<ImageDist>();
            foreach (var filePath in Directory.EnumerateFiles(dataSetPath))
            {
                var imageDist = new ImageDist();
                imageDist.name = filePath;
                imageDist.image = new Mat(filePath);
                imageDist.distance = float.Parse(Path.GetFileNameWithoutExtension(filePath));

                imageSet.Add(imageDist);
            }
            return imageSet;
        }

        public class ImageDist
        {
            public string name;
            public Mat image;
            public float distance;
        }

        public class TestImageProcessorData
        {
            DistanceSensorCamera distanceSensor;
            public float CalibrateCoefficient { get; private set; }

            public List<(float realDist, float measuredDist)> Distances { get; private set; } = new List<(float realDist, float measuredDist)>();

            public float MaxError { get; private set; }
            public float AvgError { get; private set; }
            public string ImageProcessorName { get; private set; }

            public TestImageProcessorData(IImageProcessor imageProcessor, string imageProcessorName)
            {
                distanceSensor = new DistanceSensorCamera(imageProcessor, null);
                ImageProcessorName = imageProcessorName;
            }

            public void ProcessImages(int calibrateImageIndex, List<ImageDist> images)
            {
                distanceSensor.Calibrate(images[calibrateImageIndex].image.Clone(), images[calibrateImageIndex].distance, out var calibrateCoefficient);
                CalibrateCoefficient = CalibrateCoefficient;

                foreach (var x in images)
                {
                    float measuredDistance = distanceSensor.GetDistanceFromImageAndDrawBorders(x.image.Clone());
                    Distances.Add((x.distance, measuredDistance));
                }

                MaxError = Distances.Max(x => MathF.Abs(x.measuredDist - x.realDist));
                AvgError = Distances.Average(x => MathF.Abs(x.measuredDist - x.realDist));
            }
        }
    }
}
