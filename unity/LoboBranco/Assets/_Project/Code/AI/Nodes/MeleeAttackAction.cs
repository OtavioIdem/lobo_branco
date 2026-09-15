using LoboBranco.Combat;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Um golpe da criatura, do inicio da anticipacao ao fim da recuperacao.
    ///
    /// Ela so gira durante a anticipacao, e essa e a regra que faz o telegrafo do
    /// docs/03 secao 10 significar alguma coisa. Um monstro que corrige a mira ate o
    /// ultimo instante transforma o tell em decoracao: o jogador le a anticipacao, sai de
    /// linha, e o golpe o acompanha mesmo assim. Parando de girar quando a lamina
    /// compromete, sair de linha passa a funcionar, e ler o tell vira a resposta certa.
    ///
    /// Falha quando nao dava para comecar: durante a pausa entre golpes, durante outro
    /// golpe, ou depois de cair. Falhar aqui devolve o galho para o no de aproximacao.
    ///
    /// Autoridade: o host. O <see cref="EnemyMeleeAttacker"/> recusa em silencio fora
    /// dele, e o proprio grafo se desliga no cliente pelo <c>NetcodeRunOnlyOnOwner</c>
    /// do agente de behavior tree (ADR 0008).
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Golpear",
        description: "Desfere o golpe da especie, encarando o alvo apenas durante a anticipacao.",
        story: "[Agent] golpeia",
        category: "Action/Lobo Branco",
        id: "c4fec3fcb6de449f98bce83d185f33de")]
    public partial class MeleeAttackAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        EnemyAgent m_Enemy;
        EnemyMeleeAttacker m_Attacker;

        protected override Status OnStart()
        {
            m_Enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);
            m_Attacker = EnemyNodeUtility.Resolve<EnemyMeleeAttacker>(Agent);

            if (m_Enemy == null || m_Attacker == null) return Status.Failure;

            // A vez de golpear vem antes de tudo (docs/07 secao 6). Falhar aqui e o caso
            // normal quando o alvo ja tem dois atacantes, e e a falha que manda o galho
            // seguinte da arvore rondar em vez de bater.
            if (!m_Enemy.TryTakeAttackToken()) return Status.Failure;

            // Encarar antes de comecar evita o golpe que nasce de lado e nao acerta nada
            // por causa do filtro de arco da hitbox.
            m_Enemy.FaceTarget(Time.deltaTime);

            if (m_Attacker.TryBeginSwing()) return Status.Running;

            // Nao deu para comecar, quase sempre por causa da pausa entre golpes. Devolver
            // a vez aqui e essencial: uma criatura em recarga segurando o token ocuparia
            // uma das duas vagas do alvo sem nunca bater, e o combate ficaria vazio.
            m_Enemy.ReleaseAttackToken();

            return Status.Failure;
        }

        protected override Status OnUpdate()
        {
            if (m_Enemy == null || m_Attacker == null) return Status.Failure;

            float deltaTime = Time.deltaTime;

            if (m_Attacker.Phase == AttackPhase.Anticipation)
                m_Enemy.FaceTarget(deltaTime);

            return m_Attacker.TickSwing(deltaTime) ? Status.Running : Status.Success;
        }

        protected override void OnEnd()
        {
            // Sobrou golpe em andamento significa que a arvore abortou o galho no meio.
            // Sem este fechamento a janela de dano ficaria aberta dentro do proximo no, e
            // a criatura acertaria enquanto recua.
            if (m_Attacker != null && m_Attacker.Swinging)
                m_Attacker.EndSwing();

            // A vez volta para o encontro assim que o golpe acaba, tenha ele acertado ou
            // nao. Segurar o token durante a pausa entre golpes faria duas criaturas
            // monopolizarem o alvo e as outras nunca chegarem a atacar.
            if (m_Enemy != null) m_Enemy.ReleaseAttackToken();

            m_Enemy = null;
            m_Attacker = null;
        }
    }
}
