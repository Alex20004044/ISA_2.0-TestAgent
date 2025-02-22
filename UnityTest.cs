﻿using System;
using Emgu.CV;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using static IsaTestAgent.TestService.TestConstants;
using IsaTestAgent.TestService;
using static IsaTestAgent.TestService.TestsApi;
using ISA_2.ImageProcessing.ImageProcessors;

namespace IsaTestAgent;

public class UnityTest
{
    const int width = 640;
    const int height = 480;

    VideoCaptureDevice videoSource;
    Mat shared_frame = new Mat();

    public void Start()
    {
        using var connecton = CreateConnection();

        SetupVideoStream();
        //ComplexTests(FullTest);
        //ComplexTests(FullTest, TargetSensors.all);
        ComplexTests(RotateTest, TargetSensors.all);
        //ComplexTests(ShortTest, TargetSensors.all);

        Console.WriteLine("Enter any text to close test");
        Console.ReadLine();
    }

    void ComplexTests(TestSettings testSettings, TargetSensors targetSensors = TargetSensors.all)
    {
        List<SensorTestData> sensors = GetSensorsWithCalibration(targetSensors);

        for (int i = 0; i < testSettings.PersonsCount; i++)
        {
            ChangePerson(i);
            for (float z = testSettings.MinZ; z <= testSettings.MaxZ + testSettings.DeltaZ / 2; z += testSettings.DeltaZ)
            {
                for (float rot = testSettings.MinRot; rot < testSettings.MaxRot + testSettings.DeltaRot / 2; rot += testSettings.DeltaRot)
                {
                    Move(0, z, rot);
                    using var mat = GetMat();
                    Console.WriteLine($"Person: {i}, Z: {z}, Rot: {rot}");
                    
                    //При тесте одного детектора - выводим результат его работы
                    if (sensors.Count() == 1)
                        sensors.First().GetDistanceFromImageAndDrawBorders(mat);

                    CvInvoke.Imshow("Unity", mat);

                    TestSensor(sensors, mat, z);

                    CvInvoke.WaitKey(sendDelay);
                }
            }
        }

        ShowResults(sensors);
    }

    private List<SensorTestData> GetSensorsWithCalibration(TargetSensors targetSensors = TargetSensors.all)
    {
        var sensors = GetSensorTestDatas(targetSensors);

        Calibrate(sensors);
        return sensors;
    }

    List<SensorTestData> GetSensorTestDatas(TargetSensors targetSensors)
    {
        List<SensorTestData> sensors = new List<SensorTestData>();
        bool isAll = targetSensors == TargetSensors.all;
        if (isAll || targetSensors == TargetSensors.haarCascade)
            sensors.Add(new SensorTestData(new ImageProcessorHaarCascade(), "HaarCascade"));
        if (isAll || targetSensors == TargetSensors.YNN)
            sensors.Add(new SensorTestData(new ImageProcessorYNNFaceLandmarks(width, height), "YNN"));
        if (isAll || targetSensors == TargetSensors.YNNRect)
            sensors.Add(new SensorTestData(new ImageProcessorYNNFaceRect(width, height), "YNNRect"));
        if (isAll || targetSensors == TargetSensors.HaarCascade_LBF)
            sensors.Add(new SensorTestData(new ImageProcessorLandmarksHaarCascadeLBF(), "HaarCascade_LBF"));        
        if (isAll || targetSensors == TargetSensors.YNN_LBF)
            sensors.Add(new SensorTestData(new ImageProcessorLandmarksYNN_LBF(width, height), "YNN_LBF"));

        return sensors;
    }

    void Calibrate(IEnumerable<SensorTestData> sensors)
    {
        Move(0, CalibrationDistance, 0);
        using var mat = GetMat();
        foreach (SensorTestData sensor in sensors)
        {
            using var matCopy = mat.Clone();
            sensor.Calibrate(CalibrationDistance, matCopy);
        }
    }

    void TestSensor(IEnumerable<SensorTestData> sensors, Mat frame, float realDistance)
    {
        foreach (var sensor in sensors)
        {
            sensor.AddData(realDistance, frame);
        }
    }

    void ShowResults(IEnumerable<SensorTestData> sensors)
    {
        foreach (var sensor in sensors)
        {
            Console.WriteLine($"Sensor: {sensor.ImageProcessorName}, AvgError: {sensor.AvgAbsError()}, AvgRelativeError: {sensor.AvgRealativeAbsError()}, MaxError: {sensor.MaxError()}, Measures: {sensor.Distances.Count()}, FatalErrorsCount: {sensor.FatalErrorsCount}");
        }
    }



    private void SetupVideoStream()
    {
        FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        //DisplayAvailableDevices(videoDevices);

        videoSource = new VideoCaptureDevice(videoDevices[3].MonikerString);
        //DisplayAvailableVideoCapabilities();
        videoSource.VideoResolution = videoSource.VideoCapabilities[10];

        videoSource.NewFrame += new NewFrameEventHandler(ProcessFrame);
        videoSource.Start();
        Console.WriteLine($"VideoSource active: {videoSource.IsRunning}");

        static void DisplayAvailableDevices(FilterInfoCollection videoDevices)
        {
            for (int i = 0; i < videoDevices.Count; i++)
            {
                Console.WriteLine($"{videoDevices[i].Name} : {videoDevices[i].MonikerString}");
            }
        }

        void DisplayAvailableVideoCapabilities()
        {
            for (int i = 0; i < videoSource.VideoCapabilities.Length; i++)
            {
                VideoCapabilities x = videoSource.VideoCapabilities[i];
                Console.WriteLine(i + " video: " + x.FrameSize.ToString());
            }
        }
    }

    private void ProcessFrame(object sender, NewFrameEventArgs eventArgs)
    {
        // get new frame
        Bitmap bitmap = eventArgs.Frame;

        // process the frame
        lock (shared_frame)
        {
            shared_frame.Dispose();
            shared_frame = bitmap.ToMat();
        }
        bitmap.Dispose();
        //CvInvoke.Imshow("Unity", shared_frame);
        //int key = CvInvoke.WaitKey(1);
        //if (key == (int)ConsoleKey.Escape)
        //{
        //    videoSource.SignalToStop();
        //    return;
        //}
    }
    public Mat GetMat()
    {
        lock (shared_frame)
        {
            return shared_frame.Clone();
        }
    }
}
