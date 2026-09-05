namespace SousLaVille.Minigames
{
    /// <summary>
    /// Les trois mini-jeux de l'usine a panneaux, un par personnage. C'est ce qu'un Villager
    /// serialise pour dire lequel Espace lance apres ses phrases.
    ///
    /// Un rang d'enum SERIALISE SE LIT PAR SA VALEUR : tout ajout se fait A LA FIN, jamais
    /// par insertion, comme GameLayer.Interior en phase 9a et NodeType.ReserveInlet en 8.
    /// La Fabrique et Le Plan sont donc deja la, sans ecran derriere eux : MiniGameScreen.Find
    /// rend null tant que personne ne les porte, et le personnage se contente de parler.
    /// </summary>
    public enum MiniGameKind
    {
        None = 0,
        Stock = 1,
        Fabrique = 2,
        Plan = 3
    }
}
