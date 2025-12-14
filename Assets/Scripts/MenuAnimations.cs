using UnityEngine;
using System.Collections;

public class MenuAmination : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 landPosition = new Vector3(2.93f, -0.25f, 62.3f);
    public Vector3 startOffset = new Vector3(0, 10, 0);
    public float dropDuration = 1.5f;
    public float spinSpeed = 30f;
    public float initialZRotation = -40f;

    [Header("Skin Settings")]
    public GameObject[] skinPrefabs;
    public float changeInterval = 10f;
    public Transform visualContainer;


    private GameObject currentModelInstance;

    void Start()
    {
        transform.position = landPosition + startOffset;
        transform.rotation = Quaternion.Euler(0, 0, initialZRotation);

        LoadSkin(0);

        StartCoroutine(AnimateEntrance());
    }

    void Update()
    {
        if (currentModelInstance != null)
            currentModelInstance.transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.Self);
    }

    IEnumerator AnimateEntrance()
    {
        Vector3 startPos = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < dropDuration)
        {
            float t = elapsedTime / dropDuration;
            t = Mathf.SmoothStep(0.0f, 1.0f, t); // Easing

            transform.position = Vector3.Lerp(startPos, landPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = landPosition;
    }

    void LoadSkin(int index)
    {
        if (skinPrefabs.Length == 0) return;

        if (currentModelInstance != null) Destroy(currentModelInstance);

        Transform parent = visualContainer != null ? visualContainer : transform;
        currentModelInstance = Instantiate(skinPrefabs[index], parent);
        currentModelInstance.transform.localPosition = Vector3.zero;
        currentModelInstance.transform.localRotation = Quaternion.identity;
    }
}