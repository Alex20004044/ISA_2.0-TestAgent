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

namespace IsaTestAgent
{
    public class UnityTest : TestSettings
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

        const float calibrateDistance = 0.5f;

        /*//Short Demo
        const int personsCount = 1;

        const float minZ = 0.3f;
        const float maxZ = 1f;
        const float deltaZ = 0.1f;

        const float minRot = 0f;
        const float maxRot = 10f;
        const float deltaRot = 10f;*/

        ~UnityTest()
        {
            connection.Dispose();
        }

        public void Start()
        {
            CreateConnection();
            SetupVideoStream();

            ComplexTests();
            //Demo();

            Console.WriteLine("Enter any text to close test");
            Console.ReadLine();
        }

        void ComplexTests()
        {
            var sensors = GetSensorTestDatas();
            
            Calibrate(sensors);

            for (int i = 0; i < personsCount; i++)
            {
                ChangePerson(i);
                for (float z = minZ; z <= maxZ + deltaZ / 2; z += deltaZ)
                {
                    for (float rot = minRot; rot < maxRot + deltaRot / 2; rot += deltaRot)
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
        void Demo()
        {
            //var imageProcessor = new ImageProcessorHaarCascade();
            //var imageProcessor = new ImageProcessorKeyPoints(width, height);
            var imageProcessor = new ImageProcessorKeyPointsRect(width, height);


            var sensor = new SensorTestData(imageProcessor, "Demo");

            Calibrate(new SensorTestData[] { sensor });
            Console.WriteLine($"CalibrateCoefficient: {sensor.CalibrateCoefficient}");
            for (int i = 0; i < personsCount; i++)
            {
                ChangePerson(0);
                for (float z = minZ; z <= maxZ + deltaZ / 2; z += deltaZ)
                {
                    for (float rot = minRot; rot < maxRot + deltaRot / 2; rot += deltaRot)
                    {
                        Move(0, z, rot);
                        using var mat = GetMat();

                        var dist = sensor.GetDistanceFromImageAndDrawBorders(mat);
                        Console.WriteLine($"Person: {i}, Z: {z}, Rot: {rot}, Measured: {dist}, Error: {dist - z}");

                        sensor.AddData(z, dist);

                        CvInvoke.Imshow("Unity", mat);
                        CvInvoke.WaitKey(sendDelay);
                    }
                }
            }

            ShowResults(new SensorTestData[] { sensor });
        }
        List<SensorTestData> GetSensorTestDatas()
        {
            List<SensorTestData> sensors = new List<SensorTestData>();
            sensors.Add(new SensorTestData(new ImageProcessorHaarCascade(), "HaarCascade"));
            sensors.Add(new SensorTestData(new ImageProcessorKeyPoints(width, height), "YNN"));
            sensors.Add(new SensorTestData(new ImageProcessorKeyPointsRect(width, height), "YNNRect"));

            return sensors;
        }

        void Calibrate(IEnumerable<SensorTestData> sensors)
        {
            Move(0, calibrateDistance, 0);
            using var mat = GetMat();
            foreach (SensorTestData sensor in sensors)
            {
                using var matCopy = mat.Clone();
                sensor.Calibrate(calibrateDistance, matCopy);
            }
        }

        void TestSensor(IEnumerable<SensorTestData> sensors, Mat frame, float realDistance)
        {
            foreach(var sensor in sensors)
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

        public void OpenCv()
        {
            VideoCapture videoCapture = new VideoCapture(1);
            videoCapture.Start();
            Console.WriteLine(videoCapture.CaptureSource.ToString());

            IImageSource imageSource = new ImageSourceVideoCapture(videoCapture);
            Mat frame;
            while (true)
            {
                frame = imageSource.GetImage();
                CvInvoke.Imshow("Unity", frame);

                int key = CvInvoke.WaitKey(1);
                if (key == (int)ConsoleKey.Enter)
                    break;
            }
        }
    }
}
