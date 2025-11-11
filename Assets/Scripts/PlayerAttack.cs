using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        // Get the Animator component from the character object
        animator = GetComponentInChildren<Animator>(); 
    }

    void Update()
    {
        // Check for a button press (e.g., Left Mouse Button)
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void Attack()
    {
        // Tell the Animator to fire the 'Attack' Trigger
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}