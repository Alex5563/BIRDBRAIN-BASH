using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Animator))]

// This class will manage the ragdoll behavior for the player character when they are hit by certain abilities or projectiles
public class RagdollManager : MonoBehaviour
{
    Animator animator;
    private void Awake() {
        animator = GetComponent<Animator>();
    }

    public void ActivateRagdoll()
    {
        // Disable the character's animator and enable physics on the character's rigidbodies
        GetComponentsInChildren<Rigidbody>().ToList().ForEach(rb => rb.isKinematic = false);
        GetComponentsInChildren<Collider>().ToList().ForEach(c => c.enabled = true);
        animator.enabled = false;
        Debug.Log("Ragdoll activated!");
    }

    public void DeactivateRagdoll()
    {
        // Disable ragdoll physics and re-enable the character's animator
        GetComponentsInChildren<Rigidbody>().ToList().ForEach(rb => rb.isKinematic = true);
        GetComponentsInChildren<Collider>().ToList().ForEach(c => c.enabled = false);
        animator.enabled = true;
        Debug.Log("Ragdoll deactivated!");
    }
}
