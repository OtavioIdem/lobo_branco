namespace LoboBranco.Combat
{
    /// <summary>
    /// O que um estado de ataque precisa poder mandar em quem empunha a arma.
    ///
    /// Existe como interface por um motivo so, e ele e decisivo: com ela, os testes da
    /// maquina de estados verificam a linha do tempo de um golpe sem fisica, sem cena e
    /// sem Animator, em EditMode, em milissegundos. O estado nunca sabe se quem esta do
    /// outro lado consulta a fisica de verdade ou apenas conta chamadas.
    ///
    /// As quatro chamadas sao exatamente os dois eventos de animacao do docs/07 secao 4.5
    /// (<c>HitboxOn</c> e <c>HitboxOff</c>) mais o inicio e o cancelamento. Quando o
    /// Animator existir, o clipe chama <see cref="OpenHitbox"/> e <see cref="CloseHitbox"/>
    /// direto e o estado para de contar tempo, sem que esta interface mude.
    /// </summary>
    public interface IMeleeAttacker
    {
        /// <summary>Comeca um golpe novo. Limpa a lista de ja-atingidos.</summary>
        void BeginSwing(AttackDef attack);

        /// <summary>Abre a janela de dano.</summary>
        void OpenHitbox();

        /// <summary>Uma consulta da janela aberta. Chamada uma vez por passo de simulacao.</summary>
        void TickHitbox();

        /// <summary>Fecha a janela de dano.</summary>
        void CloseHitbox();

        /// <summary>Interrompe o golpe. Chamado quando a esquiva cancela um ataque.</summary>
        void CancelSwing();
    }
}
