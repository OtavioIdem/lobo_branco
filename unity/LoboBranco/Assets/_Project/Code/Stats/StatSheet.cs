using System;
using System.Collections.Generic;

namespace LoboBranco.Stats
{
    /// <summary>
    /// A folha de atributos de uma criatura. Pocoes, talentos, mutagenios, equipamento e
    /// buffs de combate escrevem todos aqui, e e daqui que o pipeline de dano le.
    ///
    /// Ser uma classe simples, sem MonoBehaviour, e deliberado: da para instanciar em
    /// teste de EditMode sem cena, sem GameObject e sem loop de jogo.
    ///
    /// Nao e segura para uso em varias threads. Todo o jogo roda na thread principal.
    /// </summary>
    public sealed class StatSheet
    {
        /// <summary>
        /// Maior valor de <see cref="StatType"/> mais um. Os enums sao esparsos de
        /// proposito, para dar espaco a categorias novas sem renumerar as existentes,
        /// entao os vetores tem alguns buracos. Custa alguns bytes e evita um Dictionary
        /// no caminho quente.
        /// </summary>
        static readonly int Capacity = ComputeCapacity();

        readonly float[] _baseValues = new float[Capacity];
        readonly float[] _cached = new float[Capacity];
        readonly bool[] _dirty = new bool[Capacity];

        // Lista unica varrida por stat em vez de um Dictionary de listas. O numero de
        // modificadores ativos e da ordem de dezenas, e o cache faz a varredura acontecer
        // so quando algo muda. Um Dictionary aqui alocaria e nao ganharia nada.
        readonly List<StatModifier> _modifiers = new List<StatModifier>(32);

        /// <summary>Disparado quando o valor efetivo de um atributo pode ter mudado. A UI escuta isto em vez de fazer polling.</summary>
        public event Action<StatType> StatChanged;

        public StatSheet()
        {
            MarkAllDirty();
        }

        // ------------------------------------------------------------------ base

        /// <summary>
        /// Define o valor base, antes de qualquer modificador. E o que vem do
        /// <see cref="StatBlockDef"/> da criatura.
        /// </summary>
        public void SetBase(StatType stat, float value)
        {
            int i = (int)stat;
            if (_baseValues[i] == value) return;

            _baseValues[i] = value;
            Invalidate(stat);
        }

        public float GetBase(StatType stat) => _baseValues[(int)stat];

        // ------------------------------------------------------------ leitura

        /// <summary>
        /// Valor efetivo, com todos os modificadores aplicados.
        /// Barato de chamar em laco: so recalcula quando algo mudou.
        /// </summary>
        public float Get(StatType stat)
        {
            int i = (int)stat;

            if (_dirty[i])
            {
                _cached[i] = Recalculate(stat);
                _dirty[i] = false;
            }

            return _cached[i];
        }

        public int GetInt(StatType stat) => (int)Get(stat);

        // -------------------------------------------------------- modificadores

        public void AddModifier(in StatModifier modifier)
        {
            _modifiers.Add(modifier);
            Invalidate(modifier.Stat);
        }

        /// <summary>Remove uma ocorrencia exata. Para desfazer um efeito inteiro, prefira <see cref="RemoveAllFromSource"/>.</summary>
        public bool RemoveModifier(in StatModifier modifier)
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier m = _modifiers[i];

                if (m.Stat != modifier.Stat || m.Op != modifier.Op) continue;
                if (!ReferenceEquals(m.Source, modifier.Source)) continue;
                if (m.Value != modifier.Value) continue;

                _modifiers.RemoveAt(i);
                Invalidate(modifier.Stat);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove tudo que veio de uma origem. E o que uma pocao chama ao vencer, e o que
        /// um item chama ao ser desequipado.
        /// </summary>
        /// <returns>Quantos modificadores foram removidos.</returns>
        public int RemoveAllFromSource(object source)
        {
            if (source == null) return 0;

            int removed = 0;

            // De tras para frente para poder remover durante a varredura.
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_modifiers[i].Source, source)) continue;

                Invalidate(_modifiers[i].Stat);
                _modifiers.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        public bool HasSource(object source)
        {
            if (source == null) return false;

            for (int i = 0; i < _modifiers.Count; i++)
                if (ReferenceEquals(_modifiers[i].Source, source))
                    return true;

            return false;
        }

        public int ModifierCount => _modifiers.Count;

        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;

            _modifiers.Clear();
            MarkAllDirty();
        }

        // ------------------------------------------------------------- interno

        /// <summary>
        /// A formula, do docs/07 secao 4.2:
        /// <c>(base + soma dos Flat) * (1 + soma dos PercentAdd) * produto dos PercentMult</c>
        ///
        /// Somar os PercentAdd antes de multiplicar e o que torna o resultado independente
        /// da ordem de chegada. Duas pocoes de mais 30 por cento dao mais 60, nao mais 69.
        /// </summary>
        float Recalculate(StatType stat)
        {
            float flat = _baseValues[(int)stat];
            float percentAdd = 0f;
            float percentMult = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier m = _modifiers[i];
                if (m.Stat != stat) continue;

                switch (m.Op)
                {
                    case ModifierOp.Flat:
                        flat += m.Value;
                        break;
                    case ModifierOp.PercentAdd:
                        percentAdd += m.Value;
                        break;
                    case ModifierOp.PercentMult:
                        percentMult *= m.Value;
                        break;
                }
            }

            return flat * (1f + percentAdd) * percentMult;
        }

        void Invalidate(StatType stat)
        {
            _dirty[(int)stat] = true;
            StatChanged?.Invoke(stat);
        }

        void MarkAllDirty()
        {
            for (int i = 0; i < _dirty.Length; i++)
                _dirty[i] = true;
        }

        static int ComputeCapacity()
        {
            var values = (StatType[])Enum.GetValues(typeof(StatType));
            int max = 0;

            for (int i = 0; i < values.Length; i++)
                if ((int)values[i] > max)
                    max = (int)values[i];

            return max + 1;
        }
    }
}
