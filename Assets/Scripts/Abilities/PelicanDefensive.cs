using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PelicanDefensive : MonoBehaviour
{
    public float cooldown; // Cooldown in seconds
    public int holdLength; // Amount the ability increases ally's stats
    public BallInteract ballInteract;
    public GameObject ball;
    public GameManager gameManager;
    private bool onCooldown = false;
    private bool isBallEaten = false;

    void Update()
    {
        // If pressesd defensive ability button, activate ability
        if (InputSystem.actions.FindAction("Defensive Ability").WasPressedThisFrame())
        {
            EatTheBall();
        }
        if (isBallEaten && InputSystem.actions.FindAction("Serve").WasPressedThisFrame())
         {
             Debug.Log("Pelican released the ball manually.");
             if (ball != null)
             {
                 ball.SetActive(true);
                 isBallEaten = false;
             }
         }
        if (isBallEaten)        
        {
            ball.transform.position = transform.position + new Vector3(0, 1f, 0);
        }
        
    }
    
    public void Start()
    {
        ballInteract = GetComponent<BallInteract>();
        ball = GameObject.FindGameObjectWithTag("Ball");
    }

    public void EatTheBall()
    {
        // Only runs if not on cooldown
        if (!onCooldown)
        {
            if (gameManager.gameState.Equals(GameManager.GameState.PointStart) && gameManager.server == gameObject) //add some way to check if the pelican is the one serving
            {
                Debug.Log("Pelican has eaten the ball.");
                if (ball != null)
                {
                    ballInteract.ServeBall();
                    ball.SetActive(false);
                    isBallEaten = true;
                }
                StartCoroutine(Cooldown());
                StartCoroutine(HoldTime());
            }
        } else
        {
            Debug.Log("Pelican Defensive is on cooldown!");
        }
    }

    

    // Cools down cooldown seconds
    public IEnumerator Cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

    public IEnumerator HoldTime()
    {
        yield return new WaitForSeconds(holdLength);
        ball.SetActive(true);
        isBallEaten = false;
        ballInteract.ServeBall();
        Debug.Log("Pelican hold length is up!");
    }
}