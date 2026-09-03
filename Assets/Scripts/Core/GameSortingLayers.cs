namespace SousLaVille.Core
{
    /// <summary>
    /// Les quinze Sorting Layers du jeu, une famille par couche, du plus lointain au plus
    /// proche.
    ///
    /// Source unique : le runtime et les generateurs de scenes lisent ces constantes plutot
    /// que de recopier les noms. Unity retombe silencieusement sur Default pour un nom
    /// inconnu, et sous le Renderer2D un objet laisse sur Default n'est eclaire par aucune
    /// des trois lumieres globales : il apparait noir sans le moindre message d'erreur.
    ///
    /// Les interieurs ont leur famille comme les deux autres couches, cinq layers et non
    /// trois : ce tableau reste ainsi une table sans exception, et sa lumiere globale ne
    /// chevauche celle d'aucune autre. Deux lumieres globales sur un meme layer font hurler
    /// URP a chaque chargement, piege rencontre en phase 0.
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

        public const string InteriorGround = "Interior_Ground";
        public const string InteriorPipes = "Interior_Pipes";
        public const string InteriorWater = "Interior_Water";
        public const string InteriorEntities = "Interior_Entities";
        public const string InteriorOverlay = "Interior_Overlay";

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

        /// <summary>La famille des interieurs, dans l'ordre de profondeur.</summary>
        public static readonly string[] Interior =
        {
            InteriorGround,   // sol et murs des pieces
            InteriorPipes,    // inutilise, garde pour que la famille reste une table
            InteriorWater,    // inutilise, meme raison
            InteriorEntities, // joueur, personnages, echantillons exposes
            InteriorOverlay   // pictogrammes et retours visuels
        };

        /// <summary>Les trois familles, dans l'ordre de l'enum GameLayer.</summary>
        public static readonly string[][] Families = { Surface, Underground, Interior };

        /// <summary>Layer des entites de la couche demandee : c'est la que rend le joueur.</summary>
        public static string EntitiesFor(GameLayer layer)
        {
            switch (layer)
            {
                case GameLayer.Underground: return UndergroundEntities;
                case GameLayer.Interior: return InteriorEntities;
                default: return SurfaceEntities;
            }
        }

        /// <summary>Layer du decor de la couche demandee.</summary>
        public static string GroundFor(GameLayer layer)
        {
            switch (layer)
            {
                case GameLayer.Underground: return UndergroundGround;
                case GameLayer.Interior: return InteriorGround;
                default: return SurfaceGround;
            }
        }
    }
}
