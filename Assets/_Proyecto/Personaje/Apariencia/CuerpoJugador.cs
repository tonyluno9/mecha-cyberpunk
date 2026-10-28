using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Apariencia
{
    /// <summary>
    /// Cuerpo low-poly de greybox del jugador (T-123). Aplica una <see cref="AparienciaJugador"/> con el catálogo: colores
    /// de piel, ropa y peinado, y muestra una sola pieza de peinado, de ropa y de implante. Si nadie le pasa una apariencia,
    /// sigue al perfil registrado y se repinta en su <c>Cambio</c>. Sin colliders ni sombras.
    /// </summary>
    public class CuerpoJugador : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdMetalico = Shader.PropertyToID("_Metallic");
        private static readonly int IdBrillo = Shader.PropertyToID("_Smoothness");

        [SerializeField] private CatalogoApariencia catalogo;
        [Header("Partes que llevan color")]
        [SerializeField] private Renderer cabeza;
        [SerializeField] private Renderer torso;
        [SerializeField] private Renderer[] brazos;
        [SerializeField] private Renderer[] piernas;
        [Header("Piezas, una por valor del enum correspondiente")]
        [Tooltip("Indexadas por PeinadoApariencia.")]
        [SerializeField] private GameObject[] peinados;
        [Tooltip("Indexadas por PiezaRopaApariencia.")]
        [SerializeField] private GameObject[] piezasRopa;
        [Tooltip("Indexadas por ImplanteApariencia (la 0 queda vacía).")]
        [SerializeField] private GameObject[] implantes;
        [Header("Implantes instalados (T-192), una pieza por EspacioImplante; no dependen del implante cosmético")]
        [SerializeField] private GameObject[] piezasInstaladas;
        [Tooltip("Franja que se suma en el nivel 2, una por EspacioImplante.")]
        [SerializeField] private GameObject[] franjasInstaladas;
        [SerializeField] private Color colorCromo = new Color(0.8f, 0.82f, 0.88f);
        [SerializeField] private Color colorOjos = new Color(0.1f, 0.85f, 0.85f);
        [SerializeField] private Color colorFranja = new Color(0.9f, 0.1f, 0.6f);

        private MaterialPropertyBlock bloque;
        private IPerfilJugador perfilSeguido;
        private bool tieneApariencia;
        private AparienciaJugador aparienciaFija;
        private readonly SeguidorImplantes seguidorImplantes = new SeguidorImplantes();
        private readonly int[] nivelesPrueba = new int[Enum.GetValues(typeof(EspacioImplante)).Length];

        public AparienciaJugador AparienciaAplicada { get; private set; }
        public Color ColorPiel { get; private set; }
        public Color ColorRopa { get; private set; }
        public Color ColorPeinado { get; private set; }
        public ImplanteApariencia ImplanteActivo { get; private set; }
        public PeinadoApariencia PeinadoActivo { get; private set; }
        public PiezaRopaApariencia PiezaRopaActiva { get; private set; }
        public CatalogoApariencia Catalogo => catalogo;
        public Renderer Cabeza => cabeza;
        public Renderer Torso => torso;
        public GameObject[] Implantes => implantes;
        public GameObject[] Peinados => peinados;
        public GameObject[] PiezasRopa => piezasRopa;
        public GameObject[] PiezasInstaladas => piezasInstaladas;
        public GameObject[] FranjasInstaladas => franjasInstaladas;

        /// <summary>Nivel que se muestra en el espacio: el de prueba si hay uno, si no el instalado.</summary>
        public int NivelMostrado(EspacioImplante espacio)
        {
            int prueba = nivelesPrueba[(int)espacio];
            return prueba > 0 ? prueba : seguidorImplantes.Nivel(espacio);
        }

        /// <summary>Muestra un implante "de prueba" en ese espacio encima de lo instalado (vista previa de la clínica). Nivel 0 lo quita.</summary>
        public void MostrarPrueba(EspacioImplante espacio, int nivel)
        {
            nivelesPrueba[(int)espacio] = Mathf.Clamp(nivel, 0, 2);
            RepintarImplantes();
        }

        /// <summary>Quita todas las pruebas y deja lo instalado.</summary>
        public void QuitarPrueba()
        {
            Array.Clear(nivelesPrueba, 0, nivelesPrueba.Length);
            RepintarImplantes();
        }

        private void Awake()
        {
            if (catalogo == null || !catalogo.EstaCompleto || cabeza == null || torso == null)
            {
                Debug.LogError("[Personaje] CuerpoJugador en " + name + " no tiene catálogo completo o partes.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (catalogo == null || !catalogo.EstaCompleto || cabeza == null || torso == null) return;
            Registro<IPerfilJugador>.Agregado += AlLlegarPerfil;
            seguidorImplantes.Cambio += RepintarImplantes;
            seguidorImplantes.Activar();
            if (!tieneApariencia)
            {
                Seguir(Registro<IPerfilJugador>.Actual);
            }
            Repintar();
        }

        private void OnDisable()
        {
            Registro<IPerfilJugador>.Agregado -= AlLlegarPerfil;
            seguidorImplantes.Cambio -= RepintarImplantes;
            seguidorImplantes.Desactivar();
            Seguir(null);
        }

        /// <summary>Muestra esta apariencia y deja de seguir al perfil. Índices fuera de rango se toman con módulo.</summary>
        public void Aplicar(AparienciaJugador apariencia)
        {
            tieneApariencia = true;
            aparienciaFija = apariencia;
            Seguir(null);
            Repintar();
        }

        /// <summary>Vuelve a seguir al perfil registrado.</summary>
        public void SeguirPerfil()
        {
            tieneApariencia = false;
            Seguir(Registro<IPerfilJugador>.Actual);
            Repintar();
        }

        private void AlLlegarPerfil(IPerfilJugador perfil)
        {
            if (tieneApariencia) return;
            Seguir(perfil);
            Repintar();
        }

        private void Seguir(IPerfilJugador perfil)
        {
            if (perfilSeguido != null) perfilSeguido.Cambio -= Repintar;
            perfilSeguido = perfil;
            if (perfilSeguido != null) perfilSeguido.Cambio += Repintar;
        }

        private void Repintar()
        {
            if (catalogo == null || !catalogo.EstaCompleto || cabeza == null || torso == null) return;
            AparienciaJugador a = tieneApariencia ? aparienciaFija
                : perfilSeguido != null ? perfilSeguido.Apariencia : AparienciaJugador.Predeterminada;
            AparienciaAplicada = a;
            OpcionCara cara = catalogo.Cara(a.Cara);
            OpcionRopa ropa = catalogo.Ropa(a.Ropa);
            OpcionImplante implante = catalogo.Implante(a.Implante);
            ColorPiel = cara.piel;
            ColorRopa = ropa.color;
            ColorPeinado = cara.colorPeinado;
            PeinadoActivo = cara.peinado;
            PiezaRopaActiva = ropa.pieza;
            ImplanteActivo = implante.implante;

            Pintar(cabeza, cara.piel);
            Pintar(torso, ropa.color);
            Color colorBrazo = ropa.conMangas ? ropa.color : cara.piel;
            for (int i = 0; i < brazos.Length; i++) Pintar(brazos[i], colorBrazo);
            for (int i = 0; i < piernas.Length; i++) Pintar(piernas[i], ropa.pantalon);

            MostrarPieza(peinados, (int)cara.peinado, cara.colorPeinado);
            MostrarPieza(piezasRopa, (int)ropa.pieza, ropa.colorPieza);
            MostrarPieza(implantes, (int)implante.implante, implante.color);
            RepintarImplantes();
        }

        private void RepintarImplantes()
        {
            if (piezasInstaladas == null) return;
            for (int i = 0; i < piezasInstaladas.Length; i++)
            {
                int nivel = NivelMostrado((EspacioImplante)i);
                GameObject pieza = piezasInstaladas[i];
                if (pieza != null)
                {
                    pieza.SetActive(nivel > 0);
                    if (nivel > 0) PintarArbol(pieza, i == (int)EspacioImplante.Ojos ? colorOjos : colorCromo, i != (int)EspacioImplante.Ojos);
                }
                GameObject franja = franjasInstaladas != null && i < franjasInstaladas.Length ? franjasInstaladas[i] : null;
                if (franja != null)
                {
                    franja.SetActive(nivel >= 2);
                    if (nivel >= 2) PintarArbol(franja, colorFranja, false);
                }
            }
        }

        private void PintarArbol(GameObject raiz, Color color, bool cromo)
        {
            if (bloque == null) bloque = new MaterialPropertyBlock();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, color);
                bloque.SetFloat(IdMetalico, cromo ? 0.9f : 0f);
                bloque.SetFloat(IdBrillo, cromo ? 0.85f : 0.25f);
                r.SetPropertyBlock(bloque);
            }
        }

        private void Pintar(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            if (bloque == null) bloque = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(bloque);
            bloque.SetColor(IdColorBase, color);
            renderer.SetPropertyBlock(bloque);
        }

        private void MostrarPieza(GameObject[] piezas, int activa, Color color)
        {
            for (int i = 0; i < piezas.Length; i++)
            {
                if (piezas[i] == null) continue;
                bool esta = i == activa;
                piezas[i].SetActive(esta);
                if (esta)
                {
                    Pintar(piezas[i].GetComponentInChildren<Renderer>(), color);
                }
            }
        }
    }
}
