
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstaclesMainCs : MonoBehaviour {

GameObject[] obstacles;
int eyeCamLayer;

void  Start (){

obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
eyeCamLayer = LayerMask.NameToLayer ("EyeCamLayer");

for(int i= 0; i < obstacles.Length; i++){

Mesh mesh;
mesh = obstacles[i].GetComponent<MeshFilter>().sharedMesh;
GameObject duplicated;

duplicated = new GameObject("DuplicatedObstacle");
duplicated.transform.position = obstacles[i].transform.position;
duplicated.transform.rotation = obstacles[i].transform.rotation;
duplicated.transform.localScale = obstacles[i].transform.localScale;
duplicated.transform.parent = obstacles[i].transform;
duplicated.AddComponent<MeshFilter>();
duplicated.AddComponent<MeshRenderer>();
duplicated.GetComponent<MeshFilter>().mesh = mesh;
duplicated.layer = eyeCamLayer;
duplicated.tag = "DuplicatedObstacle";

}

}

}