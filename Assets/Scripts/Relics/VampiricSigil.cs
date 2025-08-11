using UnityEngine;

[CreateAssetMenu(menuName = "Relics/Vampiric Sigil")]
public class VampiricSigil : RelicBase
{
    private float lastHealTime;

    public override void OnDamageDealt(RelicContext ctx, EnemyBase enemy, ref float finalDamage, ElementTag element)
    {
        int stacks = ctx.relicManager.GetStacks(relicId);
        if (stacks <= 0) return;
        if (Time.time < lastHealTime + 0.1f) return;

        float heal = finalDamage * 0.05f * stacks;
        ctx.player?.RestoreHealth(heal);
        lastHealTime = Time.time;
    }
}
