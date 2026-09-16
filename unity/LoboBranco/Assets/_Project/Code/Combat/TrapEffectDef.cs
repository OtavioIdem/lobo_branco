using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// Um efeito de sinal que deixa uma armadilha no chao (tarefa 1.18f). A armadilha do docs/03
    /// secao 8 e um asset deste tipo: 4 m por 12 s, com lentidao de 60%.
    ///
    /// Autoridade: o host. Ele faz nascer o objeto de rede, e as outras maquinas o recebem prontas.
    /// Sem rede, a armadilha nasce local e funciona igual.
    ///
    /// <b>A armadilha nasce onde o bruxo esta</b>, e nao em quem o sinal alcanca. Por isso a area
    /// deste sinal e a forma <c>Self</c>: ela faz o efeito sair uma vez, com quem conjurou como
    /// alvo, e o que este efeito usa e a origem da conjuracao. Um sinal de area que largasse uma
    /// armadilha por criatura atingida deixaria quatro armadilhas numa matilha de quatro.
    ///
    /// A intensidade escala <b>o tempo de campo</b>, e nao os 60%. A lentidao e o numero do
    /// documento, e escalar ate 100% seria imobilizar: a armadilha deixaria de segurar o campo e
    /// passaria a prender. O que um bruxo intenso ganha e chao controlado por mais tempo.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Combat/Sign Effect/Trap", fileName = "SignEffect_Trap_")]
    public sealed class TrapEffectDef : SignEffectDef
    {
        [Tooltip("O prefab da armadilha. Precisa ter NetworkObject e estar registrado no NetworkManager.")]
        public GameObject trapPrefab;

        [Tooltip("Quanto tempo ela fica no chao com a intensidade de referencia. docs/03 secao 8: 12 s.")]
        [Min(0f)] public float seconds = 12f;

        public override void Apply(in SignCast cast, CharacterVitals target)
        {
            if (trapPrefab == null || cast.Caster == null) return;

            // Uma armadilha por conjuracao. Com a area em quem conjura ha um alvo so, e esta guarda
            // e o que impede uma area de cone virar uma armadilha por criatura, se alguem trocar a
            // forma do sinal no asset sem pensar nisto.
            if (target != cast.Caster) return;

            GameObject trap = Instantiate(trapPrefab, cast.Origin, Quaternion.identity);

            if (trap.TryGetComponent(out SignTrap armed))
                armed.Arm(cast.Scale(seconds));

            // Nascer em rede so quando ha rede. Na sandbox solo nao ha NetworkManager nenhum, e
            // chamar Spawn ali lanca excecao.
            if (!trap.TryGetComponent(out NetworkObject netObject)) return;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening && manager.IsServer) netObject.Spawn();
        }

        public override void CollectProblems(List<string> problems)
        {
            if (problems == null) return;

            if (trapPrefab == null)
                problems.Add("nao tem prefab de armadilha, e nao deixa nada no chao");
            else if (trapPrefab.GetComponent<SignTrap>() == null)
                problems.Add($"aponta para '{trapPrefab.name}', que nao e uma armadilha");

            if (seconds <= 0f)
                problems.Add("deixa uma armadilha que dura zero segundos");
        }
    }
}
