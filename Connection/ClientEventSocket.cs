using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EventSocket.Helpers;
using IsaTestAgent.Connection;

namespace EventSocket
{
    public class ClientEventSocket: IConnection
    {

        #region Properties
        private Socket client { get; set; }
        private bool IsConnected { get; set; }
        private bool IsInited { get; set; }

        // AutoResetEvent instances signal completion.  
        private AutoResetEvent connectDone { get; set; }
        private AutoResetEvent sendDone { get; set; }
        private AutoResetEvent receiveDone { get; set; }

        #endregion

        #region Events
        // Events

        /// <summary>
        /// Event occurred on Connected to TCP Server.
        /// Raise After [Connect] Method to show results.
        /// </summary>
        public EventHandler<string> OnConnected;

        /// <summary>
        /// Event occurred On Disconnected from TCP Server.
        /// Raise after [Disonnect] method to show results.
        /// Raise after disconneting from TCP Server for any reason.
        /// </summary>
        public EventHandler<string> OnDisconnected;

        /// <summary>
        /// Event occurred on Send Data To TCP Server.
        /// Raise after [Send] or [SendEof] methods to show results.
        /// </summary>
        public EventHandler<string> OnSended;

        /// <summary>
        /// Event occurred on Received Data from TCP Server.
        /// Raise after received new data and show [Data] by string parameter.
        /// </summary>
        public EventHandler<string> OnReceived;

        /// <summary>
        /// Event occurred on Errors.
        /// Raise after each error anywhere.
        /// </summary>
        public EventHandler<string> OnError;

        public event Action<string> OnMessageRecieved;
        #endregion

        #region Ctor

        public ClientEventSocket()
        {
            this.IsInited = false;
            this.IsConnected = false;
            this.connectDone = new AutoResetEvent(false);
            this.sendDone = new AutoResetEvent(false);
            this.receiveDone = new AutoResetEvent(false);

            OnReceived += (_, message) => OnMessageRecieved.Invoke(message);
        }


        #endregion

        #region Methods

        private bool checkIsConnected()
        {
            try
            {
                return !(this.client.Poll(1, SelectMode.SelectRead) && this.client.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public override string ToString()
        {
            var rep = this.client.RemoteEndPoint;
            return rep == null ? base.ToString() : rep.ToString();
        }

        #endregion

        #region Connecting
        /// <summary>
        /// Connecting to TCP Server
        /// </summary>
        /// <param name="IP">TCP Server IP Address</param>
        /// <param name="Port">TCP Server PortW</param>
        /// <returns>The result of success</returns>
        public bool Connect(string IP, int Port)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);

                this.client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                this.OnConnected?.Invoke(this.client, string.Format("Socket connecting to {0}:{1}", IP, Port));

                // Connect to the remote endpoint.  
                this.client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), this.client);
                this.connectDone.WaitOne();

