using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{

    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        private GameManagerScript GMS; //Edit By Maruf

        

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            GMS = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        }


        private void FixedUpdate()
        {
            if (GMS.CountdownDone == true)
            {            // pass the input to the car!
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);
#else
                m_Car.Move(h, v, v, 0f);
#endif
            }
        }
    }
}
