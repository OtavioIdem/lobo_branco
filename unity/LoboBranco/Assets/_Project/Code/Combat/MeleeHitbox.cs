using System.Collections.Generic;
using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// A janela de dano de um golpe corpo a corpo: uma consulta de capsula sem alocacao,
    /// filtrada por arco, com a lista de quem ja foi atingido neste golpe.
    ///
    /// Nao usa <c>OnTriggerEnter</c> em colisor de espada, e isso e deliberado
    /// (docs/07 secao 4.5). Trigger dispara quando a fisica quer, entrega ordem
    /// imprevisivel, e uma espada que atravessa um alvo em dois frames gera dois eventos.
    /// A consulta explicita entrega controle de quadro e a lista de ja-atingidos resolve
    /// o golpe duplo em um lugar so.
    ///
    /// Classe simples, sem MonoBehaviour, com todos os vetores pre-alocados. Um golpe
    /// custa zero alocacao, que e a regra 5 do CLAUDE.md.
    /// </summary>
    public sealed class MeleeHitbox
    {
        readonly Collider[] _overlap;
        readonly List<IDamageable> _alreadyHit;

        /// <summary>Quantos alvos distintos este golpe ja acertou.</summary>
        public int HitCount => _alreadyHit.Count;

        /// <param name="maxColliders">
        /// Teto de colisores por consulta. Estourar nao lanca erro: a fisica simplesmente
        /// para de preencher, e alvos alem do teto ficam invisiveis ao golpe.
        /// </param>
        public MeleeHitbox(int maxColliders = 16, int maxTargets = 8)
        {
            _overlap = new Collider[Mathf.Max(1, maxColliders)];
            _alreadyHit = new List<IDamageable>(Mathf.Max(1, maxTargets));
        }

        /// <summary>
        /// Comeca um golpe novo. Sem isto, o segundo golpe herda a lista do primeiro e
        /// nunca acerta o mesmo inimigo duas vezes na luta inteira.
        /// </summary>
        public void BeginSwing() => _alreadyHit.Clear();

        /// <summary>
        /// Uma consulta da janela aberta. Devolve quantos alvos novos entraram em
        /// <paramref name="results"/>; alvos ja atingidos neste golpe sao ignorados.
        /// </summary>
        /// <param name="origin">Base do personagem, normalmente <c>transform.position</c>.</param>
        /// <param name="forward">Para onde ele encara. E achatado no plano XZ aqui dentro.</param>
        public int Query(
            Vector3 origin,
            Vector3 forward,
            AttackDef attack,
            LayerMask targetMask,
            IDamageable[] results)
        {
            if (attack == null || results == null || results.Length == 0) return 0;
            if (_alreadyHit.Count >= attack.maxTargets) return 0;

            Vector3 planar = new Vector3(forward.x, 0f, forward.z);
            if (planar.sqrMagnitude < 0.000001f) return 0;
            planar.Normalize();

            Vector3 center = origin + Vector3.up * attack.heightOffset;
            Vector3 near = center + planar * attack.radius;
            Vector3 far = center + planar * Mathf.Max(attack.radius, attack.reach - attack.radius);

            int count = Physics.OverlapCapsuleNonAlloc(
                near, far, attack.radius, _overlap, targetMask, QueryTriggerInteraction.Collide);

            float halfArc = attack.arcDegrees * 0.5f;
            int found = 0;

            for (int i = 0; i < count; i++)
            {
                Collider collider = _overlap[i];
                if (collider == null) continue;

                if (!TryResolveTarget(collider, out IDamageable target)) continue;
                if (_alreadyHit.Contains(target)) continue;

                // O arco e o que separa "golpe a frente" de "golpe em volta". A capsula
                // sozinha ja pega alvos de lado, e sem este filtro a postura Grupo
                // deixaria de ser uma decisao: todo golpe seria em area.
                Vector3 toTarget = collider.bounds.center - center;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.000001f && Vector3.Angle(planar, toTarget) > halfArc)
                    continue;

                _alreadyHit.Add(target);
                results[found] = target;
                found++;

                if (found >= results.Length) break;
                if (_alreadyHit.Count >= attack.maxTargets) break;
            }

            return found;
        }

        /// <summary>
        /// O colisor atingido raramente e o objeto que leva dano: costuma ser um filho.
        /// Procurar no proprio primeiro evita subir a hierarquia no caso comum.
        /// </summary>
        static bool TryResolveTarget(Collider collider, out IDamageable target)
        {
            if (collider.TryGetComponent(out target)) return true;

            target = collider.GetComponentInParent<IDamageable>();
            return target != null;
        }
    }
}
