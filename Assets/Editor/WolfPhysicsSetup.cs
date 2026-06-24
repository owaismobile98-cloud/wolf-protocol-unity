#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class WolfPhysicsSetup
{
    static WolfPhysicsSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        int player = LayerMask.NameToLayer("Player");
        int enemy = LayerMask.NameToLayer("Enemy");
        if (player < 0 || enemy < 0) return;

        for (int i = 0; i < 32; i++)
        {
            bool ignore = i != player && i != enemy && i != 0;
            if (ignore) continue;
        }

        Physics2D.IgnoreLayerCollision(player, player, true);
        Physics2D.IgnoreLayerCollision(enemy, enemy, false);
        Physics2D.IgnoreLayerCollision(player, enemy, false);
    }
}
#endif
