using UnityEngine;
using System.Collections;

public class EnemyBehaviour : MonoBehaviour {

	public GameObject canvas;
	public float patrolSpeed = 6.0f;
	public float chaseSpeed = 9.0f;
	public float dampingLook = 4.0f;
	public float lookAroundDuration = 4.0f;
	public float suspisciousDuration = 0.5f;
	public float fieldOfViewAngle = 160.0f;
	public float stopChaseDistance = 2.0f;

	public Transform[] waypoints;

	private enum EnemyState {Suspiscious, Chasing, Patrolling};
	private EnemyState enemyState;
	
	private float suspisciousTime;
	private float lookAroundTime;
	private float lookingTime;

	private int currentWaypoint = 0;
	private CharacterController controller;
	private GameObject player;
	private SphereCollider col;

	private bool playerInSight = false;
	private Vector3 personalLastSighting;
	private Controller playerScript;
	private Transform playerTransform;

	void Start () {
		player = GameObject.FindWithTag("Player");
		playerTransform = player.GetComponent<Transform>();
		playerScript = player.GetComponent<Controller> ();

		changeEnemyState(EnemyState.Patrolling);

		controller = (CharacterController)GetComponent(typeof(CharacterController));

		canvas.SetActive (false);
	}

	// Update is called once per frame
	void Update () {
		switch (enemyState) {

		case EnemyState.Patrolling:
			patrol();
			break;

		case EnemyState.Chasing:
			chase();
			break;

		case EnemyState.Suspiscious:
			beSuspiscious();
			break;
		}
			
	}


	void Awake() {
		col = (SphereCollider)GetComponent (typeof(SphereCollider));
	}

	// when collided with another gameObject
	void OnTriggerStay (Collider other)
	{
		// if the other is the player
		if (other.gameObject == player) {

			// here, we detect that the player is within range (inside the sphere collider)
			if(playerScript.MovingState != Controller.MovementType.Running) {
			// detect player by sighting
				Vector3 vectorFromEnemyToPlayer = other.transform.position - transform.position;
				Vector3 forwardVector = transform.forward;

				float angle = Vector3.Angle (vectorFromEnemyToPlayer, forwardVector);

				if (angle < fieldOfViewAngle * 0.5f) {       

					RaycastHit hit;
					// check if something is blocking the enemy's view
					if (Physics.Raycast (transform.position, transform.forward, out hit, col.radius)) {
						if (hit.collider.gameObject == player) {
							//HOORAY !!!! The enemy has found the player                

							playerInSight = true;
							
							// save the player’s current position.
							personalLastSighting = player.transform.position;
							
							changeEnemyState(EnemyState.Chasing);
							
						} 
					} 
				} else if(playerScript.MovingState == Controller.MovementType.Walking){
					// save the player’s current position.
					personalLastSighting = player.transform.position;
					
					changeEnemyState(EnemyState.Suspiscious);
				}

				Vector3 moveDirection = playerTransform.position - transform.position;

				// if the player is in stealthMode, he can destroy the enemy
				if (playerScript.MovingState == Controller.MovementType.Stealth && moveDirection.magnitude < 3 && Input.GetMouseButtonDown (0)) {
					// destroy enemy
					Destroy (gameObject);
				}
			} else {
				playerInSight = true;
				
				// save the player’s current position.
				personalLastSighting = player.transform.position;
				
				changeEnemyState(EnemyState.Chasing);
			}
		}
	}

	void OnTriggerExit (Collider other) {
		// If the player leaves the trigger zone...
		if(other.gameObject == player)
		{
			playerInSight = false;
		}
	}

	void changeEnemyState(EnemyState state) {
		switch (state) {
		case EnemyState.Patrolling:
			enemyState = EnemyState.Patrolling;
			lookAroundTime = 0;
			break;
		case EnemyState.Chasing:
			enemyState = EnemyState.Chasing;
			break;
		case EnemyState.Suspiscious:
			if(enemyState != EnemyState.Suspiscious) {
				suspisciousTime = 0;
				lookAroundTime = 0;
				lookingTime = 0;
			}
			enemyState = EnemyState.Suspiscious;
			break;
		}
	}

