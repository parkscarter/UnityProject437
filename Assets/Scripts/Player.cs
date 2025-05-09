using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;


public class PlayerMovement : MonoBehaviour
{
    private Vector3 PlayerMovementInput;
    private Vector2 PlayerMouseInput;

    [SerializeField] private Transform PlayerCamera;
    [SerializeField] private Transform[] enemies;
    [SerializeField] private Rigidbody PlayerBody;

    [SerializeField] private float Speed = 10f;
    [SerializeField] private float Sensitivity = 3f;
   
    [SerializeField] private float StaminaDrainRate = 25f; 
    [SerializeField] private float StaminaRegenRate = 7f; 
    [SerializeField] private float SprintMultiplier = 1.8f;

    [SerializeField] private float HealthRegenRate = 1.5f; 
         

    private ProgressBar healthBar;
    private ProgressBar stamBar;
    private Label endLabel;

    //Tracks last time the player was damaged or used sprint, used for regen delay
    private float lastDamageTime = Mathf.NegativeInfinity;
    private float lastSprintTime = Mathf.NegativeInfinity;

    private float MaxStamina = 100f;
    private float MaxHP = 100;
    
    private float HP;
    private float Stamina;

    //Delay for health and stamina regen
    private float healthRegenDelay = 4f; 
    private float staminaRegenDelay = 2f;

    //Other variables
    public static bool playerWon = false;
    public static bool respawn = false;
    public static Vector3 RespawnPosition;
    public static Vector3 RespawnForce;


    // Start is called before the first frame update
    void Start()
    {
        // Initialize stamina and hp
        Stamina = MaxStamina;
        HP = MaxHP;

        //Find ui document
        var uiDocument = GameObject.Find("TerrainUI").GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        //For win or lose (hide for now)
        endLabel = root.Q<Label>("WinLabel");
        endLabel.visible = false;

        // Find progress bars
        healthBar = root.Q<ProgressBar>("HealthBar");
        stamBar = root.Q<ProgressBar>("StamBar");
        UpdateBars();

        // check if we are respawning after checkers
        if (respawn)
        {
            //transform.position = RespawnPosition;
            //if the player lost, switch text to lose
            if (!playerWon)
            {
                endLabel.text = "You Lose!";
            }
            //Otherwise launch the player into the air(This doesn't work rn)
            else
            {
                if (PlayerBody != null)
                {
                    PlayerBody.AddForce(RespawnForce, ForceMode.Impulse);
                    PlayerBody.useGravity = false;
                }
            }

            endLabel.visible = true; //show win or lose label
        }
    }


    void Update()
    {
        //End if HP is 0
        if (HP <= 0)
        {
            Speed = 0;
            endLabel.text = "You Lose!";
            endLabel.visible = true;
        }

        takeDamage();   //check if the player is taking damage
        CheckGates();   //checks player position against gates, move to checkers scene if in gate
        MovePlayer();   //handle player movement
        UpdateBars();   //constantly update health and stamina bars
    }


    private void MovePlayer()
    {
        //Get input first
        PlayerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        PlayerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        bool sprinting = Input.GetKey(KeyCode.C) && Stamina > 0f && HP > 0f;                //check if player is sprinting

        //multiply actual speed by sprint multiply if sprinting
        float actualSpeed = Speed;
        if (sprinting)
        {
            actualSpeed *= SprintMultiplier;
            Stamina -= StaminaDrainRate * Time.deltaTime;
            Stamina = Mathf.Max(Stamina, 0f);
            lastSprintTime = Time.time; // reset timer on sprint
        }
        else        //otherwise regen stamina
        {
            if (Time.time - lastSprintTime >= staminaRegenDelay)
            {
                Stamina += StaminaRegenRate * Time.deltaTime;
                Stamina = Mathf.Min(Stamina, MaxStamina);       //cap at 100
            }
        }

        //get direction from input, multiply by speed
        Vector3 MoveVector = transform.TransformDirection(PlayerMovementInput) * actualSpeed;
        PlayerBody.velocity = new Vector3(MoveVector.x, PlayerBody.velocity.y, MoveVector.z);
    }

    void UpdateBars()
    {
        healthBar.value = HP / MaxHP * 100f;            // ProgressBar expects a percentage
        stamBar.value = Stamina / MaxStamina * 100f;    // ProgressBar expects a percentage
    }

    private void CheckGates()
    {
        Vector3 pos = transform.position;

        //Check user position on gates (locations hard coded bc that was quick and easy) - set minimax depth and switch scene
        if (pos.x > 48 && pos.x < 52 && pos.y > 50.2f && pos.y < 52.5f && pos.z > 897 && pos.z < 902)
        {
            CheckersLogic.depth = 1; // Easy
            SceneManager.LoadScene("Board Scene");
        }
        else if (pos.x > 64 && pos.x < 70 && pos.y > 50.2f && pos.y < 52.5f && pos.z > 885 && pos.z < 890)
        {
            CheckersLogic.depth = 2; // Medium
            SceneManager.LoadScene("Board Scene");
        }
        else if (pos.x > 76 && pos.x < 81 && pos.y > 50.2f && pos.y < 52.5f && pos.z > 866 && pos.z < 871)
        {
            CheckersLogic.depth = 4; // Hard
            SceneManager.LoadScene("Board Scene");
        }
    }

    private void takeDamage()
    {
        if (HP <= 0 || enemies == null) return;

        bool tookDamage = false;
        float damageThisFrame = 0f;

        //Loop through each enemy and take damage if distance is within range; only subtract damage once (damageThisFrame)
        foreach (Transform enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);

            if (distance < 20f)
            {
                damageThisFrame += 5f * Time.deltaTime;  // close range damage
                tookDamage = true;
            }
            else if (distance < 45f)
            {
                damageThisFrame += 3f * Time.deltaTime; // medium range
                tookDamage = true;
            }
            else if (distance < 70f)
            {
                damageThisFrame += 1f * Time.deltaTime; // light damage
                tookDamage = true;
            }
        }

        //take damage and reset lastDamageTime
        if (tookDamage)
        {
            lastDamageTime = Time.time;
            HP -= damageThisFrame;
            HP = Mathf.Clamp(HP, 0f, MaxHP);
        }

        // Health regeneration
        if (HP > 0 && HP < MaxHP && Time.time - lastDamageTime >= healthRegenDelay)     //pause for healthRegenDelay
        {
            HP += HealthRegenRate * Time.deltaTime;      
            HP = Mathf.Min(HP, MaxHP);                   //cap at maxHP
        }
    }



}
