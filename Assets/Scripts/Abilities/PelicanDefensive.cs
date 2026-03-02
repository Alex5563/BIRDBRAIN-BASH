using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PelicanDefensive : MonoBehaviour
{
    [SerializeField]
    public float cooldown; // Cooldown in seconds
    [SerializeField]
    public int holdLength; // Amount the ability increases ally's stats
    public GameManager gameManager;
    private bool _onLeft;
    private bool onCooldown = false;

    void Update()
    {
        // If pressesd defensive ability button, activate ability
        if (InputSystem.actions.FindAction("Defensive Ability").WasPressedThisFrame())
        {
            EatTheBall();
        }
    }
    
    public void Start()
    {
        _onLeft = GetComponent<BallInteract>().onLeft;
    }

    public void EatTheBall()
    {
        // Only runs if not on cooldown
        if (!onCooldown)
        {
            Debug.Log("Pelician be eatin the ball!!!!!");
            StartCoroutine(Cooldown());
        } else
        {
            Debug.Log("On Cooldown :C");
        }
    }

    // Cools down cooldown seconds
    public IEnumerator Cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

}