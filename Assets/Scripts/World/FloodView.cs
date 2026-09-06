using SousLaVille.Buildings;
using SousLaVille.Core;
using SousLaVille.Network;
using SousLaVille.Seasons;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SousLaVille.World
{
    /// <summary>
    /// L'eau dans le village. Elle vient de deux endroits depuis la phase 22.
    ///
    /// LE DEBORDEMENT A ETE SUPPRIME. Il sortait des bouches d'egout et montrait
    /// SeasonSystem.LastBudget.Lost, le surplus que rien n'a retenu. Deux choses l'ont
    /// emporte, constatees le 6 septembre 2026 :
    ///
    /// 1. Il ne se recalculait qu'au tick de saison. Une nappe apparue a l'automne restait
    ///    dix minutes durant, quoi que le joueur repare. « J'ai reparé et la flaque reste. »
    /// 2. Il portait LA MEME IMAGE que la fuite. Le joueur ne pouvait pas distinguer « un
    ///    tuyau est creve ici » de « la station recoit plus qu'elle ne traite », et lisait
    ///    donc les deux comme la premiere.
    ///
    /// Le recalculer en continu aurait ete pire : reparer un tuyau RECONNECTE des maisons,
    /// donc AUGMENTE l'arrivant, donc le debordement. Le geste juste aurait fait grandir la
    /// flaque.
    ///
    /// CE QU'ON PERD, ET C'EST ASSUME : la lecon du bassin de la phase 8 n'a plus de signe
    /// visible. Le bilan de l'eau continue de tourner, le bassin continue d'encaisser ; rien
    /// ne le montre plus dans la rue.
    ///
    /// LA FUITE est une flaque posee dans la rue juste au-dessus d'un tuyau use sous le seuil.
    /// Elle dit « il y a un tuyau creve ici, sous tes pieds », et elle epargne une descente.
    /// Seule l'usure fuit : un tuyau gele ou bouche est BOUCHE, pas creve, et rien n'en sort.
    /// C'est cette distinction qui rend la flaque informative.
    ///
    /// Une seule image d'eau pour les deux : une flaque est une flaque, et c'est un symbole de
    /// moins a apprendre. C'est la position qui raconte l'histoire, une nappe autour d'une
    /// bouche ou une flaque isolee au milieu d'une rue.
    ///
    /// LE JET DE LA FONTAINE, depuis la phase 11, mouille les quatre cases autour du bassin
    /// quand le solveur lui a trouve une route. Il dit « celle-la marche ». Le bassin lui-meme
    /// bloque le passage, donc il ne prend pas l'eau : c'est autour de lui qu'elle deborde.
    ///
    /// RIEN NE BLOQUE ET RIEN NE PUNIT. L'eau se peint sur Surface_Water, jamais sur la couche
    /// bloquante : le personnage la traverse. CLAUDE.md, « un reseau qui deborde est un
    /// spectacle rigolo ».
    ///
    /// Il vit dans la scene Surface, comme SeasonAmbience : il peint le village. Il s'abonne
    /// dans OnEnable, se desabonne dans OnDisable et se repeint a chaque rallumage, le
    /// SceneRouter eteignant la racine de la couche inactive.
    /// </summary>
    public class FloodView : MonoBehaviour
    {
        [Tooltip("La carte du village : elle dit quelles cases peuvent prendre l'eau.")]
        [SerializeField] private SurfaceMap map;

        [Tooltip("La tilemap d'eau, sur Surface_Water. Repeinte en entier a chaque mise a jour.")]
        [SerializeField] private Tilemap water;

        [Tooltip("La tuile d'eau, semi-transparente : on voit le sol dessous.")]
        [SerializeField] private TileBase waterTile;

        private SeasonSystem seasons;
        private PipeNetwork network;
        private Fountain fountain;

        /// <summary>Cases mouillees au dernier passage. Sert aux verifications.</summary>
        public int FloodedCount { get; private set; }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (seasons != null)
            {
                seasons.SeasonChanged -= OnSeasonChanged;
                seasons = null;
            }
        }

        /// <summary>
        /// Resolution paresseuse : Persistent est chargee avant Surface. Tant que le systeme
        /// de saisons manque, on retente ; des qu'il est la, on s'abonne et Update n'a plus
        /// rien a faire. Meme remede que SeasonAmbience depuis la phase 5.
        /// </summary>
        private void Update()
        {
            if (seasons == null)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (seasons != null || GameManager.Instance == null)
            {
                return;
            }

            seasons = GameManager.Instance.Seasons;
            if (seasons == null)
            {
                return;
            }

            seasons.SeasonChanged += OnSeasonChanged;

            // Le bilan a pu changer, et un tuyau a pu ceder, pendant que la couche etait
            // eteinte : on se repeint des le rallumage plutot que d'attendre le tick.
            Redraw();
        }

        /// <summary>
        /// SeasonChanged est leve APRES ApplyWaterBudget, verifie dans SeasonSystem.Advance :
        /// LastBudget est donc deja a jour quand on arrive ici.
        /// </summary>
        private void OnSeasonChanged(SeasonDefinition season)
        {
            Redraw();
        }

        /// <summary>
        /// Repeint toute l'eau. Quelques dizaines de cases, et seulement aux ticks et aux
        /// rallumages : le meme choix que PipeNetworkView depuis la phase 3.
        ///
        /// Pas d'abonnement a PipeNetwork.Changed, et ce n'est pas un oubli : creuser, poser,
        /// enlever et reparer n'existent que SOUS TERRE, donc l'etat des tuyaux ne peut
        /// changer que pendant que la Surface est eteinte, ou a un tick. Le rallumage et le
        /// tick couvrent donc tous les cas, sans s'abonner a travers une couche eteinte.
        /// </summary>
        public void Redraw()
        {
            if (water == null || waterTile == null || map == null)
            {
                return;
            }

            water.ClearAllTiles();
            FloodedCount = 0;

            PaintLeaks();
            PaintFountain();
        }

        /// <summary>
        /// Les fuites. Une case du sous-sol dont un segment est tombe au seuil recoit une
        /// flaque sur LA MEME case du village : les deux cartes ont toujours la meme taille,
        /// 64x45 depuis la phase 12b, et partagent le meme repere depuis la phase 2, donc il
        /// n'y a aucune conversion a faire.
        ///
        /// La regle du « trop abime » vit dans PipeNetwork, et c'est la meme que celle qui
        /// peint le tuyau en rouge terne sous terre.
        /// </summary>
        private void PaintLeaks()
        {
            PipeNetwork pipes = ResolveNetwork();
            if (pipes == null)
            {
                return;
            }

            foreach (PipeNode node in pipes.Nodes)
            {
                if (pipes.IsWornOut(node.GridPos))
                {
                    Paint(node.GridPos);
                }
            }
        }

        /// <summary>
        /// Le jet de la fontaine. Elle est desservie exactement comme une maison, donc son
        /// eau s'arrete des qu'un tuyau de sa route gele, se bouche ou casse : c'est le seul
        /// retour qui dise « celle-la marche » sans un mot.
        ///
        /// Le bassin bloque, donc sa propre case ne prend pas l'eau. Ce sont ses quatre
        /// voisines qui la recoivent, comme un bassin qui deborde.
        /// </summary>
        private void PaintFountain()
        {
            Fountain basin = ResolveFountain();
            FlowSolver flow = GameManager.Instance != null ? GameManager.Instance.Flow : null;

            if (basin == null || flow == null || !flow.IsServed(basin.Cell))
            {
                return;
            }

            Paint(basin.Cell + Vector2Int.up);
            Paint(basin.Cell + Vector2Int.right);
            Paint(basin.Cell + Vector2Int.down);
            Paint(basin.Cell + Vector2Int.left);
        }

        /// <summary>La fontaine vit dans la meme scene que nous : elle est la quand nous le sommes.</summary>
        private Fountain ResolveFountain()
        {
            if (fountain == null)
            {
                fountain = FindAnyObjectByType<Fountain>(FindObjectsInactive.Include);
            }

            return fountain;
        }

        /// <summary>
        /// Mouille une case, si elle est praticable. L'eau ne monte pas sur les haies, les
        /// maisons ni les facades : une flaque sous un toit ne se verrait pas, et la carte de
        /// collision est la seule source de verite sur ce qui est franchissable.
        /// </summary>
        private void Paint(Vector2Int cell)
        {
            if (!map.IsWalkable(cell))
            {
                return;
            }

            Vector3Int position = new Vector3Int(cell.x, cell.y, 0);
            if (water.HasTile(position))
            {
                return;
            }

            water.SetTile(position, waterTile);
            FloodedCount++;
        }

        /// <summary>
        /// Le reseau vit dans la scene Underground, ETEINTE des qu'on est en surface. Il faut
        /// donc l'inclure explicitement dans la recherche, et le garder une fois trouve :
        /// c'est exactement le cas de l'usine a tuyaux depuis la phase 9b.
        /// </summary>
        private PipeNetwork ResolveNetwork()
        {
            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }

            return network;
        }
    }
}
