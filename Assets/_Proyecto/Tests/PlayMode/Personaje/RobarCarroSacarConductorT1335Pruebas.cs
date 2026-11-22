using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1335: al robar un carro del tráfico con conductor, el jugador se alinea junto a la puerta del conductor mirando al carro y juega PJ01_Carro_SacarConductor
    /// al mismo tiempo que el conductor juega PJ01_Conductor_SerSacado; al terminar el jugador va manejando. Necesita ConfigAnimacionParkour (T-1310).
    /// </summary>
    public class RobarCarroSacarConductorT1335Pruebas : BaseJugadorAPie
    {
        private const string RutaConfigTrafico = "Assets/_Proyecto/Trafico/Datos/ConfigTraficoCalle_Trafico.asset";
        private const string RutaPrefabCarro = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_V02.prefab";
        private const float MaxSegundos = 12f; // T-1561: SacarConductor v4 dura 4 s a su rapidez natural

        private readonly List<GameObject> extras = new List<GameObject>();
        private CarroTrafico carro;

        public override void Setup()
        {
            base.Setup();
            EventosLugares.LimpiarSuscriptores();
            EventosRuido.LimpiarSuscriptores();
            CarroTrafico.Activos.Clear();
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.Destroy(extra);
                }
            }
            extras.Clear();
            foreach (ConductorRobado c in Object.FindObjectsByType<ConductorRobado>(FindObjectsSortMode.None))
            {
                Object.Destroy(c.gameObject);
            }
            CarroTrafico.Activos.Clear();
            base.TearDown();
        }

        private IEnumerator CarroDetenido()
        {
            yield return Calentar();
            ConfigTraficoCalle configTrafico = AssetDatabase.LoadAssetAtPath<ConfigTraficoCalle>(RutaConfigTrafico);
            List<Vector2> centros = new List<Vector2> { new Vector2(0f, 0f), new Vector2(80f, 0f), new Vector2(0f, 80f), new Vector2(80f, 80f) };
            GrafoCalles calles = GrafoCalles.DeManzanas(centros, 80f, 0.2f);
            CarroTrafico molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCarro).GetComponent<CarroTrafico>();
            carro = Object.Instantiate(molde);
            extras.Add(carro.gameObject);
            carro.Preparar(configTrafico, calles, calles.MasCercano(new Vector3(40f, 0f, -40f)), new System.Random(3), () => jugador.transform.position);
            // Una cámara en escena para que Unity avance las animaciones.
            GameObject c = new GameObject("CamaraT1335");
            extras.Add(c);
            c.AddComponent<Camera>();
            yield return Esperar(0.3f);
            carro.AlChocar(5f, carro.transform.position);
            yield return Esperar(0.5f);
        }

        [UnityTest]
        public IEnumerator RobarConConductor_AlineaAPuertaJuegaClipsSincronizadosYTerminaManejando()
        {
            yield return CarroDetenido();
            Assert.IsNotNull(ConfigAnimacionParkour.Actual, "Falta Resources/ConfigAnimacionParkour (ImportarClipsRonda2T1310.Aplicar)");
            IMontable montable = carro.GetComponent<IMontable>();
            Assert.IsTrue(montable.PuedeSubir, "El carro detenido se puede subir");
            // Junto a la puerta, un poco apartado, mirando al carro.
            jugador.transform.position = montable.PuntoSubida - carro.transform.right * 0.3f;
            Physics.SyncTransforms();
            yield return null;
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.IsNotNull(anim);

            Assert.IsTrue(jugadorAPie.Subir(montable), "El jugador roba el carro");
            VisualPersonaje visualJugador = jugador.GetComponentInChildren<VisualPersonaje>(true);
            ReproductorClipUnico repJugador = visualJugador.GetComponent<ReproductorClipUnico>();
            GameObject conductor = GameObject.Find(CarroTrafico.NombreConductorRobado);
            Assert.IsNotNull(conductor, "Sacan al conductor");
            ReproductorClipUnico repConductor = conductor.GetComponentInChildren<ReproductorClipUnico>();
            Assert.IsNotNull(repJugador, "El jugador tiene su reproductor de clip");
            Assert.IsNotNull(repConductor, "El conductor tiene su reproductor de clip");

            ConfigAnimacionParkour config = ConfigAnimacionParkour.Actual;
            float inicioJugador = -1f;
            float inicioConductor = -1f;
            // T-1561 (decisión de Tony, 21-nov): con ConfigClipsNpcT1561 el conductor juega NPC_SacadoDelCarro_Huir y arranca 0.75 s (18 cuadros) DESPUÉS del clip del jugador.
            ConfigClipsNpcT1561 npc = ConfigClipsNpcT1561.Actual;
            bool conductorRetrasado = npc != null && npc.SacadoDelCarroHuir != null;
            AnimationClip clipConductor = conductorRetrasado ? npc.SacadoDelCarroHuir : config.ConductorSerSacado;
            float tope = Time.time + 1.6f;
            bool alineado = false;
            float distancia = 0f;
            float angulo = 180f;
            while (Time.time < tope)
            {
                if (inicioJugador < 0f && repJugador.ClipActual == config.CarroSacarConductor)
                {
                    inicioJugador = Time.time;
                }
                if (inicioConductor < 0f && repConductor.ClipActual == clipConductor)
                {
                    inicioConductor = Time.time;
                }
                if (!alineado && inicioJugador >= 0f && Time.time >= inicioJugador + 0.3f)
                {
                    // Ya pasó la interpolación (0.15 s): mide contra el punto de la puerta (el de subida arrimado al asiento, como AnimacionCarro).
                    alineado = true;
                    Transform modelo = anim.ModeloVisual;
                    Vector3 puertaMundo = ReglasEntradaCarro.PuntoPuerta(montable.PuntoSubida, montable.Asiento.position);
                    Vector3 dentro = modelo.position - puertaMundo;
                    dentro.y = 0f;
                    distancia = dentro.magnitude;
                    Vector3 haciaCarro = montable.Asiento.position - modelo.position;
                    haciaCarro.y = 0f;
                    angulo = Vector3.Angle(Vector3.ProjectOnPlane(modelo.forward, Vector3.up), haciaCarro);
                    if (FotoPruebaT1335.Pedida("CAPTURAR_T1335"))
                    {
                        // De lado: atrás del jugador y un poco afuera, para ver al jugador en la puerta y al conductor saliendo.
                        Vector3 afuera = -haciaCarro.normalized;
                        Vector3 lado = Vector3.Cross(Vector3.up, afuera);
                        FotoPruebaT1335.Desde(modelo.position + afuera * 3.2f + lado * 2.4f + Vector3.up * 1.7f, modelo.position + Vector3.up * 0.9f,
                            "docs/capturas/t-1335/sacar-conductor.png");
                    }
                }
                if (inicioJugador >= 0f && Time.time >= inicioJugador + 0.7f && FotoPruebaT1335.Pedida("CAPTURAR_T1335")
                    && !System.IO.File.Exists("docs/capturas/t-1335/sacar-conductor-2.png"))
                {
                    Transform m = anim.ModeloVisual;
                    Vector3 afuera = m.position - montable.Asiento.position;
                    afuera.y = 0f;
                    afuera.Normalize();
                    FotoPruebaT1335.Desde(m.position + afuera * 3.2f + Vector3.Cross(Vector3.up, afuera) * 2.4f + Vector3.up * 1.7f, m.position + Vector3.up * 0.9f,
                        "docs/capturas/t-1335/sacar-conductor-2.png");
                }
                yield return null;
            }

            Assert.GreaterOrEqual(inicioJugador, 0f, "El jugador juega PJ01_Carro_SacarConductor");
            Assert.GreaterOrEqual(inicioConductor, 0f, "El conductor juega PJ01_Conductor_SerSacado");
            Assert.AreEqual(inicioJugador + (conductorRetrasado ? ReglasTransicionesT1561.SegundosRetrasoConductor : 0f), inicioConductor, 0.15f,
                conductorRetrasado ? "El del conductor arranca 18 cuadros (0.75 s) después que el del jugador" : "Los dos clips empiezan juntos");
            Assert.IsTrue(alineado, "Se midió la alineación");
            Assert.Less(distancia, 0.35f, "El jugador queda junto al punto de la puerta (" + distancia.ToString("F2") + " m)");
            Assert.Less(angulo, 25f, "Y mira hacia el carro (" + angulo.ToString("F1") + " grados)");

            float fin = Time.time + MaxSegundos;
            while (anim.Fase != AnimacionCarro.FaseCarro.Dentro && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase, "Al terminar, el jugador va manejando");
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
            Assert.IsFalse(anim.SacandoConductor, "Ya no juega el clip de sacar");
        }
    }
}
