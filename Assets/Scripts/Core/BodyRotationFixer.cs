using UnityEngine;

public class BodyRotationFixer : MonoBehaviour
{ 

    void LateUpdate()
    {
    // Rotation'ı sıfırla
    transform.rotation = Quaternion.identity; }

}