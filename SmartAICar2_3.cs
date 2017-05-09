using UnityEngine;
using System.Collections;
using System.Collections.Generic;
	
[RequireComponent (typeof (Rigidbody))]

public class SmartAICar2_3 : MonoBehaviour {
	
	//  Raycast distances.
	public int wideRayDistance = 20;
	public int tightRayDistance = 20;
	public int longRayDistance = 20;
	public int sideRayDistance = 4;
	private float newInputSteer = 0f;
	public bool  GUIenabled = false;
	private bool  raycasting = false;
	public LayerMask raycastLayers;
	
	//Wheel colliders of the vehicle.
	public WheelCollider Wheel_FL;
	public WheelCollider Wheel_FR;
	public WheelCollider Wheel_RL;
	public WheelCollider Wheel_RR;
	
	// Wheel transforms of the vehicle.Z
	public Transform FrontLeftWheelT;
	public Transform FrontRightWheelT;
	public Transform RearLeftWheelT;
	public Transform RearRightWheelT;
		
	//Center of mass.
	public Transform COM;
	public float driftStiffness = 0.2f;
	
	// Maximum and minimum engine RPM for adjusting engine audio pitch level and gear ratio.
	private float BrakePower = 0f;
	private bool  reversing = false;
	private bool setDefaultStiffness = true;
	private float longRayBraking = 0f;
	
	// Break Torque will be applied in BrakeZone Areas.
	// Brake Torque will be affected by Tight and Long Raycasts.
	public float brakeZone = 1000;
	public float brakeRaycast = 500;
	
	// We need an array for waypoints, and decide to which waypoint will be our target waypoint.
	public List<Transform> waypoints = new List<Transform>();
	private int currentWaypoint = 0;
	
	// Steer angle and input torque.
	private float inputSteer = 0.0f;
	private float inputTorque = 0.0f;
	
	// Counts laps and how many waypoints passed.
	public int lap = 0;
	public int totalWaypointPassed = 0;
	public int nextWaypointPassRadius = 20;
	
	public UnityEngine.AI.NavMeshAgent navigator;

	// Set wheel drive of the vehicle. If you are using rwd, you have to be careful with your rear wheel collider
	// settings and com of the vehicle. Otherwise, vehicle will behave like a toy. ***My advice is use fwd always***
	public enum WheelType{FWD, RWD};
	public WheelType _wheelTypeChoise;
	private bool rwd = false, fwd = true;
	private float maximumCamberAngle = 0.0f;
	public float camberDegree = 5.0f;
	private float freeCamberDegreeFL, freeCamberDegreeFR, freeCamberDegreeRL, freeCamberDegreeRR;
	

	//Vehicle Mecanim
	public float gearShiftRate = 10.0f;
	public int CurrentGear = 0;
	public AnimationCurve EngineTorqueCurve;
	private float[] GearRatio;
	public float EngineTorque = 750.0f;
	public float MaxEngineRPM = 6000.0f;
	public float MinEngineRPM = 1000.0f;
	private float EngineRPM = 0f;
	public float SteerAngle = 20.0f;
	public float HighSpeedSteerAngle = 10.0f;
	public float HighSpeedSteerAngleAtSpeed = 80.0f;
	private float Speed = 0f;
	public float Brake = 200.0f;
	public float maxSpeed = 180.0f;

	private float StiffnessRear = 0f;
	private float StiffnessFront = 0f;
	private Vector3 acceleration;
	private Vector3 lastVelocity;

	//Sounds
	private GameObject skidAudio;
	public AudioClip skidClip;
	private GameObject crashAudio;
	public AudioClip[] crashClips;
	private GameObject engineAudio;
	public AudioClip engineClip;
	private int crashSoundLimit = 5;
	
	// Each wheel transform's rotation value.
	private float RotationValueFL, RotationValueFR, RotationValueRL, RotationValueRR;
	private float[] RotationValueExtra;
	
	public GameObject WheelSlipPrefab;
	private List <GameObject> WheelParticles = new List<GameObject>();
	
	private WheelFrictionCurve RearLeftFriction;
	private WheelFrictionCurve RearRightFriction;
	private WheelFrictionCurve FrontLeftFriction;
	private WheelFrictionCurve FrontRightFriction;
	private WheelFrictionCurve[] ExtraRearWheelsFriction;
	
