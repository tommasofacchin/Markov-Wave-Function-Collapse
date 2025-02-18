using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public float speed = 1.0f; 

    public Vector3 startPosition;
    public Vector3 targetPosition;
    private bool isMoving = false;


    void Update()
    {
        if (!isMoving)
        {
            MoveCamera();
        }
    }

    public void StartMoving(Vector3 newTargetPosition)
    {
        targetPosition = newTargetPosition;
        isMoving = true;
    }

    private void MoveCamera()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        if (transform.position == targetPosition)
        {
            isMoving = false;
        }
    }
}
