using LoboBranco.CameraSystem;
using LoboBranco.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace LoboBranco.Player
{
    /// <summary>
    /// Autoridade: nenhuma, e local por natureza. Tremor e soco de camera acontecem so na
    /// tela de quem bateu ou apanhou; o companheiro ao lado nao sente o seu golpe.
    ///
    /// E a metade de sensacao do docs/03 secao 11 que nao toca em simulacao. A outra metade,
    /// o hitstop, estende o golpe nas duas pontas da rede e mora na linha do tempo
    /// (tech/adr/0010). Esta aqui so reage.
    ///
    /// Reage a duas coisas. Ao acerto confirmado pelo host, que chega com o dano: o tremor
    /// e proporcional a ele, e o soco diz que conectou. E a vida do proprio bruxo caindo,
    /// que chega replicada: apanhar tambem treme, e nao empurra, porque o soco e a
    /// assinatura de quem acerta.
    ///
    /// Os dois eventos disparam em todas as maquinas que tem este personagem, e por isso
    /// cada reacao pergunta antes se o personagem e o desta tela. Sem essa pergunta, o golpe
    /// de um companheiro sacudiria a camera de todo mundo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [Header("Dados")]
        [Tooltip("Forca do tremor por dano e tamanho do soco. docs/03 secao 11.")]
        [SerializeField] HitFeedbackDef feedback;

        PlayerBrain _brain;
        PlayerMeleeAttacker _attacker;
        CharacterVitals _vitals;
        CinemachineImpulseSource _impulse;
        ThirdPersonCameraRig _rig;

        /// <summary>
        /// Verdadeiro no personagem que esta maquina controla. Sem cerebro ao lado vale
        /// verdadeiro, que e o caso da cena de teste sem rede.
        /// </summary>
        bool IsMine => _brain == null || _brain.DrivesThisCharacter;

        void Awake()
        {
            _brain = GetComponent<PlayerBrain>();
            _attacker = GetComponent<PlayerMeleeAttacker>();
            _vitals = GetComponent<CharacterVitals>();
            _impulse = GetComponent<CinemachineImpulseSource>();

            if (feedback == null)
            {
                Debug.LogWarning($"{name}: {nameof(PlayerHitFeedback)} sem {nameof(HitFeedbackDef)}. Golpes sem sensacao.", this);
                return;
            }

            // A duracao sai do asset agora, e nao do prefab: afinar o tremor tem que ser
            // editar o asset, e nao remontar o jogador.
            _impulse.ImpulseDefinition.ImpulseDuration = feedback.shakeDuration;
        }

        void OnEnable()
        {
            if (_attacker != null) _attacker.HitConfirmed += OnHitConfirmed;
            if (_vitals != null) _vitals.VitalityChanged += OnVitalityChanged;
        }

        void OnDisable()
        {
            if (_attacker != null) _attacker.HitConfirmed -= OnHitConfirmed;
            if (_vitals != null) _vitals.VitalityChanged -= OnVitalityChanged;
        }

        void OnHitConfirmed(float damage, float hitstopSeconds)
        {
            if (!IsMine || feedback == null) return;

            Shake(damage);

            ThirdPersonCameraRig rig = Rig;
            if (rig != null) rig.Punch(feedback.punchDegrees, feedback.punchRecovery);
        }

        void OnVitalityChanged(float before, float after)
        {
            // Subir vida nao treme: a vida inicial chega replicada no spawn, e o jogador
            // nasceria com a camera sacudindo.
            if (!IsMine || feedback == null || after >= before) return;

            Shake(before - after);
        }

        void Shake(float damage)
        {
            float force = feedback.ShakeFor(damage);
            if (force > 0f) _impulse.GenerateImpulseWithForce(force);
        }

        /// <summary>
        /// O pivo de camera e achado na primeira vez que precisa. Prefab de rede nao guarda
        /// referencia de cena, e o pivo so faz sentido no dono, entao procurar no Awake de
        /// cada companheiro seria procurar por nada.
        /// </summary>
        ThirdPersonCameraRig Rig
        {
            get
            {
                if (_rig == null) _rig = FindAnyObjectByType<ThirdPersonCameraRig>();
                return _rig;
            }
        }
    }
}
