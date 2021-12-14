using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServerStartGamePacket : Packet
{
    public ServerStartGamePacket() {
        type = PacketType.StartGame;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(startTime));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        startTime = BitConverter.ToDouble(stream, index);               //index += sizeof(double);
    }
    
    public double startTime;
}
