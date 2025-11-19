using System;
using UnityEngine;


// MapCreation instances multiple copies of a tile prefab to build a level
// following the contents of a map file


public class MapCreation : MonoBehaviour
{
    //in MAP -> 2 MEANS TILE, 3 MEANS VICTORY HOLE, 4 MEANS PLAYER START POSITION
    public TextAsset map; 		// Text file containing the map
    public GameObject tile; 	// Tile prefab used to instance and build the level
    public GameObject VictoryHole; //if stepped into, level is passed
    public GameObject player;   //reference to player to set initial position
    // Start is called once after the MonoBehaviour is created
    void Start()
    {
        char[] seps = {' ', '\n', '\r'}; 	// Characters that act as separators between numbers
        string [] snums; 					// Substrings read from the map file
        int [] nums;                        // Numbers converted from strings in snums

        //load map based on current level from resources if not assigned in inspector
        if (map == null) {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            map = Resources.Load<TextAsset>($"Maps/map_{sceneName.ToLower()}");

            if (map == null) {
                Debug.LogError($"No map found for scene {sceneName}");
                return;
            }
        }

        // Split the string of the whole map file into substrings separated by spaces
        snums = map.text.Split(seps, StringSplitOptions.RemoveEmptyEntries);
		
		// Convert the substrings in snums to integers
        nums = new int[snums.Length];
        for (int i = 0; i < snums.Length; i++)
        {
            nums[i] = int.Parse(snums[i]);
        }
		
		// Create the level. First get the size in tiles of the map from nums
        int sizeX = nums[0], sizeZ = nums[1];
		
		// Process the map. For each tileId == 2 create a copy of the tile prefab
        for(int z=0; z<sizeZ; z++)
            for(int x=0; x<sizeX; x++)
            {
                int tileValue = nums[z * sizeX + x + 2];
                if (tileValue == 2 || tileValue == 4) //we put a tile even if player initial position
                {
                    // Instantiate the copy at its corresponding location
                    //Instantiate(tile, new Vector3(x, 0.0f, z), transform.rotation);
                    GameObject obj = Instantiate(tile, new Vector3(x, -0.05f, z), transform.rotation);
					
					// Set the new object parent to be the game object containing this script
                    obj.transform.parent = transform;
                }
                else if (tileValue == 3) {
                    // Instantiate victory hole (no tile, just a trigger zone)
                    GameObject obj = Instantiate(VictoryHole != null ? VictoryHole : tile,
                                                  new Vector3(x, -0.5f, z),
                                                  transform.rotation);
                    obj.transform.parent = transform;

                    // Make sure it has the VictoryHole tag and is a trigger
                    obj.tag = "LevelPass";

                    // Ensure it has a collider set as trigger
                    Collider col = obj.GetComponent<Collider>();
                    if (col != null) {
                        col.isTrigger = true;
                    }
                }
                if (tileValue == 4) {
                    if (player != null) {
                        // Position player at start tile (slightly above so it doesn't clip)
                        player.transform.position = new Vector3(x, 0.5f, z);
                        Debug.Log($"Player spawned at ({x}, {z})");
                    }
                    else {
                        Debug.LogWarning("Player reference not set in MapCreation!");
                    }
                }
            }
    }

}
