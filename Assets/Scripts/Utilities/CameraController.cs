using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
 
public class CameraController : MonoBehaviour
{
    public InputAction leftShift; // Left Shift button press trigger
    public InputAction horizontal; // 'A' and 'D' key tracker
    public InputAction vertical; // 'W' and 'S' key tracker
    public InputAction mouse; // Mouse position
    public float Normal_Speed = 25.0f; //Normal movement speed
   
    public float Shift_Speed = 54.0f; //multiplies movement speed by how long shift is held down.
   
    public float Speed_Cap = 54.0f; //Max cap for speed when shift is held down
  
    public float Camera_Sensitivity = 0.6f; //How sensitive it with mouse
   
    private Vector3 Mouse_Location = new Vector3(255, 255, 255); //Mouse location on screen during play (Set to near the middle of the screen)
    
    private float Total_Speed = 1.0f; //Total speed variable for shift


    private void OnEnable()
    {
        leftShift.Enable();
        horizontal.Enable();
        vertical.Enable();
    }

    private void OnDisable() 
    {
        leftShift.Disable();
        horizontal.Disable();
        vertical.Disable();
    }
    void Update()
    {


      
        //Keyboard controls
        Vector3 Cam = GetBaseInput();
        if (leftShift.ReadValue<float>() > 0.0f)
        {


            Total_Speed += Time.deltaTime;
           
            Cam = Cam * Total_Speed * Shift_Speed;
           
            Cam.x = Mathf.Clamp(Cam.x, -Speed_Cap, Speed_Cap);
           
            Cam.y = Mathf.Clamp(Cam.y, -Speed_Cap, Speed_Cap);
           
            Cam.z = Mathf.Clamp(Cam.z, -Speed_Cap, Speed_Cap);



        }
        else
        {
           
            
            Total_Speed = Mathf.Clamp(Total_Speed * 0.5f, 1f, 1000f);
           
            Cam = Cam * Normal_Speed;


        }

        Cam = Cam * Time.deltaTime;
        
        Vector3 newPosition = transform.position;
       
        if (leftShift.ReadValue<float>() > 0.0f)
        {


            //If the player wants to move on X and Z axis only by pressing space (good for re-adjusting angle shots)
            transform.Translate(Cam);
            newPosition.x = transform.position.x;
            newPosition.z = transform.position.z;
            transform.position = newPosition;


        }
        else
        {


            transform.Translate(Cam);


        }

    }

    private Vector3 GetBaseInput()
    {   


        Vector3 Camera_Velocity = new Vector3();
        


        Camera_Velocity += new Vector3(horizontal.ReadValue<float>(), 0, 0);

        Camera_Velocity += new Vector3(0, 0, vertical.ReadValue<float>());
       
        return Camera_Velocity;


    }
}