	public GameObject chassis;
	public float chassisVerticalLean = 3.0f;
	public float chassisHorizontalLean = 3.0f;
	private float horizontalLean = 0.0f;
	private float verticalLean = 0.0f;
	private float gearTimeMultiplier = 0f;

	
	void Awake (){

		SetWheelFrictions();
		SoundsInitialize();
		WheelTypeInit();
		GearInit();
		if(WheelSlipPrefab)
			SmokeInit();

		InvokeRepeating("Resetting", 6, 6);
			
		// Lower the center of mass for make more stable car.
		GetComponent<Rigidbody>().centerOfMass = new Vector3(COM.localPosition.x * transform.localScale.x , COM.localPosition.y * transform.localScale.y , COM.localPosition.z * transform.localScale.z);
		GetComponent<Rigidbody>().maxAngularVelocity = 10f;
		
	}

	void SetWheelFrictions(){
		
		RearLeftFriction = Wheel_RL.sidewaysFriction;
		RearRightFriction = Wheel_RR.sidewaysFriction;
		FrontLeftFriction = Wheel_FL.sidewaysFriction;
		FrontRightFriction = Wheel_FR.sidewaysFriction;

		StiffnessRear = Wheel_RL.sidewaysFriction.stiffness;
		StiffnessFront = Wheel_FL.sidewaysFriction.stiffness;
		
	}
	
	void SoundsInitialize(){
		
		engineAudio = new GameObject("EngineSound");
		engineAudio.transform.position = transform.position;
		engineAudio.transform.rotation = transform.rotation;
		engineAudio.transform.parent = transform;
		engineAudio.AddComponent<AudioSource>();
		engineAudio.GetComponent<AudioSource>().minDistance = 5;
		engineAudio.GetComponent<AudioSource>().clip = engineClip;
		engineAudio.GetComponent<AudioSource>().loop = true;
		engineAudio.GetComponent<AudioSource>().Play();
		
		skidAudio = new GameObject("SkidSound");
		skidAudio.transform.position = transform.position;
		skidAudio.transform.rotation = transform.rotation;
		skidAudio.transform.parent = transform;
		skidAudio.AddComponent<AudioSource>();
		skidAudio.GetComponent<AudioSource>().minDistance = 10;
		skidAudio.GetComponent<AudioSource>().volume = 0;
		skidAudio.GetComponent<AudioSource>().clip = skidClip;
		skidAudio.GetComponent<AudioSource>().loop = true;
		skidAudio.GetComponent<AudioSource>().Play();
		
		crashAudio = new GameObject("CrashSound");
		crashAudio.transform.position = transform.position;
		crashAudio.transform.rotation = transform.rotation;
		crashAudio.transform.parent = transform;
		crashAudio.AddComponent<AudioSource>();
		crashAudio.GetComponent<AudioSource>().minDistance = 10;
		
	}
	
	void WheelTypeInit(){
		
		switch(_wheelTypeChoise){
		case WheelType.FWD:
			fwd = true;
			rwd = false;
			break;
		case WheelType.RWD:
			fwd = false;
			rwd = true;
			break;
		}
		
	}
	
	void GearInit(){
		
		GearRatio = new float[EngineTorqueCurve.length];
		
		for(int i = 0; i < EngineTorqueCurve.length; i++){
			
			GearRatio[i] = EngineTorqueCurve.keys[i].value;
			
		}
		
	}

