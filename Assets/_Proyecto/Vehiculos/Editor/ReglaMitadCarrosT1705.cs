using System;
using System.Collections.Generic;
using System.Linq;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>Un carro estacionado para la regla (datos puros, sin escena).</summary>
    public readonly struct CarroEstacionadoDato
    {
        public readonly int Id;
        public readonly string Modelo;
        public readonly float X;
        public readonly float Z;
        /// <summary>true si no se puede quitar (carro de guion, robable de la historia, del jugador).</summary>
        public readonly bool Intocable;

        public CarroEstacionadoDato(int id, string modelo, float x, float z, bool intocable = false)
        {
            Id = id;
            Modelo = modelo;
            X = x;
            Z = z;
            Intocable = intocable;
        }

        public float Distancia(CarroEstacionadoDato otro) => (float)Math.Sqrt((X - otro.X) * (X - otro.X) + (Z - otro.Z) * (Z - otro.Z));
    }

    /// <summary>
    /// T-1705 (T54): qué carros estacionados se apagan. Pura. 1) Duplicados: el mismo modelo a menos de <see cref="DistanciaDuplicado"/> m de otro que se queda.
    /// 2) Cerca de la casa (<see cref="RadioCasa"/> m): solo <see cref="MaximoCerca"/>, de modelos distintos si se puede. 3) El resto, por celdas de <see cref="LadoCelda"/> m:
    /// en cada una se quita la mitad (los tuneados con neón V20 y V21 se quitan al final), así la ciudad queda repartida. 4) Si aún sobran más de la mitad del total
    /// original, se quita de las celdas con más carros, primero del modelo más repetido.
    /// </summary>
    public static class ReglaMitadCarrosT1705
    {
        public const float DistanciaDuplicado = 6f;
        public const float RadioCasa = 35f;
        public const int MaximoCerca = 3;
        public const float LadoCelda = 40f;
        public static readonly string[] Neon = { "V20", "V21" };

        public static bool EsNeon(string modelo) => Array.IndexOf(Neon, modelo) >= 0;

        /// <summary>Los Id de los carros que se apagan.</summary>
        public static HashSet<int> Quitar(IReadOnlyList<CarroEstacionadoDato> carros, float casaX, float casaZ)
        {
            HashSet<int> quitar = new HashSet<int>();
            List<CarroEstacionadoDato> vivos = carros.OrderBy(c => c.Id).ToList();

            // 1) Duplicados.
            List<CarroEstacionadoDato> sinDuplicados = new List<CarroEstacionadoDato>();
            foreach (CarroEstacionadoDato c in vivos)
            {
                bool repetido = !c.Intocable && sinDuplicados.Any(k => k.Modelo == c.Modelo && k.Distancia(c) < DistanciaDuplicado);
                if (repetido)
                {
                    quitar.Add(c.Id);
                }
                else
                {
                    sinDuplicados.Add(c);
                }
            }
            vivos = sinDuplicados;

            // 2) Cerca de la casa.
            CarroEstacionadoDato casa = new CarroEstacionadoDato(-1, "", casaX, casaZ);
            List<CarroEstacionadoDato> cerca = vivos.Where(c => c.Distancia(casa) <= RadioCasa).OrderBy(c => c.Distancia(casa)).ToList();
            HashSet<string> modelosCerca = new HashSet<string>();
            int dejados = cerca.Count(c => c.Intocable);
            List<CarroEstacionadoDato> sobran = new List<CarroEstacionadoDato>();
            foreach (CarroEstacionadoDato c in cerca.Where(c => !c.Intocable))
            {
                if (dejados < MaximoCerca && modelosCerca.Add(c.Modelo))
                {
                    dejados++;
                }
                else
                {
                    sobran.Add(c);
                }
            }
            // Si no alcanzaron los modelos distintos, se completa con los más cercanos de los que sobran.
            foreach (CarroEstacionadoDato c in sobran)
            {
                if (dejados < MaximoCerca)
                {
                    dejados++;
                }
                else
                {
                    quitar.Add(c.Id);
                }
            }
            vivos = vivos.Where(c => !quitar.Contains(c.Id)).ToList();
            HashSet<int> enCasa = new HashSet<int>(cerca.Select(c => c.Id));

            // 3) Mitad por celda en el resto de la ciudad.
            foreach (IGrouping<(int, int), CarroEstacionadoDato> celda in vivos.Where(c => !enCasa.Contains(c.Id)).GroupBy(Celda))
            {
                List<CarroEstacionadoDato> orden = celda.Where(c => !c.Intocable).OrderBy(c => EsNeon(c.Modelo) ? 1 : 0).ThenBy(c => c.Id).ToList();
                int aQuitar = celda.Count() / 2;
                for (int i = 0; i < orden.Count && aQuitar > 0; i++, aQuitar--)
                {
                    quitar.Add(orden[i].Id);
                }
            }
            vivos = vivos.Where(c => !quitar.Contains(c.Id)).ToList();

            // 4) Tope: no más de la mitad del total original.
            int meta = (carros.Count + 1) / 2;
            while (vivos.Count > meta)
            {
                IGrouping<(int, int), CarroEstacionadoDato> llena = vivos.Where(c => !enCasa.Contains(c.Id)).GroupBy(Celda).OrderByDescending(g => g.Count()).FirstOrDefault();
                // Con una celda con 2 o más se quita de ahí; si cada carro está solo en su celda, de todos los que quedan (el modelo más repetido primero).
                IEnumerable<CarroEstacionadoDato> opciones = llena != null && llena.Count() >= 2 ? llena : vivos.Where(c => !enCasa.Contains(c.Id));
                List<CarroEstacionadoDato> lista = opciones.Where(c => !c.Intocable).ToList();
                if (lista.Count == 0)
                {
                    break;
                }
                string repetido = vivos.GroupBy(c => c.Modelo).OrderByDescending(g => g.Count()).First().Key;
                CarroEstacionadoDato salir = lista.OrderBy(c => EsNeon(c.Modelo) ? 1 : 0).ThenBy(c => c.Modelo == repetido ? 0 : 1).ThenBy(c => c.Id).First();
                quitar.Add(salir.Id);
                vivos.RemoveAll(c => c.Id == salir.Id);
            }
            return quitar;
        }

        /// <summary>La celda de <see cref="LadoCelda"/> m a la que pertenece el carro.</summary>
        public static (int, int) Celda(CarroEstacionadoDato c) => ((int)Math.Floor(c.X / LadoCelda), (int)Math.Floor(c.Z / LadoCelda));
    }
}
