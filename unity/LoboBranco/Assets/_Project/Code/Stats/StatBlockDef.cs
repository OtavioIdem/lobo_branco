using System;
using UnityEngine;

namespace LoboBranco.Stats
{
    /// <summary>
    /// Os valores base de uma criatura, em asset. E a aplicacao da regra 1 do CLAUDE.md:
    /// balancear vitalidade e dano de um inimigo tem que ser editar um asset, nao
    /// recompilar. Os numeros de referencia estao em docs/03 secao 12.
    /// </summary>
    [CreateAssetMenu(menuName = "LoboBranco/Stats/Stat Block", fileName = "StatBlock_")]
    public sealed class StatBlockDef : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatType stat;
            public float value;
        }

        [Tooltip("Atributos nao listados ficam em zero. Liste so o que a criatura usa.")]
        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        /// <summary>Cria uma folha nova com estes valores base. Cada criatura tem a sua.</summary>
        public StatSheet CreateSheet()
        {
            var sheet = new StatSheet();
            ApplyTo(sheet);
            return sheet;
        }

        /// <summary>
        /// Escreve os valores base em uma folha existente, sem tocar nos modificadores.
        /// Util para trocar o bloco base sem perder pocoes ativas.
        /// </summary>
        public void ApplyTo(StatSheet sheet)
        {
            if (sheet == null) return;

            for (int i = 0; i < entries.Length; i++)
                sheet.SetBase(entries[i].stat, entries[i].value);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Atributo duplicado nao quebra nada (o ultimo vence), mas quase sempre e
            // engano de quem editou, e o sintoma so aparece muito depois no balanceamento.
            for (int i = 0; i < entries.Length; i++)
            for (int j = i + 1; j < entries.Length; j++)
                if (entries[i].stat == entries[j].stat)
                    Debug.LogWarning($"{name}: '{entries[i].stat}' aparece mais de uma vez.", this);
        }
#endif
    }
}
