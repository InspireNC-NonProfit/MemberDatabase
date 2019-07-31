using UnityEngine;

public class LoadingAnimation : MonoBehaviour
{
    [SerializeField]
    private RectTransform rectComponent;
    public float rotateSpeed = 200f;

    // Update is called once per frame
    void Update()
    {
        rectComponent.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
