using System.Collections.Generic;

namespace dynamicpupspawns;

public class PersistentDataWrapper
{
    public string campaignID { get; }

    public Dictionary<string, List<PersistentPupData>> regionsData = new ();
}