	void patrol() {
		Vector3 nextWaypoint = waypoints[currentWaypoint].position;

		nextWaypoint.y = transform.position.y;

		Vector3 moveDirection = nextWaypoint - transform.position;

		if (moveDirection.magnitude < 1.5) {
			Debug.Log ("Enemy is close to the next waypoint");

			if (lookAroundTime == 0)
				// Pause over the Waypoint 
				lookAroundTime = Time.time; 
			
			if ((Time.time - lookAroundTime) >= lookAroundDuration) {
				Debug.Log ("increasing waypoint");
				
				currentWaypoint = (currentWaypoint+1 < waypoints.Length ? currentWaypoint+1 : 0);
				lookAroundTime = 0;
			}
		} else {
			Debug.Log("reaching in rotation " + moveDirection.magnitude);

			transform.rotation = Quaternion.Slerp(transform.rotation, 
			                                      Quaternion.LookRotation(nextWaypoint - transform.position), 
			                                      Time.deltaTime * dampingLook);

			controller.Move(moveDirection.normalized * patrolSpeed * Time.deltaTime);
		}
	}

	void chase(){
		
		Vector3 playerPosition = playerTransform.position;
		
		// Keep waypoint at character's height
		playerPosition.y = transform.position.y; 
		
		// Get the direction we need to move to
		// reach the next waypoint
		Vector3 moveDirection = playerPosition - transform.position;
		
		if (moveDirection.magnitude < 2) {
			canvas.SetActive (true);
		} else {
			canvas.SetActive (false);
		}
		if(moveDirection.magnitude < stopChaseDistance)
		{
			Debug.Log("Player is close to the enemy, we can stop now");
		}
		else
		{
			if (moveDirection.magnitude < col.radius)
			{
				
				Debug.Log("Player is still within chasing distance - keep running after it");
				// This code gets called every time update is called
				// while the enemy if moving towards the player.
				// so it gets called 100's of times in a few seconds  
				
				// Now we need to do two things
				// 1) Start rotating in the desired direction
				// 2) Start moving in the desired direction 
				
				// 1) Let's calculate  desired rotation by looking at playerposition
				// and comparing with current position
				transform.LookAt(playerTransform);
				
				// 2) Now also let's start moving towards our waypoint
				controller.Move(transform.forward * chaseSpeed * Time.deltaTime);
			}
			else
			{
				Debug.Log("Player is too far away - stop chasing");
				changeEnemyState(EnemyState.Suspiscious);
			}
		}  
	}

	void beSuspiscious() {

		Vector3 moveDirection = personalLastSighting - transform.position;

		if (suspisciousTime == 0)
			suspisciousTime = Time.time;

		if (moveDirection.magnitude > 1.5 && (Time.time - suspisciousTime) >= suspisciousDuration) {
			transform.rotation = Quaternion.Slerp(transform.rotation, 
			                                      Quaternion.LookRotation(moveDirection), 
			                                      Time.deltaTime * dampingLook/2);
			controller.Move(moveDirection.normalized * patrolSpeed * Time.deltaTime);
		}

		if (moveDirection.magnitude <= 1.5) {
			if(lookAroundTime == 0)
				lookAroundTime = Time.time;
			if ((Time.time - lookAroundTime) >= lookAroundDuration) {
				lookAroundTime = 0;
				changeEnemyState(EnemyState.Patrolling);
			}
			lookAround();
		}

	}

	void lookAround() {
		if (lookingTime == 0)
			lookingTime = Time.time;

		if ((Time.time - lookingTime) < lookAroundDuration / 2) {
			transform.rotation = Quaternion.Slerp (transform.rotation, 
			                                      Quaternion.LookRotation (transform.position.normalized + Vector3.forward + Vector3.right), 
			                                      Time.deltaTime * dampingLook / 16);
		} else {
			transform.rotation = Quaternion.Slerp (transform.rotation, 
			                                       Quaternion.LookRotation (transform.position.normalized - Vector3.forward - Vector3.right), 
			                                       Time.deltaTime * dampingLook / 8);
		}

	}
}
