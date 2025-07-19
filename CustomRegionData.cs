using System.Collections.Generic;

namespace dynamicpupspawns;

public struct CustomRegionData
{
    public string name;
    public List<WorldCoordinate> spawnCoords;

    public CustomRegionData(string regionName)
    {
        name = regionName;
    }

    public void AddCoordinate(string coordString)
    {
    }

    public void AddCoordinate(WorldCoordinate coord)
    {
        spawnCoords.Add(coord);
    }
}