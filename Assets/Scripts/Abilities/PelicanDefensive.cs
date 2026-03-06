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
             Debug.Log("Pelican released the ball!!!!!");
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
            Debug.Log("Pelician be eatin the ball!!!!!");
            if (gameManager.gameState.Equals(GameManager.GameState.PointStart)) //add some way to check if the pelican is the one serving
            {
                Debug.Log("BALL HAS BEEN EATEN");
                if (ball != null)
                {
                    ballInteract.ServeBall();
                    ball.SetActive(false);
                    isBallEaten = true;
                }
                StartCoroutine(Cooldown());
            }
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