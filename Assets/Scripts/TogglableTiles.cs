using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileSwitchGroup
{
    public List<BaseSwitch> controllingSwitches = new List<BaseSwitch>();
    public List<TogglableTile> controlledTiles = new List<TogglableTile>();

    [HideInInspector] public bool tilesVisible;
}

public class TogglableTiles : MonoBehaviour, ISwitchListener
{
    [SerializeField] private List<TileSwitchGroup> tileGroups = new List<TileSwitchGroup>();

    private Dictionary<ISwitchSource, TileSwitchGroup> sourceToGroupMap = new Dictionary<ISwitchSource, TileSwitchGroup>();

    private void Awake()
    {
        foreach (var group in tileGroups)
        {
            if (group.controlledTiles.Count > 0 && group.controlledTiles[0] != null)
            {
                group.tilesVisible = group.controlledTiles[0].startVisible;
            }

            foreach (var switchSource in group.controllingSwitches)
            {
                if (switchSource != null)
                {
                    switchSource.AddListener(this);
                    sourceToGroupMap[switchSource] = group;
                }
            }

            UpdateTileState(group);
        }
    }

    private void OnDestroy()
    {
        foreach (var group in tileGroups)
        {
            foreach (var switchSource in group.controllingSwitches)
            {
                if (switchSource != null)
                {
                    switchSource.RemoveListener(this);
                }
            }
        }
    }

    public void OnSwitchToggled(ISwitchSource source, bool state)
    {
        if (sourceToGroupMap.TryGetValue(source, out TileSwitchGroup group))
        {
            group.tilesVisible = !group.tilesVisible;
            UpdateTileState(group);
        }
    }

    private void UpdateTileState(TileSwitchGroup group)
    {
        foreach (var tile in group.controlledTiles)
        {
            if (tile != null)
            {
                tile.SetTileState(group.tilesVisible);
            }
        }
    }
}