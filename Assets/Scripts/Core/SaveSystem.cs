using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using SousLaVille.Buildings;
using SousLaVille.Network;
using SousLaVille.Seasons;
using SousLaVille.World;
using UnityEngine;

namespace SousLaVille.Core
{
    /// <summary>
    /// La sauvegarde. Victorien ne doit jamais y penser : il n'y a ni bouton, ni menu, ni
    /// temoin qui clignote. Un temoin dirait qu'il existe un risque de perdre quelque chose,
    /// et toute la promesse de CLAUDE.md est qu'il n'y en a pas.
    ///
    /// Vit dans Persistent, sur le GameManager, a cote du solveur, de l'horloge et des saisons.
    ///
    /// Le fichier contient des gestes, pas un etat : la liste des cases creusees et des cases
    /// posees. Dig et PlacePipe etant les seules mutations du monde, les rejouer reconstitue
    /// exactement la partie, et tout etat charge reste un etat atteignable en jouant.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public const string FileName = "partie.json";

        /// <summary>Au plus une ecriture toutes les deux secondes. Au pire, deux secondes perdues.</summary>
        private const float FlushInterval = 2f;

        [Tooltip("L'horloge, pour l'avancement dans la saison. Cablee par PersistentSceneBuilder.")]
        [SerializeField] private GameClock clock;

        [Tooltip("Les saisons, pour le rang dans le cycle.")]
        [SerializeField] private SeasonSystem seasons;

        private PipeNetwork network;
        private UndergroundMap map;
        private ManholeFactory factory;
        private WaterReserve reserve;

        private bool loaded;
        private bool restoring;
        private bool dirty;
        private float nextFlush;

        /// <summary>Chemin complet du fichier de partie.</summary>
        public string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave => File.Exists(FilePath);

        /// <summary>Nombre d'ecritures depuis le demarrage. Sert aux verifications.</summary>
        public int SaveCount { get; private set; }

        /// <summary>Vrai une fois la partie chargee, ou une fois constate qu'il n'y en a pas.</summary>
        public bool IsLoaded => loaded;

        private void Awake()
        {
            if (clock == null)
            {
                clock = GetComponent<GameClock>();
            }

            if (seasons == null)
            {
                seasons = GetComponent<SeasonSystem>();
            }
        }

