using UnityEngine;

public class TileAnimData {
    public GameObject tile;
    public Vector3 startPosition;
    public Vector3 targetPosition;
    public Vector3 initialOffset;
    public float speed;
    public float progress;

    //used for pass level animation spinning
    public TileAnimData(GameObject tile, Vector3 start, Vector3 target, Vector3 offset, float speed) {
        this.tile = tile;
        this.startPosition = start;
        this.targetPosition = target;
        this.initialOffset = offset;
        this.speed = speed;
        this.progress = 0f;
    }

    public TileAnimData(GameObject tile, Vector3 start, Vector3 target, float speed)
        : this(tile, start, target, Vector3.zero, speed) {
    }
}