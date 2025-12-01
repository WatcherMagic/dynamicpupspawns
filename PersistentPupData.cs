namespace dynamicpupspawns;

public class PersistentPupData
{
    public string PupID { get; }
    public string Room { get; }
    public bool IsTame { get; }
    public bool displayWasTamedInDebug { get; }
    
    public string roomName { get; }

    public PersistentPupData(string id, string room, bool tame)
    {
        PupID = id;
        Room = room;
        IsTame = tame;
        displayWasTamedInDebug = tame;
    }
}