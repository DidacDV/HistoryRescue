using System;
using UnityEngine;
using UnityEngine.InputSystem;


// MoveCube manages cube movement. WASD + Cursor keys rotate the cube in the
// selected direction. If the cube is not grounded (has a tile under it), it falls.
// Victory condition: cube must be standing upright and fall into the victory hole.


public class MoveCube : MonoBehaviour {
    InputAction moveAction; 		// Input action to capture player movement (WASD + cursor keys)

    bool bMoving = false;           // Is the object in the middle of moving?
    bool bFalling = false; 			// Is the object falling?
    bool bLevelPassed = false;      // Has the level been completed?

    public float rotSpeed; 			// Rotation speed in degrees per second
    public float fallSpeed; 		// Fall speed in the Y direction

    Vector3 rotPoint, rotAxis;      // Rotation movement is performed around the line formed by rotPoint and rotAxis
    float rotRemainder; 			// The angle that the cube still has to rotate before the current movement is completed
    float rotDir; 					// Has rotRemainder to be applied in the positive or negative direction?
    LayerMask layerMask; 			// LayerMask to detect raycast hits with ground tiles only

    public AudioClip[] sounds; 		// Sounds to play when the cube rotates
    public AudioClip fallSound; 	// Sound to play when the cube starts falling
    public AudioClip victorySound;  // Sound to play when level is passed


    // Determine if the cube is standing upright (vertical orientation) to recreate bloxorz o como se diga
    bool IsStandingUpright() {
        //actual logic should go here
        return true;
    }

    // Determine if the cube is grounded by shooting a ray down from the cube location and 
    // looking for hits with ground tiles
    bool isGrounded() {
        RaycastHit hit;

        // Cast ray from cube position downward
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.5f, layerMask)) {
            return true;
        }

        return false;
    }

    // Check if we're over the victory hole and in correct position
    void CheckVictoryHole() {
        RaycastHit hit;

        // Cast ray downward to detect victory hole
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2.0f)) {
            if (hit.collider.CompareTag("LevelPass")) {
                // Check if cube is standing upright
                if (IsStandingUpright()) {
                    Debug.Log("CORRECT POSITION! Falling into victory hole");
                    bFalling = true;
                    bLevelPassed = true;

                    //play victory sound
                    if (victorySound != null) {
                        //AudioSource.PlayClipAtPoint(victorySound, transform.position, 1.5f);
                    }
                }
                else {
                    Debug.Log("Wrong orientation");
                }
            }
        }
    }

    void onFallOff() {
        if (!bLevelPassed) {
            Debug.Log("FELL OFF THE MAP, YOU LOSE!");
            //reset level
        }
    }

    void onLevelPass() {
        Debug.Log("LEVEL PASSED!");
        //next level
    }

    //Start is called once after the MonoBehaviour is created
    void Start() {
        // Find the move action by name. Done once in the Start method to avoid doing it every Update call.
        moveAction = InputSystem.actions.FindAction("Move");

        // Create the layer mask for ground tiles. Done once in the Start method to avoid doing it every Update call.
        layerMask = LayerMask.GetMask("Ground");
    }

    // Update is called once per frame
    void Update() {
        if (bFalling) {
            // If we have fallen, we just move down
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

            if (transform.position.y < -5.0f) {
                // triggered thanks to falling enough
                if (bLevelPassed) {
                    onLevelPass();
                }
                else {
                    onFallOff();
                }
            }
        }
        else if (bMoving) {
            // If we are moving, we rotate around the line formed by rotPoint and rotAxis an angle depending on deltaTime
            // If this angle is larger than the remainder, we stop the movement
            float amount = rotSpeed * Time.deltaTime;
            if (amount > rotRemainder) {
                transform.RotateAround(rotPoint, rotAxis, rotRemainder * rotDir);
                bMoving = false;

                // After movement completes, check if we're on victory hole
                CheckVictoryHole();
            }
            else {
                transform.RotateAround(rotPoint, rotAxis, amount * rotDir);
                rotRemainder -= amount;
            }
        }
        else {
            // If we are not falling, nor moving, we check first if we should fall, then if we have to move
            if (!isGrounded()) {
                // Check if we're falling into victory hole or just falling off
                CheckVictoryHole();

                if (!bLevelPassed) {
                    bFalling = true;

                    // Play sound associated to falling
                    AudioSource.PlayClipAtPoint(fallSound, transform.position, 1.5f);
                }
            }

            // Read the move action for input
            Vector2 dir = moveAction.ReadValue<Vector2>();
            if (Math.Abs(dir.x) > 0.99 || Math.Abs(dir.y) > 0.99) {
                // If the absolute value of one of the axis is larger than 0.99, the player wants to move in a non diagonal direction
                bMoving = true;

                // We play a random movement sound
                int iSound = UnityEngine.Random.Range(0, sounds.Length);
                AudioSource.PlayClipAtPoint(sounds[iSound], transform.position, 1.0f);

                // Set rotDir, rotRemainder, rotPoint, and rotAxis according to the movement the player wants to make
                if (dir.x > 0.99) {
                    rotDir = -1.0f;
                    rotRemainder = 90.0f;
                    rotAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rotPoint = transform.position + new Vector3(0.5f, -0.5f, 0.0f);
                }
                else if (dir.x < -0.99) {
                    rotDir = 1.0f;
                    rotRemainder = 90.0f;
                    rotAxis = new Vector3(0.0f, 0.0f, 1.0f);
                    rotPoint = transform.position + new Vector3(-0.5f, -0.5f, 0.0f);
                }
                else if (dir.y > 0.99) {
                    rotDir = 1.0f;
                    rotRemainder = 90.0f;
                    rotAxis = new Vector3(1.0f, 0.0f, 0.0f);
                    rotPoint = transform.position + new Vector3(0.0f, -0.5f, 0.5f);
                }
                else if (dir.y < -0.99) {
                    rotDir = -1.0f;
                    rotRemainder = 90.0f;
                    rotAxis = new Vector3(1.0f, 0.0f, 0.0f);
                    rotPoint = transform.position + new Vector3(0.0f, -0.5f, -0.5f);
                }
            }
        }
    }

}