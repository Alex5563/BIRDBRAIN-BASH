using UnityEngine;
using System;

public class AIBehavior : MonoBehaviour
{
    [Header("Game Manager")]
    public GameManager gameManager;
    public bool onLeft;

    [Header("Ball Manager")]
    public BallManager ballManager;
    public float interactionRadius = 5f;

    [Header("Movement Attributes")]
    public float maxGroundSpeed = 1.0f; // Max speed that the character can move on the ground
    public float maxAirSpeed = 1.0f; // Max speed that the character can move in the air
    public float jumpForce = 1.0f; // Force the character uses to jump 

    private float directionChangeWeight = 15f; // How quickly the character can change direction
    private bool grounded = false; // If the character is touching the ground
    private GameObject ball;
    private Rigidbody ballRb;
    private Vector3 bumpToLocation; // Where the ball will go after bumping
    private Vector3 setToLocation; // Where the ball will go after setting
    private Vector3 spikeToLocation; // Where the ball will go after spiking
    private Vector3 serveToLocation; // Where the ball will go after serving
    private float spikeSpeed; // Speed of the ball when spiked
    private float timeTilServe; // Time remaining until AI can serve

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ball = GameObject.FindGameObjectWithTag("Ball");
        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
        }
        else
        {
            Debug.LogError("Ball game object was not found for AIBehavior!");
        }

        if (ballManager == null)
        {
            Debug.LogError("Ball Manager was not set in inspector for AIBehavior!");
        }

        spikeSpeed = 10f;
        timeTilServe = 2f;
    }

    // Update is called once per frame
    void Update()
    {
        if (ball != null)
        {
            CheckState();
        }
    }

    private void CheckState()
    {
        // If your team is setting up for an attack OR the other team has just spiked
        if (CanHit())
        {
            switch (gameManager.gameState)
            {
                case GameManager.GameState.Spiked: case GameManager.GameState.Served:
                    if (IsAINearBall() && ballRb.linearVelocity.y < 0)
                    {
                        BumpBall();
                    }
                    else
                    {
                        MoveAI(true);
                    }
                    break;
                case GameManager.GameState.Bumped:
                    if (IsAINearBall() && ballRb.linearVelocity.y < 0)
                    {
                        SetBall();
                    }
                    else
                    {
                        MoveAI(true);
                    }
                    break;
                case GameManager.GameState.Set:
                    // See if the AI is underneath the ball, jump when the ball is coming down, and spike it when close
                    Vector2 aiPos = new Vector2(transform.position.x, transform.position.z);
                    Vector2 ballPos = new Vector2(ballRb.transform.position.x, ballRb.transform.position.z);

                    if (ballRb.linearVelocity.y < 0)
                    {
                        if (Vector2.Distance(aiPos, ballPos) > 1f)
                        {
                            MoveAI(true);
                        }
                        else if (grounded)
                        {
                            GetComponent<Rigidbody>().linearVelocity += new Vector3(0, jumpForce, 0);
                            grounded = false;
                        }
                        else if (IsAINearBall())
                        {
                            SpikeBall();
                        }
                    }
                    else
                    {
                        if (Vector2.Distance(aiPos, ballPos) > 1f)
                        {
                            MoveAI(true);
                        }
                    }
                    break;
                case GameManager.GameState.PointStart:
                    // If this AI is the one serving
                    if (gameManager.server == gameObject)
                    {
                        // If the AI can serve ball, do so. Otherwise, wait.
                        if (timeTilServe < 0f)
                        {
                            ServeBall();
                        }
                        else
                        {
                            timeTilServe -= Time.deltaTime;
                        }
                    }
                    break;
            }
        }
        // Reposition for defense ONLY IF the ball is not about to be served
        else if (!gameManager.gameState.Equals(GameManager.GameState.PointStart))
        {
            MoveAI(false);
        }
    }

    private bool CanHit()
    {
        // If this AI just hit the ball, they cannot hit it again
        if (gameObject.Equals(gameManager.lastHit)) return false;

        // If the ball has been served by the other team, they can hit
        if (!gameManager.leftAttack.Equals(onLeft) && gameManager.gameState.Equals(GameManager.GameState.Served)) return true;

        // If it's the AI's turn to serve, they can hit
        if (gameManager.leftAttack.Equals(onLeft) && gameManager.gameState.Equals(GameManager.GameState.PointStart)
            && gameManager.server == gameObject) return true;

        // If the ball has been served by a teammate, they cannot hit it
        if (gameManager.leftAttack.Equals(onLeft) && gameManager.gameState.Equals(GameManager.GameState.Served)) return false;

        // If the ball is on this side of the court and it has not been spiked yet, they can hit it
        if (gameManager.leftAttack.Equals(onLeft) && !gameManager.gameState.Equals(GameManager.GameState.Spiked)) return true;

        // If the ball is on the other side of the court and has been spiked, they can hit, else they cannot
        return !gameManager.leftAttack.Equals(onLeft) && gameManager.gameState.Equals(GameManager.GameState.Spiked);
    }

    private bool IsAINearBall()
    {
        float distance = Vector3.Distance(transform.position, ball.transform.position);
        return distance <= interactionRadius;
    }
    
    private void MoveAI(bool towardsBall)
    {
        // Initialize target for AI
        Vector3 target = new Vector3(5, 0, 0);
        if (onLeft)
        {
            target *= -1;
            
            if (gameManager.leftPlayer1.Equals(gameObject))
            {
                target += new Vector3(0, 0, 2);
            }
            else
            {
                target -= new Vector3(0, 0, 2);
            }
        }
        else
        {
            if (gameManager.rightPlayer1.Equals(gameObject))
            {
                target += new Vector3(0, 0, 2);
            }
            else
            {
                target -= new Vector3(0, 0, 2);
            }
        }


        // If the AI should move towards the ball
        if (towardsBall)
        {
            target = ballManager.goingTo;
        }

        // Get the AI's rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();

        // Get the direction the AI needs to move in
        float dx = target.x - transform.position.x;
        float dz = target.z - transform.position.z;
        Vector2 dir = new Vector2(dx, dz);

        // Update the current direction and speed of the character based on player input
        if (!dir.Equals(Vector2.zero))
        {
            // Calculate new velocity, ensure it doesn't exceed max ground or air speed, then assign the velocity
            Vector2 newVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z) + dir * Time.fixedDeltaTime * directionChangeWeight;

            if (grounded && newVelocity.magnitude > maxGroundSpeed)
            {
                newVelocity.Normalize();
                newVelocity *= maxGroundSpeed;
            }
            else if (!grounded && newVelocity.magnitude > maxAirSpeed)
            {
                newVelocity.Normalize();
                newVelocity *= maxAirSpeed;
            }

            rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.y);
        }
    }

    private void BumpBall()
    {
        // Set bump to location to front middle of whatever side of the court is bumping
        bumpToLocation = new Vector3(2f, 0f, 0f);
        if (onLeft)
        {
            bumpToLocation *= -1;
        }
        
        // Set the ball's intial velocity and destination
        SetBallInitVelocity(ballRb, bumpToLocation, 5.0f);
        ballManager.goingTo = bumpToLocation;

        // Update game manager fields
        gameManager.gameState = GameManager.GameState.Bumped;
        gameManager.lastHit = gameObject;
        gameManager.leftAttack = onLeft;
    }

    private void SetBall()
    {        
        if (ballRb != null)
        {
            // Set the setting location to middle of court as default
            setToLocation = bumpToLocation;

            // Randomly decide to set it elsewhere
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand < 0.33f)
            {
                setToLocation += new Vector3(0, 0, 4);
            }
            else if (rand < 0.66f)
            {
                setToLocation -= new Vector3(0, 0, 4);
            }

            // Set the ball's initial velocity and destination
            SetBallInitVelocity(ballRb, setToLocation, 6.0f);
            ballManager.goingTo = setToLocation;

            // Update game manager fields
            gameManager.gameState = GameManager.GameState.Set;
            gameManager.lastHit = gameObject;
        }
        else
        {
            Debug.LogWarning("Ball has no Rigidbody component!");
        }
    }
    private void SpikeBall()
    {
        if (ballRb != null)
        {
            // Set the spiking location to middle-back of court on the rightside as default
            spikeToLocation = new Vector3(8, 0, 0);

            // If rightside is spiking, switch to spike towards leftside
            if (!onLeft)
            {
                spikeToLocation *= -1;
            }

            // Randomly decide to set it elsewhere
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand < 0.33f)
            {
                spikeToLocation += new Vector3(0, 0, 4);
            }
            else if (rand < 0.66f)
            {
                spikeToLocation -= new Vector3(0, 0, 4);
            }

            // Set the ball's initial velocity and destination
            SetBallInitVelocity(ballRb, spikeToLocation, -1.0f);
            ballManager.goingTo = spikeToLocation;

            // Update game manager fields
            gameManager.gameState = GameManager.GameState.Spiked;
            gameManager.lastHit = gameObject;
        }
        else
        {
            Debug.LogWarning("Ball has no Rigidbody component!");
        }
    }

    private void ServeBall()
    {
        if (ballRb != null)
        {
            // Set the serving location to middle-back of court on the rightside as default
            serveToLocation = new Vector3(8, 0, 0);

            // If rightside is spiking, switch to serve towards leftside
            if (!onLeft)
            {
                serveToLocation *= -1;
            }

            // Randomly decide to set it elsewhere
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand < 0.33f)
            {
                serveToLocation += new Vector3(0, 0, 4);
            }
            else if (rand < 0.66f)
            {
                serveToLocation -= new Vector3(0, 0, 4);
            }

            // Set the ball's initial velocity and destination
            SetBallInitVelocity(ballRb, serveToLocation, 5.0f);
            ballManager.goingTo = serveToLocation;

            // Update game manager fields
            gameManager.gameState = GameManager.GameState.Served;
            gameManager.lastHit = gameObject;
            gameManager.leftAttack = onLeft;
            
            // Reset timer for serve
            timeTilServe = 2.0f;
        }
        else
        {
            Debug.LogWarning("Ball has no Rigidbody component!");
        }
    }

    private void SetBallInitVelocity(Rigidbody ballRb, Vector3 endLocation, float maxHeight)
    {
        // Bumping, setting, or serving
        if (maxHeight > ballRb.transform.position.y)
        {
            // If gravity is disabled, enable it
            if (!ballRb.useGravity)
            {
                ballRb.useGravity = true;
            }

            // Calculate the velocity in the y direction for the ball to reach a height of 5 given its current y component
            float gravity = MathF.Abs(Physics.gravity.y);
            float vyInit = MathF.Sqrt(2 * gravity * (maxHeight - ballRb.transform.position.y));

            // Calculate time the ball will be in the air
            float vyFinal = MathF.Sqrt(10 * gravity);
            float t1 = vyInit / gravity;
            float t2 = vyFinal / gravity;
            float t = t1 + t2; 

            // Calculate the x and z velocities of the ball
            float vx = (endLocation.x - ballRb.transform.position.x) / t;
            float vz = (endLocation.z - ballRb.transform.position.z) / t;

            // Set the ball's intial velocity
            ballRb.linearVelocity = new Vector3(vx, vyInit, vz);
        }
        else // Spiking or blocking
        {
            // If gravity is enabled, disable it
            if (ballRb.useGravity)
            {
                ballRb.useGravity = false;
            }

            // Calculate the direction the ball will go in
            Vector3 initVel = endLocation - ballRb.transform.position;

            // Set speed of inital velocity
            initVel.Normalize();
            initVel *= spikeSpeed;

            // Set the ball's intial velocity
            ballRb.linearVelocity = initVel;
        }
    }

    // Calls whenever the character collides with another collider or rigidbody
    void OnCollisionEnter(Collision other)
    {
        // If the character collides with the court, it is now grounded
        if (other.gameObject.layer == 6)
        {
            grounded = true;
            // Debug.Log("AI has landed.");
        }
    }

    // Calls whenever the character stops colliding with another collider or rigidbody
    void OnCollisionExit(Collision other)
    {
        // If the character stops colliding with the court, it is no longer grounded
        if (other.gameObject.layer == 6)
        {
            grounded = false;
            Debug.Log("AI has jumped.");
        }
    }
}
