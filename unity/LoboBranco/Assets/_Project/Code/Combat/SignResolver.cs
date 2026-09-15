using UnityEngine;

namespace LoboBranco.Combat
{
    /// <summary>
    /// O instante do efeito de um sinal: quem esta na area, e os efeitos aplicados em cada um
    /// (tarefa 1.18b).
    ///
    /// Autoridade: do host, que e o unico lugar onde isto roda. Este tipo nao sabe disso; quem o
    /// chama e que so chama em quem resolve.
    ///
    /// Classe simples, sem MonoBehaviour e com todos os vetores pre-alocados, como o
    /// <see cref="MeleeHitbox"/>. Um sinal custa uma consulta de fisica e zero alocacao.
    ///
    /// <b>Os alvos saem do mais perto para o mais longe.</b> A fisica devolve colisores em ordem
    /// arbitraria, e quando a matilha passa do teto de alvos, ordem arbitraria quer dizer que o
    /// sinal deixa de fora um barghest colado no bruxo e pega um a seis metros. Pior: a escolha
    /// mudaria de uma conjuracao para a outra sem nada ter mudado na tela.
    ///
    /// O alvo e o <see cref="CharacterVitals"/>, e nao o <see cref="IDamageable"/> do golpe,
    /// porque nem todo sinal fere. Controle, vida e perfil de dano moram todos ao lado dele, e o
    /// efeito pede ao alvo o componente de que precisa.
    /// </summary>
    public sealed class SignResolver
    {
        readonly Collider[] _overlap;
        readonly CharacterVitals[] _targets;
        readonly float[] _sqrDistances;

        int _count;

        /// <summary>Quantos alvos a ultima consulta achou.</summary>
        public int TargetCount => _count;

        /// <summary>Teto de alvos por conjuracao. E de memoria, e nao de design: o docs/03 nao limita.</summary>
        public int MaxTargets => _targets.Length;

        /// <summary>Um alvo da ultima consulta, do mais perto para o mais longe.</summary>
        public CharacterVitals TargetAt(int index) => index >= 0 && index < _count ? _targets[index] : null;

        /// <param name="maxColliders">
        /// Teto de colisores por consulta. Estourar nao lanca erro: a fisica para de preencher, e o
        /// que ficou de fora nao entra nem na ordem por distancia.
        /// </param>
        public SignResolver(int maxColliders = 32, int maxTargets = 16)
        {
            _overlap = new Collider[Mathf.Max(1, maxColliders)];
            _targets = new CharacterVitals[Mathf.Max(1, maxTargets)];
            _sqrDistances = new float[_targets.Length];
        }

        /// <summary>
        /// Acha quem esta na area e aplica os efeitos da habilidade e depois os da variante, alvo a
        /// alvo. Devolve quantos alvos foram atingidos.
        ///
        /// A variante vem depois de proposito. Uma escola especializada soma ao sinal de todo mundo,
        /// e nao troca: o Igni do Grifo ainda queima como o Igni do Lobo (docs/13 secao 5).
        /// </summary>
        /// <param name="variantEffects">Os efeitos a mais da escola de quem conjura. Nulo sem variante.</param>
        public int Resolve(in SignCast cast, SignEffectDef[] variantEffects, LayerMask targetMask)
        {
            int count = Query(cast, targetMask);
            if (count == 0) return 0;

            SignEffectDef[] effects = cast.Ability.effects;

            for (int i = 0; i < count; i++)
            {
                ApplyAll(effects, cast, _targets[i]);
                ApplyAll(variantEffects, cast, _targets[i]);
            }

            return count;
        }

        /// <summary>
        /// So a consulta, sem efeito nenhum. Os alvos ficam em <see cref="TargetAt"/>.
        /// </summary>
        public int Query(in SignCast cast, LayerMask targetMask)
        {
            _count = 0;

            AbilityDef ability = cast.Ability;
            if (ability == null || !ability.area.HasArea) return 0;

            SignArea area = ability.area;

            Vector3 planar = new Vector3(cast.Forward.x, 0f, cast.Forward.z);
            bool hasFacing = planar.sqrMagnitude > 0.000001f;

            // Um cone sem frente acertaria para qualquer lado. O raio nao precisa de frente.
            if (area.shape == SignAreaShape.Cone && !hasFacing) return 0;
            if (hasFacing) planar.Normalize();

            int hits = Physics.OverlapSphereNonAlloc(
                cast.Origin, area.range, _overlap, targetMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits; i++)
            {
                Collider collider = _overlap[i];
                if (collider == null) continue;

                Vector3 center = collider.bounds.center;
                if (!area.IsWithinAngle(cast.Origin, planar, center)) continue;

                if (!TryResolveTarget(collider, out CharacterVitals target)) continue;
                if (target == cast.Caster || target.IsDown) continue;

                Vector3 offset = center - cast.Origin;
                offset.y = 0f;

                Insert(target, offset.sqrMagnitude);
            }

            return _count;
        }

        // ---------------------------------------------------------------- interno

        static void ApplyAll(SignEffectDef[] effects, in SignCast cast, CharacterVitals target)
        {
            if (effects == null) return;

            for (int i = 0; i < effects.Length; i++)
                if (effects[i] != null)
                    effects[i].Apply(cast, target);
        }

        /// <summary>
        /// Insercao ordenada num vetor fixo. Sao poucos alvos, e ordenar por insercao aqui custa
        /// menos que qualquer ordenacao que aloque.
        /// </summary>
        void Insert(CharacterVitals target, float sqrDistance)
        {
            // Uma criatura com dois colisores aparece duas vezes na consulta, e levaria o efeito
            // duas vezes. Fica a primeira distancia vista, que para o efeito tanto faz.
            for (int i = 0; i < _count; i++)
                if (_targets[i] == target)
                    return;

            int slot;

            if (_count < _targets.Length)
            {
                slot = _count;
                _count++;
            }
            else if (sqrDistance < _sqrDistances[_count - 1])
            {
                slot = _count - 1;
            }
            else
            {
                return;
            }

            while (slot > 0 && _sqrDistances[slot - 1] > sqrDistance)
            {
                _targets[slot] = _targets[slot - 1];
                _sqrDistances[slot] = _sqrDistances[slot - 1];
                slot--;
            }

            _targets[slot] = target;
            _sqrDistances[slot] = sqrDistance;
        }

        /// <summary>O colisor raramente e o objeto com vida: costuma ser um filho.</summary>
        static bool TryResolveTarget(Collider collider, out CharacterVitals target)
        {
            if (collider.TryGetComponent(out target)) return true;

            target = collider.GetComponentInParent<CharacterVitals>();
            return target != null;
        }
    }
}
