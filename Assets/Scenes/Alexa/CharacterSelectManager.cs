    using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// This script is used to manage the character select screen.
// It will handle the character selection and transition to the next scene when the players are ready.
public class CharacterSelectManager : MonoBehaviour
{
    private static CharacterSelectManager instance; // Singleton reference
    public static CharacterSelectManager Instance => instance;

    // All of the bird types that players can choose from (order matches the enum)
    public List<BirdType> availableBirds = new();
    public int numberOfPlayers = 4;
    public Canvas mainCanvas;
    public Transform cursorPrefab; // Need cursor prefab(s) to show player cursors on the select screen
    public Button readyButton;

    // per-player data maintained while on the select screen
    private List<int> chosenBirdIndices = new();
    private List<bool> isKBMInput = new();
    private List<bool> playerReady = new();
    private List<Transform> playerCursors = new();
    private List<PlayerInputState> playerInputStates = new();

    // name of the scene to load once selections are done (MAKE SURE THIS MATCHES MULTIPLAYER MANAGER AND CHANGES WHEN NEEDED)
    private const string mainSceneName = "RodericM2";

    // Tracks important input info for each player
    private class PlayerInputState
    {
        public int playerIndex;
        public bool isKBM;
        public InputDevice device;
        public Vector2 cursorPosition; // Screen space
        public Vector2 inputDirection; // For gamepad stick input
        public bool readyPressed = false;

        public PlayerInputState(int index, bool kbm, InputDevice dev)
        {
            playerIndex = index;
            isKBM = kbm;
            device = dev;
            cursorPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
    }

    private void Awake()
    {
        // singleton setup
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // If the previous menu passed player/input data use it; otherwise use defaults
        if (DataTransferManager.isKBMInput != null && DataTransferManager.isKBMInput.Count > 0)
        {
            numberOfPlayers = Mathf.Clamp(DataTransferManager.isKBMInput.Count, 1, 4);
            isKBMInput = new List<bool>(DataTransferManager.isKBMInput);
        }
        else
        {
            // ensure transfer lists exist so other code can reference them
            if (DataTransferManager.isKBMInput == null) DataTransferManager.isKBMInput = new List<bool>();
            if (DataTransferManager.selectedBirds == null) DataTransferManager.selectedBirds = new List<BirdType>();
        }

        // make sure internal lists are sized correctly
        ResizePlayerLists(numberOfPlayers);

        // Setup input state for each player, prefer the transferred input scheme where available
        SetupPlayerInputStates();
    }

    // Start is called before the first frame update
    private void Start()
    {
        CreatePlayerCursors();

        // Wire up the ready button if available
        /// <summary>
        /// EJ: I know there's no ready button right now but I'm assuming there will be eventually
        /// so I just want to code for what makes sense right now to make things easier on me.
        /// Feel free to change if this doesn't match the vision.
        /// </summary>
        if (readyButton != null) readyButton.onClick.AddListener(CheckAllPlayersReady);
    }

    // Update is called once per frame
    private void Update()
    {
        // Update cursor positions and handle input for each player
        for (int i = 0; i < playerInputStates.Count; ++i)
        {
            UpdatePlayerInput(i);
            UpdatePlayerCursor(i);
        }
    }

