using System.Collections.Generic;
using UnityEngine;

public class TogglableTiles : MonoBehaviour, ISwitchListener
{
    [SerializeField] private int requiredSwitches = 1;
    [SerializeField] private List<GameObject> controlledTiles = new List<GameObject>();

    private bool areTilesActive = false;
    private HashSet<PressurePlate> currentSwitchesPressed = new HashSet<PressurePlate>();

    void Start()
    {
        if (controlledTiles.Count > 0 && controlledTiles[0] != null)
        {
            areTilesActive = controlledTiles[0].activeSelf;
        }
    }

    public void RegisterSwitch(PressurePlate plate)
    {
        if (!currentSwitchesPressed.Contains(plate))
        {
            currentSwitchesPressed.Add(plate);
            if (currentSwitchesPressed.Count == requiredSwitches)
            {
                areTilesActive = !areTilesActive;
                UpdateTileState();
            }
        }
    }

    public void RemoveSwitch(PressurePlate plate)
    {
        if (currentSwitchesPressed.Contains(plate))
        {
            currentSwitchesPressed.Remove(plate);
        }
    }

    private void UpdateTileState()
    {
        foreach (GameObject tile in controlledTiles)
        {
            if (tile != null)
            {
                tile.SetActive(areTilesActive);
            }
        }
    }
}