// Defines the types of abilities available to the player.
// The order determines the cycling order (E key).

public enum AbilityType 
{
    // None is used internally by GauntletAbilities to signal that the Gauntlet is off 
    // and visuals should be reset to default.
    None, 
    Fire, 
    Ice, 
    Invincible, 
    Light 
}