using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServerPositionUpdateClientPacket : Packet
{
    public ServerPositionUpdateClientPacket() {
        type = PacketType.PositionUpdateClient;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(playerId))
        .Concat(BitConverter.GetBytes(position.x))
        .Concat(BitConverter.GetBytes(position.y))
        .Concat(BitConverter.GetBytes(position.z))
        .Concat(BitConverter.GetBytes(velocity.x))
        .Concat(BitConverter.GetBytes(velocity.y))
        .Concat(BitConverter.GetBytes(velocity.z));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        playerId = BitConverter.ToInt32(stream, index);                 index += sizeof(int);
        position.x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        position.y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        position.z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        velocity.x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        velocity.y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        velocity.z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
    }
    public int playerId;    
    public Vector3 position;
    public Vector3 velocity;
}
