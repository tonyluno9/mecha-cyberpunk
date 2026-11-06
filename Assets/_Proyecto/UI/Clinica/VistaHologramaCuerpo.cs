using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// El holograma del cuerpo de la clínica (T-815): un set lejos de la ciudad (<see cref="ConfigPantallaClinica.PosicionSet"/>) con su cámara
    /// que dibuja en una RenderTexture (<see cref="Textura"/>) el cuerpo del personaje horneado en el editor (solo la malla: sin scripts, huesos
    /// ni colisiones) con el shader holográfico del banco de armas. La malla trae una submalla por <see cref="EspacioImplante"/>: el espacio
    /// elegido late en naranja, los que ya tienen implante van en verde y el resto en cian. Se gira con <see cref="Girar"/>, se acerca con
    /// <see cref="Acercar"/>, gira solo cuando no se toca y la cámara se va un poco hacia el espacio elegido. Sin malla dibuja solo el pedestal.
    /// </summary>
    public class VistaHologramaCuerpo : MonoBehaviour
    {
        public const int Espacios = 4;
        private const float CercaCamara = 0.05f;
        private const float LejosCamara = 12f;
        private const int ProfundidadTextura = 24;
        private const int Suavizado = 4;
        private const float GiroInicial = 180f;
        private const float InclinacionInicial = 0f;
        private const float AnchoPedestal = 1.1f;
        private const float GrosorPedestal = 0.004f;
        private const float AlfaPedestal = 0.25f;
        private const float AlfaNormal = 0.75f;
        private const float ResalteMinimo = 0.55f;
        private const float UnidadesPorPaso = 120f;
        private const float GradosPorVuelta = 360f;
        private const float FraccionBrazo = 0.55f;

        private static readonly int IdResalte = Shader.PropertyToID("_Resalte");
        private static readonly int IdDestello = Shader.PropertyToID("_Destello");
        private static readonly int IdAlfa = Shader.PropertyToID("_Alfa");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        private readonly List<Object> creados = new List<Object>();
        private readonly bool[] instalados = new bool[Espacios];
        private readonly Vector3[] centros = new Vector3[Espacios];
        private MaterialPropertyBlock bloque;
        private ConfigPantallaClinica config;
        private RenderTexture textura;
        private Transform set;
        private Transform pivote;
        private Transform cuerpo;
        private Camera camara;
        private Renderer render;
        private Renderer pedestal;
        private EspacioImplante elegido;
        private Vector3 mira;
        private float giro = GiroInicial;
        private float inclinacion = InclinacionInicial;
        private float distancia;
        private float esperaGiro;
        private float destello;
        private float tiempo;

        public RenderTexture Textura => textura;
        public Camera Camara => camara;
        /// <summary>True mientras el set está armado y la cámara dibuja.</summary>
        public bool Encendida => set != null;
        /// <summary>True si hay cuerpo horneado (false: solo el pedestal).</summary>
        public bool ConCuerpo => render != null;
        public float Giro => giro;
        public float Inclinacion => inclinacion;
        public float Distancia => distancia;
        public EspacioImplante Elegido => elegido;
        /// <summary>El renderer del cuerpo (una submalla por espacio), o null.</summary>
        public Renderer Render => render;

        /// <summary>Qué tan resaltado va el espacio ahora (0 a 1): el elegido late entre ~0.55 y 1, los demás en 0.</summary>
        public float Resalte(EspacioImplante espacio) => espacio == elegido && render != null ? Pulso() : 0f;

        /// <summary>El centro del espacio en el mundo (para el marcador de la pantalla y las pruebas).</summary>
        public Vector3 CentroEspacio(EspacioImplante espacio)
        {
            int i = (int)espacio;
            return cuerpo != null && i >= 0 && i < Espacios ? cuerpo.TransformPoint(centros[i]) : (set != null ? set.position : Vector3.zero);
        }

        /// <summary>Arma el set con el cuerpo (si ya había uno, lo rehace).</summary>
        public void Mostrar(ConfigPantallaClinica configuracion, Mesh malla)
        {
            config = configuracion;
            Limpiar();
            bloque ??= new MaterialPropertyBlock();
            if (textura == null)
            {
                textura = new RenderTexture(config.AnchoTextura, config.AltoTextura, ProfundidadTextura, RenderTextureFormat.ARGB32)
                {
                    name = "HologramaCuerpo", antiAliasing = Suavizado
                };
                textura.Create();
            }
            distancia = config.Distancia;
            giro = GiroInicial;
            inclinacion = InclinacionInicial;
            esperaGiro = 0f;
            destello = 0f;
            set = new GameObject("SetHologramaCuerpo").transform;
            set.SetParent(transform, false);
            set.position = config.PosicionSet;
            creados.Add(set.gameObject);
            pivote = new GameObject("Pivote").transform;
            pivote.SetParent(set, false);
            pivote.localPosition = Vector3.up * (config.AlturaCuerpo * 0.5f);
            if (malla != null && config.MaterialHolograma != null)
            {
                CrearCuerpo(malla);
            }
            CrearPedestal();
            mira = Objetivo();
            CrearCamara();
            AplicarGiro();
            Pintar();
        }

        /// <summary>Quita el set y libera la textura.</summary>
        public void Ocultar()
        {
            Limpiar();
            if (textura != null)
            {
                textura.Release();
                Destroy(textura);
                textura = null;
            }
        }

        /// <summary>Cambia el espacio elegido y cuáles tienen implante (uno por espacio, en el orden de <see cref="EspacioImplante"/>).</summary>
        public void Elegir(EspacioImplante espacio, IReadOnlyList<bool> conImplante)
        {
            elegido = espacio;
            for (int i = 0; i < Espacios; i++)
            {
                instalados[i] = conImplante != null && i < conImplante.Count && conImplante[i];
            }
            Pintar();
        }

        /// <summary>Destello al instalar: el cuerpo entero brilla y se apaga en <see cref="ConfigPantallaClinica.DuracionDestello"/>.</summary>
        public void Destellar() => destello = 1f;

        /// <summary>Gira con el arrastre del ratón (px).</summary>
        public void Girar(Vector2 arrastre)
        {
            if (config == null)
            {
                return;
            }
            giro = Mathf.Repeat(giro - arrastre.x * config.Sensibilidad, GradosPorVuelta);
            inclinacion = Mathf.Clamp(inclinacion + arrastre.y * config.Sensibilidad, -config.InclinacionMaxima, config.InclinacionMaxima);
            esperaGiro = config.EsperaGiroSolo;
            AplicarGiro();
        }

        /// <summary>Acerca (positivo) o aleja con la rueda (unidades de scroll; 120 = un paso).</summary>
        public void Acercar(float rueda)
        {
            if (config == null)
            {
                return;
            }
            distancia = Mathf.Clamp(distancia - rueda / UnidadesPorPaso * config.PasoRueda, config.DistanciaMinima, config.DistanciaMaxima);
            ColocarCamara();
        }

        private void OnDestroy() => Ocultar();

        private void Update()
        {
            if (set == null)
            {
                return;
            }
            float dt = Time.unscaledDeltaTime;
            tiempo += dt;
            if (esperaGiro > 0f)
            {
                esperaGiro -= dt;
            }
            else
            {
                giro = Mathf.Repeat(giro + config.GiroSolo * dt, GradosPorVuelta);
                AplicarGiro();
            }
            if (destello > 0f)
            {
                destello = Mathf.Max(0f, destello - dt / config.DuracionDestello);
            }
            mira = Vector3.Lerp(mira, Objetivo(), 1f - Mathf.Exp(-config.RapidezEnfoque * dt));
            ColocarCamara();
            Pintar();
        }

        private float Pulso() => Mathf.Lerp(ResalteMinimo, 1f, 0.5f + 0.5f * Mathf.Sin(tiempo * config.PulsoEspacio * Mathf.PI * 2f));

        // Adónde mira la cámara (en el set): el centro del cuerpo, corrido hacia el espacio elegido. Solo la altura: el cuerpo gira.
        private Vector3 Objetivo()
        {
            float centro = config.AlturaCuerpo * 0.5f;
            float espacio = cuerpo != null ? set.InverseTransformPoint(CentroEspacio(elegido)).y : centro;
            return new Vector3(0f, Mathf.Lerp(centro, espacio, config.EnfoqueEspacio), 0f);
        }

        private void AplicarGiro()
        {
            if (pivote != null)
            {
                pivote.localRotation = Quaternion.Euler(inclinacion, giro, 0f);
            }
        }

        private void CrearCamara()
        {
            GameObject go = new GameObject("CamaraHologramaCuerpo");
            go.transform.SetParent(set, false);
            camara = go.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camara.fieldOfView = config.CampoVision;
            camara.nearClipPlane = CercaCamara;
            camara.farClipPlane = LejosCamara;
            camara.allowHDR = false;
            camara.targetTexture = textura;
            camara.depth = -50f;
            ColocarCamara();
        }

        private void ColocarCamara()
        {
            if (camara == null)
            {
                return;
            }
            camara.transform.localPosition = mira + new Vector3(0f, 0f, -distancia);
            camara.transform.LookAt(set.TransformPoint(mira));
        }

        // El cuerpo queda con los pies en el piso del set, centrado y de frente a la cámara (que mira hacia +Z).
        private void CrearCuerpo(Mesh malla)
        {
            GameObject go = new GameObject("Cuerpo");
            cuerpo = go.transform;
            cuerpo.SetParent(pivote, false);
            Bounds limites = malla.bounds;
            float escala = limites.size.y > 0f ? config.AlturaCuerpo / limites.size.y : 1f;
            cuerpo.localScale = Vector3.one * escala;
            cuerpo.localPosition = new Vector3(-limites.center.x * escala, -config.AlturaCuerpo * 0.5f - limites.min.y * escala, -limites.center.z * escala);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            render = go.AddComponent<MeshRenderer>();
            Material[] mats = new Material[Mathf.Max(1, malla.subMeshCount)];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = config.MaterialHolograma;
            }
            render.sharedMaterials = mats;
            ApagarSombras(render);
            for (int i = 0; i < Espacios; i++)
            {
                centros[i] = i < malla.subMeshCount ? malla.GetSubMesh(i).bounds.center : limites.center;
            }
            // Los brazos son dos: el marcador va al brazo que queda a la derecha de la pantalla al empezar (el cuerpo está girado 180°).
            if ((int)EspacioImplante.Brazos < malla.subMeshCount)
            {
                Bounds brazos = malla.GetSubMesh((int)EspacioImplante.Brazos).bounds;
                centros[(int)EspacioImplante.Brazos].x = Mathf.Lerp(brazos.center.x, brazos.min.x, FraccionBrazo);
            }
        }

        private void CrearPedestal()
        {
            if (config.MaterialHolograma == null)
            {
                return;
            }
            GameObject disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Pedestal";
            Destroy(disco.GetComponent<Collider>());
            disco.transform.SetParent(set, false);
            disco.transform.localPosition = Vector3.zero;
            disco.transform.localScale = new Vector3(AnchoPedestal, GrosorPedestal, AnchoPedestal);
            pedestal = disco.GetComponent<Renderer>();
            pedestal.sharedMaterial = config.MaterialHolograma;
            ApagarSombras(pedestal);
        }

        private void Pintar()
        {
            if (bloque == null || config == null)
            {
                return;
            }
            if (render != null)
            {
                int cuantas = render.sharedMaterials.Length;
                for (int i = 0; i < cuantas; i++)
                {
                    bool esElegido = i == (int)elegido;
                    render.GetPropertyBlock(bloque, i);
                    bloque.SetColor(IdColor, esElegido ? config.ColorEspacio : i < Espacios && instalados[i] ? config.ColorInstalado : config.ColorHolograma);
                    bloque.SetFloat(IdResalte, esElegido ? Pulso() : 0f);
                    bloque.SetFloat(IdDestello, destello);
                    bloque.SetFloat(IdAlfa, esElegido ? 1f : AlfaNormal);
                    render.SetPropertyBlock(bloque, i);
                }
            }
            if (pedestal != null)
            {
                pedestal.GetPropertyBlock(bloque);
                bloque.SetFloat(IdAlfa, AlfaPedestal);
                bloque.SetColor(IdColor, config.ColorHolograma);
                pedestal.SetPropertyBlock(bloque);
            }
        }

        private static void ApagarSombras(Renderer r)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private void Limpiar()
        {
            foreach (Object o in creados)
            {
                if (o != null)
                {
                    Destroy(o);
                }
            }
            creados.Clear();
            set = null;
            pivote = null;
            cuerpo = null;
            camara = null;
            render = null;
            pedestal = null;
        }
    }
}
