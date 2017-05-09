using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class s_Waypoints : MonoBehaviour
{
    private Rigidbody rb; //Current rigidbody
    private CarController m_CarController;

    public Transform[] checkPoints; //Array of cubes
    public GameObject[] Enemies; //Array of Enemy cars
    static int[] enemyPos;  //Static Array of ENemy position
    static int[] enemyLap;
    public Text rankText;  //Rank of Current Player
    public int lap=1;  //Number of lap
    public Text LapText;
    private int curLap=0;
    private int curRank = 0;

    string LapString1st = "Lap: ";
    string LapStringlast = "/";
    string rankString1st = "Rank: ";
    string rankStringlast = "/";


    int lastCube = 0;
    int flag = -1;  //
    bool updateRank = true;
    int stop = 0;
    float curTime;
    int[] checkcubes;

    void Start()
    {
        curRank = Enemies.Length + 1;
        curLap = 1;
        flag = -1;

        rankStringlast = "/" + (Enemies.Length+1).ToString();
        LapStringlast = "/" + lap.ToString();

        m_CarController = GetComponent<CarController>();
        rb = GetComponent<Rigidbody>();

        int n = Enemies.Length;
        enemyPos = new int[n];
        enemyLap = new int[n];

        checkcubes = new int[checkPoints.Length];

        if (gameObject.CompareTag("Player"))
        {
            rankText.text = rankString1st + curRank.ToString() + LapStringlast;
            LapText.text = LapString1st + curLap.ToString() + LapStringlast;
        }

        for (int i = 0; i < enemyPos.Length; i++)
        {
            enemyPos[i] = 0;
            enemyLap[i]=1;
        }
  
        for (int i = 0; i < checkcubes.Length; i++)
            checkcubes[i] = -1;
        
    }


    int cnt = 0;
    void Update()
    {
        if (!gameObject.CompareTag("Player"))
        {
            if (curLap > lap) //Race stops
                stop++;
            if (stop > 40)
            {
                rb.velocity = Vector3.zero;
                m_CarController.Move(0, 0, -1f, 1f);
            }
        }
        else
        {
            if (curLap <= lap)
                LapText.text = LapString1st + curLap.ToString() + LapStringlast;
            if (curLap > lap) //Race stops
                stop++;
            if (stop > 40)
            {
                rb.velocity = Vector3.zero;
                m_CarController.Move(0, 0, -1f, 1f);
            }

            int rank = 1;
            int nextCube=(lastCube+1)%checkPoints.Length;
            for (int i = 0; i < enemyPos.Length; i++)
            {
                float enemyDist=Vector3.Distance(checkPoints[nextCube].position,Enemies[i].transform.position);
                float playerDist=Vector3.Distance(checkPoints[nextCube].position,transform.position);
                
                if (enemyPos[i] > lastCube || enemyLap[i]>curLap)
                    rank++;
                else if (enemyLap[i]==curLap && enemyPos[i] == lastCube && enemyDist < playerDist)
                    rank++;
            }
            if(updateRank)
                rankText.text = rankString1st + rank.ToString() + rankStringlast;

        }
            
            
    }


    void OnTriggerEnter(Collider other)
    {
        int EnemyId = -1;
        for (int i = 0; i < Enemies.Length; i++)
        {
            if (Enemies[i].GetInstanceID() == gameObject.GetInstanceID())
            {
                EnemyId = i;
                break;
            }
        }
        int cubeID = 0;

        for (int i = 0; i < checkPoints.Length; i++)
        {
            if (other.GetInstanceID() == checkPoints[i].GetComponent<Collider>().GetInstanceID())
            {
                cubeID = i;
                break;
            }
        }
        if (EnemyId != -1)    //Update Static Array of Enemy positions 
            enemyPos[EnemyId] = cubeID;

        checkcubes[cubeID] = curLap;

        if (cubeID + 1 == checkPoints.Length && flag == -1)
        {
            int i;
            for ( i = 0; i < checkcubes.Length; i++)
            {
                if (checkcubes[i] != curLap)
                    break;
            }
            if (i == checkcubes.Length)
                curLap++;
            flag = 1;
        }
        else if (cubeID + 1 != checkPoints.Length)
            flag = -1;

        if (EnemyId != -1)    //Update Static Array of Enemy positions 
            enemyLap[EnemyId] = curLap;

        if (curLap > lap)
            updateRank = false;
        if (gameObject.CompareTag("Player"))
        {
            lastCube = cubeID;
        }
    }

}