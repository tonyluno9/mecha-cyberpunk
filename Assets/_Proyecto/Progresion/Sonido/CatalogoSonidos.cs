using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
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

        [Header("Mezcla (T-470): voces y disparos por encima, ambientes bajos")]
        [Range(0f, 1f)] [SerializeField] private float gananciaEfectos = 1f;
        [Range(0f, 1f)] [SerializeField] private float gananciaVoces = 1f;
        [Range(0f, 1f)] [SerializeField] private float gananciaAmbiente = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float gananciaMusica = 0f;
        [Range(0f, 1f)] [SerializeField] private float gananciaInterfaz = 0.7f;
        [Tooltip("Mientras ReglaOpciones deje el ambiente en 0 por defecto, el ambiente suena al menos a este nivel de la opción. Ponlo en 0 cuando la opción ya arranque encendida.")]
        [Range(0f, 1f)] [SerializeField] private float pisoOpcionAmbiente = 0.8f;

        private readonly Dictionary<string, Entrada> porId = new Dictionary<string, Entrada>();
        private readonly Dictionary<string, int> ultimo = new Dictionary<string, int>();
        private readonly HashSet<string> avisados = new HashSet<string>();
        private System.Random azar = new System.Random();

        public IReadOnlyList<Entrada> Entradas => entradas;

        public float PisoOpcionAmbiente => pisoOpcionAmbiente;

        /// <summary>Ganancia de mezcla de la sección (se multiplica por el volumen general y por el de las opciones).</summary>
        public float Ganancia(SeccionSonido seccion)
        {
            switch (seccion)
            {
                case SeccionSonido.Efectos: return gananciaEfectos;
                case SeccionSonido.Voces: return gananciaVoces;
                case SeccionSonido.Ambiente: return gananciaAmbiente;
                case SeccionSonido.Musica: return gananciaMusica;
                case SeccionSonido.Interfaz: return gananciaInterfaz;
                default: return 1f;
            }
        }

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
            AudioClip clip = Alias.TryGetValue(id, out string otro) ? ElegirDe(otro) : null;
            if (clip == null) clip = ElegirDe(id);
            if (clip == null && avisados.Add(id))
            {
                Debug.LogWarning("[Sonido] El ID '" + id + "' no tiene archivos de audio; no suena.");
            }
            return clip;
        }

        /// <summary>El código pide ARM_SUB_* para el fusil y el equipo entregó ARM_FUS_*; mientras la Sesión A no renombre, suenan los ARM_FUS_.</summary>
        private static readonly Dictionary<string, string> Alias = new Dictionary<string, string>
        {
            { "ARM_SUB_DISPARO", "ARM_FUS_DISPARO" },
            { "ARM_SUB_RECARGA", "ARM_FUS_RECARGA" },
        };

        private AudioClip ElegirDe(string id)
        {
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
            if (clips == null || clips.Count == 0) return null;
            int previo = ultimo.TryGetValue(id, out int guardado) ? guardado : -1;
            int indice = ReglaCatalogoSonidos.ElegirVariante(clips.Count, previo, () => (float)azar.NextDouble());
            ultimo[id] = indice;
            return clips[indice];
        }
    }
}
