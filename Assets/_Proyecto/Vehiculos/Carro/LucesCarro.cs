using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Faros y calaveras del carro (T-590). Dos faros delanteros (reflectores blancos) y dos luces rojas atrás. T-659: ya no llevan lentes
    /// (los cuadros que fingían los faros): solo las luces; el modelo del carro es una sola malla con una sola textura y no tiene faros aparte.
    /// Quién las prende:
    /// - El conductor con la tecla L (acción "Luces" de ControlesCarro): <see cref="Alternar"/>. Al bajarse vuelve al modo automático.
    /// - Automático: de noche (reloj del mundo, <see cref="IRelojMundo.EsDeNoche"/>) se prenden en el tráfico y en el carro que alguien maneja;
    ///   un carro estacionado vacío las lleva apagadas. Un carro explotado no las prende.
    /// El brillo sube y baja en ~1 s (no de golpe) y las luces reales solo existen cerca de la cámara (decenas de carros a la vez).
    /// Las motos no las llevan (T-660).
    /// Lo agrega <see cref="CarroViejo"/> al nacer; mide las luces con las cajas de choque del carro.
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class LucesCarro : MonoBehaviour
    {
        private const float AnchoPorDefecto = 1.8f;
        private const float LargoPorDefecto = 4f;
        private const float AlturaFaros = 0.65f;
        private const float FraccionSeparacionFaros = 0.33f;
        private const float RangoFaros = 22f;
        private const float AnguloFaros = 70f;
        private const float IntensidadFaros = 12f;
        private const float RangoCalaveras = 4f;
        private const float IntensidadCalaveras = 2f;
        private const float SegundosDeFundido = 1f;
        private const float SegundosEntreRevisiones = 0.4f;
        private const float DistanciaMaximaLuces = 80f;
        private const float InclinacionFaros = 4f;
        private static readonly Color ColorFaro = new Color(1f, 0.96f, 0.82f);
        private static readonly Color ColorCalavera = new Color(1f, 0.1f, 0.08f);

        private CarroViejo carro;
        private DanioCarro danio;
        private Light[] luces;
        private Color[] colores;
        private float[] intensidadMaxima;
        private float brillo;
        private float proximaRevision;
        private bool cercaDeCamara = true;
        private bool manual;
        private bool manualPrendido;
        private bool eraOcupado;

        /// <summary>Brillo actual de 0 (apagadas) a 1 (prendidas del todo).</summary>
        public float Brillo => brillo;

        /// <summary>Si las luces están prendidas o prendiéndose (la meta, no el brillo del momento).</summary>
        public bool Encendidas => Meta() > 0.5f;

        /// <summary>Cuántas luces reales (Light) tiene: 2 faros y 2 calaveras.</summary>
        public Light[] Luces => luces;

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            danio = GetComponent<DanioCarro>();
            CrearLuces();
            Aplicar(0f);
        }

        /// <summary>La tecla de luces: pasa a modo manual y cambia el estado de ahora. Un carro explotado no las prende.</summary>
        public void Alternar()
        {
            if (danio != null && danio.Explotado)
            {
                return;
            }
            bool prendidasAhora = Meta() > 0.5f;
            manual = true;
            manualPrendido = !prendidasAhora;
        }

        private float Meta()
        {
            if (danio != null && danio.Explotado)
            {
                return 0f;
            }
            if (manual)
            {
                return manualPrendido ? 1f : 0f;
            }
            return EsDeNoche() && (carro.Ocupado || carro.EstaEnTrafico) ? 1f : 0f;
        }

        private static bool EsDeNoche()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            return reloj != null && reloj.EsDeNoche;
        }

        private void Update()
        {
            if (eraOcupado && !carro.Ocupado)
            {
                manual = false;
            }
            eraOcupado = carro.Ocupado;

            float meta = Meta();
            if (Mathf.Approximately(meta, brillo))
            {
                return;
            }
            RevisarCamara();
            brillo = Mathf.MoveTowards(brillo, meta, Time.deltaTime / SegundosDeFundido);
            Aplicar(brillo);
        }

        // Solo se encienden las luces reales cerca de la cámara.
        private void RevisarCamara()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosEntreRevisiones;
            Camera camara = Camera.main;
            cercaDeCamara = camara == null
                || (camara.transform.position - transform.position).sqrMagnitude < DistanciaMaximaLuces * DistanciaMaximaLuces;
        }

        private void Aplicar(float valor)
        {
            bool visible = valor > 0.001f;
            for (int i = 0; i < luces.Length; i++)
            {
                luces[i].enabled = visible && cercaDeCamara;
                luces[i].intensity = intensidadMaxima[i] * valor;
            }
        }

        private void CrearLuces()
        {
            Bounds caja = MedirCaja();
            float ancho = caja.size.x;
            float adelante = caja.max.z;
            float atras = caja.min.z;
            float x = ancho * FraccionSeparacionFaros;
            float y = Mathf.Max(AlturaFaros, caja.min.y + caja.size.y * 0.35f);
            luces = new Light[4];
            colores = new Color[4];
            intensidadMaxima = new float[4];
            Crear(0, "Faro_I", new Vector3(-x, y, adelante), true);
            Crear(1, "Faro_D", new Vector3(x, y, adelante), true);
            Crear(2, "Calavera_I", new Vector3(-x, y, atras), false);
            Crear(3, "Calavera_D", new Vector3(x, y, atras), false);
        }

        private void Crear(int indice, string nombre, Vector3 posicion, bool faro)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.SetParent(transform, false);
            objeto.transform.localPosition = posicion;
            objeto.transform.localRotation = faro
                ? Quaternion.Euler(InclinacionFaros, 0f, 0f)
                : Quaternion.Euler(0f, 180f, 0f);
            Light luz = objeto.AddComponent<Light>();
            luz.type = faro ? LightType.Spot : LightType.Point;
            luz.color = faro ? ColorFaro : ColorCalavera;
            luz.range = faro ? RangoFaros : RangoCalaveras;
            luz.spotAngle = AnguloFaros;
            luz.shadows = LightShadows.None;
            luz.enabled = false;
            luces[indice] = luz;
            intensidadMaxima[indice] = faro ? IntensidadFaros : IntensidadCalaveras;
            colores[indice] = faro ? ColorFaro : ColorCalavera;
        }

        // Cajas de choque del carro, en el espacio local del carro (el visual cambia de modelo a modelo, las cajas están a la medida).
        private Bounds MedirCaja()
        {
            bool hay = false;
            Bounds total = new Bounds(Vector3.up * AlturaFaros, new Vector3(AnchoPorDefecto, 1f, LargoPorDefecto));
            foreach (BoxCollider c in GetComponentsInChildren<BoxCollider>(true))
            {
                if (c.isTrigger)
                {
                    continue;
                }
                for (int i = 0; i < 8; i++)
                {
                    Vector3 esquina = c.center + Vector3.Scale(c.size * 0.5f,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = transform.InverseTransformPoint(c.transform.TransformPoint(esquina));
                    if (!hay)
                    {
                        total = new Bounds(local, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        total.Encapsulate(local);
                    }
                }
            }
            return total;
        }
    }
}
