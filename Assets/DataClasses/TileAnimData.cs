using UnityEngine;

public class TileAnimData {
    public GameObject tile;
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public float speed;
    public float progress;

    public TileAnimData(GameObject tile, Vector3 start, Vector3 target, float speed) {
        this.tile = tile;
        this.startPosition = start;
        this.targetPosition = target;
        this.speed = speed;
        this.progress = 0f;
    }
}