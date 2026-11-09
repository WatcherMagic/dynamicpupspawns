namespace DynamicPupSpawns;

public static class DPSOptions
{
    public static bool DynamicSpawnsPossible = true;
    public static bool RandomizeUsingGaussian = true;
    public static bool OverrideNoPupCampaigns = false;
    public static bool Persistence = true;
    public static int MinPups = _defaultMin;
    public static int MaxPups = _defaultMax;
    public static int SpawnChance = _defaultChance;
    public static bool UseAllRooms = false;
    public static bool WeighRooms = true;
    public static bool AllowSubmergedDens = false;

    public static bool OverrideSurvivor = true;
    public static bool OverrideMonk = true;
    public static bool OverrideHunter = true;
    public static bool OverrideGourmand = true;
    public static bool OverrideArtificer = false;
    public static bool OverrideSpearmaster = false;
    public static bool OverrideRivulet = false;
    public static bool OverrideSaint = false;
    public static bool OverrideWatcher = false;
    public static bool OverrideMods = false;

    public static bool SurvivorPupsSpawn = true;
    public static bool SurvivorPersistence = true;
    public static int SurvivorMinPups = _defaultMin;
    public static int SurvivorMaxPups = _defaultMax;
    public static int SurvivorSpawnChance = _defaultChance;
    
    public static bool MonkPupsSpawn = true;
    public static bool MonkPersistence = true;
    public static int MonkMinPups = _defaultMin;
    public static int MonkMaxPups = _defaultMax;
    public static int MonkSpawnChance = _defaultChance;
    
    public static bool HunterPupsSpawn = true;
    public static bool HunterPersistence = true;
    public static int HunterMinPups = _defaultMin;
    public static int HunterMaxPups = _defaultMax;
    public static int HunterSpawnChance = _defaultChance;
    
    public static bool GourmandPupsSpawn = true;
    public static bool GourmandPersistence = true;
    public static int GourmandMinPups = _defaultMin;
    public static int GourmandMaxPups = _defaultMax;
    public static int GourmandSpawnChance = _defaultChance;
    
    public static bool ArtificerPupsSpawn = false;
    public static bool ArtificerPersistence = true;
    public static int ArtificerMinPups = _defaultMin;
    public static int ArtificerMaxPups = _defaultMax;
    public static int ArtificerSpawnChance = _defaultChance;
    
    public static bool SpearmasterPupsSpawn = false;
    public static bool SpearmasterPersistence = true;
    public static int SpearmasterMinPups = _defaultMin;
    public static int SpearmasterMaxPups = _defaultMax;
    public static int SpearmasterSpawnChance = _defaultChance;
    
    public static bool RivuletPupsSpawn = false;
    public static bool RivuletPersistence = true;
    public static int RivuletMinPups = _defaultMin;
    public static int RivuletMaxPups = _defaultMax;
    public static int RivuletSpawnChance = _defaultChance;
    
    public static bool SaintPupsSpawn = false;
    public static bool SaintPersistence = true;
    public static int SaintMinPups = _defaultMin;
    public static int SaintMaxPups = _defaultMax;
    public static int SaintSpawnChance = _defaultChance;
    
    public static bool WatcherPupsSpawn = false;
    public static bool WatcherPersistence = true;
    public static int WatcherMinPups = _defaultMin;
    public static int WatcherMaxPups = _defaultMax;
    public static int WatcherSpawnChance = _defaultChance;
    
    private static int _defaultMin = 1;
    private static int _defaultMax = 4;
    private static int _defaultChance = 5;
}