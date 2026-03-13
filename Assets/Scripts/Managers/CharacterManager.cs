using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [Header("Penguin")]
    [SerializeField] private GameObject penguinKBM;
    [SerializeField] private GameObject penguinC;
    [SerializeField] private GameObject penguinAI;

    [Header("Seagull")]
    [SerializeField] private GameObject seagullKBM;
    [SerializeField] private GameObject seagullC;
    [SerializeField] private GameObject seagullAI;

    [HideInInspector] public GameObject PenguinKBM => penguinKBM;
    [HideInInspector] public GameObject PenguinC => penguinC;
    [HideInInspector] public GameObject PenguinAI => penguinAI;
    [HideInInspector] public GameObject SeagullKBM => seagullKBM;
    [HideInInspector] public GameObject SeagullC => seagullC;
    [HideInInspector] public GameObject SeagullAI => seagullAI;
}
