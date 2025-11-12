using UnityEngine;

public enum AbilityType { Block, Smash, Fire, Ice, Invincible }

public class PlayerAbilitySelector : MonoBehaviour
{
    public AbilityType currentAbility = AbilityType.Block;
}