	void SmokeInit(){
		
		Instantiate(WheelSlipPrefab, Wheel_FR.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_FL.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_RR.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_RL.transform.position, transform.rotation);
		
		foreach(GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
		{
			if(go.name == "WheelSlip(Clone)")
				WheelParticles.Add (go);
		}
		
		WheelParticles[0].transform.parent = Wheel_FR.transform;
		WheelParticles[1].transform.parent = Wheel_FL.transform;
		WheelParticles[2].transform.parent = Wheel_RR.transform;
		WheelParticles[3].transform.parent = Wheel_RL.transform;
		
	}
	
	void  OnGUI (){
	
		if(GUIenabled){
				
			GUI.color = Color.blue;
			GUI.Button(new Rect(48, 48, Mathf.Abs(Speed), 30), "");
			GUI.Box(new Rect(48, 48, 500, 30), Mathf.Round(Speed) +"");
			GUI.Box(new Rect(270, 10, 50, 30), "Speed");
				
			GUI.color = Color.red;
			GUI.Button(new Rect (48, 125, Mathf.Abs(BrakePower), 30), "");
			GUI.Box(new Rect(48, 125, 500, 30), Mathf.Round(BrakePower) + "");
			GUI.Box(new Rect(255, 85, 90, 30), "BrakePower");
			GUI.Box(new Rect(48, 200, 300, 100), "InputSteer");
			GUI.HorizontalSlider (new Rect (48, 250, 300, 100), inputSteer, -2.0f, 2.0f);
		
		}
		
	}

	void OnDrawGizmos() {
		
			if(waypoints.Count > 0)
				if(waypoints[0])
					Gizmos.DrawIcon(waypoints[0].transform.position, "FinishIcon.png", false);
		
			for(int i = 0; i < waypoints.Count; i ++){
					if(i < waypoints.Count-1){
							if(waypoints[i] && waypoints[i+1]){
									Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.3f);
									Gizmos.DrawSphere (waypoints[i+1].transform.position, 2);
									Gizmos.DrawWireSphere (waypoints[i+1].transform.position, nextWaypointPassRadius);
									if (waypoints.Count > 0) {
											Gizmos.color = Color.green;
													if(i < waypoints.Count - 1)
														Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position); 
													else Gizmos.DrawLine(waypoints[i].position, waypoints[0].position); 
									}
							}
					}
			}
		
	}

	void Update(){

		WheelAlign();

	}
		
	void  FixedUpdate (){
	
		Engine();
		Navigation();
		FixedRaycasts();
		ShiftGears();
		SkidAudio();
		if(chassis)
			Chassis();
	
		if(WheelSlipPrefab)
			SmokeInstantiateRate();
		
		if(setDefaultStiffness){
				
			RearLeftFriction.stiffness = Mathf.Lerp(RearLeftFriction.stiffness, StiffnessRear, Time.deltaTime * 7);
			RearRightFriction.stiffness = Mathf.Lerp(RearRightFriction.stiffness, StiffnessRear, Time.deltaTime * 7);
			FrontLeftFriction.stiffness = Mathf.Lerp(FrontLeftFriction.stiffness, StiffnessFront, Time.deltaTime * 7);
			FrontRightFriction.stiffness = Mathf.Lerp(FrontRightFriction.stiffness, StiffnessFront, Time.deltaTime * 7);
				
			Wheel_FR.sidewaysFriction = FrontRightFriction;
			Wheel_FL.sidewaysFriction = FrontLeftFriction; 
			Wheel_RL.sidewaysFriction = RearLeftFriction;
			Wheel_RR.sidewaysFriction = RearRightFriction;
			
		}
		
	}

	void Engine(){
		
		if(EngineTorqueCurve.keys.Length >= 2){
			if(CurrentGear == EngineTorqueCurve.length-2) gearTimeMultiplier = (((-EngineTorqueCurve[CurrentGear].time / gearShiftRate) / (maxSpeed * 3)) + 1f); else gearTimeMultiplier = ((-EngineTorqueCurve[CurrentGear].time / (maxSpeed * 3)) + 1f);
		}else{
			gearTimeMultiplier = 1;
			Debug.Log ("You DID NOT CREATE any engine torque curve keys!, Please create 1 key at least...");
		}
		
		Speed = GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
		acceleration = (GetComponent<Rigidbody>().velocity - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = GetComponent<Rigidbody>().velocity;
		GetComponent<Rigidbody>().drag = Mathf.Clamp01(transform.InverseTransformDirection(acceleration).z / 50);
		maximumCamberAngle = Mathf.Lerp (maximumCamberAngle, (Mathf.Clamp ((GetComponent<Rigidbody>().angularVelocity.y * 5f), -camberDegree, camberDegree)), Time.deltaTime * 10);
		
		if(EngineTorqueCurve.keys.Length >= 2){
			EngineRPM = ((((Mathf.Abs ((Wheel_FR.rpm * gearShiftRate * Mathf.Clamp01(inputTorque)) + (Wheel_FL.rpm * gearShiftRate * inputTorque)) / 2 ) * (GearRatio[CurrentGear])) * gearTimeMultiplier ) + MinEngineRPM);
		}else{
			EngineRPM = ((((Mathf.Abs ((Wheel_FR.rpm * gearShiftRate * Mathf.Clamp01(inputTorque)) + (Wheel_FL.rpm * gearShiftRate * inputTorque)) / 2 )) * gearTimeMultiplier ) + MinEngineRPM);
		}

		if(Speed < HighSpeedSteerAngleAtSpeed){
			Wheel_FL.steerAngle = SteerAngle * inputSteer;
			Wheel_FR.steerAngle = SteerAngle * inputSteer;
		}else{
			Wheel_FL.steerAngle = HighSpeedSteerAngle * inputSteer;
			Wheel_FR.steerAngle = HighSpeedSteerAngle * inputSteer;
		}

		//Audio.
		engineAudio.GetComponent<AudioSource>().volume = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().volume, Mathf.Clamp (inputTorque, .25f, 1.0f), Time.deltaTime*5);
		engineAudio.GetComponent<AudioSource>().pitch = Mathf.Lerp (1f, 2f, (EngineRPM - MinEngineRPM / 1.5f) / (MaxEngineRPM + MinEngineRPM));
		
		//Applying Torque.
		if(rwd){
			
			if(Speed > maxSpeed){
				Wheel_RL.motorTorque = 0;
				Wheel_RR.motorTorque = 0;
			}else if(!reversing){
				Wheel_RL.motorTorque = EngineTorque  * Mathf.Clamp(inputTorque, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
				Wheel_RR.motorTorque = EngineTorque  * Mathf.Clamp(inputTorque, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
			}else{
				Wheel_RL.motorTorque = (EngineTorque  * -EngineTorqueCurve.Evaluate(Speed) / 4);
				Wheel_RR.motorTorque = (EngineTorque  * -EngineTorqueCurve.Evaluate(Speed) / 4);
			}
			
		}
		
		if(fwd){
			
			if(Speed > maxSpeed){
				Wheel_FL.motorTorque = 0;
				Wheel_FR.motorTorque = 0;
			}else if(!reversing){
				Wheel_FL.motorTorque = EngineTorque * Mathf.Clamp(inputTorque, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
				Wheel_FR.motorTorque = EngineTorque * Mathf.Clamp(inputTorque, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
			}else{
				Wheel_FL.motorTorque = (EngineTorque  * (-EngineTorqueCurve.Evaluate(Speed) / 4));
				Wheel_FR.motorTorque = (EngineTorque  * (-EngineTorqueCurve.Evaluate(Speed) / 4));
			}
				
		}

		// Apply the brake torque values to the rear wheels.
			Wheel_RL.brakeTorque = BrakePower;
			Wheel_RR.brakeTorque = BrakePower;
		
	}
	
	void ShiftGears(){
		
		for(int i = 0; i < EngineTorqueCurve.length; i++){
			
			if(EngineTorqueCurve.Evaluate(Speed) < EngineTorqueCurve.keys[i].value)
				CurrentGear = i;
			
		}
		
	}
	
	void  Navigation (){
			
		// Next waypoint's position.
		Vector3 nextWaypointPosition = transform.InverseTransformPoint( new Vector3(waypoints[currentWaypoint].position.x, transform.position.y, waypoints[currentWaypoint].position.z));

		navigator.SetDestination(waypoints[currentWaypoint].position);
		navigator.transform.localPosition = new Vector3(0, 0, 0);
		if(!reversing)
			inputSteer = Mathf.Clamp((transform.InverseTransformDirection(navigator.desiredVelocity).x + newInputSteer), -2f, 2f);
		else inputSteer = Mathf.Clamp((-transform.InverseTransformDirection(navigator.desiredVelocity).x + newInputSteer), -2f, 2f);
		
		if( Speed >= 25 ){
			BrakePower = Mathf.Abs(inputSteer * brakeRaycast) + longRayBraking;
		}else{
			BrakePower = Mathf.Lerp( BrakePower, 0, Time.deltaTime*10);
		}

		inputTorque = Mathf.Clamp(transform.InverseTransformDirection(navigator.desiredVelocity).z, 0.5f, 1f) ;


		// Checks for the distance to next waypoint. If it is less than written value, then pass to next waypoint.
		if ( nextWaypointPosition.magnitude < nextWaypointPassRadius ) {
				currentWaypoint ++;
				totalWaypointPassed ++;
			
		// If all waypoints are passed, sets the current waypoint to first waypoint and increase lap.
			if ( currentWaypoint >= waypoints.Count ) {
				currentWaypoint = 0;
				lap ++;
			}
		}
			
	}
		
	void  Resetting (){
		
		Vector3 thisT = ( new Vector3( transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z));
			
		if(Speed < 2)
			reversing = true;
				
		else
			reversing = false;
				
		if( thisT.z < 300 && thisT.z > 60 && Speed <= 5 )
			transform.localEulerAngles = new Vector3( transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
			
	}
		
	void  WheelAlign (){
		
		RaycastHit hit;
		WheelHit CorrespondingGroundHit;
		
		freeCamberDegreeFL += Time.deltaTime*5;
		freeCamberDegreeFR += Time.deltaTime*5;
		freeCamberDegreeRL += Time.deltaTime*5;
		freeCamberDegreeRR += Time.deltaTime*5;
		
		
		//Front Left Wheel Transform.
		Vector3 ColliderCenterPointFL = Wheel_FL.transform.TransformPoint( Wheel_FL.center );
		Wheel_FL.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointFL, -Wheel_FL.transform.up, out hit, (Wheel_FL.suspensionDistance + Wheel_FL.radius) * transform.localScale.y) ) {
			FrontLeftWheelT.transform.position = hit.point + (Wheel_FL.transform.up * Wheel_FL.radius) * transform.localScale.y;
			Wheel_FL.transform.rotation = Quaternion.Slerp(Wheel_FL.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_FL.transform.rotation.x, Wheel_FL.transform.rotation.y, (FrontLeftWheelT.transform.localPosition.y*camberDegree - Wheel_FL.transform.localPosition.y*camberDegree) + maximumCamberAngle), Time.deltaTime*50);
			freeCamberDegreeFL = 0;
			float extension = (-Wheel_FL.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_FL.radius) / Wheel_FL.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_FL.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FL.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FL.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			FrontLeftWheelT.transform.position = ColliderCenterPointFL - (Wheel_FL.transform.up * Wheel_FL.suspensionDistance) * transform.localScale.y;
			Wheel_FL.transform.rotation = Quaternion.Slerp(Wheel_FL.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_FL.transform.rotation.x, Wheel_FL.transform.rotation.y, Mathf.Lerp(Wheel_FL.transform.rotation.z, 5f, freeCamberDegreeFL)), Time.deltaTime*50);
			FrontLeftFriction.stiffness = 0;
			Wheel_FL.sidewaysFriction = FrontLeftFriction;
		}
		FrontLeftWheelT.transform.rotation = Wheel_FL.transform.rotation * Quaternion.Euler( RotationValueFL, Wheel_FL.steerAngle, Wheel_FL.transform.rotation.z);
		RotationValueFL += Wheel_FL.rpm * ( 6 ) * Time.deltaTime;
		
		
		//Front Right Wheel Transform.
		Vector3 ColliderCenterPointFR = Wheel_FR.transform.TransformPoint( Wheel_FR.center );
		Wheel_FR.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointFR, -Wheel_FR.transform.up, out hit, (Wheel_FR.suspensionDistance + Wheel_FR.radius) * transform.localScale.y ) ) {
			FrontRightWheelT.transform.position = hit.point + (Wheel_FR.transform.up * Wheel_FR.radius) * transform.localScale.y;
			Wheel_FR.transform.rotation = Quaternion.Slerp(Wheel_FR.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_FR.transform.rotation.x, Wheel_FR.transform.rotation.y,  ((FrontRightWheelT.transform.localPosition.y*-camberDegree) - (Wheel_FR.transform.localPosition.y*-camberDegree))), Time.deltaTime*50);
			freeCamberDegreeFR = 0f;
			float extension = (-Wheel_FR.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_FR.radius) / Wheel_FR.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_FR.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FR.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FR.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			FrontRightWheelT.transform.position = ColliderCenterPointFR - (Wheel_FR.transform.up * Wheel_FR.suspensionDistance) * transform.localScale.y;
			Wheel_FR.transform.rotation = Quaternion.Slerp(Wheel_FR.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_FR.transform.rotation.x, Wheel_FR.transform.rotation.y, Mathf.Lerp(Wheel_FR.transform.rotation.z, -5f, freeCamberDegreeFR)), Time.deltaTime*50);
			FrontRightFriction.stiffness = 0;
			Wheel_FR.sidewaysFriction = FrontRightFriction;
		}
		RotationValueFR += Wheel_FR.rpm * ( 6 ) * Time.deltaTime;
		FrontRightWheelT.transform.rotation = Wheel_FR.transform.rotation * Quaternion.Euler( RotationValueFR, Wheel_FR.steerAngle, Wheel_FR.transform.rotation.z);
		
		
		//Rear Left Wheel Transform.
		Vector3 ColliderCenterPointRL = Wheel_RL.transform.TransformPoint( Wheel_RL.center );
		Wheel_RL.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointRL, -Wheel_RL.transform.up, out hit, (Wheel_RL.suspensionDistance + Wheel_RL.radius) * transform.localScale.y ) ) {
			RearLeftWheelT.transform.position = hit.point + (Wheel_RL.transform.up * Wheel_RL.radius) * transform.localScale.y;
			Wheel_RL.transform.rotation = Quaternion.Slerp(Wheel_RL.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_RL.transform.rotation.x, Wheel_RL.transform.rotation.y, (RearLeftWheelT.transform.localPosition.y*camberDegree - Wheel_RL.transform.localPosition.y*camberDegree)), Time.deltaTime*50);
			freeCamberDegreeRL = 0f;
			float extension = (-Wheel_RL.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_RL.radius) / Wheel_RL.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_RL.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RL.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RL.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			RearLeftWheelT.transform.position = ColliderCenterPointRL - (Wheel_RL.transform.up * Wheel_RL.suspensionDistance) * transform.localScale.y;
			Wheel_RL.transform.rotation = Quaternion.Slerp(Wheel_RL.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_RL.transform.rotation.x, Wheel_RL.transform.rotation.y, Mathf.Lerp(Wheel_RL.transform.rotation.z, 5f, freeCamberDegreeRL)), Time.deltaTime*50);
			RearLeftFriction.stiffness = 0;
			Wheel_RL.sidewaysFriction = RearLeftFriction;
		}
		RearLeftWheelT.transform.rotation = Wheel_RL.transform.rotation * Quaternion.Euler( RotationValueRL, 0, Wheel_RL.transform.rotation.z);
		RotationValueRL += Wheel_RL.rpm * ( 6 ) * Time.deltaTime;
		Wheel_RL.GetGroundHit( out CorrespondingGroundHit );
		
		
		//Rear Right Wheel Transform.
		Vector3 ColliderCenterPointRR = Wheel_RR.transform.TransformPoint( Wheel_RR.center );
		Wheel_RR.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointRR, -Wheel_RR.transform.up, out hit, (Wheel_RR.suspensionDistance + Wheel_RR.radius) * transform.localScale.y ) ) {
			RearRightWheelT.transform.position = hit.point + (Wheel_RR.transform.up * Wheel_RR.radius) * transform.localScale.y;
			Wheel_RR.transform.rotation = Quaternion.Slerp(Wheel_RR.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_RR.transform.rotation.x, Wheel_RR.transform.rotation.y, (RearRightWheelT.transform.localPosition.y*-camberDegree - Wheel_RR.transform.localPosition.y*-camberDegree)), Time.deltaTime*50);
			freeCamberDegreeRR = 0f;
			float extension = (-Wheel_RR.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_RR.radius) / Wheel_RR.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_RR.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RR.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RR.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			RearRightWheelT.transform.position = ColliderCenterPointRR - (Wheel_RR.transform.up * Wheel_RR.suspensionDistance) * transform.localScale.y;
			Wheel_RR.transform.rotation = Quaternion.Slerp(Wheel_RR.transform.rotation, transform.rotation * Quaternion.Euler(Wheel_RR.transform.rotation.x, Wheel_RR.transform.rotation.y, Mathf.Lerp(Wheel_RR.transform.rotation.z, -5f, freeCamberDegreeRR)), Time.deltaTime*50);
			RearRightFriction.stiffness = 0;
			Wheel_RR.sidewaysFriction = RearRightFriction;
		}
		RearRightWheelT.transform.rotation = Wheel_RR.transform.rotation * Quaternion.Euler( RotationValueRR, 0, Wheel_RR.transform.rotation.z);
		RotationValueRR += Wheel_RR.rpm * ( 6 ) * Time.deltaTime;
		Wheel_RR.GetGroundHit( out CorrespondingGroundHit );
		
	}
		
	void FixedRaycasts(){
	
		bool  tightTurn = false;
		bool  wideTurn = false;
		bool  tightTurn1 = false;
		bool  wideTurn1 = false;
		bool sideTurn = false;
		bool sideTurn1 = false;
		bool longRay = false;
			
		Vector3 fwd = transform.TransformDirection ( new Vector3(0, 0, 1));
		RaycastHit hit;
		
		// New input steers effected by fixed raycasts.
		float newinputSteer1 = 0.0f;
		float newinputSteer2 = 0.0f;
		float newinputSteer3 = 0.0f;
		float newinputSteer4 = 0.0f;
		float newinputSteer5 = 0.0f;
		float newinputSteer6 = 0.0f;
		
		// Drawing Rays.
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(25, transform.up) * fwd * wideRayDistance, Color.white);
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(-25, transform.up) * fwd * wideRayDistance, Color.white);
		
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(7, transform.up) * fwd * tightRayDistance, Color.white);
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(-7, transform.up) * fwd * tightRayDistance, Color.white);
		
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(0, transform.up) * fwd * longRayDistance, Color.white);
		
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(90, transform.up) * fwd * sideRayDistance, Color.white);
		Debug.DrawRay (transform.position, Quaternion.AngleAxis(-90, transform.up) * fwd * sideRayDistance, Color.white);
		
		// Wide Raycasts.
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(25, transform.up) * fwd, out hit, wideRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(25, transform.up) * fwd * wideRayDistance, Color.red);
			newinputSteer1 = Mathf.Lerp (-.5f, 0, (hit.distance / wideRayDistance));
			wideTurn = true;
		}
		
		else{
			newinputSteer1 = 0;
			wideTurn = false;
		}
		
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(-25, transform.up) * fwd, out hit, wideRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(-25, transform.up) * fwd * wideRayDistance, Color.red);
			newinputSteer4 = Mathf.Lerp (.5f, 0, (hit.distance / wideRayDistance));
			wideTurn1 = true;
		}
		
		else{
			newinputSteer4 = 0;
			wideTurn1 = false;
		}
		
		// Tight Raycasts.
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(7, transform.up) * fwd, out hit, tightRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(7, transform.up) * fwd * tightRayDistance , Color.red);
			newinputSteer3 = Mathf.Lerp (-1, 0, (hit.distance / tightRayDistance));
			tightTurn = true;
		}
		
		else{
			newinputSteer3 = 0;
			tightTurn = false;
		}
		
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(-7, transform.up) * fwd, out hit, tightRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(-7, transform.up) * fwd * tightRayDistance, Color.red);
			newinputSteer2 = Mathf.Lerp (1, 0, (hit.distance / tightRayDistance));
			tightTurn1 = true;
		}
		
		else{
			newinputSteer2 = 0;
			tightTurn1 = false;
		}
		
		// Side Raycasts.
		
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(90, transform.up) * fwd, out hit, sideRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(90, transform.up) * fwd * sideRayDistance, Color.red);
			newinputSteer5 = Mathf.Lerp (-.5f, 0, (hit.distance / sideRayDistance));
			sideTurn = true;
		}else{
			newinputSteer5 = 0;
			sideTurn = false;
		}
		
		if (Physics.Raycast (transform.position, Quaternion.AngleAxis(-90, transform.up) * fwd, out hit, sideRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(-90, transform.up) * fwd * sideRayDistance, Color.red);
			newinputSteer6 = Mathf.Lerp (.5f, 0, (hit.distance / sideRayDistance));
			sideTurn1 = true;
		}else{
			newinputSteer6 = 0;
			sideTurn1 = false;
		}
		
		// Long Raycasts.
		if(Physics.Raycast (transform.position, Quaternion.AngleAxis(0, transform.up) * fwd, out hit, longRayDistance, raycastLayers)) {
			Debug.DrawRay (transform.position, Quaternion.AngleAxis(0, transform.up) * fwd * longRayDistance, Color.red);
			longRayBraking = Mathf.Lerp (brakeRaycast, 0, (hit.distance / longRayDistance));
		}else{
			longRayBraking = 0;
			reversing = false;
		}

		if(wideTurn || wideTurn1 || tightTurn || tightTurn1 || sideTurn || sideTurn1 || longRay)
			raycasting = true;
		else
			raycasting = false;

		if(raycasting)
			newInputSteer = (newinputSteer1 + newinputSteer2 + newinputSteer3 + newinputSteer4 + newinputSteer5 + newinputSteer6);
		else 
			newInputSteer = 0;

	}
		
	void SkidAudio(){
		
		WheelHit CorrespondingGroundHit;
		Wheel_FR.GetGroundHit( out CorrespondingGroundHit );
		
		if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7) 
			skidAudio.GetComponent<AudioSource>().volume = Mathf.Abs(CorrespondingGroundHit.sidewaysSlip)/20 + Mathf.Abs(CorrespondingGroundHit.forwardSlip)/20;
		else skidAudio.GetComponent<AudioSource>().volume -= Time.deltaTime;
		
	}
		
	void SmokeInstantiateRate () {
		
		WheelHit CorrespondingGroundHit;
		
		if ( WheelParticles.Count > 0 ) {	
			
			Wheel_FR.GetGroundHit( out CorrespondingGroundHit );
			if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7 ) 
				WheelParticles[0].GetComponent<ParticleEmitter>().emit = true;
			else WheelParticles[0].GetComponent<ParticleEmitter>().emit = false;
			
			Wheel_FL.GetGroundHit( out CorrespondingGroundHit );
			if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7 ) 
				WheelParticles[1].GetComponent<ParticleEmitter>().emit = true;
			else WheelParticles[1].GetComponent<ParticleEmitter>().emit = false;
			
			Wheel_RR.GetGroundHit( out CorrespondingGroundHit );
			if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7 ) 
				WheelParticles[2].GetComponent<ParticleEmitter>().emit = true;
			else WheelParticles[2].GetComponent<ParticleEmitter>().emit = false;
			
			Wheel_RL.GetGroundHit( out CorrespondingGroundHit );
			if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7 ) 
				WheelParticles[3].GetComponent<ParticleEmitter>().emit = true;
			else WheelParticles[3].GetComponent<ParticleEmitter>().emit = false;
			
		}
		
	}

	void Chassis(){
		
		verticalLean = Mathf.Clamp(Mathf.Lerp (verticalLean, GetComponent<Rigidbody>().angularVelocity.x * chassisVerticalLean, Time.deltaTime * 5), -3.0f, 3.0f);
		horizontalLean = Mathf.Clamp(Mathf.Lerp (horizontalLean, GetComponent<Rigidbody>().angularVelocity.y * chassisHorizontalLean, Time.deltaTime * 5), -5.0f, 5.0f);
		
		Quaternion targetAngle = Quaternion.Euler(verticalLean, chassis.transform.localRotation.y, horizontalLean);
		chassis.transform.localRotation = targetAngle;
		
	}
		
	void  OnTriggerStay ( Collider other  ){
			
		if(other.gameObject.tag == "BrakeZone" && Speed >= 25){
			BrakePower = brakeZone;
		}
			
		if(other.gameObject.tag == "DriftZone" && Speed >= 15){
				
			setDefaultStiffness = false;
				
			RearLeftFriction.stiffness = Mathf.Lerp(RearLeftFriction.stiffness, driftStiffness, Time.deltaTime * 5);
			RearRightFriction.stiffness = Mathf.Lerp(RearRightFriction.stiffness, driftStiffness, Time.deltaTime * 5);
			FrontLeftFriction.stiffness = Mathf.Lerp(FrontLeftFriction.stiffness, driftStiffness * 2, Time.deltaTime * 5);
			FrontRightFriction.stiffness = Mathf.Lerp(FrontRightFriction.stiffness, driftStiffness * 2, Time.deltaTime * 5);
				
			Wheel_FR.sidewaysFriction = FrontRightFriction;
			Wheel_FL.sidewaysFriction = FrontLeftFriction;
			Wheel_RL.sidewaysFriction = RearLeftFriction;
			Wheel_RR.sidewaysFriction = RearRightFriction; 
				
		}
		
	}

	void OnCollisionEnter( Collision collision ){
		
		
		if (collision.contacts.Length > 0){	
			
			if(collision.relativeVelocity.magnitude > crashSoundLimit && crashClips.Length > 0){
				if (collision.contacts[0].thisCollider.gameObject.layer != LayerMask.NameToLayer("Wheel") ){
					crashAudio.GetComponent<AudioSource>().clip = crashClips[Random.Range(0, crashClips.Length)];
					crashAudio.GetComponent<AudioSource>().pitch = Random.Range (1f, 1.2f);
					crashAudio.GetComponent<AudioSource>().Play ();
				}
			}
			
		}
		
	}
			
	void  OnTriggerExit ( Collider other  ){
			
		if(other.gameObject.tag == "BrakeZone"){
			BrakePower = 0;
		}
			
		setDefaultStiffness = true;
			
	}
		
}