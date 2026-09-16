using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um efeito de sinal que fere pelo pipeline de dano (tarefa 1.18d). O fogo do docs/03 secao 8
    /// e um asset deste tipo: 0,8 vezes o dano da espada na mao, como fogo.
    ///
    /// Autoridade: o host, como todo efeito de sinal (tech/adr/0012). A vida do alvo e escrita pelo
    /// <see cref="DamageReceiver"/>, que ja recusa dano resolvido fora do host.
    ///
    /// <b>A base e a espada na mao</b>, e nao um numero proprio. O documento fala em "0,8x", e um
    /// multiplicador sem base seria um numero de dano solto que ninguem lembraria de subir quando a
    /// espada melhorar. O material da espada nao entra: fogo nao e aco nem prata.
    ///
    /// Passa pelos estagios de <see cref="DamagePipeline.CreateSignStages"/>, e nao pelos onze.
    /// A intensidade multiplica a base antes do pipeline, e por isso passa pela armadura como o
    /// resto do dano.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Sign Effect/Damage", fileName = "SignEffect_Damage_")]
    public sealed class DamageEffectDef : SignEffectDef
    {
        [Tooltip("Os multiplicadores do combate. Sem asset, o efeito nao fere.")]
        public CombatTuningDef tuning;

        [Tooltip("Vezes o dano base da espada na mao de quem conjura. docs/03 secao 8: 0,8.")]
        [Min(0f)] public float weaponDamageMultiplier = 0.8f;

        [Tooltip("O tipo decide a resistencia do alvo, no estagio 11.")]
        public DamageType damageType = DamageType.Fire;

        // Um pipeline por asset, criado na primeira vez. O contexto dele e reaproveitado entre
        // alvos, entao um sinal que acerta a matilha inteira nao aloca.
        [NonSerialized] DamagePipeline _pipeline;

        public override void Apply(in SignCast cast, CharacterVitals target)
        {
            if (target == null || cast.Caster == null || tuning == null) return;
            if (!target.TryGetComponent(out IDamageable damageable)) return;

            // Os dois contratos moram no atacante do bruxo. Achados por interface porque ele e de
            // um modulo acima deste.
            var dealer = cast.Caster.GetComponent<IDamageDealer>();
            var holder = cast.Caster.GetComponent<IWeaponHolder>();
            MeleeWeaponDef weapon = holder != null ? holder.EquippedWeapon : null;

            float amount = cast.Scale((weapon != null ? weapon.baseDamage : 0f) * weaponDamageMultiplier);
            if (amount <= 0f) return;

            // Postura e material vao no pedido porque o struct pede, e nenhum estagio de sinal le.
            var request = new DamageRequest(
                dealer, damageable, amount, damageType, Stance.Fast,
                weapon != null ? weapon.material : WeaponMaterial.Steel);

            _pipeline ??= new DamagePipeline(tuning, DamagePipeline.CreateSignStages());
            _pipeline.Deal(request);

            // Morte causada rende uma carga (docs/03 secao 7), como no golpe.
            if (damageable.IsDown) cast.Caster.GainAdrenaline();
        }

        public override void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (tuning == null)
                problems.Add("nao tem os multiplicadores do combate, e nao fere");

            if (weaponDamageMultiplier <= 0f)
                problems.Add("multiplica o dano da espada por zero");
        }
    }
}
