/// <summary>
/// Defines the types of abilities available to the player.
/// The order determines the cycling order (E key).
/// </summary>
public enum AbilityType 
{
    // None is used internally by GauntletAbilities to signal that the Gauntlet is off 
    // and visuals should be reset to default.
    None, 

    // The primary, active abilities start here.
    Fire, 
    Ice, 
    Invincible, 
    Light 
}