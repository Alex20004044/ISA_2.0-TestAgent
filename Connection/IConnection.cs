using System;
using System.Text.Json;

namespace IsaTestAgent.Connection
{
    public interface IConnection: IDisposable
    {
        event Action<string> OnMessageRecieved;
        void SendMessage(string message);

    }

}
