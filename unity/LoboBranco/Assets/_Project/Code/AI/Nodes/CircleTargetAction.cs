using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Ronda o alvo a distancia de engajamento, encarando ele, enquanto espera a vez de
    /// golpear.
    ///
    /// Este no e o outro lado do token de ataque (docs/07 secao 6), e sem ele o token
    /// pioraria o combate em vez de melhora-lo: a terceira criatura seria recusada, cairia
    /// no galho de perseguir, colaria no bruxo e ficaria parada encostada nele. Um inimigo
    /// imovel a meio metro parece travado, e travado e pior do que injusto. Rondando, a
    /// espera vira ameaca: o jogador ve quem esta na fila.
    ///
    /// O deslocamento angular e por criatura, e nao um numero fixo, porque duas que
    /// cheguem pelo mesmo lado iriam para o mesmo ponto do anel e se empurrariam.
    ///
    /// Devolve sucesso ao chegar no anel, e nao fica rodando para sempre: assim a arvore
    /// reavalia e o galho de golpear tem chance de ganhar assim que uma vaga abrir.
    /// </summary>
    [System.Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Rondar Alvo",
        description: "Anda ate o anel de engajamento, encarando o alvo. E a espera da vez de golpear.",
        story: "[Agent] ronda o alvo",
        category: "Action/Lobo Branco",
        id: "c28a4133edbd4c6fb6e4243e7f3656fb")]
    public partial class CircleTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        [Tooltip("Quanto ela desliza pelo anel a cada aproximacao, em graus. Zero fica parada no lugar.")]
        [SerializeReference] public BlackboardVariable<float> DriftDegrees = new BlackboardVariable<float>(35f);

        [Tooltip("Quao perto do ponto do anel conta como chegou, em metros.")]
        [SerializeReference] public BlackboardVariable<float> Tolerance = new BlackboardVariable<float>(0.6f);

        EnemyAgent m_Enemy;
        Vector3 m_Destination;
        float m_Drift;

        protected override Status OnStart()
        {
            m_Enemy = EnemyNodeUtility.Resolve<EnemyAgent>(Agent);
            if (m_Enemy == null || m_Enemy.Target == null) return Status.Failure;

            // O sinal do deslize sai do identificador da propria criatura, entao metade
            // ronda para um lado e metade para o outro, sem sorteio e sem estado guardado.
            m_Drift = DriftDegrees != null ? DriftDegrees.Value : 35f;
            if ((m_Enemy.EntityKey & 1UL) == 0UL) m_Drift = -m_Drift;

            m_Destination = m_Enemy.CirclePosition(m_Drift);
            m_Enemy.MoveTo(m_Destination);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_Enemy == null || m_Enemy.Target == null) return Status.Failure;

            // Encarar enquanto ronda e o que faz a espera parecer intencao. Uma criatura
            // que circula de costas parece um bug de navegacao.
            m_Enemy.FaceTarget(Time.deltaTime);

            // O alvo anda, entao o ponto do anel anda com ele. O mesmo deslize do inicio
            // continua valendo: recalcular sem ele faria a criatura so ajustar a distancia
            // e nunca rondar de fato.
            m_Destination = m_Enemy.CirclePosition(m_Drift);
            m_Enemy.MoveTo(m_Destination);

            float tolerance = Tolerance != null ? Tolerance.Value : 0.6f;

            Vector3 delta = m_Destination - m_Enemy.transform.position;
            delta.y = 0f;

            return delta.sqrMagnitude <= tolerance * tolerance ? Status.Success : Status.Running;
        }

        protected override void OnEnd()
        {
            // Parar ao sair evita que o caminho velho continue empurrando a criatura
            // durante o golpe do no seguinte.
            if (m_Enemy != null) m_Enemy.StopMoving();

            m_Enemy = null;
        }
    }
}
