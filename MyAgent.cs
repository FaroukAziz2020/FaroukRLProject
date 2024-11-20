using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;


public class MyAgent : Agent
{
    // Rigidbody component used to control physics-related movements of the agent.
    Rigidbody m_rigidbody;
    // Movement speed of the agent.
    float m_speed = 20;

    // Reference to the Spawner GameObject, used to access and destroy donuts in the scene.
    public GameObject Spawner;

    // Initial starting position for the agent.
    private Vector3 startingPosition = new Vector3(0f, 0f, 0f);

    // Horizontal bounds for the agent's movement.
    private float boundXLeft = -19f;
    private float boundXRight = 19f;

    // Enum defining possible actions the agent can take.
    private enum ACTIONS
    {
        LEFT = 0,
        NOTHING = 1,
        RIGHT = 2
    }

    
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>(); // Retrieves the Rigidbody component attached to the agent.
    }

    // Resets the agent's position at the beginning of each training episode.
    public override void OnEpisodeBegin()
    {
        transform.localPosition = startingPosition; 
    }

    
    [SerializeField] private Transform targetTransform;

    // Collects observations for the agent to perceive the environment.
    public override void CollectObservations(VectorSensor sensor)
    {
        // Adds the agent's position to the observation space.
        sensor.AddObservation(transform.position);
        
    }

    // Defines agent actions based on human input during training/testing.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> actions = actionsOut.DiscreteActions;

        // Gets horizontal and vertical input for movement.
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        // Sets the action based on the horizontal input (left, right, or no movement).
        if (horizontal == -1)
        {
            actions[0] = (int)ACTIONS.LEFT;
        }
        else if (horizontal == +1)
        {
            actions[0] = (int)ACTIONS.RIGHT;
        }
        else
        {
            actions[0] = (int)ACTIONS.NOTHING;
        }
    }

    // Handles actions received from the ML model and applies them to the agent's movement.
    public override void OnActionReceived(ActionBuffers actions)
{
    var actionTaken = actions.DiscreteActions[0]; 

    Vector3 movementDirection = Vector3.zero;

    // Executes the action based on its type (left, right, or no movement).
    switch (actionTaken)
    {
        case (int)ACTIONS.NOTHING:
            break;
        case (int)ACTIONS.LEFT:
            // Moves left if within bounds, otherwise ends the episode with a negative reward.
            if (transform.localPosition.x > boundXLeft)
                movementDirection = -Vector3.right; 
            else
            {
                AddReward(-1); // Penalizes agent for moving out of bounds.
                EndEpisode(); // Ends the episode if out of bounds.
            }
            break;
        case (int)ACTIONS.RIGHT:
            // Moves right if within bounds, otherwise ends the episode with a negative reward.
            if (transform.localPosition.x < boundXRight)
                movementDirection = Vector3.right; // Moves the agent right.
            else
            {
                AddReward(-1); // Penalizes agent for moving out of bounds.
                EndEpisode(); // Ends the episode if out of bounds.
            }
            break;
    }

    
    if (AvoidDonuts())
    {
        movementDirection = -movementDirection; // If donuts detected, reverse direction to avoid them.
    }

    // Apply the movement based on the direction and speed.
    transform.Translate(movementDirection * m_speed * Time.deltaTime);

    AddReward(0.1f); 
}


private bool AvoidDonuts()
{
   
    Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 5f); 

    foreach (var obj in nearbyObjects)
    {
        if (obj.CompareTag("donut"))
        {
            
            AddReward(-0.2f); // Apply a small penalty for proximity to a donut.
            return true;
        }
    }
    return false;
}


    // Detects collisions with other objects in the game.
    private void OnTriggerEnter(Collider other)
{
    
    if (other.tag == "donut")
    {
        // Gets a reference to the Spawner's transform, then destroys all donuts under it.
        var parent = Spawner.transform;
        int numberOfChildren = parent.childCount;

        // Loops through children of the Spawner to find and destroy objects tagged "donut".
        for (int i = 0; i < numberOfChildren; i++)
        {
            if (parent.GetChild(i).tag == "donut")
            {
                Destroy(parent.GetChild(i).gameObject); // Destroys the donut object.
            }
        }

        AddReward(-0.5f); // Penalizes agent for hitting a donut.

        // Resets the agent's position to the starting position before ending the episode.
        transform.localPosition = startingPosition;

        EndEpisode(); // Ends the episode after resetting the position.
    }
}

}
