using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenguinScript : BirdAbility
{
    [Header("Dash Ability")]
    public float dashCooldown = 5.0f;
    public float dashDuration = 1.0f;
    public float dashSpeed = 10.0f; // Forward movement speed during dash
    public float rotationSpeed = 8.0f; // How fast penguin rotates
    [SerializeField] public GameObject tempIce;
    GameObject iceInstance;

    public float penguinHeight; // christofort: height of the penguin for ground check

    [HideInInspector] public bool isDashing = false;
    private bool isReturningUpright = false; // Ensure penguin returns to upright after dash
    private float dashTimer = 0.0f; // Tracker for the cooldown
    private float cooldownTimer = 0.0f;
    private CharacterMovement characterMovement; // Track movement from character movement script
    private BallInteract ballInteraction; // Christofort: get the ball interaction spike code
    private BallManager ballManager; // Christofort: get the balls info
    private Rigidbody rb;
    private PlayerInput playerInput; // Input for this specific player

    [Header("Snowball Ability")]
    private float snowBallCooldown = 12.0f; // Christofort: temporary cooldown
    private float iceLength = 5.0f; // christofort: how long the ice effect lasts
    private float iceTimer = 0.0f; // christofort: tracker for the ice effect
    private float snowBallTimer = 0.0f; // Christofort: tracker for the cooldown
    private bool iceMode = false; // Christofort: track if snowball is active
    private bool iceSpawned = false; // Christofort: track if ice has been spawned to prevent multiple spawns
    private GameManager.GameState currentState; // Christofort: var for the gameState
    private GameManager.GameState stateCheck; // Christofort: to check the current gameState
    public GameObject dodgeBall; // Christofort: grabs the dodgeball's position
    public Collider ballCollider; // Christofort: grabs the dodgeball's collider
    public BoxCollider iceCollider; // Christofort: grabs the ice's collider
    Vector3 spawnPoint; // Christofort: where to spawn the Dodgeball
    [HideInInspector] public bool usingSnowBall = false;

    [Header("Snowball Visual")]
    public Material normalBallMaterial; // Default ball material to restore after snowball ends
    public Material snowballMaterial; // Material to apply to the ball when snowball is active
    private Renderer[] dodgeBallRenderers; // Christofort: grabs the dodgeball's renderer to swap materials

    // New: keep track of the active coroutine so we can actually stop it correctly
    private Coroutine spawnIceCoroutine;

    // New: optional flag in case the ball hits the net first
    private bool hitNet = false;


    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        characterMovement = GetComponent<CharacterMovement>();
        ballInteraction = GetComponent<BallInteract>();
        ballManager = GetComponent<BallManager>();

        // christofort: automatically sets canJump and canMove to True
        if (characterMovement != null) characterMovement.controlMovement(true, true);

        // Subscribe to ball collision event if ballManager is available
        if (ballManager != null)
            ballManager.onBallCollision += checkNetCollision;

        // Ensure dodgeBall is assigned at runtime if not set in inspector
        if (dodgeBall == null)
        {
            dodgeBall = GameObject.FindWithTag("Ball"); // Try to find by tag
            if (dodgeBall == null)
            {
                Debug.LogWarning("dodgeBall reference is still null after runtime search. Please check assignment in inspector and scene.", this);
            }
            else
            {
                Debug.Log("dodgeBall reference assigned at runtime.", this);
            }
        }
        if (dodgeBall != null)
        {
            // Christofort: grab all renderers on the dodgeball object and its children
            dodgeBallRenderers = dodgeBall.GetComponentsInChildren<Renderer>();
            if (dodgeBallRenderers == null || dodgeBallRenderers.Length == 0)
                Debug.LogWarning("Could not find any dodgeBall renderers in Start()", this);
        }
    }

    void Update()
    {
        // Handle dash input using Input System
        InputAction dash = playerInput.actions.FindAction("Defensive Ability");
        bool dashPressed = (dash != null && dash.WasPressedThisFrame()) ||
                           (dash == null && Keyboard.current?.spaceKey.wasPressedThisFrame == true);

        GameManager gameManager = GameManager.Instance;
        bool validGamestate = !gameManager.gameState.Equals(GameManager.GameState.PointStart);

        penguinHeight = transform.position.y; // christofort: grabs the Y value of the penguin
        // chrIStofort: added a check for the penguin's y value, to make sure it isn't higher than the ground
        if (dashPressed && validGamestate && cooldownTimer <= 0 && !isDashing
            && characterMovement.grounded && canUseAbilities())
        {
            StartDash();
        }

        // Update timers
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) EndDash();
        }

        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        // Handle returning to upright position
        if (isReturningUpright && characterMovement != null)
        {
            float angleFromUpright = Vector3.Angle(transform.up, Vector3.up);
            if (angleFromUpright < 5.0f)
            {
                isReturningUpright = false;
                characterMovement.overrideRotation = false;
            }
        }

        // Christofort: updates current state with the current gameState
        currentState = gameManager != null ? gameManager.gameState : GameManager.GameState.PointStart;

        // Christofort: Handling SnowBall Input System (per-player input, consistent with dash)
        InputAction snowBall = playerInput.actions.FindAction("Offensive Ability");
        bool useSnowBall = (snowBall != null && snowBall.WasPressedThisFrame()) ||
                           (snowBall == null && Keyboard.current?.spaceKey.wasPressedThisFrame == true);

        // Christofort: activating the SnowBall
        if (useSnowBall && snowBallTimer <= 0 && !usingSnowBall && canUseAbilities())
        {
            if (ballInteraction != null && ballInteraction.IsPlayerNearBall())
            {
                startSnowBall();
                Debug.Log("Started snowball", this);
            }
        }

        // Christofort: snowball timers
        if (snowBallTimer > 0)
            snowBallTimer -= Time.deltaTime;

        if (iceInstance != null)
        {
            iceTimer -= Time.deltaTime;
            if (iceTimer <= 0)
            {
                endSnowBall();
                Debug.Log("On cooldown", this);
            }
        }

        if (dodgeBall != null) spawnPoint = dodgeBall.transform.position;
    }


    void StartDash()
    {
        characterMovement.controlMovement(true, false); // christofort: makes canJump false
        isDashing = true;
        dashTimer = dashDuration;
        // Apply forward force in the direction penguin is currently facing, accounting for rotation offset (sideways prefab)
        Vector3 slideDirection = transform.forward;
        if (characterMovement != null)
        {
            Vector3 offset = characterMovement.rotationOffsetEuler;
            // Add 180 degrees to Y to invert the direction
            offset.y += 180f;
            slideDirection = (transform.rotation * Quaternion.Euler(offset)) * Vector3.forward;
        }
        rb.AddForce(slideDirection.normalized * dashSpeed, ForceMode.Impulse);

        // Trigger dash animation if animator exists
        if (characterMovement != null && characterMovement.animator != null)
        {
            characterMovement.animator.SetTrigger("Dash");
        }

        // Override CharacterMovement rotation to do belly slide
        if (characterMovement != null)
        {
            characterMovement.overrideRotation = true;
            // characterMovement.targetRotation = transform.rotation * Quaternion.Euler(90, 0, 0);
        }

        // Play slide sound
        AudioManager.PlayBirdSound(BirdType.PENGUIN, SoundType.DEFENSIVE, 1.0f);
    }

    void EndDash()
    {
        isDashing = false;
        characterMovement.controlMovement(true, true); // christofort: sets canJump back to True
        cooldownTimer = dashCooldown;

        // Start transition back to upright position
        if (characterMovement != null)
        {
            Vector3 currentEuler = transform.eulerAngles;
            Quaternion uprightRotation = Quaternion.Euler(0, currentEuler.y, 0);
            characterMovement.targetRotation = uprightRotation;
            isReturningUpright = true;
        }
    }


    void startSnowBall()
    {
        iceMode = true;
        usingSnowBall = true;
        iceSpawned = false; // New: reset spawn flag every time the ability starts
        hitNet = false; // New: reset net flag every time the ability starts
        iceTimer = iceLength;

        // Christofort: remember the state before the spike happens
        stateCheck = GameManager.Instance.gameState;

        ballInteraction.SpikeBall();

        // New: stop any old coroutine before starting a new one
        if (spawnIceCoroutine != null)
        {
            StopCoroutine(spawnIceCoroutine);
            spawnIceCoroutine = null;
        }

        // will spawn ice after the conditions in the coroutine are confirmed true
        if (iceMode && !iceSpawned)
            spawnIceCoroutine = StartCoroutine(spawnIce());

        // Christofort: swap the dodgeball's material to the snowball material
        ApplySnowballMaterial();
    }

    void ApplySnowballMaterial()
    {
        // Always refresh renderers before swapping materials
        if (dodgeBall == null)
        {
            Debug.LogError("dodgeBall reference is null in ApplySnowballMaterial", this);
            return;
        }
        dodgeBallRenderers = dodgeBall.GetComponentsInChildren<Renderer>();
        Debug.Log($"ApplySnowballMaterial: dodgeBallRenderers count = {dodgeBallRenderers?.Length}", this);
        Debug.Log($"ApplySnowballMaterial: snowballMaterial reference = {(snowballMaterial != null ? snowballMaterial.name : "null")}", this);

        if (dodgeBallRenderers == null || dodgeBallRenderers.Length == 0)
        {
            Debug.LogError("No renderers found on dodgeBall in ApplySnowballMaterial", this);
            return;
        }
        if (snowballMaterial == null)
        {
            Debug.LogError("snowballMaterial reference is null in ApplySnowballMaterial", this);
            return;
        }
        foreach (Renderer rend in dodgeBallRenderers)
        {
            if (rend != null)
            {
                rend.material = snowballMaterial;
                Debug.Log($"Applied snowball material to {rend.gameObject.name}", this);
            }
            else
            {
                Debug.LogWarning("Renderer is null in ApplySnowballMaterial loop", this);
            }
        }
        Debug.Log("Snowball material applied to all renderers", this);
    }

    void RestoreNormalBallMaterial()
    {
        // Always refresh renderers before restoring materials
        if (dodgeBall == null)
        {
            Debug.LogError("dodgeBall reference is null in RestoreNormalBallMaterial", this);
            return;
        }
        dodgeBallRenderers = dodgeBall.GetComponentsInChildren<Renderer>();
        Debug.Log($"RestoreNormalBallMaterial: dodgeBallRenderers count = {dodgeBallRenderers?.Length}", this);
        Debug.Log($"RestoreNormalBallMaterial: normalBallMaterial reference = {(normalBallMaterial != null ? normalBallMaterial.name : "null")}", this);

        if (dodgeBallRenderers == null || dodgeBallRenderers.Length == 0)
        {
            Debug.LogError("No renderers found on dodgeBall in RestoreNormalBallMaterial", this);
            return;
        }
        if (normalBallMaterial == null)
        {
            Debug.LogError("normalBallMaterial reference is null in RestoreNormalBallMaterial", this);
            return;
        }
        foreach (Renderer rend in dodgeBallRenderers)
        {
            if (rend != null)
            {
                rend.material = normalBallMaterial;
                Debug.Log($"Restored normal material to {rend.gameObject.name}", this);
            }
            else
            {
                Debug.LogWarning("Renderer is null in RestoreNormalBallMaterial loop", this);
            }
        }
        Debug.Log("Normal ball material restored to all renderers", this);
    }

    IEnumerator spawnIce()
    {
        // New: wait until the snowball gets touched by someone and the state becomes
        // either Bumped or Blocked. These are the states that mean the other side made contact.
        GameManager gameManager = GameManager.Instance;
        yield return new WaitUntil(() =>
            usingSnowBall &&
            gameManager != null &&
            gameManager.lastHit != null &&
            (gameManager.gameState == GameManager.GameState.Bumped ||
            gameManager.gameState == GameManager.GameState.Blocked));

        // New: if the snowball got canceled while waiting, stop here
        if (!usingSnowBall || gameManager == null || gameManager.lastHit == null)
            yield break;

        // New: make sure the player who touched it is actually on the opposing team
        if (!IsOpponentPlayer(gameManager.lastHit))
            yield break;

        if (!iceSpawned)
        {
            Debug.Log("Valid opponent contact detected", this);

            // New: spawn the ice under the opposing player who last touched the ball
            Vector3 hitterPos = gameManager.lastHit.transform.position;
            Vector3 iceSpawnPos = new Vector3(hitterPos.x, 0, hitterPos.z);

            iceInstance = Instantiate(tempIce, iceSpawnPos, Quaternion.identity);
            iceSpawned = true;
            Debug.Log("Ice Spawned under opposing player", this);

            iceCollider = iceInstance.GetComponent<BoxCollider>();
            if (ballCollider != null && iceCollider != null)
                Physics.IgnoreCollision(ballCollider, iceCollider, true);

            // New: revert the ball texture as soon as the ice spawns
            Debug.Log("Reverting ball material after ice spawns", this);
            RestoreNormalBallMaterial();
        }

        spawnIceCoroutine = null;
    }

    bool IsOpponentPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.Log("IsOpponentPlayer: Player is null", this);
            return false;
        }

        // Always return true if game state is Blocked or Bumped
        GameManager gameManager = GameManager.Instance;
        if (gameManager.gameState == GameManager.GameState.Blocked || gameManager.gameState == GameManager.GameState.Bumped)
        {
            Debug.Log($"IsOpponentPlayer: gameState is {gameManager.gameState}, returning true", this);
            return true;
        }
        Debug.Log($"IsOpponentPlayer: gameState is {gameManager.gameState}, returning false", this);
        return false;
    }

    // New: optional net catch in case the ball hits the net first
    void checkNetCollision(Collision colInfo)
    {
        if (!usingSnowBall || colInfo == null)
            return;

        if (colInfo.gameObject.CompareTag("Net"))
        {
            hitNet = true;
            Debug.Log("Snowball hit the net first, ignoring until valid Blocked/Spiked state", this);
        }
    }

    // void spawnIceOnEnemy(Collision colInfo)
    // {
    //     // spawns ice if it hits floor or enemy
    //     Debug.Log("Enemy collision Ice Spawn", this);
    //     if (usingSnowBall && colInfo.gameObject.CompareTag("Enemy") && !iceSpawned)
    //     {
    //         // Christofort: Instantiate the temporary ice object at the collision point
    //         ContactPoint contact = colInfo.contacts[0];
    //         Vector3 spawnPosition = contact.point;
    //         iceInstance = Instantiate(tempIce, spawnPosition, Quaternion.identity);
    //     }
    // }

    void endSnowBall()
    {
        // New: stop the active coroutine properly
        if (spawnIceCoroutine != null)
        {
            StopCoroutine(spawnIceCoroutine);
            spawnIceCoroutine = null;
        }

        iceMode = false;
        iceSpawned = false;
        usingSnowBall = false;
        hitNet = false; // New: reset net flag
        snowBallTimer = snowBallCooldown;
        Debug.Log("endSnowBall used", this);

        if (iceInstance != null)
            Debug.Log("Ice Destroyed", this);

        Destroy(iceInstance);
        iceInstance = null;
        Debug.LogFormat("Ice instance is null: {0}", iceInstance == null);

        RestoreNormalBallMaterial();
    }


    private void OnEnable()
    {
        Debug.Log("Signal Received", this);

        // Christofort: Check if ballManager exists first to avoid errors
        if (ballManager != null)
            ballManager.onBallCollision += checkNetCollision;
    }

    private void OnDisable()
    {
        Debug.Log("Signal Revoked", this);

        if (ballManager != null)
            ballManager.onBallCollision -= checkNetCollision;
    }

    private void OnDestroy()
    {
        if (ballManager != null)
            ballManager.onBallCollision -= checkNetCollision;
    }
}