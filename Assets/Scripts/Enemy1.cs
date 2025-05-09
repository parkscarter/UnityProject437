using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System;
using System.Numerics;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy1 : MonoBehaviour
{

    [Header("Setup")]
    [SerializeField] Transform player;                  // player position
    public NavMeshAgent agent;                          // navmesh agent
    [SerializeField] List<Transform> patrolPoints;      // optional list of patrol points

    [Header("Ranges & Speeds")]
    [SerializeField] float awarenessRange = 50f;        // player detected
    [SerializeField] float chaseRange = 30f;            // switch to full chase
    [SerializeField] float loseRange = 40f;             // player lost again
    [SerializeField] float patrolSpeed = 2f;            // speed in patrol state
    [SerializeField] float chaseSpeed = 4.5f;           // speed in chase state
    [SerializeField] State currentState = State.Idle;   // tracks current state
    [SerializeField] float patrolRadius = 10f;          // distance for random patrol points
    [SerializeField] float patrolPauseTime = 1.5f;      // pause time while at patrol point


    enum State { Idle, Patrol, Seek, Chase }

    // Time Trackers for patrolling 
    int currentPatrolIndex = 0;
    float idleTimer;
    float patrolTimer = 0f;

    //These variables are useful for fixing a bug where the agent would choose a point it couldn't get to
    private UnityEngine.Vector3 lastPosition;   
    private float stuckDistance = 1f;
    private float stuckMax = 1f;
    private float stuckTimer = 0f;
    

    void Start()
    {
        //Get agent on navmesh
        if (!agent.isOnNavMesh)
        { 
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }

        ChangeState(State.Patrol);   // start patrolling

    }


    void Update()
    {
        if (player == null) return;             

        //Always check if stuck
        CheckIfStuck();

        switch (currentState)
        {
            case State.Idle: IdleUpdate(); break;
            case State.Patrol: PatrolUpdate(); break;
            case State.Seek: SeekUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
        }
        
    }

   //This function sets the enemy's active state to whatever is input (next)
    void ChangeState(State next)
    {
        switch (currentState)
        {
            case State.Idle: agent.ResetPath(); break;
            case State.Patrol:
            case State.Seek:  
            case State.Chase:
                break;
        }
        
        currentState = next;

        switch (currentState)
        {
            case State.Idle:
                idleTimer = UnityEngine.Random.Range(1f, 3f);   // Remain idle for a random moment
                break;

            case State.Patrol:
                agent.speed = patrolSpeed;                      // Ensure speed is correct
                GoToNextPatrolPoint();                          // Travel to patrol points
                break;

            case State.Seek:
                agent.speed = patrolSpeed * 1.3f;               // Slight boost while seeking
                agent.SetDestination(player.position);          // Chase player directly
                break;

            case State.Chase:
                agent.speed = chaseSpeed;                       // Set to max speed
                break;
        }
    }

    
    void IdleUpdate()
    {
        // Subtract from idle timer
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            ChangeState(State.Patrol);      //Switch to patrol when timer runs out
        }

        //Always check if player is in range
        if (PlayerInRange(awarenessRange))
        {
            ChangeState(State.Seek);
        }
    }

    void PatrolUpdate()
    {
        patrolTimer += Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance < 0.3f && patrolTimer >= patrolPauseTime)
        {
            GoToNextPatrolPoint();  //Continually go to next patrol point
        }

        //Always check if player is in range
        if (PlayerInRange(awarenessRange))
        {
            ChangeState(State.Seek);
        }
    }

    void SeekUpdate()
    {
        agent.SetDestination(player.position);  //Continually chase player (just slower than chase)

        //Check distance and change state if required
        if (PlayerInRange(chaseRange))
        {
            ChangeState(State.Chase);
        }
        else if (!PlayerInRange(awarenessRange))
        {
            ChangeState(State.Patrol);
        }
            
    }

    void ChaseUpdate()
    {
        agent.SetDestination(player.position);  //Chase player

        if (!PlayerInRange(loseRange))
        {
            ChangeState(State.Seek);            // change to seek if too far away
        }
    }

    // Check if the player has moved since the last check, increase timer; if ai is stuck, choose a new patrol point
    void CheckIfStuck()
    {
        float distanceMoved = UnityEngine.Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < stuckDistance)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckMax)
            {
                stuckTimer = 0f;
                GoToNextPatrolPoint();  //Choose new patrol point if we haven't moved for stuckMax
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
    }


    // Return True if player is in range r
    bool PlayerInRange(float r)
    {
        return(UnityEngine.Vector3.SqrMagnitude(player.position - transform.position) <= r * r);
    }

        
    // Choose new patrol point, random if none are preset
    void GoToNextPatrolPoint()
    {
        patrolTimer = 0f;   // Reset Timer

        // Use preset patrol points if given; iterate through them in loop
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            // Use fixed patrol points
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }
        else 
        {           
            // Use random patrol point
            UnityEngine.Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;

            //Ensure random point is on navmesh
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // Retry on next update
                patrolTimer = patrolPauseTime;
            }
        }
    }

}
