using UnityEngine;

public class TeleporterBullet : BaseBullet
{
    protected override void DefaultCollisionBehavior(GameObject hitObject)
    {
        // Duvar'a çarparsa dur
        if (hitObject.GetComponent<Wall>() != null)
        {
            DestroyBullet();
            return;
        }

        if (hitObject.GetComponent<Hay>() != null)
        {
            InitializeTeleport(hitObject);
            DestroyBullet();
            return;
        }

        IRobot robot = hitObject.GetComponent<IRobot>();
        if (robot != null)
        {
            InitializeTeleport(hitObject);
            DestroyBullet();
            return;
        }

        Health health = hitObject.GetComponent<Health>();
        if (health != null)
        {
            InitializeTeleport(hitObject);
            DestroyBullet();
            return;
        }
    }

    void InitializeTeleport(GameObject hitObject)
    {
        if (owner != null && hitObject != null)
        {
            Vector3 temp = owner.transform.position;
            owner.transform.position = hitObject.transform.position;
            hitObject.transform.parent.gameObject.transform.position = temp;

            Debug.Log($"Teleported {owner.name} to {hitObject.name} position and vice versa.");
        }
        else
        {
            string debugtext = (owner == null)
                ? "Owner is null, cannot teleport."
                : "Hit object is null, cannot teleport.";

            Debug.LogWarning(debugtext);
        }
    }
}
