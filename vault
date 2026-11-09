using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vault : MonoBehaviour
{
    [Header("Keybinds")]
    [SerializeField] private KeyCode jump_key;

    [Space]
    [SerializeField] private Transform player_transform, vault_end_point, climb_end_point, wall_check_transform, wall_check_for_climb_transform, climb_end_point_2;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody player_body;
    
    [Header("Player Animator")]
    [SerializeField] private Animator player_anim;

    [Header("Lerp Duration")]
    [SerializeField] private float desired_duration;

    [Header("Wall Distance")]
    [SerializeField] private float wall_distance;

    private bool lerp, wall_collision, wall_collision_after_jump, start_vault_timer, can_climb;

    public static bool player_vault, player_vault_2, can_vault,can_vault_2;

    private float timer, vault_timer;

    private RaycastHit wall_hit, air_wall_hit;

    private Vector3 current_pos, move_pos;

    private void Update()
    {
        wall_check();

        if (wall_collision && wall_hit.collider.tag == "vault")
            can_vault = true;

        else if (wall_collision && wall_hit.collider.tag == "vault_2")
            can_vault_2 = true;


        else if (wall_collision_after_jump && air_wall_hit.collider.tag == "hang")
        {
            can_climb = true;       
            player_anim.SetBool("hang_to_climb", true);
        }

        else
        {
            can_vault = false;
            can_vault_2 = false;
        }

        if (start_vault_timer)
        {
            vault_timer += Time.deltaTime;

            if (vault_timer > 0.5f)
            {
                player_vault = false;
                player_vault_2 = false;

                start_vault_timer = false;
                vault_timer = 0;

                player_anim.SetBool("vault", false);
                player_anim.SetBool("vault_2", false);
            }
        }
    }

    private void FixedUpdate()
    {
        // player vault
        if (can_vault && Input.GetKey(jump_key) && Input.GetAxisRaw("Vertical") > 0f && !player_movement.start_sliding)
        {
            player_vault = true;
            lerp = true;
            player_anim.SetBool("vault", true);
        }

        // player vault 2
        if (can_vault_2 && Input.GetKey(jump_key) && Input.GetAxisRaw("Vertical") > 0f && !player_movement.start_sliding)
        {
            player_vault_2 = true;
            lerp = true;
            player_anim.SetBool("vault_2", true);
        }


        // player climb
        if (player_movement.climb_up || can_climb)
        {

            if (can_climb)
            {
                // secondary position (the player will reach here after the vault)
                move_pos = climb_end_point_2.position;
            }

            else
            {
                // secondary position (the player will reach here after the vault)
                move_pos = climb_end_point.position;
            }

            // move player from his current position to the second position (move_pos)
            player_transform.position = Vector3.Lerp(current_pos, move_pos, 1f);
            player_movement.climb_up = false;
            can_climb = false;
            player_anim.SetBool("hang_to_climb", false);
        }


        // player lerp
        if (lerp)
            player_lerp();
      
    }


    void wall_check()
    {
        int layerMask = 1 << 6;

        // collide against everything except layer 6
        layerMask = ~layerMask;

        wall_collision = Physics.Raycast(wall_check_transform.position, transform.forward, out wall_hit, wall_distance, layerMask);
        wall_collision_after_jump = Physics.Raycast(wall_check_for_climb_transform.position, transform.forward, out air_wall_hit, wall_distance, layerMask);
    }



    void player_lerp()
    {
        timer += Time.deltaTime;
        float completion = timer / desired_duration;
        player_body.isKinematic = true;

        // get player's current position
        current_pos = player_transform.position;


        if (player_vault || player_vault_2)
        {
            // secondary position (the player will reach here after the vault)
            move_pos = vault_end_point.position;

            // move player from his current position to the second position (move_pos)
            player_transform.position = Vector3.Lerp(current_pos, move_pos, completion);

            start_vault_timer = true;
        }       
        else
        {
            timer = 0;
            lerp = false;
            player_body.isKinematic = false;
        }
    } 
}
