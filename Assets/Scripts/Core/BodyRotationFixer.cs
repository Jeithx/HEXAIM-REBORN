using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    void LateUpdate()  // Body d�nd�kten sonra
    {
        transform.rotation = Quaternion.identity;
    }
}
