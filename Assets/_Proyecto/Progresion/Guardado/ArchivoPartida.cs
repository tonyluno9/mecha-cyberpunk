using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Guardado
{
    /// <summary>Una pieza del archivo de guardado: el texto que dio un IGuardable bajo su clave.</summary>
    [Serializable]
    public class PiezaGuardada
    {
        public string clave;
        public string datos;
    }

    /// <summary>
    /// Formato del archivo de la partida (D-tony-50): versión, fecha ISO y una lista de piezas {clave, datos}.
    /// Funciones puras para pasar de piezas a JSON y de JSON a piezas; nunca lanzan excepciones al leer.
    /// </summary>
    [Serializable]
    public class ArchivoPartida
    {
        /// <summary>Versión del formato que escribe y entiende esta versión del juego.</summary>
        public const int VersionActual = 1;

        public int version;
        public string fecha;
        public List<PiezaGuardada> piezas = new List<PiezaGuardada>();

        /// <summary>Arma el JSON de la partida con las piezas dadas y la fecha dada (texto ISO).</summary>
        public static string Escribir(IEnumerable<PiezaGuardada> piezas, string fechaIso)
        {
            var archivo = new ArchivoPartida { version = VersionActual, fecha = fechaIso ?? string.Empty };
            if (piezas != null)
            {
                foreach (PiezaGuardada pieza in piezas)
                {
                    if (pieza != null && !string.IsNullOrEmpty(pieza.clave))
                    {
                        archivo.piezas.Add(new PiezaGuardada { clave = pieza.clave, datos = pieza.datos ?? string.Empty });
                    }
                }
            }
            return JsonUtility.ToJson(archivo);
        }

        /// <summary>
        /// Lee el JSON. Devuelve false (y piezas vacías) si el texto está vacío o roto, o si la versión no es la actual
        /// (desconocida o más nueva). No lanza excepciones.
        /// </summary>
        public static bool TryLeer(string json, out List<PiezaGuardada> piezas, out string fecha)
        {
            piezas = new List<PiezaGuardada>();
            fecha = string.Empty;
            if (string.IsNullOrWhiteSpace(json)) return false;
            ArchivoPartida leido;
            try
            {
                leido = JsonUtility.FromJson<ArchivoPartida>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (leido == null || leido.version != VersionActual || leido.piezas == null) return false;
            foreach (PiezaGuardada pieza in leido.piezas)
            {
                if (pieza != null && !string.IsNullOrEmpty(pieza.clave))
                {
                    piezas.Add(new PiezaGuardada { clave = pieza.clave, datos = pieza.datos ?? string.Empty });
                }
            }
            fecha = leido.fecha ?? string.Empty;
            return true;
        }
    }
}