                return this.IsConnected;
            }
            catch (Exception ex)
            {
                this.OnError?.Invoke(this.client, string.Format("Socket connecting error {0}", ex.Message));
                return false;
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                this.IsConnected = false;

                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                this.IsConnected = true;

                this.OnConnected?.Invoke(this.client, string.Format("Socket connected to {0}", client.RemoteEndPoint.ToString()));

                // Signal that the connection has been made.  
                this.connectDone.Set();

                if (!this.IsInited)
                {
                    this.Disconnecting();
                    this.Receiving();
                    this.IsInited = true;
                }

            }
            catch (Exception ex)
            {
                // Signal that the connection has been made.  
                this.connectDone.Set();

                this.OnError?.Invoke(this.client, string.Format("Socket connecting error {0}", ex.Message));
            }
        }
        #endregion

        #region Disconnecting
        /// <summary>
        /// Disconnecting from TCP Server
        /// </summary>
        /// <returns>The result of success</returns>
        public bool Disconnect()
        {
            try
            {
                this.OnDisconnected?.Invoke(this.client, "Socket Disconnecting");

                // Release the socket.  
                this.client.Shutdown(SocketShutdown.Both);
                this.client.Close();

                this.IsConnected = false;
                this.OnDisconnected?.Invoke(this.client, "Socket Diconnected");
                return true;
            }
            catch (Exception ex)
            {
                this.OnError?.Invoke(this.client, string.Format("Socket Diconnecting error {0}", ex.Message));
                return false;
            }
        }
        private void Disconnecting()
        {
            ThreadTimer.Start((s) =>
            {
                try
                {
                    if (!checkIsConnected())
                    {
                        this.OnDisconnected?.Invoke(s, "Socket Diconnected");
                        this.IsConnected = false;
                        return false;
                    }
                    else
                    {
                        if (!this.IsConnected) this.OnError?.Invoke(this.client, "Socket Reconnecting...");
                        this.IsConnected = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    this.OnError?.Invoke(this.client, string.Format("Socket Diconnecting error {0}", ex.Message));
                    return false;
                }
            }, 500, this.client);
        }

        #endregion

        #region Receiving
        private void Receiving()
        {
            ThreadTimer.Start((s) =>
            {
                try
                {
                    if (this.checkIsConnected())
                    {
                        // Receive the response from the remote device.  
                        this.Receive(s);
                        this.receiveDone.WaitOne();
                        return true;
                    }
                    else return false;
                }
                catch (Exception ex)
                {
                    this.OnError?.Invoke(this.client, string.Format("Socket Receiving error {0}", ex.Message));
                    return false;
                }
            }, 500, this.client);
        }
        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket s = state.workSocket;

                //if (client == null || !client.Connected || !this.IsConnected) return;

                // Read data from the remote device.  
                int bytesRead = s.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    string data = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    this.OnReceived?.Invoke(s, data);
                    //state.sb.Append(data);

                    // Get the rest of the data.  
                    s.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    if (this.client.Available > 0)
                    {
                        bytesRead = this.client.Available;
                        this.client.Receive(state.buffer);
                        string data = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                        if (!string.IsNullOrEmpty(data)) this.OnReceived?.Invoke(s, data);
                    }
                    else
                    {
                        //// All the data has arrived; put it in response.  
                        string data = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                        if (!string.IsNullOrEmpty(data)) this.OnReceived?.Invoke(s, data);
                        //if (state.sb.Length > 1)  this.OnReceived?.Invoke(client, state.sb.ToString());
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception ex)
            {
                this.OnError?.Invoke(this.client, string.Format("Socket Receiving error {0}", ex.Message));
            }
        }
        #endregion

        #region Sending

        /// <summary>
        /// Send Some Data
        /// </summary>
        /// <param name="Data">Data content in string</param>
        /// <param name="EOF">Append a char for end of data content</param>
        /// <returns>The result of success</returns>
        public bool SendEof(string Data, char EOF = '\r')
        {
            return this.Send(string.Concat(Data, EOF));
        }
        /// <summary>
        /// Send Some Data
        /// </summary>
        /// <param name="Data">Data content in string</param>
        /// <returns>The result of success</returns>
        public bool Send(string Data)
        {
            try
            {
                if (this.checkIsConnected())
                {
                    this.OnSended?.Invoke(this.client, string.Format("Socket Sending {0}", Data));

                    this.SendData(this.client, Data);
                    this.sendDone.WaitOne();

                    return true;
                }
                else
                {
                    this.OnError?.Invoke(this.client, string.Format("Socket Sending failed {0} Socket disconnected", Data));
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.OnError?.Invoke(this.client, string.Format("Socket Sending error {0}", ex.Message));
                return false;
            }
        }
        private void SendData(Socket client, string data)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                // Begin sending the data to the remote device.  
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket s = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = s.EndSend(ar);
                this.OnSended?.Invoke(s, string.Format("Socket Sent {0} bytes", bytesSent));

                // Signal that all bytes have been sent.  
                this.sendDone.Set();
            }
            catch (Exception ex)
            {
                this.OnError?.Invoke(this.client, string.Format("Socket Sening error {0}", ex.Message));
            }
        }

        public void SendMessage(string message)
        {
            SendData(this.client, message);
        }

        public void Dispose()
        {
            Disconnect();
        }
        #endregion

        // State object for receiving data from remote device.  
        protected class StateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new StringBuilder();
        }

    }
}

