namespace SousLaVille.Core
{
    /// <summary>
    /// Les dix Sorting Layers du jeu, une famille par couche, du plus lointain au plus proche.
    ///
    /// Source unique : le runtime et les generateurs de scenes lisent ces constantes plutot
    /// que de recopier les noms. Unity retombe silencieusement sur Default pour un nom
    /// inconnu, et sous le Renderer2D un objet laisse sur Default n'est eclaire par aucune
    /// des deux lumieres globales : il apparait noir sans le moindre message d'erreur.
    /// </summary>
    public static class GameSortingLayers
    {
        public const string SurfaceGround = "Surface_Ground";
        public const string SurfacePipes = "Surface_Pipes";
        public const string SurfaceWater = "Surface_Water";
        public const string SurfaceEntities = "Surface_Entities";
        public const string SurfaceOverlay = "Surface_Overlay";

        public const string UndergroundGround = "Underground_Ground";
        public const string UndergroundPipes = "Underground_Pipes";
        public const string UndergroundWater = "Underground_Water";
        public const string UndergroundEntities = "Underground_Entities";
        public const string UndergroundOverlay = "Underground_Overlay";

        /// <summary>La famille de la surface, dans l'ordre de profondeur.</summary>
        public static readonly string[] Surface =
        {
            SurfaceGround,   // herbe, chemins, dalles
            SurfacePipes,    // raccordements visibles en surface
            SurfaceWater,    // flaques, fontaine
            SurfaceEntities, // joueur, maisons, batiments
            SurfaceOverlay   // pictogrammes et retours visuels
        };

        /// <summary>La famille du sous-sol, dans l'ordre de profondeur.</summary>
        public static readonly string[] Underground =
        {
            UndergroundGround,   // terre, roche
            UndergroundPipes,    // canalisations posees
            UndergroundWater,    // particules d'eau, geysers
            UndergroundEntities, // joueur, echelles, station
            UndergroundOverlay   // pictogrammes et retours visuels
        };

        /// <summary>Layer des entites de la couche demandee : c'est la que rend le joueur.</summary>
        public static string EntitiesFor(GameLayer layer)
        {
            return layer == GameLayer.Underground ? UndergroundEntities : SurfaceEntities;
        }

        /// <summary>Layer du decor de la couche demandee.</summary>
        public static string GroundFor(GameLayer layer)
        {
            return layer == GameLayer.Underground ? UndergroundGround : SurfaceGround;
        }
    }
}
