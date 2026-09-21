namespace LoboBranco.Player
{
    /// <summary>
    /// Os estados do jogador, na lista do docs/07 secao 4.4.
    ///
    /// Valores contiguos comecando em zero de proposito: a maquina indexa um vetor por
    /// este numero, o que torna a busca do estado uma leitura de vetor em vez de uma
    /// consulta de dicionario. Inserir um estado no meio renumera os de baixo, e nenhum
    /// asset serializa este enum, entao isso e seguro. Se um dia serializar, insira no fim.
    /// </summary>
    public enum PlayerStateId
    {
        Locomotion = 0,
        Attack = 1,
        Dodge = 2,
        Roll = 3,
        Parry = 4,
        Riposte = 5,
        CastSign = 6,
        UseItem = 7,
        Stagger = 8,
        Death = 9,
        Interact = 10,
        DialogueLocked = 11,

        /// <summary>Troca de aco para prata ou o contrario. 0,7 s e nem a esquiva corta (docs/03 secao 3).</summary>
        SwapWeapon = 12,
    }
}