        /// <summary>
        /// Resolution paresseuse, comme le solveur depuis la phase 4 : Persistent est chargee
        /// AVANT l'Underground. On retente tant que la carte et le reseau ne repondent pas,
        /// puis on charge une fois, et plus jamais.
        /// </summary>
        private void Update()
        {
            if (!loaded)
            {
                TryLoad();
                return;
            }

            if (dirty && Time.unscaledTime >= nextFlush)
            {
                Flush();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        // La fermeture du jeu est la derniere occasion d'ecrire : on ne passe pas par
        // l'anti-rebond.
        private void OnApplicationQuit()
        {
            if (dirty)
            {
                Flush();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && dirty)
            {
                Flush();
            }
        }

        private void TryLoad()
        {
            if (map == null)
            {
                map = FindAnyObjectByType<UndergroundMap>(FindObjectsInactive.Include);
            }

            if (network == null)
            {
                network = FindAnyObjectByType<PipeNetwork>(FindObjectsInactive.Include);
            }

            // PHASE 9A : l'atelier a demenage de la scene Surface a la scene Interiors, que
            // LoadGameplayScenesAsync charge APRES l'Underground. Le raisonnement d'avant,
            // « si le reseau repond, l'atelier existe deja », etait vrai et ne l'est plus :
            // il laissait factory a null, donc aucune plaque sauvegardee ni relue, en
            // silence. Une garde qui repose sur l'ordre de chargement est une garde qui
            // ment des qu'une scene change de rang. On retente donc, comme pour les autres.
            if (factory == null)
            {
                factory = FindAnyObjectByType<ManholeFactory>(FindObjectsInactive.Include);
            }

            if (map == null || network == null || factory == null)
            {
                return;
            }

            // Le bassin vit dans la meme scene que le reseau : s'il repond, le bassin est la.
            reserve = FindAnyObjectByType<WaterReserve>(FindObjectsInactive.Include);

            // Marque pose avant la lecture : rien ne doit s'ecrire tant que la partie n'est
            // pas chargee, sinon un monde vide ecraserait un bon fichier.
            loaded = true;

            Subscribe();
            LoadFromDisk();
        }

        private void Subscribe()
        {
            network.Changed += MarkDirty;
            map.Dug += OnDug;

            if (seasons != null)
            {
                seasons.SeasonChanged += OnSeasonChanged;
            }

            if (factory != null)
            {
                factory.Changed += MarkDirty;
            }

            if (reserve != null)
            {
                reserve.Changed += MarkDirty;
            }
        }

        private void Unsubscribe()
        {
            if (network != null)
            {
                network.Changed -= MarkDirty;
            }

            if (map != null)
            {
                map.Dug -= OnDug;
            }

            if (seasons != null)
            {
                seasons.SeasonChanged -= OnSeasonChanged;
            }

            if (factory != null)
            {
                factory.Changed -= MarkDirty;
            }

            if (reserve != null)
            {
                reserve.Changed -= MarkDirty;
            }
        }

        private void OnDug(Vector2Int cell)
        {
            MarkDirty();
        }

        private void OnSeasonChanged(SeasonDefinition season)
        {
            MarkDirty();
        }

        /// <summary>Un geste de plus a sauver. L'ecriture, elle, attend l'anti-rebond.</summary>
        private void MarkDirty()
        {
            if (!loaded || restoring)
            {
                return;
            }

            dirty = true;
        }

        // ---------------------------------------------------------------- lecture

        private void LoadFromDisk()
        {
            if (!File.Exists(FilePath))
            {
                // Partie neuve. Aucun message : Victorien n'a pas a savoir qu'un fichier existe.
                return;
            }

            SaveData data;

            try
            {
                data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception error)
            {
                SetAside("illisible", error.Message);
                return;
            }

            if (data == null)
            {
                SetAside("vide", "le fichier ne contient aucune partie");
                return;
            }

            if (data.version != SaveData.CurrentVersion)
            {
                SetAside("d'une version inconnue",
                    $"version {data.version}, attendue {SaveData.CurrentVersion}");
                return;
            }

            try
            {
                Restore(data);
            }
            catch (Exception error)
            {
                // Le monde est peut-etre a moitie reconstruit, mais le fichier, lui, est
                // conserve : rien n'est perdu pour de bon.
                SetAside("interrompue en cours de lecture", error.Message);
            }
        }

        /// <summary>
        /// L'ordre compte. Sans terrain ouvert, PlacePipe refuse ; sans tuyaux poses, il n'y a
        /// aucun segment a abimer.
        /// </summary>
        private void Restore(SaveData data)
        {
            restoring = true;

            // Reposer cinquante tuyaux ne doit lever Changed qu'une fois, donc ne declencher
            // qu'une resolution au lieu de cinquante.
            network.BeginBatch();

            try
            {
                foreach (SaveCell cell in data.dugCells)
                {
                    map.Dig(cell.ToCell());
                }

                foreach (SaveCell cell in data.pipeCells)
                {
                    network.PlacePipe(cell.ToCell());
                }

                foreach (SaveSegment saved in data.segments)
                {
                    if (saved == null || saved.a == null || saved.b == null)
                    {
                        continue;
                    }

                    PipeSegment segment = network.SegmentBetween(saved.a.ToCell(), saved.b.ToCell());
                    if (segment == null)
                    {
                        // Le plan du monde a change depuis l'ecriture : on saute, sans bruit.
                        continue;
                    }

                    segment.Condition = saved.condition;
                    segment.IsFrozen = saved.isFrozen;
                    segment.IsClogged = saved.isClogged;
                }

                if (factory != null)
                {
                    var assignments = new Dictionary<Vector2Int, int>();

                    foreach (SaveCover saved in data.covers)
                    {
                        if (saved != null && saved.cell != null)
                        {
                            assignments[saved.cell.ToCell()] = saved.cover;
                        }
                    }

                    factory.Restore(assignments);
                }

                // Un fichier d'avant la phase 8 n'a pas le champ : Newtonsoft laisse zero,
                // et le bassin repart vide. Aucune erreur, aucune version a monter.
                if (reserve != null)
                {
                    reserve.Restore(data.reserveLevel);
                }

                if (seasons != null)
                {
                    seasons.Restore(data.seasonIndex);
                }

                if (clock != null)
                {
                    clock.SeasonProgress = data.seasonProgress;
                }
            }
            finally
            {
                // EndBatch leve Changed, et le solveur resout. Le drapeau tombe apres, sinon
                // cette unique notification declencherait aussitot une reecriture inutile.
                network.EndBatch();
                restoring = false;
            }
        }

        /// <summary>
        /// Un fichier qu'on n'a pas su lire n'est jamais ecrase. On le met de cote, on demarre
        /// une partie neuve, et on ne dit rien a l'ecran : un avertissement en console suffit,
        /// il est pour moi et pas pour le joueur.
        /// </summary>
        private void SetAside(string reason, string detail)
        {
            string stamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture);
            string target = Path.Combine(Application.persistentDataPath,
                $"partie-illisible-{stamp}.json");

            try
            {
                File.Move(FilePath, target);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Sous la Ville] Sauvegarde {reason} ({detail}), et impossible " +
                                 $"de la mettre de côté : {error.Message}");
                return;
            }

