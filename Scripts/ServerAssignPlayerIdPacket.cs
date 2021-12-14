using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ServerAssignPlayerIdPacket : Packet
{
    public ServerAssignPlayerIdPacket() {
        type = PacketType.ServerAssignPlayerId;
    }

    public byte[] Serialise() {
        var array = 
                BitConverter.GetBytes(((Int32)type))
        .Concat(BitConverter.GetBytes(networkId))
        .Concat(BitConverter.GetBytes(acknowledgementToken))
        .Concat(BitConverter.GetBytes(playerId))
        .Concat(BitConverter.GetBytes(playerTotal));

        return array.ToArray();
    }
    
    public void Deserialise(byte[] stream) {
        int index = 0;
        
        type = (PacketType)BitConverter.ToInt32(stream, index);         index += sizeof(int); 
        networkId = BitConverter.ToInt32(stream, index);                index += sizeof(int);
        acknowledgementToken = BitConverter.ToInt16(stream, index);     index += sizeof(short);
        playerId = BitConverter.ToInt32(stream, index);                 index += sizeof(int);
        playerTotal = BitConverter.ToInt32(stream, index);              //index += sizeof(int);
    }
    public int playerId;
    public int playerTotal;
}
