using System.Collections.Generic;

namespace DynamicPupSpawns;

public class PersistentDataWrapper
{
    public string campaignID { get; }

    public Dictionary<string, List<PersistentPupData>> regionsData = new ();
}