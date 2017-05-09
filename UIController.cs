using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour {

    public static UIController instance;
    public Dropdown dropDown;
    bool mute = false;

	// Use this for initialization
	void Start () {
		
	}
    void Awake()
    {
    }
    public void OnClickExit()
    {
            Application.Quit();
    }
    public void OnClickMute()
    {
        if (mute == false)
        {
            mute = true;
            AudioListener.volume = 0;
        }
        else
        {
            mute = false;
            AudioListener.volume = 100;
        }
    }
    public void StartGame()
    {
        SceneManager.LoadScene("select");
    }
    public void Track1()
    {
        SceneManager.LoadScene("Countryside");
    }
    public void Track2()
    {
        SceneManager.LoadScene("Race Track");
    }
    public void Track3()
    {
        SceneManager.LoadScene("Rain Forest");
    }
    public void Track4()
    {
        SceneManager.LoadScene("Dark Moon");
    }
    public void Track5()
    {
        SceneManager.LoadScene("Rocky Mountains");
    }
    public void Track6()
    {
        SceneManager.LoadScene("Snowy Peaks");
    }
	// Update is called once per frame
	void Update () {
		 if ( Input.GetKeyDown(KeyCode.Escape) == true )
             SceneManager.LoadScene("uitest");
	}
}
