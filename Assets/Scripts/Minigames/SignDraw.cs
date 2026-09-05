using System.Collections.Generic;

namespace SousLaVille.Minigames
{
    /// <summary>
    /// Le tirage PAR FAMILLE, partage par les mini-jeux de l'usine a panneaux. Sorti de
    /// SignMemory.Deal en phase 15 pour servir aussi a La Fabrique : la planche est lue par
    /// tranches egales — 24 rangs, 4 familles de 6 — et chaque manche prend autant de rangs
    /// dans chacune.
    ///
    /// C'est la lecon de la piece qui l'exige : la FORME dit la famille avant que le dessin
    /// dise le detail. Un tirage global pourrait sortir six triangles rouges et la contredire.
    ///
    /// Pur et deterministe : aucun MonoBehaviour, aucune image. Il se verifie sur mille
    /// graines sans ecran.
    /// </summary>
    public static class SignDraw
    {
        /// <summary>
        /// count rangs distincts, count / families dans chaque famille, dans l'ordre des
        /// familles. Rend null si le partage est impossible : count non divisible par
        /// families, planche non partageable, ou plus de rangs demandes qu'une famille n'en a.
        /// </summary>
        public static int[] PerFamily(int count, int slots, int families, System.Random random)
        {
            if (count <= 0 || slots <= 0 || families <= 0 || random == null)
            {
                return null;
            }

            if (count % families != 0 || slots % families != 0)
            {
                return null;
            }

            int perFamily = slots / families;
            int take = count / families;

            if (take > perFamily)
            {
                return null;
            }

            int[] chosen = new int[count];
            int[] pool = new int[perFamily];

            for (int family = 0; family < families; family++)
            {
                for (int i = 0; i < perFamily; i++)
                {
                    pool[i] = family * perFamily + i;
                }

                Shuffle(pool, random);

                for (int i = 0; i < take; i++)
                {
                    chosen[family * take + i] = pool[i];
                }
            }

            return chosen;
        }

        /// <summary>
        /// Des rangs de la meme famille qu'un rang donne, distincts de lui et entre eux ; puis,
        /// si l'on en demande plus qu'il n'en reste, rien : l'appelant choisit lui-meme ou
        /// prendre le complement. Rend null si la famille n'en a pas assez.
        /// </summary>
        public static int[] SameFamily(int slot, int count, int slots, int families,
            System.Random random)
        {
            if (count < 0 || slots <= 0 || families <= 0 || slots % families != 0 || random == null)
            {
                return null;
            }

            int perFamily = slots / families;
            if (count > perFamily - 1)
            {
                return null;
            }

            int family = slot / perFamily;
            List<int> pool = new List<int>(perFamily - 1);
            for (int i = 0; i < perFamily; i++)
            {
                int candidate = family * perFamily + i;
                if (candidate != slot)
                {
                    pool.Add(candidate);
                }
            }

            int[] array = pool.ToArray();
            Shuffle(array, random);

            int[] picked = new int[count];
            System.Array.Copy(array, picked, count);
            return picked;
        }

        /// <summary>
        /// Des rangs d'AUTRES familles que celle d'un rang donne, distincts entre eux. Rend
        /// null s'il n'y en a pas assez.
        /// </summary>
        public static int[] OtherFamilies(int slot, int count, int slots, int families,
            System.Random random)
        {
            if (count < 0 || slots <= 0 || families <= 1 || slots % families != 0 || random == null)
            {
                return null;
            }

            int perFamily = slots / families;
            int family = slot / perFamily;

            List<int> pool = new List<int>(slots - perFamily);
            for (int i = 0; i < slots; i++)
            {
                if (i / perFamily != family)
                {
                    pool.Add(i);
                }
            }

            if (count > pool.Count)
            {
                return null;
            }

            int[] array = pool.ToArray();
            Shuffle(array, random);

            int[] picked = new int[count];
            System.Array.Copy(array, picked, count);
            return picked;
        }

        /// <summary>
        /// Fisher-Yates. i > 0 et non i > 1 : la derniere carte doit pouvoir bouger, sinon une
        /// carte reste a sa place a chaque donne.
        /// </summary>
        public static void Shuffle(int[] array, System.Random random)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
