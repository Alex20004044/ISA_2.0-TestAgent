using System.Text.Json;
using System;
using System.Threading;
using EventSocket;
using IsaTestAgent.Connection;

namespace IsaTestAgent.TestService
{
    internal static class TestsApi
    {
        public const string ipAdress = "127.0.0.1";
        public const int port = 54010;

        public const int sendDelay = 1000;
        public const int recieveDelay = 1000;


        public static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        public static CommandMove lastRecievedCoords = new CommandMove(-1, -1, -1);


        static IConnection connection;

        public static IConnection CreateConnection()
        {
            var socket = new ClientEventSocket();
            if (!socket.Connect(ipAdress, port))
            {
                Console.WriteLine("Connection error");
                return null;
            }
            Console.WriteLine("Succesfully connected");

            connection = socket;

            connection.OnMessageRecieved += Connection_OnMessageRecieved;

            return connection;
        }

        private static void Connection_OnMessageRecieved(string message)
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

        #region Commands Utils
        public static void Send(CommandBase commandBase)
        {
            commandBase.Send(connection);
            Thread.Sleep(sendDelay);
        }

        public static void Move(float x, float z, float rotation)
        {
            Send(new CommandMove(x, z, rotation));
        }

        public static void ChangePerson(int index)
        {
            Send(new CommandChangePerson(index));
        }

        public static CommandMove GetPose()
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
