using UnityEngine;

namespace LoboBranco.Core
{
    /// <summary>
    /// Nomes e mascaras das layers do projeto, em um lugar so.
    /// Strings de layer espalhadas pelo codigo sao uma fonte silenciosa de bug:
    /// um erro de digitacao devolve -1 e a query simplesmente nao acerta nada.
    /// Ver docs/11_SETUP_AMBIENTE.md secao 4.4.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string EnemyHitbox = "EnemyHitbox";
        public const string PlayerHitbox = "PlayerHitbox";
        public const string Environment = "Environment";
        public const string Interactable = "Interactable";
        public const string Clue = "Clue";
        public const string NPC = "NPC";
        public const string Projectile = "Projectile";
        public const string IgnoreCamera = "IgnoreCamera";
        public const string Water = "Water";

        /// <summary>Superficies em que o jogador e os inimigos andam.</summary>
        public static LayerMask Walkable => LayerMask.GetMask(Environment);

        /// <summary>O que a camera considera obstaculo ao se aproximar.</summary>
        public static LayerMask CameraBlockers => LayerMask.GetMask(Environment);

        /// <summary>Alvos validos de um golpe do jogador.</summary>
        public static LayerMask PlayerAttackTargets => LayerMask.GetMask(Enemy);

        /// <summary>Alvos validos de um golpe de inimigo.</summary>
        public static LayerMask EnemyAttackTargets => LayerMask.GetMask(Player);

        /// <summary>O que os Sentidos de Bruxo destacam.</summary>
        public static LayerMask SenseTargets => LayerMask.GetMask(Clue, Interactable);

        /// <summary>Layer numerica, ou -1 se a layer nao existir no projeto.</summary>
        public static int IndexOf(string layerName) => LayerMask.NameToLayer(layerName);
    }
}
