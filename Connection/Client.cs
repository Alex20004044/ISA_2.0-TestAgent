using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IsaTestAgent.Connection;

internal class Client : IConnection, IDisposable
{

    public event Action<string> OnMessageRecieved;


    public string ipAdress = "127.0.0.1";
    public int port = 54010;
    public float waitingMessagesFrequency = 1;

    TcpClient m_Client;

    public Client()
    {
        StartClient();
    }

    ~Client()
    {
        CloseClient();
    }

    void StartClient()
    {
        try
        {
            m_Client = new TcpClient();
            //Set and enable client
            m_Client.Connect(ipAdress, port);
            Log($"Client Started on {ipAdress}::{port}");

         
        }

        catch (SocketException)
        {
            Log("Socket Exception: Start Server first");
            CloseClient();
        }
    }


    private void CloseClient()
    {
        Log("Client Closed");

        //Reset everything to defaults
        if (m_Client.Connected)
            m_Client.Close();
    }



    public void SendMessage(string message)
    {
        throw new NotImplementedException();
    }

    void Log(string message)
    {
        Console.WriteLine(message);
    }

    public void Dispose()
    {
        CloseClient();
    }
}

