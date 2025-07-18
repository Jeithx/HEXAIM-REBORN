using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    void LateUpdate()  // Body döndükten sonra
    {
        transform.rotation = Quaternion.identity;
    }
}
