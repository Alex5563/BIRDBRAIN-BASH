using UnityEngine;

public class BallManager : MonoBehaviour
{
    public Vector3 goingTo; // Where the ball is going to
    public GameManager gameManager; // Game manager object
    private Rigidbody rb; // Rigidbody of the ball

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the ball's rigidbody
        rb = GetComponent<Rigidbody>();

        if (gameManager == null)
        {
            Debug.LogError("Game Manager was not set in the inspector for Ball Manager!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Calls whenever the character collides with another collider or rigidbody
    void OnCollisionEnter(Collision other)
    {
        // Activate gravity it's not already activated and the game state is NOT serving
        if (!rb.useGravity && !gameManager.gameState.Equals(GameManager.GameState.PointStart))
        {
            rb.useGravity = true;
        }
    }
}
