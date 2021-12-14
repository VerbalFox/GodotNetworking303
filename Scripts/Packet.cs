using Godot;
using System;

public enum PacketType
{
    ConnectionRequest,
    TimeSyncServer,
    PositionUpdateClient,
    PositionUpdateServer,
    GameStateUpdateServer,
    StartGame,
    ServerAssignPlayerId,
    CollisionClient,
    CollisionServer,
    AcknowledgementResponse
}
public class Packet
{ 
    protected PacketType type;
    public int networkId;
    public short acknowledgementToken;
    public static PacketType GetPacketTypeFromStream(byte[] stream) {
        PacketType type = (PacketType)BitConverter.ToInt32(stream, 0);
        return type;
    }

    public static int GetNetworkIdFromStream(byte[] stream) {
        return BitConverter.ToInt32(stream, 4);
    }

    public static short GetAcknowledgementTokenFromStream(byte[] stream) {
        return BitConverter.ToInt16(stream, 8);
    }
}