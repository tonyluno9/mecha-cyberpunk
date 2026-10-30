using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>ID de la LISTA 1 -> sus clips (T-407). Los finales ganan sobre los provisionales. Un ID sin clips devuelve null y avisa una sola vez.</summary>
    [CreateAssetMenu(fileName = "CatalogoSonidos", menuName = "Mecha Cyberpunk/Progresion/Catalogo de sonidos")]
    public class CatalogoSonidos : ScriptableObject
    {
        [Serializable]
        public class Entrada
        {
            public string id;
            public AudioClip[] finales = new AudioClip[0];
            public AudioClip[] provisionales = new AudioClip[0];
        }

        [SerializeField] private List<Entrada> entradas = new List<Entrada>();

        private readonly Dictionary<string, Entrada> porId = new Dictionary<string, Entrada>();
        private readonly Dictionary<string, int> ultimo = new Dictionary<string, int>();
        private readonly HashSet<string> avisados = new HashSet<string>();
        private System.Random azar = new System.Random();

        public IReadOnlyList<Entrada> Entradas => entradas;

        /// <summary>Cuántos IDs distintos avisaron que no tienen archivo (para pruebas).</summary>
        public int IdsFaltantesAvisados => avisados.Count;

        public void Fijar(List<Entrada> nuevas)
        {
            entradas = nuevas;
            porId.Clear();
        }

        /// <summary>Semilla del azar, para pruebas.</summary>
        public void FijarSemilla(int semilla) => azar = new System.Random(semilla);

        /// <summary>Un clip del ID (variante al azar sin repetir la última), o null si el ID no tiene archivos.</summary>
        public AudioClip Elegir(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (porId.Count == 0)
            {
                foreach (Entrada e in entradas)
                {
                    if (e != null && !string.IsNullOrEmpty(e.id)) porId[e.id] = e;
                }
            }
            IReadOnlyList<AudioClip> clips = null;
            if (porId.TryGetValue(id, out Entrada entrada))
            {
                clips = ReglaCatalogoSonidos.Efectivos<AudioClip>(entrada.finales, entrada.provisionales);
            }
            if (clips == null || clips.Count == 0)
            {
                if (avisados.Add(id))
                {
                    Debug.LogWarning("[Sonido] El ID '" + id + "' no tiene archivos de audio; no suena.");
                }
                return null;
            }
            int previo = ultimo.TryGetValue(id, out int guardado) ? guardado : -1;
            int indice = ReglaCatalogoSonidos.ElegirVariante(clips.Count, previo, () => (float)azar.NextDouble());
            ultimo[id] = indice;
            return clips[indice];
        }
    }
}
