using UnityEngine;

[CreateAssetMenu(menuName = "Relics/Immortal Quiver")]
public class ImmortalQuiver : RelicBase
{
    public override void OnProjectileSpawned(RelicContext ctx, PooledProjectile proj)
    {
        int stacks = ctx.relicManager.GetStacks(relicId);
        if (stacks <= 0) return;

        proj.ApplySpeedMultiplier(Mathf.Pow(1.3f, stacks));
        proj.Pierce += 1 * stacks;
    }
}
