using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class AcknowledgementResponsePacket : Packet
{
    public AcknowledgementResponsePacket() {
        type = PacketType.AcknowledgementResponse;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
    }
    
}
