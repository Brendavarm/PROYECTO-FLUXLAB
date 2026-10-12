using System.Collections.Generic;
using UnityEngine;

namespace FluxLab.Nivel2
{
    /// <summary>
    /// Reglas de la receta: cómo se arma, qué orden se espera y cómo se baraja la bandeja.
    /// No dibuja nada, solo decide.
    /// </summary>
    public static class RecetaFrascos
    {
        public static readonly IngredientKind[] Todos =
        {
            IngredientKind.Fuego,
            IngredientKind.Gota,
            IngredientKind.Polvo,
            IngredientKind.Hierba,
            IngredientKind.Cristal,
            IngredientKind.Lagrima
        };

        public static List<IngredientKind> Generar(int largo, bool repetidos)
        {
            var receta = new List<IngredientKind>();
            if (!repetidos)
            {
                var mezcla = Barajar(Todos);
                for (int i = 0; i < largo && i < mezcla.Length; i++)
                    receta.Add(mezcla[i]);
                return receta;
            }

            // Con repetidos: nunca el mismo ingrediente dos veces seguidas,
            // así la repetición se nota y no parece un error de la pantalla.
            for (int i = 0; i < largo; i++)
            {
                IngredientKind elegido;
                do
                    elegido = Todos[Random.Range(0, Todos.Length)];
                while (i > 0 && elegido == receta[i - 1]);
                receta.Add(elegido);
            }

            return receta;
        }

        /// <summary>
        /// Lo que el jugador debe poner. En la ronda espejo es la receta al revés.
        /// </summary>
        public static List<IngredientKind> Esperada(List<IngredientKind> receta, bool espejo)
        {
            var esperada = new List<IngredientKind>(receta);
            if (espejo)
                esperada.Reverse();
            return esperada;
        }

        public static bool AciertaEn(List<IngredientKind> esperada, int posicion, IngredientKind elegido)
        {
            return posicion >= 0 && posicion < esperada.Count && esperada[posicion] == elegido;
        }

        /// <summary>
        /// Estrellas del nivel (igual que el nivel 1): 3 = todo bien, 2 = al menos dos tercios, 1 = algo.
        /// Si se rompieron todos los frascos, como máximo 1.
        /// </summary>
        public static int Estrellas(int aciertos, int rondas, bool seRompieron)
        {
            int estrellas = aciertos <= 0 ? 0 : aciertos >= rondas ? 3 : aciertos * 3 >= rondas * 2 ? 2 : 1;
            return seRompieron ? Mathf.Min(estrellas, 1) : estrellas;
        }

        public static IngredientKind[] Barajar(IngredientKind[] origen)
        {
            var copia = (IngredientKind[])origen.Clone();
            for (int i = 0; i < copia.Length; i++)
            {
                int cambio = Random.Range(i, copia.Length);
                IngredientKind temporal = copia[i];
                copia[i] = copia[cambio];
                copia[cambio] = temporal;
            }

            return copia;
        }
    }
}
