using UnityEngine;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public int side1Score = 0;
    public int side2Score = 0;
    public GameManager gameManager;
    private bool leftLastScored;
    private bool inPlay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        side1Score = 0;
        side2Score = 0;
        leftLastScored = false;
        inPlay = true;

        if (gameManager == null)
        {
            Debug.LogError("Game Manager was not set in inspector for Score Manager!");
        }
    }

    // checking to see if the ball hits the court on either side
    void OnCollisionEnter(Collision collision)
    {
        // if it touches side 1, then side 2 scores
        if (collision.gameObject.CompareTag("Side1") && inPlay)
        {
            side2Score += 1;
            inPlay = false;
            Debug.Log("side 2 scored! points: " + side2Score);
            CheckWin(true);
        } 
        // if it touches side 2, then side 1 scores
        else if (collision.gameObject.CompareTag("Side2") && inPlay) 
        {
            side1Score += 1;
            inPlay = false;
            Debug.Log("side 1 scored! points: " + side1Score);
            CheckWin(false);
        }
    }

    // after each score, check the win conditions for both sides
    void CheckWin(bool leftJustScored)
    {
        if (side1Score >= 3 && side1Score > side2Score + 2)
        {
            Debug.Log("side 1 wins! final score: " + side1Score + " to " + side2Score);
        } 
        else if (side2Score >= 3 && side2Score > side1Score + 2)
        {
            Debug.Log("side 2 wins! final score: " + side1Score + " to " + side2Score);
        }
        else
        {
            StartCoroutine(StartNextPoint(leftJustScored));
        }
    }

    // Start next point if nobody has won yet
    private IEnumerator StartNextPoint(bool leftJustScored)
    {
        // Check for rotation of server
        if (leftJustScored != leftLastScored)
        {
            gameManager.RotateServer();
            leftLastScored = !leftLastScored;
        }

        // Wait 2 seconds
        yield return new WaitForSeconds(2.0f);

        // Start the next point
        gameManager.leftAttack = leftLastScored;
        gameManager.NextPoint();
        inPlay = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
