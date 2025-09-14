namespace dynamicpupspawns;

public class PersistentPupData
{
    public string ID { get; }
    public string Room { get; }
    public bool IsTame { get; }

    public PersistentPupData(string id, string room, bool tame)
    {
        ID = id;
        Room = room;
        IsTame = tame;
    }
}