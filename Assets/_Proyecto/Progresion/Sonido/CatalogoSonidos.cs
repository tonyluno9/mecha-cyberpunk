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

        /// <summary>T-620: interruptor de un ID. Apagado = el gestor no lo toca (el pedido se sigue contando).</summary>
        [Serializable]
        public class Interruptor
        {
            public string id;
            public bool encendido;
        }

        [SerializeField] private List<Entrada> entradas = new List<Entrada>();

        [Header("Interruptores por ID (T-620): marca encendido para prender un sonido. Lista vacia = todo suena.")]
        [SerializeField] private List<Interruptor> interruptores = new List<Interruptor>();

        [Header("Mezcla (T-470, final en T-977): disparos y voces arriba, interfaz en medio, ambientes muy abajo (cada AMB trae su nivel)")]
        [Range(0f, 1f)] [SerializeField] private float gananciaEfectos = MezclaEfectos;
        [Range(0f, 1f)] [SerializeField] private float gananciaVoces = MezclaVoces;
        [Range(0f, 1f)] [SerializeField] private float gananciaAmbiente = MezclaAmbiente;
        [Range(0f, 1f)] [SerializeField] private float gananciaMusica = MezclaMusica;
        [Range(0f, 1f)] [SerializeField] private float gananciaInterfaz = MezclaInterfaz;

        /// <summary>T-977: la mezcla final por sección. Los ambientes llegan bajos por su archivo y su nivel (ReglaMezclaSonidos.VolumenAmbiente).</summary>
        public const float MezclaEfectos = 1f;
        public const float MezclaVoces = 0.85f;
        public const float MezclaAmbiente = 1f;
        public const float MezclaMusica = 0.5f;
        public const float MezclaInterfaz = 0.6f;
        [Tooltip("Mientras ReglaOpciones deje el ambiente en 0 por defecto, el ambiente suena al menos a este nivel de la opción. Ponlo en 0 cuando la opción ya arranque encendida.")]
        [Range(0f, 1f)] [SerializeField] private float pisoOpcionAmbiente = 0.8f;

        private readonly Dictionary<string, Entrada> porId = new Dictionary<string, Entrada>();
        private readonly Dictionary<string, int> ultimo = new Dictionary<string, int>();
        private readonly HashSet<string> avisados = new HashSet<string>();
        private System.Random azar = new System.Random();

        public IReadOnlyList<Entrada> Entradas => entradas;

        public IReadOnlyList<Interruptor> Interruptores => interruptores;

        /// <summary>Si el ID puede sonar. Sin interruptores configurados suena todo; con ellos, un ID que no esta en la lista no suena.</summary>
        public bool Encendido(string id)
        {
            if (interruptores.Count == 0) return true;
            foreach (Interruptor i in interruptores)
            {
                if (i != null && i.id == id) return i.encendido;
            }
            return false;
        }

        /// <summary>Prende o apaga el ID (lo agrega a la lista si no estaba; para pruebas).</summary>
        public void FijarEncendido(string id, bool encendido)
        {
            foreach (Interruptor i in interruptores)
            {
                if (i != null && i.id == id)
                {
                    i.encendido = encendido;
                    return;
                }
            }
            interruptores.Add(new Interruptor { id = id, encendido = encendido });
        }

        /// <summary>Deja un interruptor por cada ID del catalogo (y por cada encendido por defecto). Conserva lo ya puesto; lo nuevo usa el valor por defecto.</summary>
        public void SincronizarInterruptores()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (Entrada e in entradas)
            {
                if (e != null && !string.IsNullOrEmpty(e.id)) ids.Add(e.id);
            }
            foreach (string id in ReglaMezclaSonidos.IdsEncendidos) ids.Add(id);
            foreach (string id in ReglaMezclaSonidos.IdsRegresados) ids.Add(id);
            interruptores.RemoveAll(i => i == null || !ids.Contains(i.id));
            HashSet<string> tienen = new HashSet<string>();
            foreach (Interruptor i in interruptores) tienen.Add(i.id);
            foreach (string id in ids)
            {
                if (!tienen.Contains(id)) interruptores.Add(new Interruptor { id = id, encendido = ReglaMezclaSonidos.EncendidoPorDefecto(id) });
            }
            interruptores.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        }

        public float PisoOpcionAmbiente => pisoOpcionAmbiente;

        /// <summary>T-977: deja la mezcla final de las secciones (la usa el editor para el asset del juego).</summary>
        public void FijarMezclaFinal()
        {
            gananciaEfectos = MezclaEfectos;
            gananciaVoces = MezclaVoces;
            gananciaAmbiente = MezclaAmbiente;
            gananciaMusica = MezclaMusica;
            gananciaInterfaz = MezclaInterfaz;
        }

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

        /// <summary>Si el ID tiene algún clip. No avisa ni consume variantes.</summary>
        public bool Tiene(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (porId.Count == 0)
            {
                foreach (Entrada e in entradas)
                {
                    if (e != null && !string.IsNullOrEmpty(e.id)) porId[e.id] = e;
                }
            }
            if (!porId.TryGetValue(id, out Entrada entrada)) return false;
            return ReglaCatalogoSonidos.Efectivos<AudioClip>(entrada.finales, entrada.provisionales).Count > 0;
        }

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
            // T-676: el disparo de cada arma es siempre el mismo clip.
            if (id.EndsWith("_DISPARO", System.StringComparison.Ordinal)) return clips[0];
            int previo = ultimo.TryGetValue(id, out int guardado) ? guardado : -1;
            int indice = ReglaCatalogoSonidos.ElegirVariante(clips.Count, previo, () => (float)azar.NextDouble());
            ultimo[id] = indice;
            return clips[indice];
        }
    }
}
