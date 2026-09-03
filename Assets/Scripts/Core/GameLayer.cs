namespace SousLaVille.Core
{
    /// <summary>
    /// Les trois couches de jeu. Le village en surface, le reseau en dessous, et les
    /// interieurs des batiments.
    /// </summary>
    /// <remarks>
    /// Interior est ajoute A LA FIN, jamais insere : ManholePortal.destinationLayer est un
    /// enum serialise dans les scenes par son rang, et inserer decalerait les huit portails
    /// poses en phase 2. Meme regle que NodeType.ReserveInlet en phase 8.
    ///
    /// La scene Interiors est une CINQUIEME scene, ecart explicite aux quatre scenes de
    /// CLAUDE.md, accepte le 3 septembre 2026. Voir PLAN-PHASE-09.md.
    /// </remarks>
    public enum GameLayer
    {
        Surface,
        Underground,
        Interior
    }
}
