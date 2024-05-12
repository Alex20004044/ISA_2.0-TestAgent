using System.Text.Json;
using System.Text.Json.Serialization;

namespace IsaTestAgent.Connection;
[JsonDerivedType(typeof(CommandMove))]
[JsonDerivedType(typeof(CommandGetPose))]
[JsonDerivedType(typeof(CommandChangePerson))]
public abstract class CommandBase
{
    public abstract CommandType CommandType { get; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public void Send(IConnection connection)
    {
        connection.SendMessage(this.ToString());
    }
}

public enum CommandType { move, getPose, changePerson };


public class CommandMove : CommandBase
{
    public override CommandType CommandType => CommandType.move;

    public float X { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }

    public CommandMove(float x, float z, float rotation)
    {
        this.X = x;
        this.Z = z;
        this.Rotation = rotation;
    }
}

public class CommandGetPose : CommandBase
{
    public override CommandType CommandType => CommandType.getPose;
}

public class CommandChangePerson : CommandBase
{
    public override CommandType CommandType => CommandType.changePerson;

    public int PersonIndex { get; set; }


    public CommandChangePerson(int personIndex)
    {
        PersonIndex = personIndex;
    }
}
