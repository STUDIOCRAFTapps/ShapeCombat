using System;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;

[System.Serializable]
public class UserData : AutoNetworkSerializable {
    public string name;
    public byte agentId;
    public ulong clientId;

    public UserData () {
    }

    public UserData (string name, byte agentId, ulong clientId) {
        this.name = name;
        this.agentId = agentId;
        this.clientId = clientId;
    }
}
