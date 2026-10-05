using UnityEngine;

namespace FluxLab
{
    public struct Taller
    {
        public string nombre;
        public string corto;
        public string detalle;
        public int receta;
        public int rondas;
        public float segundos;
    }

    public static class NivelesTaller
    {
        public static readonly Taller[] Lista =
        {
            new Taller
            {
                nombre = "Mesa del Aprendiz",
                corto = "Aprendiz",
                detalle = "Tres ingredientes y cinco segundos para mirar.",
                receta = 3,
                rondas = 3,
                segundos = 5f
            },
            new Taller
            {
                nombre = "Pasillo de Frascos",
                corto = "Frascos",
                detalle = "La receta sigue siendo de tres, con menos tiempo.",
                receta = 3,
                rondas = 3,
                segundos = 4f
            },
            new Taller
            {
                nombre = "Jardín de Luna",
                corto = "Jardín",
                detalle = "Entran cuatro ingredientes en el caldero.",
                receta = 4,
                rondas = 3,
                segundos = 4f
            },
            new Taller
            {
                nombre = "Torre de Cristal",
                corto = "Cristal",
                detalle = "Cuatro ingredientes y el tiempo se acorta.",
                receta = 4,
                rondas = 4,
                segundos = 3.2f
            },
            new Taller
            {
                nombre = "Caldero Mayor",
                corto = "Maestro",
                detalle = "Cinco ingredientes. La prueba del maestro.",
                receta = 5,
                rondas = 4,
                segundos = 2.5f
            }
        };

        public static Taller De(int nivel)
        {
            int indice = Mathf.Clamp(nivel, 1, Lista.Length) - 1;
            return Lista[indice];
        }
    }
}
