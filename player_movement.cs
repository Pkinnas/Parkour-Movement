using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class player_movement : MonoBehaviour
{
    private Vector3 move_direction, slope_move_direction;

    private float horiz_input, vert_input, slide_timer, initial_movement_speed, 
         player_y_scale, land_hard_timer, upforce, forward_animation_timer, initial_ledge_dist;

    public static float slope_rotation, camera_rotation_x;

    private bool ready_to_jump, regenerate_energy, 
        wall_front, wall_back, roll_check, wall_for_front_flip, player_flip, down_wall_front, down_wall_back, down_wall_right, down_wall_left, 
        player_droping_from_ledge, set_hanging_pos, start_wallrunning, wall_jumping, wall_run_right, wall_run_left, 
        player_run_up, ledge_hit, wall_run_hang_pos, move_rigidbody, run_hang;
        
    public static bool wallrunning, is_grounded, rotate_camera, show_tip_message, start_sliding, wall_left, wall_right,
        player_front_flip, player_roll, player_hanging, climb_up;

    public static int ring_counter;

    private RaycastHit slope_hit, wall_left_hit, wall_right_hit, wall_front_flip, wall_front_hit, ledge_front_hit;

    private Rigidbody player_body;

    private GameObject slope;

    private CapsuleCollider player_capsule;


    [Header("Movement & Jump Vars")]
    [SerializeField] private float movement_speed;
    [SerializeField] private float drag_value;
    [SerializeField] private float jump_val;
    [SerializeField] private float jump_cooldown;
    [SerializeField] private float max_y_speed;
    [SerializeField] private float vault_val;
    [SerializeField] private float collision_drag_value;

    [Header("Wallrun Vars")]
    [SerializeField] private float wallrun_speed;
    [SerializeField] private float wallrun_upforce;
    [SerializeField] private float wallrun_decrease_rate;
    [SerializeField] private Transform wall_check;
    [SerializeField] private float wall_dist;

    [Header("Wall Jump Vars")]
    [SerializeField] private float jump_forward_velocity;
    [SerializeField] private float jump_up_velocity;

    [Header("Keybinds")] 
    [SerializeField] private KeyCode jump_key;
    [SerializeField] private KeyCode dash_key;
    [SerializeField] private KeyCode slide_key;
    [SerializeField] private KeyCode slide_key_2;
    [SerializeField] private KeyCode roll_key;
    [SerializeField] private KeyCode drop_key;

    [Header("Slide Vars")]
    [SerializeField] private float initial_slide_timer;
    [SerializeField] private float slide_speed;
    [SerializeField] private float player_dash_y_scale;

    [Header("Player Camera")]
    [SerializeField] private Camera player_camera;
    [SerializeField] private Animator camera_animator;
    [SerializeField] private ParentConstraint cam_constraint;
    
    [Header("Slope")]
    [SerializeField] private float slope_raycast_length;
    [SerializeField] private Transform slope_check_transform;

    [Header("Motion Blur")]
    [SerializeField] private GameObject motion_blur_effect;

    [Header("Player Animator")]
    [SerializeField] private Animator player_animator;

    [Header("Velocity Y")]
    [SerializeField] private float negative_vel_y;
    [SerializeField] private float min_negative_vel_y;

    [Header("Physic Material")]
    [SerializeField] private PhysicMaterial player_collider_physic_mat;
    [SerializeField] private float dynamic_friction;
    [SerializeField] private float static_friction;

    [Header("Player Roll")]
    [SerializeField] private Transform roll_check_obj;
    [SerializeField] private float ground_dist;

    [Header("Player Front Flip")]
    [SerializeField] private Transform wall_check_for_front_flip;
    [SerializeField] private float front_flip_dist;

    [Header("Wall Collision")]
    [SerializeField] private Transform wall_check_down;

    [Header("Ledge Grab")]
    [SerializeField] private Transform check_ledge;
    [SerializeField] private float ledge_dist;

    [Header("Ring Counter")]
    [SerializeField] private TextMeshProUGUI ring_counter_text;


    [Header("Player SFX")]
    [SerializeField] private AudioSource wall_run_sound, run_up_sound, roll_sound, slide_sound, climb_up_sound, jump_sound;


    private bool run_on_slope, play_wall_run_sound, play_run_up_sound, play_roll_sound;
    private float hang_to_climb_timer;
 
    private void Start()
    {
        player_capsule = this.transform.GetComponent<CapsuleCollider>();
        player_body = this.transform.GetComponent<Rigidbody>();

        player_body.freezeRotation = true;
        ready_to_jump = true;
        
        initial_movement_speed = movement_speed;
        initial_ledge_dist = ledge_dist;

        player_y_scale = this.transform.localScale.y;
                
        wall_left = false;
        wall_right = false;
        wall_front = false;
        wall_back = false;

        camera_rotation_x = 0;
        land_hard_timer = 0;
        forward_animation_timer = 0;
        hang_to_climb_timer = 0;
        ring_counter = 0;

        timer_controller.stop_game = false;
    }

    private void FixedUpdate()
    {
        if (move_rigidbody)
        {
            transform.forward = -ledge_front_hit.normal;
            player_body.MovePosition(transform.position + transform.up * 0.18f + transform.forward * 0.07f);            
            move_rigidbody = false;
        }
    }

    private void Update()
    {



        ring_counter_text.text = ring_counter.ToString();

        

        if (is_grounded)
        {
            start_wallrunning = false;
            player_animator.SetBool("falling", false);
        }
      
        if (player_body.velocity.y < 0)
        {
            player_animator.SetBool("wall_run_up", false);
            play_run_up_sound = false;
        }

        if (wall_run_up_animation())
            ledge_dist = 0.3f;

        else if (!player_hanging)
            ledge_dist = initial_ledge_dist;


        if (hang_to_crouch())
        {
            hang_to_climb_timer += Time.deltaTime;

            if (hang_to_climb_timer > 1)
            {
                player_animator.SetBool("hang_to_climb", false);
                hang_to_climb_timer = 0;
            }

        }




                
        // enable/disable camera constraint
        camera_constraint();

        // limit player's speed
        limit_player_speed();
      
        // movement -------------------------------------------------------------------------------------
     
        // get input
        horiz_input = Input.GetAxisRaw("Horizontal");
        vert_input = Input.GetAxisRaw("Vertical");


        // calculate movement direction
        if (!player_roll)
            move_direction = this.transform.forward * vert_input + this.transform.right * horiz_input;
        else
            move_direction = this.transform.forward * vert_input;


        // collision check
        check_front_wall();
        check_back_wall();

        // remove friction if the player is colliding with a wall
        if (wall_front || wall_back || wall_right || wall_left || down_wall_front || down_wall_right || down_wall_left || down_wall_back)
        {
             physic_mat_on_collision();
        }
        else
            default_physic_mat();


        // trigger fall animation
        if (player_body.velocity.y < negative_vel_y && player_body.velocity.y > min_negative_vel_y)
        {
            player_animator.SetBool("falling", true);
            player_animator.SetBool("fall_to_land", true);
            player_animator.SetBool("fall_to_land_hard", false);            
        }
        else if (player_body.velocity.y < min_negative_vel_y)
        {
            player_animator.SetBool("falling", true);

            player_roll_check();

            if (roll_check)
            {
                // player roll
                if (Input.GetKeyDown(roll_key))
                {
                    player_animator.SetBool("roll", true);
                }
                else
                    player_animator.SetBool("fall_to_land_hard", true);
            }
            player_animator.SetBool("fall_to_land", false);
        }
        else if (player_body.velocity.y == 0f)
        {
            player_animator.SetBool("falling", false);
            player_animator.SetBool("roll", false);          
        }
           
        // resets the "fall_to_land_hard" & "fall_to_land" var
        reset_boolean_vars();

        // stop animation
        if (horiz_input == 0f && vert_input == 0f)
        {
            player_animator.SetBool("player_is_moving", false);

            player_animator.SetBool("forward", false);
            player_animator.SetBool("backward", false);

            player_animator.SetBool("strafe_left", false);
            player_animator.SetBool("strafe_right", false);

            player_animator.SetBool("diagonal_forward_left", false);
            player_animator.SetBool("diagonal_forward_right", false);

            player_animator.SetBool("diagonal_backward_left", false);
            player_animator.SetBool("diagonal_backward_right", false);
        }
        // trigger animation
        else
        {
            if (!timer_controller.stop_game)
            {
                player_animator.SetBool("player_is_moving", true);

                // forward!
                if (vert_input > 0f && horiz_input == 0f && !wall_jumping && !wallrunning)
                {
                    if (is_grounded)
                    {
                        player_animator.SetBool("forward", true);
                        player_animator.SetBool("strafe_left", false);
                        player_animator.SetBool("strafe_right", false);
                        player_animator.SetBool("diagonal_forward_left", false);
                        player_animator.SetBool("diagonal_forward_right", false);
                    }
                    else
                    {
                        forward_animation_timer += Time.deltaTime;
                        if (forward_animation_timer > 0.75f)
                        {
                            player_animator.SetBool("forward", true);
                            forward_animation_timer = 0;
                        }
                    }
                }
                // backward!
                else if (vert_input < 0f && horiz_input == 0f)
                {
                    player_animator.SetBool("backward", true);
                    player_animator.SetBool("strafe_left", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("diagonal_backward_right", false);
                    player_animator.SetBool("diagonal_backward_left", false);

                }
                // strafe left!
                else if (horiz_input < 0f && vert_input == 0f)
                {
                    player_animator.SetBool("strafe_left", true);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("diagonal_forward_left", false);
                    player_animator.SetBool("diagonal_backward_left", false);

                }
                // strafe right!
                else if (horiz_input > 0f && vert_input == 0f)
                {
                    player_animator.SetBool("strafe_right", true);
                    player_animator.SetBool("strafe_left", false);
                    player_animator.SetBool("diagonal_forward_right", false);
                    player_animator.SetBool("diagonal_backward_right", false);
                }
                // diagonal forward left!
                else if (horiz_input < 0f && vert_input > 0f)
                {
                    player_animator.SetBool("diagonal_forward_left", true);
                    player_animator.SetBool("diagonal_backward_left", false);
                    player_animator.SetBool("diagonal_forward_right", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("strafe_left", false);
                    player_animator.SetBool("forward", false);
                }
                // diagonal forward right!
                else if (horiz_input > 0f && vert_input > 0f)
                {
                    player_animator.SetBool("diagonal_forward_right", true);
                    player_animator.SetBool("diagonal_backward_right", false);
                    player_animator.SetBool("diagonal_forward_left", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("strafe_left", false);
                    player_animator.SetBool("forward", false);
                }
                // diagonal backward right!
                else if (vert_input < 0f && horiz_input > 0f)
                {
                    player_animator.SetBool("diagonal_backward_right", true);
                    player_animator.SetBool("diagonal_backward_left", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("diagonal_forward_right", false);
                    player_animator.SetBool("backward", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("strafe_left", false);
                }
                // diagonal backward left!
                else if (vert_input < 0f && horiz_input < 0f)
                {
                    player_animator.SetBool("diagonal_backward_left", true);
                    player_animator.SetBool("diagonal_backward_right", false);
                    player_animator.SetBool("strafe_left", false);
                    player_animator.SetBool("diagonal_forward_left", false);
                    player_animator.SetBool("backward", false);
                    player_animator.SetBool("strafe_right", false);
                    player_animator.SetBool("strafe_left", false);
                }
            }
        }
       
        // calculate slope movement direction
        slope_move_direction = Vector3.ProjectOnPlane(move_direction, slope_hit.normal);
        
        if (is_grounded && !run_on_slope && !fall_to_land_hard_animation() && !fall_from_lower_height_animation() && !fall_to_land_from_lower_height_animation() 
            && !hang_drop_animation() && !hang_to_crouch() && !wall_run_up_animation() && !timer_controller.stop_game)
        {
            

            if (player_is_on_slope() && slope_hit.collider.tag == "slope")
            {
                run_on_slope = true;
                return;
            }


            // move player            
            if (player_roll)
            {
                if (vert_input > 0)
                    player_body.AddForce(move_direction * movement_speed * 10, ForceMode.Force);
            }
            else
            {               
                player_body.AddForce(move_direction * movement_speed * 10, ForceMode.Force);
            }

            player_body.drag = drag_value;

            // physic mat change
            if (wall_left || wall_right)
            {
                physic_mat_on_collision();
                player_body.drag = collision_drag_value;
            }
            else if (!wall_front && !wall_back)
            {
                default_physic_mat();
                player_body.drag = drag_value;
            }

        }
        else if (is_grounded && run_on_slope && slope_hit.collider.tag == "slope")
        {
            // move player
            player_body.AddForce(slope_move_direction * movement_speed * 15, ForceMode.Force);
            default_physic_mat();
        }        
        else
        {
            if (!wallrunning && !fall_to_land_hard_animation() && !crouch_to_stand_animation() && !fall_from_lower_height_animation() 
                && !fall_to_land_from_lower_height_animation() && !hang_drop_animation() && !hang_to_crouch() && !wall_run_up_animation() && !timer_controller.stop_game)
            {
                player_body.AddForce(move_direction * movement_speed * 10, ForceMode.Force);
            }
            player_body.drag = 0;
        }

        // ------------------------------------------------------------------------------------------


        // player flip ------------------------------------------------------------------------------
        check_wall_for_front_flip();

        flip_player();

        // -------------------------------------------------------------------------------------------

        // player climb ------------------------------------------------------------------------------
        if (!player_animator.GetBool("jump_forward"))            
            Ledge_Grab();

        player_hang();

        wall_run_up();

        wall_run_ledge_grab();

        // ------------------------------------------------------------------------------------------

        // wallrun ----------------------------------------------------------------------------------
        wallrun_function();

        check_wall_for_wallrun();

        // move the player when wallrunning
        if (wallrunning)
        {               
            player_body.velocity = new Vector3(player_body.velocity.x, upforce, player_body.velocity.z);
            upforce -= wallrun_decrease_rate * Time.deltaTime;
            player_body.AddForce(this.transform.forward * wallrun_speed * Time.deltaTime, ForceMode.Force);
            player_animator.SetBool("forward", false);

            if (Input.GetKeyDown(jump_key))
            {
                wall_jumping = true;

                player_body.velocity = transform.forward * jump_forward_velocity + transform.up * jump_up_velocity; //wall jump

                player_animator.SetBool("jump_forward", true);
                player_animator.SetBool("wall_run_left", false);
                player_animator.SetBool("wall_run_right", false);
                player_animator.SetBool("forward", false);

                wall_run_right = false;
                wall_run_left = false;
                
                Invoke(nameof(reset_player_jump), 0.35f);
            }

            if (is_grounded)
            {
                wall_run_left = false;
                wall_run_right = false;
            }
        }


        // ------------------------------------------------------------------------------------------


        // player jump -----------------------------------------------------------------------------
        if (Input.GetKey(jump_key) && !player_body.isKinematic && !wall_jumping && !player_hanging && is_grounded && ready_to_jump 
            && !check_fall_to_land_animation() && !jump_forward_animation() && !jump_backward_animation() 
            && !slide_animation() && !fall_to_land_hard_animation() && !crouch_to_stand_animation() 
            && !fall_to_land_from_lower_height_animation() && !vault_animation() && !vault_animation_2() && !falling_animation() && !hang_drop_animation() && !hang_to_crouch() && !player_animator.GetBool("fall_to_land_hard") && !timer_controller.stop_game)
        {

            if (vert_input > 0f && horiz_input == 0f)
            {

                if (vault.player_vault)
                {
                    player_animator.SetBool("vault", true);
                    player_flip = false;
                }

                else if (vault.player_vault_2)
                    player_animator.SetBool("vault_2", true);

                else if (player_flip)
                    player_animator.SetBool("flip", true);

                else if (player_run_up)
                {
                    player_animator.SetBool("wall_run_up", true);
                    if (!play_run_up_sound)
                    {
                        run_up_sound.Play();
                        play_run_up_sound = true;
                    }
                }

                else
                    player_animator.SetBool("jump_forward", true);
            }

            else if (vert_input < 0f && horiz_input == 0f)
                player_animator.SetBool("jump_backward", true);

            else
               player_animator.SetBool("jumping", true);

            ready_to_jump = false;

            if (!vault.player_vault && !vault.player_vault_2 && !player_run_up && !player_hanging && !hang_to_crouch())
            {
                int randomInt = Random.Range(0, 2);

                if (randomInt == 0)
                    climb_up_sound.Play();

                else if (randomInt == 1)
                    jump_sound.Play();

                player_jump();
            }

            // wait a few secs then call reset_player_jump
            Invoke(nameof(reset_player_jump), jump_cooldown);

        }

        // ------------------------------------------------------------------------------------------


        // Slide ------------------------------------------------------------------------------------

        // start player slide 
        if ((Input.GetKeyDown(slide_key) || Input.GetKeyDown(slide_key_2)) && Input.GetAxisRaw("Vertical") > 0f && !start_sliding && is_grounded)
        {
            start_player_slide();
            slide_sound.Play();
        }

        // player slide (forward!)
        if (start_sliding)
            player_slide();

        // --------------------------------------------------------------------------------------------       
    }


    void limit_player_speed()
    {
        // get current velocity
        Vector3 player_vel = new Vector3(player_body.velocity.x, 0f, player_body.velocity.z);

        // if the current velocity is greater than the movement_speed value then limit that velocity
        // the velocity wont be greater than the movement_speed value
        if (player_vel.magnitude > movement_speed && !player_hanging)
        {
            // limit player velocity
            Vector3 limited_speed = player_vel.normalized * movement_speed;
            player_body.velocity = new Vector3(limited_speed.x, player_body.velocity.y, limited_speed.z);
        }

        if (player_body.velocity.y > max_y_speed && !player_hanging)
            player_body.velocity = new Vector3(player_body.velocity.x, max_y_speed, player_body.velocity.y);
    }


    void player_jump()
    {
        if (!player_body.isKinematic && player_body.useGravity)
        {
            // set y velocity to 0
            player_body.velocity = new Vector3(player_body.velocity.x, 0f, player_body.velocity.z);

            // player jump
            player_body.AddForce(this.transform.up * jump_val, ForceMode.Impulse);
        }
    }

     
    void start_player_slide()
    {
        start_sliding = true;
        player_body.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        player_animator.SetBool("sliding", true);
        player_capsule.height = player_capsule.height / 2;
        player_capsule.center = new Vector3(player_capsule.center.x, player_capsule.center.y / 2, player_capsule.center.z);
        slide_timer = initial_slide_timer;
    }

    void stop_player_slide()
    {
        start_sliding = false;
        slide_timer = initial_slide_timer;
        player_animator.SetBool("sliding", false);
        player_capsule.height = player_capsule.height * 2;
        player_capsule.center = new Vector3(player_capsule.center.x, player_capsule.center.y * 2, player_capsule.center.z);
    }

    void player_slide()
    {
        Vector3 slide_force = this.transform.forward * slide_speed;
        player_body.AddForce(slide_force, ForceMode.Force);
        slide_timer -= Time.deltaTime;

        if (slide_timer <= 0)
            stop_player_slide();
    }

    
    void reset_player_jump()
    {
        ready_to_jump = true;
        wall_jumping = false;

        player_animator.SetBool("jumping", false);
        player_animator.SetBool("jump_forward", false);
        player_animator.SetBool("jump_backward", false);

        player_animator.SetBool("vault", false);
        player_animator.SetBool("vault_2", false);

        player_animator.SetBool("flip", false);
                
        vault.player_vault = false;
        vault.player_vault_2 = false;
    }


    void wallrun_function()
    {        
        check_left_wall();
        check_right_wall();

        if (wall_left && !is_grounded && !start_wallrunning && vert_input > 0f && player_body.velocity.y > 0 && wall_left_hit.collider.tag == "env_obj")
        {
            player_animator.SetBool("wall_running", true);
            player_animator.SetBool("wall_run_left", true);

            wall_run_left = true;
            upforce = wallrun_upforce;
            start_wallrunning = true;
        }
        if (wall_right && !is_grounded && !start_wallrunning && vert_input > 0f && player_body.velocity.y > 0 && wall_right_hit.collider.tag == "env_obj")
        {            
            player_animator.SetBool("wall_running", true);
            player_animator.SetBool("wall_run_right", true);

            wall_run_right = true;
            upforce = wallrun_upforce;
            start_wallrunning = true;
        }
    }

    void check_wall_for_wallrun()
    {
        if (wall_run_left || wall_run_right)
        {
            start_wallrun();
            if (!play_wall_run_sound)
            {
                wall_run_sound.Play();
                play_wall_run_sound = true;
            }

        }
        else
        {
            stop_wallrun();
            wall_run_sound.Stop();
            play_wall_run_sound = false;
        }

        if (wall_run_left && !wall_left)
        {
            wall_run_left = false;
            wall_run_right = false;
        }

        if (wall_run_right && !wall_right)
        {
            wall_run_left = false;
            wall_run_right = false;
        }

        if (wall_run_left && horiz_input > 0f)
            wall_run_left = false;

        if (wall_run_right && horiz_input < 0f)
            wall_run_right = false;
    }


    void start_wallrun()
    {
        wallrunning = true;
    }

    void stop_wallrun()
    {
        wallrunning = false;

        player_animator.SetBool("wall_run_left", false);        
        player_animator.SetBool("wall_run_right", false);
        player_animator.SetBool("wall_running", false);
    }

    void reset_player_pos()
    {
        this.transform.position = new Vector3(0, 0, 0);
    }

    
    // checks if the player is on a slope
    private bool player_is_on_slope()
    {

        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        // create a raycast
        if (Physics.Raycast(slope_check_transform.position, Vector3.down, out slope_hit, slope_raycast_length, layerMask))
        {
            // if the raycastHit.normal (slope_hit) isnt equal with Vector3.up then the player is on a slope
            if (slope_hit.normal != Vector3.up)
                return true;

            else
                return false;            
        }
        return false;
    }
    
   
    // wall run - check left collision 
    void check_left_wall()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_left = Physics.Raycast(wall_check.position, -transform.right, out wall_left_hit, wall_dist, layerMask);

        down_wall_left = Physics.Raycast(wall_check_down.position, -transform.right, wall_dist, layerMask);
    }

    // wall run - check right collision
    void check_right_wall()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_right = Physics.Raycast(wall_check.position, transform.right, out wall_right_hit, wall_dist, layerMask);

        down_wall_right = Physics.Raycast(wall_check_down.position, transform.right, wall_dist, layerMask);
    }

    // front wall - collision check
    void check_front_wall()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_front = Physics.Raycast(wall_check.position, transform.forward, out wall_front_hit, wall_dist, layerMask);
       
        down_wall_front = Physics.Raycast(wall_check_down.position, transform.forward, wall_dist, layerMask);
    }

    void check_ledge_function()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        ledge_hit = Physics.Raycast(check_ledge.position, transform.forward, out ledge_front_hit, ledge_dist, layerMask);

    }

    // back wall - collision check
    void check_back_wall()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_back = Physics.Raycast(wall_check.position, -transform.forward, wall_dist, layerMask);

        down_wall_back = Physics.Raycast(wall_check_down.position, -transform.forward, wall_dist, layerMask);
    }
     
    void check_wall_for_front_flip()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_for_front_flip = Physics.Raycast(wall_check_for_front_flip.position, transform.forward, out wall_front_flip, front_flip_dist, layerMask);
    }

    void player_roll_check()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        roll_check = Physics.Raycast(roll_check_obj.position, -transform.up, ground_dist, layerMask);
    }

    void flip_player()
    {
        if (wall_for_front_flip && wall_front_flip.collider.tag == "vault" && !vault.can_vault)
            player_flip = true;

        else if (!wall_for_front_flip)
            player_flip = false;
    }

    void camera_constraint()
    {
        if (roll_animation())
        {
            player_roll = true;
            cam_constraint.enabled = true;
            camera_rotation_x = player_camera.transform.rotation.x;

            if (!play_roll_sound)
            {
                roll_sound.Play();
                play_roll_sound = true;
            }
            
        }
        else if (front_flip_animation())
        {
            player_front_flip = true;
            cam_constraint.enabled = true;
            camera_rotation_x = player_camera.transform.rotation.x;
        }
        else
        {
            player_camera.transform.localPosition = new Vector3(0, 0, 0);
            cam_constraint.enabled = false;                                                        
            player_roll = false;
            player_front_flip = false;

            play_roll_sound = false;
        }
    }
        
    void reset_boolean_vars()
    {
        if (player_animator.GetBool("fall_to_land_hard") || player_animator.GetBool("fall_to_land"))
        {
            land_hard_timer += Time.deltaTime;
            if (land_hard_timer > 0.5f)
            {
                player_animator.SetBool("fall_to_land_hard", false);
                player_animator.SetBool("fall_to_land", false);
                land_hard_timer = 0;
            }
        }
    }

    void Ledge_Grab()
    {
        check_ledge_function();

        if (ledge_hit && ledge_front_hit.collider.tag == "hang" && !player_droping_from_ledge && !wall_run_up_animation() && !is_grounded && player_body.velocity.y >= 0)
        {           
            player_body.useGravity = false;
            player_body.isKinematic = true;
            player_hanging = true;
            player_animator.SetBool("hang", true);
                       
        }        
    }

    void player_hang()
    {
        if (player_hanging)
        {            
            if (!set_hanging_pos && !wall_run_hang_pos && !run_hang)
            {                
                Vector3 hang_position_offset = -transform.up * 0.11f + transform.forward * 0.065f;
                transform.position += hang_position_offset;
                transform.forward = -ledge_front_hit.normal;               
                set_hanging_pos = true;
            }
            else if (wall_run_hang_pos)
            {
                wall_run_hang_pos = false;
                run_hang = true;
                move_rigidbody = true;
            }

            if (Input.GetKeyDown(drop_key))
            {
                player_animator.SetBool("hang_drop", true);
                player_body.useGravity = true;
                player_body.isKinematic = false;
                player_hanging = false;
                player_droping_from_ledge = true;
                run_hang = false;
            }
            else if (Input.GetKeyDown(jump_key))
            {
                player_animator.SetBool("hang_to_climb", true);
                climb_up = true;
                player_body.useGravity = true;
                player_body.isKinematic = false;
                player_hanging = false;
                run_hang = false;
                climb_up_sound.Play();
                
            }
        }
    }

    void wall_run_up()
    {
        check_front_wall();

        if (wall_front && wall_front_hit.collider.tag == "env_obj")
        {
            if (vert_input > 0f && Input.GetKeyDown(jump_key))
            {
                player_run_up = true;
                
            }
        }
        else
        {
            player_run_up = false;
            play_run_up_sound = false;
        }

    }

    void wall_run_ledge_grab()
    {
        if (wall_run_up_animation())
        {
            check_ledge_function();

            if (ledge_hit && ledge_front_hit.collider.tag == "hang")
            {
                player_animator.SetBool("hang", true);
                player_body.useGravity = false;
                player_hanging = true;
                player_body.isKinematic = true;
                wall_run_hang_pos = true;
            }
        }
    }


    // changes the mat's properties
    void physic_mat_on_collision()
    {
        player_collider_physic_mat.dynamicFriction = dynamic_friction;
        player_collider_physic_mat.staticFriction = static_friction;
        player_collider_physic_mat.frictionCombine = PhysicMaterialCombine.Minimum;
        player_collider_physic_mat.bounceCombine = PhysicMaterialCombine.Minimum;
    }

    // resets the mat's properties
    void default_physic_mat()
    {
        player_collider_physic_mat.dynamicFriction = 0.6f;
        player_collider_physic_mat.staticFriction = 0.6f;
        player_collider_physic_mat.frictionCombine = PhysicMaterialCombine.Average;
        player_collider_physic_mat.bounceCombine = PhysicMaterialCombine.Average;
    }

    private bool vault_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Vault") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool vault_animation_2()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Vault_2") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool roll_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Roll") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool check_fall_to_land_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Fall_To_Land_State") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool jump_forward_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Jump_Forward") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool jump_backward_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Jump_Backward") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool slide_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Slide") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }


    private bool fall_to_land_hard_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Fall_To_Land_Hard") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }


    private bool crouch_to_stand_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch_To_Stand") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool fall_from_lower_height_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Falling_From_Lower_Height") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool fall_to_land_from_lower_height_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Fall_To_Land_From_Lower_Height") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool falling_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Falling_Idle") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool front_flip_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Front_Flip") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool hang_drop_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Hang_Drop") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool hang_to_crouch()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("Hang_to_crouch") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }

    private bool wall_run_up_animation()
    {
        if (player_animator.GetCurrentAnimatorStateInfo(0).IsName("wall_run_up") && player_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            return true;

        else
            return false;
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ground") || other.CompareTag("vault") || other.CompareTag("vault_2") || other.CompareTag("wall_climb_up") || other.CompareTag("wall_run") || other.CompareTag("env_obj") || other.CompareTag("slope"))
        {
            is_grounded = true;
        }        
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("ground") || other.CompareTag("wall_climb_up") || other.CompareTag("wall_run") || other.CompareTag("env_obj") || other.CompareTag("slope")) && !player_body.isKinematic)
        {
            player_body.velocity = new Vector3(player_body.velocity.x, 0f, player_body.velocity.z);

            player_animator.SetBool("falling", false);
            player_animator.SetBool("hang", false);
            player_animator.SetBool("hang_drop", false);

            player_droping_from_ledge = false;
            set_hanging_pos = false;
            run_on_slope = false;

            play_run_up_sound = false;
        }        
    }


    private void OnTriggerExit(Collider other)
    {
        is_grounded = false;                     
    }
}
