using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-167: las piezas puestas cambian al mecha (velocidad, golpe, vida) y se guardan con él.
    /// Prefab real MechaEstacionado.prefab, arrancado en Directo para poder pilotarlo sin la secuencia de encendido
    /// (lo que se prueba aquí son las piezas, no el encendido), con entrada simulada.
    /// </summary>
    public class EquipoMechaPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const string RutaDatosJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float VidaSaco = 1000f;
        private const float SegundosCaminando = 3f;
        private const float VentanaMedicion = 1f;
        private const float ToleranciaVelocidad = 0.05f;
        private const float Tolerancia = 1e-3f;
        private const float DanioInicial = 40f;
        private const float DanioGuardado = 25f;
        private const int CambiosSeguidos = 20;
        private const string SufijoEjecucion = "(ejecución)";

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private GameObject saco;
        private DatosMecha datosSaco;
        private GameObject raizServicio;
        private ServicioGuardado servicio;
        private string carpeta;
        private CatalogoPiezas catalogo;
        private EquipoMecha equipo;
        private VidaMecha vida;
        private float velocidadMedida;
        private readonly List<string> avisos = new List<string>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            CambioEscena.ConsumirTransicion();
            carpeta = Path.Combine(Application.temporaryCachePath, "equipo_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            Assert.IsNotNull(catalogo, "Existe CatalogoPiezas_Base");
            avisos.Clear();
            Application.logMessageReceived += AlRegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlRegistrarLog;
            DestruirMecha();
            if (piso != null) Object.Destroy(piso);
            if (saco != null) Object.Destroy(saco);
            if (datosSaco != null) Object.Destroy(datosSaco);
            if (raizServicio != null) Object.Destroy(raizServicio);
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private void AlRegistrarLog(string texto, string pila, LogType tipo)
        {
            if (tipo == LogType.Warning || tipo == LogType.Error || tipo == LogType.Exception) avisos.Add(texto);
        }

        private void CrearPiso()
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        // Instancia el mecha estacionado con arranque Directo: se instancia bajo un padre apagado, se cambia el arranque y
        // se suelta el padre, así el Awake ya ve el arranque Directo.
        private void CrearMecha()
        {
            var padre = new GameObject("PadreApagado");
            padre.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity, padre.transform);
            var so = new SerializedObject(mecha.GetComponent<MontajeMecha>());
            so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha.transform.SetParent(null);
            Object.Destroy(padre);
            equipo = mecha.GetComponent<EquipoMecha>();
            vida = mecha.GetComponent<VidaMecha>();
            Assert.IsNotNull(equipo, "MechaEstacionado lleva EquipoMecha");
        }

        private void DestruirMecha()
        {
            if (mecha != null)
            {
                Object.DestroyImmediate(mecha);
                mecha = null;
            }
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        private void CrearSaco()
        {
            saco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            saco.name = "SacoEquipo";
            saco.transform.localScale = new Vector3(1f, 8f, 1f);
            saco.transform.position = new Vector3(0f, 4f, 2.5f);
            VidaMecha vidaSaco = saco.AddComponent<VidaMecha>();
            saco.AddComponent<SacoDePrueba>();
            datosSaco = DatosMecha.CrearEnEjecucion(new ValoresMecha
            {
                nombre = "Saco", vidaMaxima = VidaSaco, velocidadMaxima = 1f, aceleracion = 1f, desaceleracion = 1f,
                velocidadGiroMaxima = 1f, danioGolpe = 1f, alcanceGolpe = 1f, preparacionGolpe = 0.1f, recargaGolpe = 0.1f,
                tiempoSubirGuardia = 0.1f, reduccionBloqueo = 0.5f, ventanaBloqueoPerfecto = 0.1f
            });
            vidaSaco.Configurar(datosSaco);
            Physics.SyncTransforms();
        }

        private DatosPieza Pieza(string id)
        {
            DatosPieza p = catalogo.Buscar(id);
            Assert.IsNotNull(p, "Existe la pieza " + id);
            return p;
        }

        // Camina hacia adelante SegundosCaminando y mide la velocidad en el último segundo (en velocidadMedida).
        private IEnumerator MedirVelocidad()
        {
            Press(teclado.wKey);
            float t = 0f;
            float inicioVentana = 0f;
            float zInicio = 0f;
            bool abierta = false;
            while (t < SegundosCaminando)
            {
                yield return null;
                t += Time.deltaTime;
                if (!abierta && t >= SegundosCaminando - VentanaMedicion)
                {
                    abierta = true;
                    inicioVentana = t;
                    zInicio = mecha.transform.position.z;
                }
            }
            velocidadMedida = (mecha.transform.position.z - zInicio) / (t - inicioVentana);
            Release(teclado.wKey);
            yield return null;
        }

        private static string[] Ids(EquipoMecha e) => e.Puestas.Select(p => p.Id).ToArray();

        private static int ContarDatosEjecucion() =>
            Resources.FindObjectsOfTypeAll<DatosMecha>().Count(d => d.name.EndsWith(SufijoEjecucion));

        [UnityTest]
        public IEnumerator Inicio_LasCincoSonDeSerie_YLosValoresSonLosDelJugador()
        {
            CrearMecha();
            yield return Calentar();
            DatosMecha jugador = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosJugador);

            Assert.AreEqual(5, equipo.Puestas.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(RamaPieza.DeSerie, equipo.Puestas[i].Rama, "Ranura " + i);
                Assert.AreEqual((RanuraPieza)i, equipo.Puestas[i].Ranura);
            }
            Assert.AreEqual(jugador.Valores.vidaMaxima, vida.Datos.VidaMaxima, Tolerancia);
            Assert.AreEqual(jugador.Valores.velocidadMaxima, vida.Datos.VelocidadMaxima, Tolerancia);
            Assert.AreEqual(jugador.Valores.danioGolpe, vida.Datos.DanioGolpe, Tolerancia);
            Assert.AreEqual(jugador.Valores.recargaGolpe, vida.Datos.RecargaGolpe, Tolerancia);
            Assert.AreEqual(jugador.Valores.vidaMaxima, equipo.Valores.vidaMaxima, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Piernas_CuerpoACuerpoII_CorrenA4Punto5_YDeSerieOtraVezA4()
        {
            CrearPiso();
            CrearMecha();
            yield return Calentar();

            DatosPieza quitada = equipo.Equipar(Pieza("pieza_piernas_cuerpoacuerpo_2"));
            Assert.AreEqual(RamaPieza.DeSerie, quitada.Rama, "Devuelve la que se quitó");
            yield return MedirVelocidad();
            Assert.AreEqual(4.5f, velocidadMedida, ToleranciaVelocidad);

            yield return Esperar(1f);
            Assert.IsNotNull(equipo.Equipar(catalogo.DeSerie(RanuraPieza.Piernas)));
            yield return MedirVelocidad();
            Assert.AreEqual(4.0f, velocidadMedida, ToleranciaVelocidad);
        }

        [UnityTest]
        public IEnumerator Golpe_ConBrazoDerechoMilitarI_QuitaTrece()
        {
            CrearPiso();
            CrearMecha();
            yield return Calentar();
            equipo.Equipar(Pieza("pieza_brazoder_militar_1"));
            CrearSaco();
            yield return null;

            yield return Clic();
            yield return Esperar(0.6f);

            Assert.AreEqual(VidaSaco - 13f, saco.GetComponent<VidaMecha>().Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Torso_ConDanio_ConservaLosPuntos_AlPonerYAlQuitar()
        {
            CrearMecha();
            yield return Calentar();
            vida.FijarVida(vida.Maxima - DanioInicial);

            equipo.Equipar(Pieza("pieza_torso_cuerpoacuerpo_1"));
            Assert.AreEqual(130f, vida.Maxima, Tolerancia);
            Assert.AreEqual(90f, vida.Actual, Tolerancia);

            equipo.Equipar(catalogo.DeSerie(RanuraPieza.Torso));
            Assert.AreEqual(100f, vida.Maxima, Tolerancia);
            Assert.AreEqual(60f, vida.Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Equipar_MismaPiezaONull_DevuelveNullYNoAvisa()
        {
            CrearMecha();
            yield return Calentar();
            int avisosCambio = 0;
            equipo.Cambio += () => avisosCambio++;

            Assert.IsNull(equipo.Equipar(catalogo.DeSerie(RanuraPieza.Torso)));
            Assert.IsNull(equipo.Equipar(null));
            Assert.AreEqual(0, avisosCambio);
            equipo.Equipar(Pieza("pieza_torso_militar_1"));
            Assert.AreEqual(1, avisosCambio);
        }

        private ServicioGuardado CrearServicio()
        {
            raizServicio = new GameObject("ServicioGuardado");
            servicio = raizServicio.AddComponent<ServicioGuardado>();
            servicio.FijarRuta(Path.Combine(carpeta, "partida.json"));
            return servicio;
        }

        // Guarda un mecha con piezas y 25 de daño; deja el mecha destruido y devuelve la vida máxima guardada.
        private IEnumerator GuardarYDestruir(System.Action<float> alTerminar)
        {
            CrearMecha();
            CrearServicio();
            yield return Calentar();
            equipo.Equipar(Pieza("pieza_brazoizq_fuego_2"));
            equipo.Equipar(Pieza("pieza_piernas_tech_3"));
            vida.FijarVida(vida.Maxima - DanioGuardado);
            float maxima = vida.Maxima;
            Assert.IsTrue(servicio.Guardar(), "Guardó");
            DestruirMecha();
            yield return null;
            alTerminar(maxima);
        }

        private void VerificarCargado(float maximaGuardada)
        {
            Assert.AreEqual(RamaPieza.Fuego, equipo.Puesta(RanuraPieza.BrazoIzquierdo).Rama);
            Assert.AreEqual(2, equipo.Puesta(RanuraPieza.BrazoIzquierdo).Nivel);
            Assert.AreEqual(RamaPieza.Tech, equipo.Puesta(RanuraPieza.Piernas).Rama);
            Assert.AreEqual(3, equipo.Puesta(RanuraPieza.Piernas).Nivel);
            Assert.AreEqual(RamaPieza.DeSerie, equipo.Puesta(RanuraPieza.Torso).Rama);
            Assert.AreEqual(maximaGuardada, vida.Maxima, Tolerancia);
            Assert.AreEqual(maximaGuardada - DanioGuardado, vida.Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Cargar_ConElMechaYaPresente_PonePiezasYDanio()
        {
            float maxima = 0f;
            yield return GuardarYDestruir(m => maxima = m);
            Assert.Greater(maxima, 100f, "Las piezas subieron la vida máxima");

            CrearMecha();
            yield return Calentar();
            Assert.IsTrue(servicio.Cargar(), "Cargó");

            VerificarCargado(maxima);
        }

        [UnityTest]
        public IEnumerator Cargar_ConElServicioAntesQueElMecha_PonePiezasYDanio()
        {
            float maxima = 0f;
            yield return GuardarYDestruir(m => maxima = m);

            Assert.IsTrue(servicio.Cargar(), "Cargó sin mecha: queda pendiente");
            CrearMecha();
            yield return Calentar();

            VerificarCargado(maxima);
        }

        [UnityTest]
        public IEnumerator Cargar_TextoViejoSinPiezas_DejaLasDeSerieYLaVidaPorFraccion_SinAvisos()
        {
            CrearMecha();
            yield return Calentar();
            avisos.Clear();

            mecha.GetComponent<MechaPersistente>().Cargar("{\"vida\":30,\"vidaMaxima\":60}");

            Assert.AreEqual(5, equipo.Puestas.Count(p => p.Rama == RamaPieza.DeSerie));
            Assert.AreEqual(100f, vida.Maxima, Tolerancia);
            Assert.AreEqual(50f, vida.Actual, Tolerancia);
            Assert.IsEmpty(avisos, "Sin warnings");
        }

        [UnityTest]
        public IEnumerator Cargar_IdDesconocido_DejaLaDeSerieConWarning()
        {
            CrearMecha();
            yield return Calentar();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("pieza_no_existe"));

            mecha.GetComponent<MechaPersistente>().Cargar(
                "{\"vida\":90,\"vidaMaxima\":100,\"danio\":10,\"piezas\":[\"pieza_no_existe\",\"pieza_torso_militar_1\"]}");

            Assert.AreEqual(RamaPieza.Militar, equipo.Puesta(RanuraPieza.Torso).Rama);
            Assert.AreEqual(RamaPieza.DeSerie, equipo.Puesta(RanuraPieza.Piernas).Rama);
            Assert.AreEqual(vida.Maxima - 10f, vida.Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Cambios_VeinteSeguidos_NoDejanDatosDeEjecucionDeMas()
        {
            CrearMecha();
            yield return Calentar();
            int antes = ContarDatosEjecucion();
            DatosPieza a = Pieza("pieza_torso_militar_1");
            DatosPieza b = Pieza("pieza_torso_tech_2");

            for (int i = 0; i < CambiosSeguidos; i++)
            {
                equipo.Equipar(i % 2 == 0 ? a : b);
            }
            yield return null;

            Assert.AreEqual(antes + 1, ContarDatosEjecucion(), "Solo vive el DatosMecha actual");
            DestruirMecha();
            yield return null;
            Assert.AreEqual(antes, ContarDatosEjecucion(), "Al destruir el mecha no queda ninguno");
        }
    }
}
