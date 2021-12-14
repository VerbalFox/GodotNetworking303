using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CollisionClientPacket : Packet
{
    public CollisionClientPacket() {
        type = PacketType.CollisionClient;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(position.x))
        .Concat(BitConverter.GetBytes(position.y))
        .Concat(BitConverter.GetBytes(position.z))
        .Concat(BitConverter.GetBytes(normal.x))
        .Concat(BitConverter.GetBytes(normal.y))
        .Concat(BitConverter.GetBytes(normal.z))
        .Concat(BitConverter.GetBytes(playerPosition.x))
        .Concat(BitConverter.GetBytes(playerPosition.y))
        .Concat(BitConverter.GetBytes(playerPosition.z));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        position.x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        position.y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        position.z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        normal.x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        normal.y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        normal.z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        playerPosition.x = BitConverter.ToSingle(stream, index);        index += sizeof(float);
        playerPosition.y = BitConverter.ToSingle(stream, index);        index += sizeof(float);
        playerPosition.z = BitConverter.ToSingle(stream, index);        index += sizeof(float);
    }
    public Vector3 position;
    public Vector3 normal;
    public Vector3 playerPosition;
}
