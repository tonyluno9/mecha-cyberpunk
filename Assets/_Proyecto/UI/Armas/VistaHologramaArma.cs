using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.UI.Armas
{
    /// <summary>
    /// El holograma del banco de armas (T-814): un set lejos de la ciudad (<see cref="ConfigMenuArmas.PosicionSet"/>) con su cámara que dibuja
    /// en una RenderTexture (<see cref="Textura"/>) el arma con el shader holográfico. Con modelo 3D copia sus mallas (sin scripts ni
    /// colisiones), lo deja de 1 m de largo y separa la pieza elegida (vista explotada); lo que el modelo no trae no se inventa.
    /// simples. Sin modelo gira la imagen holográfica en un quad. Se gira con <see cref="Girar"/>, se acerca con <see cref="Acercar"/> y gira
    /// solo cuando no se toca. Sin capas propias: el set está a 500 m bajo la ciudad y la cámara solo ve 10 m.
    /// </summary>
    public class VistaHologramaArma : MonoBehaviour
    {
        private const float LargoArma = 1f;
        private const float AlturaCamara = 0.12f;
        private const float CercaCamara = 0.05f;
        private const float LejosCamara = 10f;
        private const float BajoPedestal = 0.42f;
        private const float AnchoPedestal = 1.25f;
        private const float GrosorPedestal = 0.004f;
        private const float AlfaPedestal = 0.22f;
        private const float AlfaBloqueada = 0.3f;
        private const float AnchoImagen = 2.2f;
        private const float InclinacionInicial = 12f;
        private const float GiroInicial = -18f;
        private const float MinimoDireccion = 0.0001f;
        private const float MargenPedestal = 0.03f;
        private const int ProfundidadTextura = 24;
        private const float MargenEscaneo = 0.08f;
        // La imagen (un quad) no da la vuelta: se mece de lado a lado y el arrastre la gira hasta LimiteImagen.
        private const float AmplitudVaiven = 22f;
        private const float VelocidadVaiven = 0.7f;
        private const float LimiteImagen = 40f;

        private static readonly int IdResalte = Shader.PropertyToID("_Resalte");
        private static readonly int IdDestello = Shader.PropertyToID("_Destello");
        private static readonly int IdAlfa = Shader.PropertyToID("_Alfa");
        private static readonly int IdTextura = Shader.PropertyToID("_MainTex");
        private static readonly int IdModoImagen = Shader.PropertyToID("_ModoImagen");
        private static readonly int IdCull = Shader.PropertyToID("_Cull");
        private static readonly int IdColor = Shader.PropertyToID("_Color");
        private static readonly int IdBarridoDir = Shader.PropertyToID("_BarridoDir");
        private static readonly int IdBarridoPos = Shader.PropertyToID("_BarridoPos");
        private static readonly int IdBarridoFuerza = Shader.PropertyToID("_BarridoFuerza");

        private sealed class Pieza
        {
            public Transform Transform;
            public Vector3 Base;
            public Vector3 Separado;
            public float Abierta;
            public readonly List<Renderer> Renderers = new List<Renderer>();
        }

        private readonly Dictionary<PiezaArma, Pieza> piezas = new Dictionary<PiezaArma, Pieza>();
        private readonly List<Renderer> todos = new List<Renderer>();
        private readonly Dictionary<PiezaArma, EstadoPieza> estados = new Dictionary<PiezaArma, EstadoPieza>();
        private readonly List<Object> creados = new List<Object>();
        private MaterialPropertyBlock bloque;
        private ConfigMenuArmas config;
        private RenderTexture textura;
        private Transform set;
        private Transform pivote;
        private Transform contenido;
        private Camera camara;
        private Renderer pedestal;
        private Material materialImagen;
        private PiezaArma elegida;
        private float giro = GiroInicial;
        private float inclinacion = InclinacionInicial;
        private float distancia;
        private float esperaGiro;
        private float destello;
        private float vaiven;
        private bool conModelo;
        private Tween tweenEscaneo;
        private bool escaneando;
        private float avanceEscaneo;
        private float inicioEscaneo;
        private float finEscaneo;
        private PiezaArma? piezaRegresada;

        public RenderTexture Textura => textura;
        public Camera Camara => camara;
        /// <summary>True si se está viendo el modelo 3D (false: la imagen).</summary>
        public bool ConModelo => conModelo;
        public float Giro => giro;
        public float Inclinacion => inclinacion;
        public float Distancia => distancia;
        public PiezaArma Elegida => elegida;
        /// <summary>T-895: true mientras la franja de escaneo recorre el arma.</summary>
        public bool Escaneando => escaneando;
        /// <summary>T-895: cuánto ha recorrido la franja (0 a 1).</summary>
        public float AvanceEscaneo => avanceEscaneo;

        /// <summary>Qué tan separada está la pieza (0 en su lugar, 1 fuera). 0 si el arma no tiene esa pieza.</summary>
        public float Separacion(PiezaArma pieza) => piezas.TryGetValue(pieza, out Pieza p) ? p.Abierta : 0f;

        /// <summary>True si el modelo actual tiene esa pieza.</summary>
        public bool TienePieza(PiezaArma pieza) => piezas.ContainsKey(pieza);

        /// <summary>La posición en el mundo de la pieza (para las pruebas), o la del set.</summary>
        public Vector3 PosicionPieza(PiezaArma pieza) => piezas.TryGetValue(pieza, out Pieza p) ? p.Transform.position : config.PosicionSet;

        /// <summary>El centro visible de la pieza en el mundo; false si el arma no la tiene.</summary>
        public bool CentroPieza(PiezaArma pieza, out Vector3 centro)
        {
            centro = Vector3.zero;
            if (!piezas.TryGetValue(pieza, out Pieza p) || p.Renderers.Count == 0)
            {
                return false;
            }
            centro = LimitesDe(p.Renderers).center;
            return true;
        }

        /// <summary>Arma el set con el arma; si ya había uno, lo rehace.</summary>
        public void Mostrar(EntradaArmaMenu entrada, ConfigMenuArmas configuracion)
        {
            config = configuracion;
            Limpiar();
            bloque ??= new MaterialPropertyBlock();
            if (textura == null)
            {
                textura = new RenderTexture(config.AnchoTextura, config.AltoTextura, ProfundidadTextura, RenderTextureFormat.ARGB32)
                {
                    name = "HologramaArma", antiAliasing = 4
                };
                textura.Create();
            }
            distancia = config.Distancia;
            giro = GiroInicial;
            inclinacion = InclinacionInicial;
            vaiven = 0f;
            set = new GameObject("SetHologramaArma").transform;
            set.SetParent(transform, false);
            set.position = config.PosicionSet;
            creados.Add(set.gameObject);
            pivote = new GameObject("Pivote").transform;
            pivote.SetParent(set, false);
            contenido = new GameObject("Contenido").transform;
            contenido.SetParent(pivote, false);
            CrearCamara();
            conModelo = entrada != null && entrada.Modelo != null && config.MaterialHolograma != null;
            if (!conModelo)
            {
                giro = 0f;
                inclinacion = 0f;
            }
            if (conModelo)
            {
                CopiarModelo(entrada);
            }
            else
            {
                CrearImagen(entrada);
            }
            CrearPedestal();
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

        /// <summary>Cambia la pieza separada y resaltada; <paramref name="estadosPiezas"/> apaga las bloqueadas.</summary>
        public void ElegirPieza(PiezaArma pieza, IReadOnlyDictionary<PiezaArma, EstadoPieza> estadosPiezas)
        {
            elegida = pieza;
            if (piezaRegresada.HasValue && piezaRegresada.Value != pieza)
            {
                piezaRegresada = null;
            }
            estados.Clear();
            if (estadosPiezas != null)
            {
                foreach (KeyValuePair<PiezaArma, EstadoPieza> par in estadosPiezas)
                {
                    estados[par.Key] = par.Value;
                }
            }
            Pintar();
        }

        /// <summary>Destello al comprar: la pieza elegida brilla y se apaga en <see cref="ConfigMenuArmas.DuracionDestello"/>.</summary>
        public void Destellar() => destello = 1f;

        /// <summary>
        /// T-895: al mejorar, la franja recorre el arma de punta a punta mientras la pieza elegida vuelve a su lugar; al terminar la pieza destella y
        /// avisa <paramref name="alTerminar"/>. Si se cambia de arma o se cierra el menú a la mitad, se cancela sin avisar.
        /// </summary>
        public void Escanear(Action alTerminar)
        {
            if (set == null || config == null)
            {
                alTerminar?.Invoke();
                return;
            }
            tweenEscaneo?.Kill();
            Bounds arma = Limites();
            inicioEscaneo = arma.min.x - MargenEscaneo;
            finEscaneo = arma.max.x + MargenEscaneo;
            piezaRegresada = elegida;
            escaneando = true;
            avanceEscaneo = 0f;
            tweenEscaneo = DOTween.To(() => avanceEscaneo, v => { avanceEscaneo = v; Pintar(); }, 1f, config.DuracionEscaneo)
                .SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(this)
                .OnComplete(() =>
                {
                    escaneando = false;
                    Destellar();
                    Pintar();
                    alTerminar?.Invoke();
                });
        }

        /// <summary>Gira con el arrastre del ratón (px).</summary>
        public void Girar(Vector2 arrastre)
        {
            giro -= arrastre.x * config.Sensibilidad;
            float limite = conModelo ? config.InclinacionMaxima : LimiteImagen * 0.5f;
            inclinacion = Mathf.Clamp(inclinacion + arrastre.y * config.Sensibilidad, -limite, limite);
            if (!conModelo)
            {
                giro = Mathf.Clamp(Mathf.DeltaAngle(0f, giro), -LimiteImagen, LimiteImagen);
                vaiven = Mathf.Asin(Mathf.Clamp(giro / AmplitudVaiven, -1f, 1f)) / VelocidadVaiven;
            }
            esperaGiro = config.EsperaGiroSolo;
            AplicarGiro();
        }

        /// <summary>Acerca (positivo) o aleja con la rueda (unidades de scroll; 120 = un paso).</summary>
        public void Acercar(float rueda)
        {
            const float UnidadesPorPaso = 120f;
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
            if (esperaGiro > 0f)
            {
                esperaGiro -= dt;
            }
            else
            {
                vaiven += dt;
                giro = conModelo ? Mathf.Repeat(giro + config.GiroSolo * dt, 360f) : Mathf.Sin(vaiven * VelocidadVaiven) * AmplitudVaiven;
                AplicarGiro();
            }
            float paso = 1f - Mathf.Exp(-config.RapidezPieza * dt);
            foreach (KeyValuePair<PiezaArma, Pieza> par in piezas)
            {
                Pieza p = par.Value;
                bool afuera = par.Key == elegida && piezaRegresada != par.Key;
                p.Abierta = Mathf.Lerp(p.Abierta, afuera ? 1f : 0f, paso);
                p.Transform.localPosition = Vector3.Lerp(p.Base, p.Separado, p.Abierta);
            }
            if (destello > 0f)
            {
                destello = Mathf.Max(0f, destello - dt / config.DuracionDestello);
                Pintar();
            }
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
            GameObject go = new GameObject("CamaraHolograma");
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
            camara.transform.localPosition = new Vector3(0f, AlturaCamara, -distancia);
            camara.transform.LookAt(pivote != null ? pivote.position : set.position);
        }

        // Copia las mallas del prefab (no lo instancia: sin scripts, colisiones ni animaciones) y deja el arma de 1 m, centrada y de lado.
        private void CopiarModelo(EntradaArmaMenu entrada)
        {
            Transform origen = entrada.Modelo.transform;
            Transform raiz = new GameObject("Modelo").transform;
            raiz.SetParent(contenido, false);
            Dictionary<string, Transform> porNombre = new Dictionary<string, Transform>();
            Matrix4x4 aRaiz = origen.worldToLocalMatrix;
            foreach (MeshFilter mf in origen.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null)
                {
                    continue;
                }
                AgregarMalla(raiz, mf.name, mf.sharedMesh, aRaiz * mf.transform.localToWorldMatrix, porNombre);
            }
            foreach (SkinnedMeshRenderer sk in origen.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (sk.sharedMesh != null)
                {
                    AgregarMalla(raiz, sk.name, sk.sharedMesh, aRaiz * sk.transform.localToWorldMatrix, porNombre);
                }
            }
            Bounds limites = Limites();
            // De lado: el largo del arma sobre X.
            if (limites.size.z > limites.size.x)
            {
                raiz.localRotation = Quaternion.Euler(0f, 90f, 0f);
                limites = Limites();
            }
            float largo = Mathf.Max(limites.size.x, Mathf.Max(limites.size.y, limites.size.z));
            if (largo > 0f)
            {
                raiz.localScale = Vector3.one * (LargoArma / largo);
                limites = Limites();
                raiz.position += pivote.position - limites.center;
                limites = Limites();
            }
            foreach (PiezaArma pieza in new[] { PiezaArma.Cargador, PiezaArma.Canon, PiezaArma.Mira })
            {
                string nombre = entrada.NombrePieza(pieza);
                if (!string.IsNullOrEmpty(nombre) && porNombre.TryGetValue(nombre, out Transform t))
                {
                    RegistrarPieza(pieza, t, limites);
                }
            }
            // T-971: lo que el modelo no trae no se inventa (ya ningún arma usa mallas simples): la escopeta no lleva mira.
        }

        private void AgregarMalla(Transform raiz, string nombre, Mesh malla, Matrix4x4 matriz, Dictionary<string, Transform> porNombre)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(raiz, false);
            go.transform.localPosition = matriz.GetColumn(3);
            go.transform.localRotation = matriz.rotation;
            go.transform.localScale = matriz.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            Material[] mats = new Material[Mathf.Max(1, malla.subMeshCount)];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = config.MaterialHolograma;
            }
            r.sharedMaterials = mats;
            ApagarSombras(r);
            todos.Add(r);
            porNombre[nombre] = go.transform;
        }

        // La pieza sale hacia afuera desde el centro del arma (el cargador baja, la corredera y la mira suben).
        private void RegistrarPieza(PiezaArma pieza, Transform t, Bounds arma)
        {
            Pieza p = new Pieza { Transform = t, Base = t.localPosition };
            p.Renderers.AddRange(t.GetComponentsInChildren<Renderer>(true));
            Bounds propios = LimitesDe(p.Renderers);
            Vector3 direccion = propios.center - arma.center;
            switch (pieza)
            {
                case PiezaArma.Canon:
                    direccion = new Vector3(direccion.x >= 0f ? 1f : -1f, 0.55f, 0f);
                    break;
                case PiezaArma.Mira:
                    direccion = Vector3.up;
                    break;
                default:
                    direccion = new Vector3(direccion.x * 0.15f, -1f, 0f);
                    break;
            }
            if (direccion.sqrMagnitude < MinimoDireccion)
            {
                direccion = Vector3.up;
            }
            Vector3 mundo = direccion.normalized * config.Pieza(pieza).Separacion * LargoArma;
            // T-942: hacia abajo la pieza nunca cruza el pedestal.
            float techoPedestal = set.position.y - BajoPedestal + GrosorPedestal;
            mundo *= CalculoPiezasPrimitivas.FactorSobrePedestal(mundo.y, propios.min.y, techoPedestal, MargenPedestal);
            Transform padre = t.parent;
            p.Separado = p.Base + (padre != null ? padre.InverseTransformVector(mundo) : mundo);
            piezas[pieza] = p;
        }

        private static Transform BuscarHijo(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private void CrearImagen(EntradaArmaMenu entrada)
        {
            Texture2D imagen = entrada != null ? entrada.Imagen : null;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ImagenHolografica";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(contenido, false);
            float aspecto = imagen != null && imagen.height > 0 ? (float)imagen.width / imagen.height : 16f / 9f;
            quad.transform.localScale = new Vector3(AnchoImagen, AnchoImagen / aspecto, 1f);
            Renderer r = quad.GetComponent<Renderer>();
            if (config.MaterialHolograma != null)
            {
                materialImagen = new Material(config.MaterialHolograma) { name = "HologramaImagen" };
                materialImagen.SetFloat(IdCull, 0f);
                materialImagen.SetFloat(IdModoImagen, 1f);
                if (imagen != null)
                {
                    materialImagen.SetTexture(IdTextura, imagen);
                }
                creados.Add(materialImagen);
                r.sharedMaterial = materialImagen;
            }
            ApagarSombras(r);
            todos.Add(r);
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
            disco.transform.localPosition = new Vector3(0f, -BajoPedestal, 0f);
            disco.transform.localScale = new Vector3(AnchoPedestal, GrosorPedestal, AnchoPedestal * 0.55f);
            pedestal = disco.GetComponent<Renderer>();
            pedestal.sharedMaterial = config.MaterialHolograma;
            ApagarSombras(pedestal);
        }

        private void Pintar()
        {
            if (bloque == null)
            {
                return;
            }
            foreach (Renderer r in todos)
            {
                if (r == null)
                {
                    continue;
                }
                PiezaArma? de = PiezaDe(r);
                bool esElegida = de.HasValue && de.Value == elegida;
                bool bloqueada = de.HasValue && estados.TryGetValue(de.Value, out EstadoPieza e) && e == EstadoPieza.Bloqueada;
                r.GetPropertyBlock(bloque);
                bloque.SetFloat(IdResalte, esElegida && conModelo ? 1f : 0f);
                bloque.SetFloat(IdDestello, esElegida || !conModelo ? destello : destello * 0.25f);
                bloque.SetFloat(IdAlfa, bloqueada && !esElegida ? AlfaBloqueada : 1f);
                bloque.SetVector(IdBarridoDir, Vector3.right);
                bloque.SetFloat(IdBarridoPos, Mathf.Lerp(inicioEscaneo, finEscaneo, avanceEscaneo));
                bloque.SetFloat(IdBarridoFuerza, escaneando ? 1f : 0f);
                r.SetPropertyBlock(bloque);
            }
            if (pedestal != null)
            {
                pedestal.GetPropertyBlock(bloque);
                bloque.SetFloat(IdAlfa, AlfaPedestal);
                bloque.SetColor(IdColor, config.ColorHolograma);
                pedestal.SetPropertyBlock(bloque);
            }
        }

        private PiezaArma? PiezaDe(Renderer r)
        {
            foreach (KeyValuePair<PiezaArma, Pieza> par in piezas)
            {
                if (par.Value.Renderers.Contains(r))
                {
                    return par.Key;
                }
            }
            return null;
        }

        private Bounds Limites() => LimitesDe(todos);

        private static Bounds LimitesDe(List<Renderer> lista)
        {
            bool primero = true;
            Bounds b = new Bounds();
            foreach (Renderer r in lista)
            {
                if (r == null)
                {
                    continue;
                }
                if (primero)
                {
                    b = r.bounds;
                    primero = false;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
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
            tweenEscaneo?.Kill();
            tweenEscaneo = null;
            escaneando = false;
            avanceEscaneo = 0f;
            piezaRegresada = null;
            foreach (Object o in creados)
            {
                if (o != null)
                {
                    Destroy(o);
                }
            }
            creados.Clear();
            piezas.Clear();
            todos.Clear();
            set = null;
            pivote = null;
            contenido = null;
            camara = null;
            pedestal = null;
            materialImagen = null;
        }
    }
}
