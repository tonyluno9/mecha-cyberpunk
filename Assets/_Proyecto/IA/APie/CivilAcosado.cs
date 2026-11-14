using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// El civil al que molesta un <see cref="GrupoAcoso"/> (S2, 14-nov). No pelea. Se encoge mientras lo acosan, retrocede (y a veces cae) con
    /// un empujón, y cuando vencen al grupo se levanta, da las gracias, corre lejos del jugador y se apaga. Si el jugador lo golpea, huye igual.
    /// Sin clips propios todavía: usa Idle, Dano_*, Caer/Levantar y correr del visual (ver docs/pedido-arte-pandillas.md).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CivilAcosado : MonoBehaviour, IRecibeDanio
    {
        public enum Fase
        {
            Quieto,
            Retrocediendo,
            Caido,
            Levantandose,
            Huyendo,
            Fuera
        }

        private const float Gravedad = -20f;
        private const float AlturaGrito = 2.3f;
        private const float SegundosGrito = 1.8f;
        private const float AlturaRayoPared = 0.9f;
        private const float AdelanteRayo = 0.5f;
        private const float AlturaSondeoBorde = 0.5f;
        private const int IntentosPared = 8;
        private const float GradosPorVuelta = 360f;

        [SerializeField] private DatosAcoso datos;

        private CharacterController controlador;
        private VisualPersonaje visual;
        private Transform raizVisual;
        private TextMesh textoGrito;
        private float finGrito = -1f;
        private Fase fase = Fase.Quieto;
        private float restante;
        private Vector3 velocidadRetroceso;
        private Vector3 direccionHuida;
        private Vector3 inicioHuida;
        private float recorrido;
        private float velocidadVertical;
        private float escalaActual = 1f;
        private bool encogido = true;
        private Vector3 mirarHacia;

        public Fase Estado => fase;
        public bool Huyendo => fase == Fase.Huyendo || fase == Fase.Levantandose;
        /// <summary>True una vez que terminó de huir y se apagó.</summary>
        public bool Terminado => fase == Fase.Fuera;
        public string GritoActual => textoGrito != null && textoGrito.gameObject.activeSelf ? textoGrito.text : null;
        public VisualPersonaje Visual => visual;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (datos == null || visual == null)
            {
                Debug.LogError("[IA] Faltan referencias en CivilAcosado de " + name, this);
                enabled = false;
                return;
            }
            raizVisual = visual.transform;
            mirarHacia = transform.position;
        }

        /// <summary>Hacia dónde mira mientras lo acosan (el centro del grupo o el que lo empuja).</summary>
        public void Mirar(Vector3 punto) => mirarHacia = punto;

        /// <summary>Empujón: retrocede <c>RetrocesoEmpujon</c> metros alejándose de <paramref name="direccion"/>, y si <paramref name="tira"/> cae un rato.</summary>
        public void Empujar(Vector3 direccion, bool tira)
        {
            if (fase != Fase.Quieto && fase != Fase.Retrocediendo)
            {
                return;
            }
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 1e-6f)
            {
                direccion = transform.forward;
            }
            direccion.Normalize();
            visual.Herir(direccion);
            velocidadRetroceso = direccion * (datos.RetrocesoEmpujon / datos.SegundosRetroceso);
            fase = Fase.Retrocediendo;
            restante = datos.SegundosRetroceso;
            if (tira)
            {
                visual.Tumbar();
                fase = Fase.Caido;
                restante = datos.SegundosCaido;
                retrocesoPendiente = datos.SegundosRetroceso;
            }
        }

        private float retrocesoPendiente;

        /// <summary>Se levanta si estaba caído, da las gracias y corre lejos de <paramref name="desde"/> (la posición del jugador).</summary>
        public void Huir(Vector3 desde, string gracias)
        {
            if (fase == Fase.Huyendo || fase == Fase.Levantandose || fase == Fase.Fuera)
            {
                return;
            }
            Vector3 lejos = transform.position - desde;
            lejos.y = 0f;
            direccionHuida = lejos.sqrMagnitude > 1e-6f ? lejos.normalized : transform.forward;
            encogido = false;
            if (!string.IsNullOrEmpty(gracias))
            {
                Gritar(gracias);
            }
            visual.Levantar();
            fase = Fase.Levantandose;
            restante = datos.SegundosLevantarse;
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (info.Cantidad > 0f && fase != Fase.Fuera && fase != Fase.Huyendo && fase != Fase.Levantandose)
            {
                Huir(transform.position - info.Direccion, null);
            }
            return ResultadoDanio.Ignorado;
        }

        private void Update()
        {
            ActualizarGrito();
            float dt = Time.deltaTime;
            Vector3 mover = Vector3.zero;
            switch (fase)
            {
                case Fase.Quieto:
                    GirarHacia(mirarHacia - transform.position, datos.GiroHuida, dt);
                    break;
                case Fase.Retrocediendo:
                    mover = velocidadRetroceso;
                    restante -= dt;
                    if (restante <= 0f)
                    {
                        fase = Fase.Quieto;
                    }
                    break;
                case Fase.Caido:
                    if (retrocesoPendiente > 0f)
                    {
                        mover = velocidadRetroceso;
                        retrocesoPendiente -= dt;
                    }
                    restante -= dt;
                    if (restante <= 0f)
                    {
                        visual.Levantar();
                        fase = Fase.Quieto;
                    }
                    break;
                case Fase.Levantandose:
                    restante -= dt;
                    if (restante <= 0f)
                    {
                        fase = Fase.Huyendo;
                        inicioHuida = transform.position;
                        recorrido = 0f;
                    }
                    break;
                case Fase.Huyendo:
                    mover = Correr(dt);
                    break;
            }
            ActualizarEscala(dt);
            if (fase == Fase.Fuera)
            {
                return;
            }
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * dt;
            Vector3 paso = mover;
            paso.y = velocidadVertical;
            controlador.Move(paso * dt);
            if (fase == Fase.Huyendo && recorrido >= datos.DistanciaHuida)
            {
                fase = Fase.Fuera;
                gameObject.SetActive(false);
            }
        }

        private Vector3 Correr(float dt)
        {
            direccionHuida = ElegirDireccionLibre(direccionHuida);
            GirarHacia(direccionHuida, datos.GiroHuida, dt);
            Vector3 velocidad = direccionHuida * datos.VelocidadHuida;
            recorrido += datos.VelocidadHuida * dt;
            return velocidad;
        }

        // Si hay una pared adelante, o el piso se acaba (borde de azotea), gira de a poco hasta encontrar paso libre.
        private Vector3 ElegirDireccionLibre(Vector3 deseada)
        {
            for (int i = 0; i < IntentosPared; i++)
            {
                int lado = i % 2 == 0 ? 1 : -1;
                int pasos = (i + 1) / 2;
                Vector3 d = Quaternion.Euler(0f, lado * pasos * datos.GiroPared, 0f) * deseada;
                if (Libre(d))
                {
                    return d;
                }
            }
            return -deseada;
        }

        private bool Libre(Vector3 d)
        {
            Vector3 origen = transform.position + Vector3.up * AlturaRayoPared + d * AdelanteRayo;
            if (Physics.Raycast(origen, d, datos.DistanciaRayoHuida, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            Vector3 sondeo = transform.position + d * (AdelanteRayo + datos.DistanciaRayoHuida) + Vector3.up * AlturaSondeoBorde;
            return Physics.Raycast(sondeo, Vector3.down, AlturaSondeoBorde + datos.CaidaMaximaHuida, ~0, QueryTriggerInteraction.Ignore);
        }

        private void GirarHacia(Vector3 direccion, float gradosPorSegundo, float dt)
        {
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 1e-6f)
            {
                return;
            }
            Quaternion meta = Quaternion.LookRotation(direccion.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, Mathf.Min(gradosPorSegundo, GradosPorVuelta) * dt);
        }

        // Mientras lo acosan se achica un poco (hasta que haya un clip de miedo); al huir vuelve a su altura.
        private void ActualizarEscala(float dt)
        {
            float meta = encogido ? datos.EscalaEncogido : 1f;
            escalaActual = Mathf.MoveTowards(escalaActual, meta, dt);
            raizVisual.localScale = new Vector3(1f, escalaActual, 1f);
        }

        /// <summary>Muestra un grito corto sobre la cabeza.</summary>
        public void Gritar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return;
            }
            finGrito = Time.time + SegundosGrito;
            if (textoGrito == null)
            {
                GameObject objeto = new GameObject("Grito");
                objeto.transform.SetParent(transform, false);
                objeto.transform.localPosition = Vector3.up * AlturaGrito;
                textoGrito = objeto.AddComponent<TextMesh>();
                textoGrito.anchor = TextAnchor.LowerCenter;
                textoGrito.alignment = TextAlignment.Center;
                textoGrito.characterSize = 0.12f;
                textoGrito.fontSize = 48;
                textoGrito.color = new Color(0.6f, 0.9f, 1f);
                Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textoGrito.font = fuente;
                objeto.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
            textoGrito.text = texto;
            textoGrito.gameObject.SetActive(true);
        }

        private void ActualizarGrito()
        {
            if (textoGrito == null || !textoGrito.gameObject.activeSelf)
            {
                return;
            }
            if (Time.time >= finGrito)
            {
                textoGrito.gameObject.SetActive(false);
                return;
            }
            Camera camara = Camera.main;
            if (camara != null)
            {
                textoGrito.transform.rotation = camara.transform.rotation;
            }
        }
    }
}
