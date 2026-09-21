using System;
using LoboBranco.Combat;
using Unity.Netcode;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade dividida (ADR 0008, tech/adr/0011): o dono pede, o host decide. O dono sabe
    /// quando a conjuracao comeca e quando o efeito sai, porque e a maquina de estados dele
    /// que conta o tempo. O host confere recarga e vigor, cobra, arma a recarga, e e o unico
    /// lugar onde o efeito acontece.
    ///
    /// Tres pedidos por habilidade, como no golpe: comecou, efeito, cortou. O efeito vai como
    /// pedido do dono, e nao como conta do host a partir do asset, por causa da esquiva. Se o
    /// host contasse o tempo sozinho, uma esquiva no ultimo centesimo antes do efeito chegaria
    /// depois da conta dele, e o sinal sairia no host com o bruxo ja rolando na tela do dono.
    /// Com o efeito pedido, as duas mensagens chegam na ordem em que o dono as mandou.
    ///
    /// <b>A recarga viaja como instante, e nao como contagem.</b> O host arma e manda um
    /// numero no relogio do servidor; cada maquina calcula quanto falta sozinha. Nenhuma
    /// mensagem por quadro, e o painel do dono conta para baixo junto com o do host.
    ///
    /// <b>O host pode recusar, e avisa o dono.</b> Para o vigor do golpe, o host cobra o que
    /// tem e nunca recusa (CharacterVitals). Aqui a regra e outra, porque sinal de graca cria
    /// janela de graca, e esse e o recurso mais caro do combate. A recusa quase nunca
    /// acontece: o dono ja conferiu com a mesma regra, contra a copia replicada, e durante a
    /// meia ida e volta o vigor do host so sobe e a recarga so anda. Quando acontece, o dono
    /// corta a conjuracao e o efeito nao sai.
    ///
    /// As habilidades sao da escola. Este componente nao tem nenhuma e nao tem numero nenhum.
    ///
    /// Sem rede, o mesmo objeto e dono e host, e o relogio e o local (risco X8 do doc 13).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterVitals))]
    public sealed class PlayerAbilityCaster : NetworkBehaviour, IAbilityCaster
    {
        /// <summary>
        /// Folga que o host concede a recarga de um pedido vindo pela rede. O relogio do
        /// servidor no cliente e uma estimativa, e ela oscila alguns centesimos; sem folga, um
        /// sinal apertado no quadro exato em que a recarga volta seria recusado de vez em
        /// quando, e o sintoma seria "as vezes o Q nao sai". Nao e balanceamento: ninguem mexe
        /// nisto para mudar o jogo.
        /// </summary>
        const float NetworkCooldownTolerance = 0.1f;

        /// <summary>Vaga que nao existe. Vai no lugar de -1 pela rede.</summary>
        const byte NoSlot = byte.MaxValue;

        // Server como permissao de escrita faz o proprio NGO recusar recarga escrita por
        // cliente, pelo mesmo motivo da vida em CharacterVitals.
        readonly NetworkVariable<AbilityReadyTimes> _replicatedReadyAt = new NetworkVariable<AbilityReadyTimes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // No host e sem rede, a verdade. No cliente, a copia replicada, mais a previsao do dono
        // entre pedir e a resposta chegar.
        readonly AbilityCooldowns _cooldowns = new AbilityCooldowns(AbilityReadyTimes.Capacity);

        PlayerSchool _school;
        CharacterVitals _vitals;

        // Estado do lado de quem resolve: a vaga aceita que ainda nao disparou o efeito.
        int _resolvingSlot = PlayerStateContext.NoAbilitySlot;

        // ---------------------------------------------------------------- eventos

        /// <summary>
        /// O efeito saiu: vaga e habilidade. Dispara so em quem resolve, uma vez por conjuracao
        /// aceita. E onde os sinais da tarefa 1.18 vao ser aplicados.
        /// </summary>
        public event Action<int, AbilityDef> CastTriggered;

        /// <summary>O host recusou a conjuracao em andamento. Dispara so no dono.</summary>
        public event Action<AbilityRefusal> CastRefused;

        // ---------------------------------------------------------------- leitura

        /// <summary>Quem pode pedir: o dono, ou eu mesmo sem rede.</summary>
        public bool CanRequest => !IsSpawned || IsOwner;

        /// <summary>Quem cobra e arma: o host, ou eu mesmo sem rede.</summary>
        public bool CanResolve => !IsSpawned || IsServer;

        /// <summary>Com rede, o relogio do servidor, que no host e o dele mesmo. Sem rede, o local.</summary>
        double Now => IsSpawned ? NetworkManager.ServerTime.Time : Time.timeAsDouble;

        // Achado na primeira vez que precisa, como no atacante: o host resolve a vaga do
        // companheiro, e a pergunta chega pela rede.
        PlayerSchool School => _school != null ? _school : (_school = GetComponent<PlayerSchool>());

        public int AbilityCount => School != null ? School.AbilityCount : 0;

        /// <summary>Segundos ate a vaga voltar, no relogio desta maquina.</summary>
        public float CooldownRemaining(int slot) => _cooldowns.Remaining(slot, Now);

        /// <summary>A ultima recusa do host. O painel de debug mostra isto.</summary>
        public AbilityRefusal LastRefusal { get; private set; }

        /// <summary>A ultima habilidade cujo efeito saiu. So vale em quem resolve.</summary>
        public AbilityDef LastTriggered { get; private set; }

        // ---------------------------------------------------------- IAbilityCaster

        public AbilityDef AbilityAt(int slot) => School != null ? School.AbilityAt(slot) : null;

        /// <summary>A pergunta do dono, antes de pedir. Mesma regra que o host usa para decidir.</summary>
        public AbilityRefusal CanCast(int slot) => AbilityRules.Check(AbilityAt(slot), CooldownRemaining(slot), _vitals);

        public void BeginCast(int slot)
        {
            if (!CanRequest) return;

            if (CanResolve)
            {
                ResolveBegin(slot, cooldownTolerance: 0f);
                return;
            }

            // A previsao do dono. Sem ela, o painel mostraria a vaga pronta durante a ida e
            // volta, e saltaria para quatro segundos quando a resposta chegasse. A copia
            // replicada sobrescreve isto quando chega, e a recusa tambem.
            AbilityDef ability = AbilityAt(slot);
            if (ability != null)
                _cooldowns.Start(slot, Now, ability.cooldownSeconds);

            BeginCastRpc(ToWire(slot));
        }

        public void TriggerCast()
        {
            if (!CanRequest) return;

            if (CanResolve) ResolveTrigger();
            else TriggerCastRpc();
        }

        public void CancelCast()
        {
            if (!CanRequest) return;

            if (CanResolve) ResolveCancel();
            else CancelCastRpc();
        }

        // ------------------------------------------------------------------ ciclo

        void Awake()
        {
            _vitals = GetComponent<CharacterVitals>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // O relogio de referencia deixa de ser o local e passa a ser o do servidor.
                // O que falta de cada recarga continua faltando.
                _cooldowns.Rebase(NetworkManager.ServerTime.Time - Time.timeAsDouble);
                _replicatedReadyAt.Value = AbilityReadyTimes.From(_cooldowns);
                return;
            }

            _replicatedReadyAt.OnValueChanged += OnReplicatedReadyAtChanged;
            _replicatedReadyAt.Value.CopyTo(_cooldowns);
        }

        public override void OnNetworkDespawn()
        {
            _replicatedReadyAt.OnValueChanged -= OnReplicatedReadyAtChanged;
        }

        // ------------------------------------------------------------------- rede

        // InvokePermission.Owner faz o NGO recusar pedido que nao venha do dono. Sem isso, um
        // participante poderia gastar o vigor do personagem dos outros.

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void BeginCastRpc(byte slot) => ResolveBegin(FromWire(slot), NetworkCooldownTolerance);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void TriggerCastRpc() => ResolveTrigger();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void CancelCastRpc() => ResolveCancel();

        // Server como permissao: so o host recusa. Sem isso, um participante poderia cortar a
        // conjuracao de outro quando quisesse.
        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void CastRefusedRpc(byte reason) => Refused((AbilityRefusal)reason);

        static byte ToWire(int slot) => slot >= 0 && slot < AbilityReadyTimes.Capacity ? (byte)slot : NoSlot;

        static int FromWire(byte slot) => slot == NoSlot ? PlayerStateContext.NoAbilitySlot : slot;

        // -------------------------------------------------------------- resolucao

        /// <summary>
        /// Cobrar e armar no inicio, e nao no efeito, e de proposito: e o momento em que o
        /// bruxo se compromete. Cobrar no efeito deixaria a esquiva cortar a conjuracao de
        /// graca, e todo sinal viraria uma finta sem custo.
        /// </summary>
        void ResolveBegin(int slot, float cooldownTolerance)
        {
            double now = Now;
            AbilityDef ability = AbilityAt(slot);
            AbilityRefusal refusal = AbilityRules.Check(ability, _cooldowns.Remaining(slot, now), _vitals, cooldownTolerance);

            if (refusal != AbilityRefusal.None)
            {
                _resolvingSlot = PlayerStateContext.NoAbilitySlot;

                if (!IsSpawned || IsOwner) Refused(refusal);
                else CastRefusedRpc((byte)refusal);

                return;
            }

            _resolvingSlot = slot;

            _vitals.TrySpendStamina(ability.staminaCost);
            _cooldowns.Start(slot, now, ability.cooldownSeconds);

            if (IsSpawned)
                _replicatedReadyAt.Value = AbilityReadyTimes.From(_cooldowns);
        }

        void ResolveTrigger()
        {
            // Efeito sem conjuracao aceita: a recusa ainda nao tinha chegado ao dono quando o
            // tempo de conjurar dele acabou. O host ja decidiu que este sinal nao existe.
            if (_resolvingSlot == PlayerStateContext.NoAbilitySlot) return;

            int slot = _resolvingSlot;
            _resolvingSlot = PlayerStateContext.NoAbilitySlot;

            LastTriggered = AbilityAt(slot);
            CastTriggered?.Invoke(slot, LastTriggered);
        }

        void ResolveCancel() => _resolvingSlot = PlayerStateContext.NoAbilitySlot;

        // ---------------------------------------------------------------- interno

        void OnReplicatedReadyAtChanged(AbilityReadyTimes previous, AbilityReadyTimes current)
            => current.CopyTo(_cooldowns);

        void Refused(AbilityRefusal refusal)
        {
            LastRefusal = refusal;

            // A previsao do dono estava errada. A copia replicada volta a mandar.
            if (!CanResolve)
                _replicatedReadyAt.Value.CopyTo(_cooldowns);

            CastRefused?.Invoke(refusal);
        }
    }
}