            Debug.LogWarning($"[Sous la Ville] Sauvegarde {reason} ({detail}). Mise de côté : {target}");
        }

        // ---------------------------------------------------------------- ecriture

        /// <summary>
        /// Ecrit la partie maintenant, sans passer par l'anti-rebond. C'est Update qui decide
        /// du moment ; cette methode ne fait qu'ecrire.
        /// </summary>
        public void Flush()
        {
            if (!loaded || restoring || network == null || map == null)
            {
                return;
            }

            string json = JsonConvert.SerializeObject(Capture(), Formatting.Indented);
            string temporary = FilePath + ".tmp";

            try
            {
                // Ecriture atomique : une coupure de courant en pleine ecriture ne doit jamais
                // laisser un fichier tronque a la place d'une bonne partie.
                File.WriteAllText(temporary, json);

                if (File.Exists(FilePath))
                {
                    File.Replace(temporary, FilePath, null);
                }
                else
                {
                    File.Move(temporary, FilePath);
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Sous la Ville] Sauvegarde impossible : {error.Message}");
                return;
            }

            dirty = false;
            nextFlush = Time.unscaledTime + FlushInterval;
            SaveCount++;
        }

        private SaveData Capture()
        {
            SaveData data = new SaveData();

            data.seasonIndex = seasons != null ? seasons.CurrentIndex : 0;
            data.seasonProgress = clock != null ? clock.SeasonProgress : 0f;
            data.reserveLevel = reserve != null ? reserve.Level : 0;

            foreach (Vector2Int cell in map.DugCells)
            {
                data.dugCells.Add(new SaveCell(cell));
            }

            foreach (PipeNode node in network.Nodes)
            {
                // La scene recree la station et les maisons. Les figer dans le fichier
                // gelerait le plan du monde dans les parties de Victorien.
                if (node.IsPermanent)
                {
                    continue;
                }

                data.pipeCells.Add(new SaveCell(node.GridPos));
            }

            foreach (PipeSegment segment in network.Segments)
            {
                if (segment.Condition >= 1f && !segment.IsFrozen && !segment.IsClogged)
                {
                    // Reposer un tuyau le recree neuf : ecrire un segment intact ne changerait
                    // rien au chargement.
                    continue;
                }

                data.segments.Add(new SaveSegment
                {
                    a = new SaveCell(segment.NodeA.GridPos),
                    b = new SaveCell(segment.NodeB.GridPos),
                    condition = segment.Condition,
                    isFrozen = segment.IsFrozen,
                    isClogged = segment.IsClogged
                });
            }

            if (factory != null)
            {
                foreach (KeyValuePair<Vector2Int, int> entry in factory.Assignments)
                {
                    data.covers.Add(new SaveCover
                    {
                        cell = new SaveCell(entry.Key),
                        cover = entry.Value
                    });
                }
            }

            return data;
        }
    }
}
