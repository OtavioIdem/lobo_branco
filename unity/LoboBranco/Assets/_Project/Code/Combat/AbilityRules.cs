using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>Por que uma habilidade nao saiu. Viaja pela rede como byte, na recusa do host.</summary>
    public enum AbilityRefusal : byte
    {
        None = 0,

        /// <summary>A vaga pedida esta vazia na escola.</summary>
        NoAbility = 1,

        OnCooldown = 2,

        NoStamina = 3,
    }

    /// <summary>
    /// A pergunta "esta habilidade pode sair agora?", em um lugar so.
    ///
    /// Um lugar so pelo mesmo motivo do <see cref="CombatTuningDef.StaminaCost"/>: o dono
    /// pergunta para saber se vale pedir, o host pergunta para decidir, e contas diferentes nas
    /// duas pontas dariam um sinal que sai na tela de quem conjura e nao sai na do host.
    /// </summary>
    public static class AbilityRules
    {
        /// <summary>
        /// A recarga vem antes do vigor, e a ordem importa para quem joga: "ainda nao voltou" e
        /// uma resposta que o jogador resolve esperando, e "falta vigor" nao. Mostrar a segunda
        /// com a primeira ainda valendo faria o jogador economizar vigor a toa.
        ///
        /// <paramref name="cooldownTolerance"/> e folga de rede, e so o host usa: o pedido do
        /// dono sai quando o relogio estimado dele diz que a recarga voltou, e a estimativa pode
        /// errar por alguns centesimos para o lado errado. Nao e balanceamento.
        ///
        /// <paramref name="stamina"/> nulo quer dizer que vigor nao cobra, que e a convencao do
        /// contexto da maquina de estados.
        /// </summary>
        public static AbilityRefusal Check(
            AbilityDef ability, float cooldownRemaining, IStaminaSource stamina, float cooldownTolerance = 0f)
        {
            if (ability == null) return AbilityRefusal.NoAbility;

            if (cooldownRemaining > Mathf.Max(0f, cooldownTolerance)) return AbilityRefusal.OnCooldown;

            if (stamina != null && !stamina.CanAfford(ability.staminaCost)) return AbilityRefusal.NoStamina;

            return AbilityRefusal.None;
        }
    }
}
