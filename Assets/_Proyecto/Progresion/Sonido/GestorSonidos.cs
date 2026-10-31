using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// Toca los sonidos por su ID de la LISTA 1 (T-407). 16 fuentes reutilizables; 3D con distancia 2 a 40 m; volumen = general x sección.
    /// Ambiente y Música arrancan en 0 (silencio por defecto, plan de la noche). Un ID sin archivo no suena y avisa una vez (CatalogoSonidos).
    /// Ambientes (T-470, ver ReglaAmbienteZona): por lugar y por zona del jugador, de noche o de día, con cruce de 1 s.
    /// La mezcla (ganancia por sección) vive en el CatalogoSonidos.
    /// </summary>
    public class GestorSonidos : MonoBehaviour, ISonidos
    {
        public const string IdAmbienteCalle = "AMB_CALLE_NOCHE";
        public const string IdAmbienteCasa = "AMB_INTERIOR_CASA";
        public const string IdAmbienteMercado = ReglaAmbienteZona.Mercado;
        public const string IdAmbienteTianguis = ReglaAmbienteZona.Tianguis;

        [SerializeField] private CatalogoSonidos catalogo;
        [Min(1)] [SerializeField] private int cantidadFuentes = 16;
        [Min(0f)] [Tooltip("Distancia mínima del sonido 3D (m).")] [SerializeField] private float distanciaMinima = 2f;
        [Min(0f)] [Tooltip("Distancia máxima del sonido 3D (m).")] [SerializeField] private float distanciaMaxima = 40f;
        [Min(0.01f)] [Tooltip("Duración del cruce entre ambientes (s).")] [SerializeField] private float segundosCruce = 1f;
        [Min(0.05f)] [Tooltip("Cada cuántos segundos se revisa la zona del jugador (s).")] [SerializeField] private float segundosRevision = 0.5f;

        private readonly Dictionary<SeccionSonido, float> volumenes = new Dictionary<SeccionSonido, float>();
        private readonly Dictionary<string, int> conteo = new Dictionary<string, int>();
        private readonly Dictionary<int, Bucle> bucles = new Dictionary<int, Bucle>();
        private AudioSource[] fuentes;
        private AudioSource[] cruce;
        private string[] idCruce = new string[2];
        private float[] nivelCruce = new float[2];
        private int cruceActivo;
        private string ambienteDeseado;
        private IdLugar lugar = IdLugar.Calle;
        private int siguienteBucle;
        private float proximaRevision;

        private class Bucle
        {
            public AudioSource fuente;
            public Transform sigue;
            public SeccionSonido seccion;
        }

        public string UltimoId { get; private set; }
        public string AmbienteDeseado => ambienteDeseado;

        /// <summary>Cuántas veces se pidió el ID (hubiera o no archivo). Es el contador de las pruebas.</summary>
        public int Pedidos(string id) => conteo.TryGetValue(id, out int n) ? n : 0;

        public CatalogoSonidos Catalogo { get => catalogo; set => catalogo = value; }

        private void Awake()
        {
            foreach (SeccionSonido s in System.Enum.GetValues(typeof(SeccionSonido)))
            {
                volumenes[s] = s == SeccionSonido.General ? 1f : ReglaCatalogoSonidos.VolumenInicial(s);
            }
            fuentes = new AudioSource[cantidadFuentes];
            for (int i = 0; i < fuentes.Length; i++)
            {
                fuentes[i] = CrearFuente("Fuente_" + i);
            }
            cruce = new[] { CrearFuente("Ambiente_A"), CrearFuente("Ambiente_B") };
            foreach (AudioSource a in cruce)
            {
                a.loop = true;
                a.spatialBlend = 0f;
            }
        }

        private void OnEnable()
        {
            Registro<ISonidos>.Agregar(this);
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
        }

        private void OnDisable()
        {
            Registro<ISonidos>.Quitar(this);
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
        }

        private AudioSource CrearFuente(string nombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            AudioSource a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.minDistance = distanciaMinima;
            a.maxDistance = distanciaMaxima;
            a.rolloffMode = AudioRolloffMode.Linear;
            return a;
        }

        public float Volumen(SeccionSonido seccion) => volumenes.TryGetValue(seccion, out float v) ? v : 0f;

        public void FijarVolumen(SeccionSonido seccion, float volumen) => volumenes[seccion] = Mathf.Clamp01(volumen);

        private float VolumenFinal(SeccionSonido seccion)
        {
            if (seccion == SeccionSonido.General) return Volumen(seccion);
            float opcion = Volumen(seccion);
            if (seccion == SeccionSonido.Ambiente && catalogo != null) opcion = Mathf.Max(opcion, catalogo.PisoOpcionAmbiente);
            float ganancia = catalogo != null ? catalogo.Ganancia(seccion) : 1f;
            return Volumen(SeccionSonido.General) * opcion * ganancia;
        }

        private AudioSource FuenteLibre()
        {
            foreach (AudioSource f in fuentes)
            {
                if (!f.isPlaying) return f;
            }
            return fuentes[0];
        }

        private AudioClip Preparar(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            UltimoId = id;
            conteo[id] = Pedidos(id) + 1;
            return catalogo != null ? catalogo.Elegir(id) : null;
        }

        public void Reproducir(string id) => Tocar(id, null, Vector3.zero);

        public void ReproducirEn(string id, Vector3 posicion) => Tocar(id, true, posicion);

        private void Tocar(string id, bool? en3D, Vector3 posicion)
        {
            AudioClip clip = Preparar(id);
            if (clip == null) return;
            SeccionSonido seccion = ReglaCatalogoSonidos.SeccionDe(id);
            AudioSource f = FuenteLibre();
            f.Stop();
            f.clip = clip;
            f.loop = false;
            f.pitch = 1f;
            f.volume = VolumenFinal(seccion);
            f.ignoreListenerPause = seccion == SeccionSonido.Interfaz;
            f.spatialBlend = en3D == true ? 1f : 0f;
            if (en3D == true) f.transform.position = posicion;
            f.Play();
        }

        public int ReproducirBucle(string id, Transform sigue)
        {
            AudioClip clip = Preparar(id);
            if (clip == null) return Sonidos.SinBucle;
            SeccionSonido seccion = ReglaCatalogoSonidos.SeccionDe(id);
            AudioSource f = CrearFuente("Bucle_" + id);
            f.clip = clip;
            f.loop = true;
            f.volume = VolumenFinal(seccion);
            f.spatialBlend = sigue != null ? 1f : 0f;
            if (sigue != null) f.transform.position = sigue.position;
            f.Play();
            int numero = siguienteBucle++;
            bucles[numero] = new Bucle { fuente = f, sigue = sigue, seccion = seccion };
            return numero;
        }

        public void DetenerBucle(int bucle)
        {
            if (!bucles.TryGetValue(bucle, out Bucle b)) return;
            bucles.Remove(bucle);
            if (b.fuente != null) Destroy(b.fuente.gameObject);
        }

        public void FijarTono(int bucle, float tono)
        {
            if (bucles.TryGetValue(bucle, out Bucle b) && b.fuente != null) b.fuente.pitch = tono;
        }

        private void AlEntrar(IdLugar nuevo)
        {
            lugar = nuevo;
            ActualizarAmbiente();
        }

        private void AlSalir(IdLugar anterior)
        {
            if (lugar == anterior) lugar = IdLugar.Calle;
            ActualizarAmbiente();
        }

        private string AmbienteDe()
        {
            ITerritorio t = Registro<ITerritorio>.Actual;
            IJugador j = Registro<IJugador>.Actual;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            string zona = t != null && j != null ? t.ZonaEn(j.Raiz.position) : string.Empty;
            return ReglaAmbienteZona.Elegir(lugar, zona, reloj == null || reloj.EsDeNoche);
        }

        private void ActualizarAmbiente()
        {
            string nuevo = AmbienteDe();
            if (nuevo == ambienteDeseado) return;
            ambienteDeseado = nuevo;
            int otro = 1 - cruceActivo;
            AudioClip clip = nuevo != null ? Preparar(nuevo) : null;
            idCruce[otro] = clip != null ? nuevo : null;
            cruce[otro].Stop();
            if (clip != null)
            {
                cruce[otro].clip = clip;
                cruce[otro].volume = 0f;
                cruce[otro].Play();
            }
            nivelCruce[otro] = 0f;
            cruceActivo = otro;
        }

        private void Update()
        {
            if (Time.unscaledTime >= proximaRevision)
            {
                proximaRevision = Time.unscaledTime + segundosRevision;
                ActualizarAmbiente();
            }
            float paso = Time.unscaledDeltaTime / segundosCruce;
            for (int i = 0; i < 2; i++)
            {
                float meta = i == cruceActivo && idCruce[i] != null ? 1f : 0f;
                nivelCruce[i] = Mathf.MoveTowards(nivelCruce[i], meta, paso);
                cruce[i].volume = nivelCruce[i] * VolumenFinal(SeccionSonido.Ambiente);
                if (meta == 0f && nivelCruce[i] == 0f && cruce[i].isPlaying) cruce[i].Stop();
            }
            foreach (Bucle b in bucles.Values)
            {
                if (b.sigue != null && b.fuente != null) b.fuente.transform.position = b.sigue.position;
                if (b.fuente != null) b.fuente.volume = VolumenFinal(b.seccion);
            }
        }
    }
}
