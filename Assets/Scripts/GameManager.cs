using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Left Team")]
    public GameObject leftPlayer1; // First player on left side
    public GameObject leftPlayer2; // Second player on left side

    [Header("Right Team")]
    public GameObject rightPlayer1; // First player on right side
    public GameObject rightPlayer2; // Second player on right side

    [Header("Game Manager Stuff")]
    public GameState gameState; // State of the match
    public GameObject lastHit; // Player that had the last hit
    public GameObject server; // Player who serves this point
    public bool leftAttack; // If left is attacking
    public GameObject ball; // Ball object

    private Vector3 leftPlayer1Origin;
    private Vector3 leftPlayer2Origin;
    private Vector3 rightPlayer1Origin;
    private Vector3 rightPlayer2Origin;
    private Vector3 ballOrigin;
    private Vector3 leftServeLocation;
    private Vector3 rightServeLocation;

    public enum GameState
    {
        PointStart,
        Served,
        Bumped,
        Set,
        Spiked,
        Blocked
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set the game state to bumping
        gameState = GameState.PointStart;

        // Set the last hit to null
        lastHit = null; 

        // Set the server to the first player on the right team
        server = rightPlayer1;
        leftAttack = false;

        // Get all starting positions of players and ball
        if (leftPlayer1 != null)
        {
            leftPlayer1Origin = leftPlayer1.transform.position;
        }
        else
        {
            Debug.LogError("Left player 1 not set in inspector for Game Manager!");
        }

        if (leftPlayer2 != null)
        {
            leftPlayer2Origin = leftPlayer2.transform.position;
        }
        else
        {
            Debug.LogError("Left player 2 not set in inspector for Game Manager!");
        }

        if (rightPlayer1 != null)
        {
            rightPlayer1Origin = rightPlayer1.transform.position;
        }
        else
        {
            Debug.LogError("Right player 1 not set in inspector for Game Manager!");
        }

        if (rightPlayer2 != null)
        {
            rightPlayer2Origin = rightPlayer2.transform.position;
        }
        else
        {
            Debug.LogError("Right player 2 not set in inspector for Game Manager!");
        }

        if (ballOrigin != null)
        {
            ballOrigin = ball.transform.position;
        }
        else
        {
            Debug.LogError("Ball origin not set in inspector for Game Manager!");
        }

        // Set the locations for the left and right serve location to be just outside of the court
        leftServeLocation = new Vector3(-10, 1, 0);
        rightServeLocation = new Vector3(10, 1, 0);

        // Start the point
        NextPoint();
    }

    public void RotateServer()
    {
        // Order for serve rotation:
        // 1st: RP1, 2nd: LP1, 3rd: RP2, 4th: LP2, then start over
        if (server == rightPlayer1)
        {
            server = leftPlayer1;
            leftAttack = true;
        }
        else if (server == leftPlayer1)
        {
            server = rightPlayer2;
            leftAttack = false;
        }
        else if (server == rightPlayer2)
        {
            server = leftPlayer2;
            leftAttack = true;
        }
        else
        {
            server = rightPlayer1;
            leftAttack = false;
        }
        Debug.LogFormat("Rotated server: {0}", server);
    }

    void OnReset()
    {
        // Reset all positions and velocities for all players and ball
        leftPlayer1.transform.position = leftPlayer1Origin;
        leftPlayer2.transform.position = leftPlayer2Origin;
        rightPlayer1.transform.position = rightPlayer1Origin;
        rightPlayer2.transform.position = rightPlayer2Origin;
        ball.transform.position = ballOrigin;

        leftPlayer1.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        leftPlayer2.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        rightPlayer1.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        rightPlayer2.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        // Enable gravity for the ball
        ball.GetComponent<Rigidbody>().useGravity = true;

        // Reset the game manager fields
        gameState = GameState.PointStart;
        server = rightPlayer1;
        lastHit = null;
        leftAttack = false;
    }

    public void NextPoint()
    {
        // Reset all positions and velocities for all players
        leftPlayer1.transform.position = leftPlayer1Origin;
        leftPlayer2.transform.position = leftPlayer2Origin;
        rightPlayer1.transform.position = rightPlayer1Origin;
        rightPlayer2.transform.position = rightPlayer2Origin;

        leftPlayer1.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        leftPlayer2.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        rightPlayer1.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        rightPlayer2.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        Debug.LogFormat("Left side is attacking: {0}", leftAttack);
        // Set server's and ball's position
        if (leftAttack)
        {
            server.transform.position = leftServeLocation;
            ball.transform.position = leftServeLocation + new Vector3(1, 0, 0);
        }
        else
        {
            server.transform.position = rightServeLocation;
            ball.transform.position = rightServeLocation - new Vector3(1, 0, 0);
        }

        // Disable gravity for the ball
        ball.GetComponent<Rigidbody>().useGravity = false;

        // Reset the game manager fields
        gameState = GameState.PointStart;
        lastHit = null;
    }
}
