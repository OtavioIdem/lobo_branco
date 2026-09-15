using Unity.Behavior;
using UnityEngine;

namespace LoboBranco.AI
{
    /// <summary>
    /// Resolve o componente de IA a partir da variavel <c>Agent</c> do quadro negro.
    ///
    /// Existe para que os nos nao repitam, cada um, as mesmas quatro linhas de checagem
    /// de nulo. O motivo de importar nao e economia de digitacao: um no que esqueca uma
    /// delas lanca <c>NullReferenceException</c> dentro do grafo, e excecao dentro de
    /// behavior tree para o galho inteiro sem dizer qual no foi.
    /// </summary>
    internal static class EnemyNodeUtility
    {
        /// <summary>
        /// O componente no objeto apontado pela variavel, ou nulo. Procura nos filhos
        /// tambem porque o corpo greybox e o colisor costumam ser filhos da raiz.
        /// </summary>
        public static T Resolve<T>(BlackboardVariable<GameObject> variable) where T : Component
        {
            GameObject target = variable != null ? variable.Value : null;
            if (target == null) return null;

            if (target.TryGetComponent(out T component)) return component;

            return target.GetComponentInChildren<T>();
        }
    }
}
