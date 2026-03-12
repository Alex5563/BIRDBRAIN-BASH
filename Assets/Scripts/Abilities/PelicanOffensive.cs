using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]

// Slip Fish - Spit a fish into the opponent's court (wherever the bird is facing) and watch them slip 
// (fish will disappear after 15s) (15s cooldown after fish disappears)
public class PelicanOffensive : MonoBehaviour
{
    [SerializeField]
    private float cooldown = 30f; // Cooldown in seconds (30 because the fish disappears after 15s, so 15s cooldown after that)
    private bool onCooldown = false;
    [SerializeField]
    private float slipFishSpeed = 15f; // Speed at which the fish is spit out
    [SerializeField]
    private float fishLifetime = 15f;
    [SerializeField]
    private GameObject fishPrefab; // assign in inspector until there's a permanent spot for it
    private PlayerInput playerInput;

    private void Awake() 
    {
        playerInput = GetComponent<PlayerInput>();   
    }

    // Update is called once per frame
    private void Update()
    {
        // If pressed offensive ability button, activate ability
        if (playerInput.actions.FindAction("Offensive Ability").WasPressedThisFrame())
        {
            SlipFish();
        }      
    }

    private void SlipFish()
    {
        if (onCooldown) return;

        // Instantiate at the pelican's position and rotation
        GameObject fish = Instantiate(fishPrefab, transform.position + transform.forward, transform.rotation);
        
        // Add velocity to the fish to make it move forward at an arc so it goes over the net
        if (fish.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = transform.forward * slipFishSpeed + Vector3.up * (slipFishSpeed / 2);
        }

        // Destroy the fish after its lifetime expires
        Destroy(fish, fishLifetime);

        onCooldown = true;
        StartCoroutine(Cooldown());

        // Play sound effect for spitting fish (TO BE IMPLEMENTED)
        // AudioSource.PlayClipAtPoint(spitSound, transform.position);
    }

    private System.Collections.IEnumerator Cooldown()
    {
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
}
