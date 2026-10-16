using MechaCyberpunk.Core;
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

        private MaterialPropertyBlock bloque;
        private IPerfilJugador perfilSeguido;
        private bool tieneApariencia;
        private AparienciaJugador aparienciaFija;

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
            if (!tieneApariencia)
            {
                Seguir(Registro<IPerfilJugador>.Actual);
            }
            Repintar();
        }

        private void OnDisable()
        {
            Registro<IPerfilJugador>.Agregado -= AlLlegarPerfil;
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
