using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class TimeSyncServerPacket : Packet
{
    public TimeSyncServerPacket() {
        type = PacketType.TimeSyncServer;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(serverTime))
        .Concat(BitConverter.GetBytes(serverTimestamp));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        serverTime = BitConverter.ToDouble(stream, index);              index += sizeof(double);
        serverTimestamp = BitConverter.ToDouble(stream, index);         //index += sizeof(double);
    }
    
    public double serverTime;
    public double serverTimestamp;
}
