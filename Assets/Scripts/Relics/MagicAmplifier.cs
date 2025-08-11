using UnityEngine;

[CreateAssetMenu(menuName = "Relics/Magic Amplifier")]
public class MagicAmplifier : RelicBase
{
    public override void ModifyWeaponStats(RelicContext ctx, WeaponBase weapon, ref float damage, ref float cooldown, ref float range)
    {
        int stacks = ctx.relicManager.GetStacks(relicId);
        damage *= 1f + 0.15f * stacks;
        range *= 1f + 0.10f * stacks;
    }
}
