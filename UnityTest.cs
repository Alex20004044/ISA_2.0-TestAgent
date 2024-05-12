using System;
using Emgu.CV;
using ISA_2.ImageProcessing;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using IsaTestAgent.Connection;
using EventSocket;
using System.Threading;
using System.Text.Json;
using System.Collections.Generic;
using ISA_2;
using System.Linq;
using static IsaTestAgent.TestConstants;


namespace IsaTestAgent
{
    public class UnityTest
    {
        public string ipAdress = "127.0.0.1";
        public int port = 54010;
        const int sendDelay = 1000;
        const int recieveDelay = 1000;

        const int width = 640;
        const int height = 480;

        VideoCaptureDevice videoSource;
        IConnection connection;

        static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        static CommandMove lastRecievedCoords = new CommandMove(-1, -1, -1);
        static Mat shared_frame = new Mat();

        ~UnityTest()
        {
            connection.Dispose();
        }

        public void Start()
        {
            CreateConnection();
            SetupVideoStream();

            ComplexTests(FullTest);

            Console.WriteLine("Enter any text to close test");
            Console.ReadLine();
        }

        void ComplexTests(TestSettings testSettings)
        {
            List<SensorTestData> sensors = GetSensorsWithCalibration(testSettings.TargetSensors);

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
            if (isAll || targetSensors == TargetSensors.keyPoints)
                sensors.Add(new SensorTestData(new ImageProcessorKeyPoints(width, height), "YNN"));
            if (isAll || targetSensors == TargetSensors.keyPointsRect)
                sensors.Add(new SensorTestData(new ImageProcessorKeyPointsRect(width, height), "YNNRect"));

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

        void CreateConnection()
        {
            var socket = new ClientEventSocket();
            if (!socket.Connect(ipAdress, port))
            {
                Console.WriteLine("Connection error");
                return;
            }
            Console.WriteLine("Succesfully connected");

            connection = socket;

            connection.OnMessageRecieved += Connection_OnMessageRecieved;
        }

        private void Connection_OnMessageRecieved(string message)
        {
            Console.WriteLine("Message recieved in UnityTest: " + message);
            try
            {
                lock (lastRecievedCoords)
                {
                    lastRecievedCoords = JsonSerializer.Deserialize<CommandMove>(message);
                    Thread.Sleep(100);
                    waitHandle.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unsupported command: " + message);
                Console.WriteLine(ex.ToString());
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
            videoSource.Start(); ;
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

        Mat GetMat()
        {
            lock (shared_frame)
            {
                return shared_frame.Clone();
            }
        }

        #region Commands Utils
        public void Send(CommandBase commandBase)
        {
            commandBase.Send(connection);
            Thread.Sleep(sendDelay);
        }

        public void Move(float x, float z, float rotation)
        {
            Send(new CommandMove(x, z, rotation));
        }

        public void ChangePerson(int index)
        {
            Send(new CommandChangePerson(index));
        }

        public CommandMove GetPose()
        {
            new CommandGetPose().Send(connection);
            Thread.Sleep(recieveDelay);
            waitHandle.WaitOne();
            lock (lastRecievedCoords)
            {
                return new CommandMove(lastRecievedCoords.X, lastRecievedCoords.Z, lastRecievedCoords.Rotation);
            }
        }
        #endregion
    }
}