    /// <summary>
    /// EJ: For now, I'm just assuming player 0 is KBM and the rest are gamepads 
    /// just because alexa told me there's only one KBM allowed, but this can be changed to be more flexible if needed. 
    /// The important part is that the playerInputStates list is set up correctly and matches the order of players for the rest of the code to work, 
    /// and that the isKBMInput list is populated to match what the multiplayer manager will expect when it reads from the transfer manager.
    /// Though I'm sure you can change this with the MultiplayerManager to just read whatever you set up there if you need to be more flexible.
    /// You have my discord if you have concerns.
    /// </summary>
    private void SetupPlayerInputStates()
    {
        playerInputStates.Clear();
        // If transfer manager provided an input scheme, use that
        if (DataTransferManager.isKBMInput != null && DataTransferManager.isKBMInput.Count == numberOfPlayers)
        {
            int gamepadIndex = 0;
            for (int i = 0; i < numberOfPlayers; ++i)
            {
                bool kbm = DataTransferManager.isKBMInput[i];
                InputDevice dev = kbm ? (InputDevice)Keyboard.current : (gamepadIndex < Gamepad.all.Count ? (InputDevice)Gamepad.all[gamepadIndex++] : null);
                playerInputStates.Add(new PlayerInputState(i, kbm, dev));
            }

            isKBMInput = new List<bool>(DataTransferManager.isKBMInput);
        }
        else
        {
            // Default: first player KBM, others gamepads if present
            playerInputStates.Add(new PlayerInputState(0, true, Keyboard.current));

            int gamepadIndex = 0;
            for (int i = 1; i < numberOfPlayers; ++i)
            {
                InputDevice dev = gamepadIndex < Gamepad.all.Count ? (InputDevice)Gamepad.all[gamepadIndex++] : null;
                playerInputStates.Add(new PlayerInputState(i, false, dev));
            }

            isKBMInput.Clear();
            foreach (var state in playerInputStates) isKBMInput.Add(state.isKBM);
        }
    }

    // Create a cursor for each player. (CURSOR PREFABS ARE NEEEDED)
    private void CreatePlayerCursors()
    {
        if (cursorPrefab == null)
        {
            Debug.LogWarning("Cursor prefab not assigned in CharacterSelectManager!");
            return;
        }

        playerCursors.Clear();

        for (int i = 0; i < numberOfPlayers; ++i)
        {
            Transform cursor = Instantiate(cursorPrefab, mainCanvas.transform);
            cursor.name = $"Cursor_Player{i}";

            // Color cursors per player
            Image cursorImage = cursor.GetComponent<Image>();
            if (cursorImage != null)
            {
                cursorImage.color = GetPlayerColor(i);
            }

            playerCursors.Add(cursor);
        }
    }

    // Update input for a given player and check for any button selections.
    private void UpdatePlayerInput(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerInputStates.Count) return;
        PlayerInputState state = playerInputStates[playerIndex];

