using UnityEngine;
using System.Collections;

public class Controller : MonoBehaviour {
	
	public float rotationDamping = 20f;
	public float walkSpeed = 6f;
	public float runSpeed = 10f;
	public float stealthSpeed = 3f;
	public float gravity = 9.8f;

	private float moveSpeed;
	private CharacterController controller;

	public enum MovementType {Stealth, Running, Walking};
	private MovementType movingState;
	public MovementType MovingState { 
		get{
			return movingState;
		}
	}

	void Start() {
		controller = (CharacterController)GetComponent(typeof(CharacterController));

		movingState = MovementType.Walking;
	}

	void UpdateMovement() {

		// Movement
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		Vector3 inputVec = new Vector3(x, 0, z);
		inputVec *= moveSpeed * Time.deltaTime;

		controller.Move(inputVec);

		// Rotation
		if (inputVec != Vector3.zero)
			transform.rotation = Quaternion.Slerp(transform.rotation, 
			                                      Quaternion.LookRotation(inputVec), 
			                                      Time.deltaTime * rotationDamping);
	}

	void Update()
	{
		if (Input.GetAxis ("Stealth") > 0) {
			moveSpeed = stealthSpeed;
			movingState = MovementType.Stealth;
		} else if (Input.GetAxis ("Run") > 0) {
			moveSpeed = runSpeed;
			movingState = MovementType.Running;
		} else {
			moveSpeed = walkSpeed;
			movingState = MovementType.Walking;
		}
		// Actually move the character
		UpdateMovement();
	}
}