using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServerPositionUpdateServerPacket : Packet
{
    public ServerPositionUpdateServerPacket() {
        type = PacketType.PositionUpdateServer;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(playerCount));

        for (int i = 0; i < playerCount; i++) {
            array = array.Concat(BitConverter.GetBytes(position[i].x))
            .Concat(BitConverter.GetBytes(position[i].y))
            .Concat(BitConverter.GetBytes(position[i].z))
            .Concat(BitConverter.GetBytes(velocity[i].x))
            .Concat(BitConverter.GetBytes(velocity[i].y))
            .Concat(BitConverter.GetBytes(velocity[i].z));
        }
        
        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        playerCount = BitConverter.ToInt32(stream, index);              index += sizeof(int);

        position = new Vector3[playerCount];
        velocity = new Vector3[playerCount];

        for (int i = 0; i < playerCount; i++) {
            position[i] = new Vector3();
            velocity[i] = new Vector3();

            position[i].x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
            position[i].y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
            position[i].z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
            velocity[i].x = BitConverter.ToSingle(stream, index);              index += sizeof(float);
            velocity[i].y = BitConverter.ToSingle(stream, index);              index += sizeof(float);
            velocity[i].z = BitConverter.ToSingle(stream, index);              index += sizeof(float);
        }
    }
    
    public int playerCount;
    public Vector3[] position;
    public Vector3[] velocity;
}
