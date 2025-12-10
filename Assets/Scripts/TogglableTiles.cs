using System.Collections.Generic;
using UnityEngine;

public class TogglableTiles : MonoBehaviour, ISwitchListener
{
    [SerializeField] private int requiredSwitches = 1;
    [SerializeField] private List<TogglableTile> controlledTiles = new List<TogglableTile>();

    private HashSet<ISwitchSource> _pressedPlates = new HashSet<ISwitchSource>();
    private bool _currentState;

    private void Start()
    {
        if (controlledTiles.Count > 0 && controlledTiles[0] != null)
        {
            _currentState = controlledTiles[0].startVisible;
        }

        UpdateTileState();
    }

    public void RegisterSwitch(ISwitchSource plate)
    {
        if (_pressedPlates.Add(plate))
        {
            CheckToggleCondition();
        }
    }

    public void RemoveSwitch(ISwitchSource plate)
    {
        _pressedPlates.Remove(plate);
    }

    private void CheckToggleCondition()
    {
        if (_pressedPlates.Count >= requiredSwitches)
        {
            _currentState = !_currentState;
            UpdateTileState();
        }
    }

    private void UpdateTileState()
    {
        foreach (TogglableTile tile in controlledTiles)
        {
            if (tile != null)
            {
                tile.SetTileState(_currentState);
            }
        }
    }
}