        if (state.isKBM)
        {
            state.cursorPosition = Mouse.current.position.ReadValue();

            // Check for mouse clicks on buttons
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandlePlayerButtonPress(playerIndex);
            }
        }
        else
        {
            // Players 1-3: Gamepad input
            Gamepad pad = state.device as Gamepad;
            if (pad != null)
            {
                // Left stick controls cursor movement
                state.inputDirection = pad.leftStick.ReadValue();

                // Update cursor position
                float moveSpeed = 300f * Time.deltaTime;
                state.cursorPosition += state.inputDirection * moveSpeed;

                // Clamp to screen bounds
                state.cursorPosition.x = Mathf.Clamp(state.cursorPosition.x, 0, Screen.width);
                state.cursorPosition.y = Mathf.Clamp(state.cursorPosition.y, 0, Screen.height);

                // Check for button presses (South button)
                if (pad.aButton.wasPressedThisFrame) HandlePlayerButtonPress(playerIndex);

                // Check for "ready" input (Start button, or similar)
                if (pad.startButton.wasPressedThisFrame) playerReady[playerIndex] = !playerReady[playerIndex];
            }
        }
    }

    private void UpdatePlayerCursor(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerCursors.Count) return;
        if (playerIndex >= playerInputStates.Count) return;

        Transform cursor = playerCursors[playerIndex];
        Vector2 screenPos = playerInputStates[playerIndex].cursorPosition;

        // Convert screen position to canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            screenPos,
            mainCanvas.worldCamera,
            out Vector2 localPos
        );

        cursor.GetComponent<RectTransform>().localPosition = localPos;
    }

    // Check if UI button is under the cursor and respond to it
    private void HandlePlayerButtonPress(int playerIndex)
    {
        if (playerIndex >= playerInputStates.Count) return;
        Vector2 screenPos = playerInputStates[playerIndex].cursorPosition;

        // Raycast to find UI element under cursor
        PointerEventData pointerData = new(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // Check if this is a bird selection button
            BirdSelectButton birdButton = result.gameObject.GetComponent<BirdSelectButton>();
            if (birdButton != null)
            {
                birdButton.OnPressed(playerIndex);
                return;
            }
        }
    }


    // Make sure the it's the correct size and has defaults
    // Needs to be called whenever player count changes
    public void ResizePlayerLists(int count)
    {
        numberOfPlayers = Mathf.Clamp(count, 1, 4);

        while (chosenBirdIndices.Count < numberOfPlayers)
            chosenBirdIndices.Add(0);
        while (isKBMInput.Count < numberOfPlayers)
            isKBMInput.Add(true);
        while (playerReady.Count < numberOfPlayers)
            playerReady.Add(false);
        while (chosenBirdIndices.Count > numberOfPlayers)
            chosenBirdIndices.RemoveAt(chosenBirdIndices.Count - 1);
        while (isKBMInput.Count > numberOfPlayers)
            isKBMInput.RemoveAt(isKBMInput.Count - 1);
        while (playerReady.Count > numberOfPlayers)
            playerReady.RemoveAt(playerReady.Count - 1);
    }

    /// <summary>
    /// EJ: Alexa has not yet told me which player will be what color, 
    /// so for now I'm just assigning some arbitrary colors to the cursors based on player index, 
    /// but this can be changed to whatever. 
    /// </summary>
    private Color GetPlayerColor(int playerIndex)
    {
        return playerIndex switch
        {
            0 => Color.cyan,    
            1 => Color.yellow,  
            2 => Color.magenta, 
            3 => Color.green,  
            _ => Color.white
        };
    }

    // Set the chosen bird for a specific player
    public void SetPlayerBirdIndex(int playerIndex, int birdIndex)
    {
        if (playerIndex < 0 || playerIndex >= chosenBirdIndices.Count) return;
        if (birdIndex < 0 || birdIndex >= availableBirds.Count) return;
        chosenBirdIndices[playerIndex] = birdIndex;
        UpdatePlayerBirdUI(playerIndex);
    }

    // Get the bird type that the specified player has chosen.
    public BirdType GetSelectedBird(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= chosenBirdIndices.Count) return BirdType.OTHER;
        int idx = chosenBirdIndices[playerIndex];
        if (idx < 0 || idx >= availableBirds.Count) return BirdType.OTHER;
        return availableBirds[idx];
    }

    /// <summary>
    /// Once again, this is just a placeholder for now since we don't have a lot of UI elements yet,
    /// but this is where you would update any UI to reflect the player's current bird choice when they select a new one.
    /// So in the future, override this method to update any UI elements that display the player's current bird.
    /// </summary>
    /// <param name="playerIndex"></param>
    protected virtual void UpdatePlayerBirdUI(int playerIndex)
    {
        // placeholder: add visual feedback here
    }

    // Check if all players are ready and get ready to start the match if they are
    private void CheckAllPlayersReady()
    {
        for (int i = 0; i < playerReady.Count; ++i)
        {
            if (!playerReady[i])
            {
                Debug.Log($"Player {i + 1} is not ready yet."); // Should prob be UI element later
                return;
            }
        }

        // All players ready. Prep to start the match
        BeginMatch();
    }


    // This is pretty much just data transfer to the multiplayer manager
    public void BeginMatch()
    {
        // copy lists to the global transfer object; multiplayer manager will read them
        DataTransferManager.isKBMInput = new List<bool>(isKBMInput);
        DataTransferManager.selectedBirds = new List<BirdType>();
        for (int i = 0; i < numberOfPlayers; ++i)
            DataTransferManager.selectedBirds.Add(GetSelectedBird(i));

        SceneManager.LoadScene(mainSceneName);
    }

    public bool IsPlayerReady(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerReady.Count) return false;
        return playerReady[playerIndex];
    }

    /// <summary>
    /// Once again, this is just a placeholder for now since we don't have a lot of UI elements yet,
    /// but this is where you would update any UI to reflect whether the player is ready or not when they press the ready button.
    /// Again, override this in the future.
    /// </summary>
    public void SetPlayerReady(int playerIndex, bool ready)
    {
        if (playerIndex < 0 || playerIndex >= playerReady.Count) return;
        playerReady[playerIndex] = ready;
    }
}
