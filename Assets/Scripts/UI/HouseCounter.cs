using SousLaVille.Core;
using SousLaVille.Network;
using UnityEngine;
using UnityEngine.UI;

namespace SousLaVille.UI
{
    /// <summary>
    /// La rangee de gouttes du HUD : une par maison, remplie quand une maison de plus est
    /// desservie. C'est le seul but affiche du jeu, et il tient sans un mot.
    ///
    /// Un compteur, pas une liste : les N premieres gouttes se remplissent, l'ordre des
    /// maisons n'a pas a etre appris.
    /// </summary>
    public class HouseCounter : MonoBehaviour
    {
        [SerializeField] private Image[] drops;
        [SerializeField] private Sprite dropServed;
        [SerializeField] private Sprite dropIdle;

        private FlowSolver flow;

        // Start et non Awake : le GameManager s'enregistre dans son propre Awake, et le
        // solveur resout son reseau au Start.
        private void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.Flow == null)
            {
                Debug.LogError("[Sous la Ville] Compteur de maisons sans solveur.");
                return;
            }

            flow = GameManager.Instance.Flow;
            flow.Solved += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (flow != null)
            {
                flow.Solved -= Refresh;
            }
        }

        private void Refresh()
        {
            if (drops == null)
            {
                return;
            }

            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i] == null)
                {
                    continue;
                }

                drops[i].sprite = i < flow.ServedCount ? dropServed : dropIdle;
            }
        }
    }
}
