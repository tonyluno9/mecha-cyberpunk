using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>
    /// T-176: usar las habilidades con las teclas 1 y 2, con munición del inventario y enfriamiento. Prefab real
    /// MechaEstacionado.prefab (arranque Directo) con un saco enfrente, un ServicioInventario y entrada simulada. La pelea se marca
    /// publicando EventosPelea.PublicarInicio y PublicarFin.
    /// </summary>
    public class UsoHabilidadesPruebas : InputTestFixture
    {
        private const string RutaEstacionado = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaCatalogo = "Assets/_Proyecto/Mecha/Datos/Piezas/CatalogoPiezas_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float VidaSaco = 1000f;
        private const float DanioMisil = 20f;
        private const float Tolerancia = 0.01f;
        private const float SegundosGuardia = 0.6f;
        private const float EsperaImpacto = 1.5f;
        private const float EnfriamientoMisil = 8f;
        private const float EnfriamientoEscudoTech = 4.8f;
        private const float ToleranciaEnfriamiento = 0.1f;
        private const float EsperaMaxima = 12f;
        private const float GiroMouse = 400f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private GameObject saco;
        private DatosMecha datosSaco;
        private GameObject raizInventario;
        private ServicioInventario inventario;
        private CatalogoPiezas catalogo;
        private EquipoMecha equipo;
        private UsoHabilidades uso;
        private LanzadorHabilidades lanzador;
        private VidaMecha vidaSaco;
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogo);
            Assert.IsNotNull(catalogo, "Existe CatalogoPiezas_Base");
            errores.Clear();
            Application.logMessageReceived += AlRegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= AlRegistrarLog;
            if (mecha != null) Object.DestroyImmediate(mecha);
            if (piso != null) Object.Destroy(piso);
            if (saco != null) Object.Destroy(saco);
            if (datosSaco != null) Object.Destroy(datosSaco);
            if (raizInventario != null) Object.Destroy(raizInventario);
            foreach (ProyectilHabilidad p in Object.FindObjectsByType<ProyectilHabilidad>(FindObjectsSortMode.None)) Object.DestroyImmediate(p.gameObject);
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private void AlRegistrarLog(string texto, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errores.Add(texto);
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
            while (Time.time < fin) yield return null;
        }

        // Instancia el mecha con arranque Directo (bajo un padre apagado, para que el Awake ya vea el arranque).
        private void CrearMecha(string ruta)
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            var padre = new GameObject("PadreApagado");
            padre.SetActive(false);
            mecha = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), new Vector3(0f, 0.05f, 0f), Quaternion.identity, padre.transform);
            var so = new SerializedObject(mecha.GetComponent<MontajeMecha>());
            so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha.transform.SetParent(null);
            Object.Destroy(padre);
            equipo = mecha.GetComponent<EquipoMecha>();
            uso = mecha.GetComponent<UsoHabilidades>();
            lanzador = mecha.GetComponent<LanzadorHabilidades>();
            Assert.IsNotNull(uso, "El mecha lleva UsoHabilidades");
            Assert.IsNotNull(lanzador, "El mecha lleva LanzadorHabilidades");
        }

        private void CrearInventario()
        {
            raizInventario = new GameObject("InventarioPrueba");
            inventario = raizInventario.AddComponent<ServicioInventario>();
        }

        private void CrearSaco()
        {
            saco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            saco.name = "SacoHabilidades";
            saco.transform.localScale = new Vector3(1f, 8f, 1f);
            saco.transform.position = new Vector3(0f, 4f, 2.5f);
            vidaSaco = saco.AddComponent<VidaMecha>();
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

        private IEnumerator Tecla(Key tecla)
        {
            KeyControl control = teclado[tecla];
            Press(control);
            yield return null;
            Release(control);
            yield return null;
        }

        private IEnumerator Escenario(bool conSaco = true)
        {
            CrearMecha(RutaEstacionado);
            CrearInventario();
            if (conSaco) CrearSaco();
            yield return Calentar();
        }

        [UnityTest]
        public IEnumerator PiezasDeSerie_Teclas1y2_NoHacenNada_ElInventarioNoCambia()
        {
            yield return Escenario();
            inventario.Agregar(Pieza("pieza_brazoizq_militar_1").Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;

            yield return Tecla(Key.Digit1);
            yield return Tecla(Key.Digit2);
            yield return Esperar(EsperaImpacto);

            Assert.AreEqual(MotivoHabilidad.SinHabilidad, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(MotivoHabilidad.SinHabilidad, uso.Motivo(RanuraPieza.Torso));
            Assert.AreEqual(3, inventario.Cantidad(Pieza("pieza_brazoizq_militar_1").Municion));
            Assert.AreEqual(VidaSaco, vidaSaco.Actual, Tolerancia);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator MisilMilitarI_Dispara_Enfria8Segundos_YDisparaOtraVez()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;

            float t0 = Time.time;
            yield return Tecla(Key.Digit1);
            yield return Esperar(EsperaImpacto);
            Assert.AreEqual(2, inventario.Cantidad(brazo.Municion), "Gastó una munición");
            Assert.AreEqual(VidaSaco - DanioMisil, vidaSaco.Actual, Tolerancia, "El misil pegó 20");
            Assert.AreEqual(EnfriamientoMisil, uso.EnfriamientoTotal(RanuraPieza.BrazoIzquierdo), Tolerancia);

            yield return Esperar(2f - (Time.time - t0));
            yield return Tecla(Key.Digit1);
            Assert.AreEqual(MotivoHabilidad.Enfriando, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(2, inventario.Cantidad(brazo.Municion), "A los 2 s no gasta");

            yield return Esperar(8.1f - (Time.time - t0));
            yield return Tecla(Key.Digit1);
            Assert.AreEqual(1, inventario.Cantidad(brazo.Municion), "A los 8.1 s sí dispara");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator SinMunicion_NoDispara_YElClicIzquierdoSigueGolpeando()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            EventosPelea.PublicarInicio();
            yield return null;

            yield return Tecla(Key.Digit1);
            yield return Esperar(EsperaImpacto);
            Assert.AreEqual(MotivoHabilidad.SinMunicion, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(VidaSaco, vidaSaco.Actual, Tolerancia, "No salió misil");

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(EsperaImpacto);
            Assert.Less(vidaSaco.Actual, VidaSaco, "El golpe de los brazos sigue funcionando");
        }

        [UnityTest]
        public IEnumerator FueraDePelea_NoDispara_NiGasta()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 3);

            yield return Tecla(Key.Digit1);
            Assert.AreEqual(MotivoHabilidad.FueraDePelea, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(3, inventario.Cantidad(brazo.Municion), "Antes de PeleaIniciada no gasta");

            EventosPelea.PublicarInicio();
            yield return null;
            EventosPelea.PublicarFin(new ResultadoPelea(LadoPelea.Jugador, 1f));
            yield return null;
            yield return Tecla(Key.Digit1);
            Assert.AreEqual(MotivoHabilidad.FueraDePelea, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(3, inventario.Cantidad(brazo.Municion), "Después de PeleaTerminada no gasta");
            Assert.AreEqual(VidaSaco, vidaSaco.Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator GuardiaArriba_LaDelBrazoNoSale_YLaDelTorsoSi()
        {
            yield return Escenario();
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            DatosPieza torso = Pieza("pieza_torso_tech_1");
            equipo.Equipar(brazo);
            equipo.Equipar(torso);
            inventario.Agregar(brazo.Municion, 3);
            inventario.Agregar(torso.Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;

            Press(raton.rightButton);
            yield return Esperar(SegundosGuardia);
            yield return Tecla(Key.Digit1);
            Assert.AreEqual(MotivoHabilidad.GuardiaArriba, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(3, inventario.Cantidad(brazo.Municion), "La del brazo no sale con la guardia arriba");

            yield return Tecla(Key.Digit2);
            Assert.IsTrue(lanzador.EscudoActivo, "El escudo del torso se activa");
            Assert.AreEqual(2, inventario.Cantidad(torso.Municion), "Gastó una celda");
            Release(raton.rightButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CincoPiezasTechIV_ElEnfriamientoDelEscudoEs4Punto8()
        {
            yield return Escenario(false);
            foreach (string id in new[] { "pieza_brazoizq_tech_4", "pieza_brazoder_tech_4", "pieza_torso_tech_4", "pieza_piernas_tech_4", "pieza_cabina_tech_4" })
            {
                equipo.Equipar(Pieza(id));
            }
            DatosPieza torso = Pieza("pieza_torso_tech_4");
            inventario.Agregar(torso.Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;

            float t0 = Time.time;
            yield return Tecla(Key.Digit2);
            Assert.AreEqual(2, inventario.Cantidad(torso.Municion), "Usó el escudo");
            Assert.AreEqual(EnfriamientoEscudoTech, uso.EnfriamientoTotal(RanuraPieza.Torso), ToleranciaEnfriamiento);

            while (uso.Motivo(RanuraPieza.Torso) == MotivoHabilidad.Enfriando && Time.time - t0 < EsperaMaxima) yield return null;
            Assert.AreEqual(EnfriamientoEscudoTech, Time.time - t0, ToleranciaEnfriamiento, "Enfriamiento medido");
        }

        [UnityTest]
        public IEnumerator PeleaNueva_PoneLosEnfriamientosEnCero()
        {
            yield return Escenario(false);
            DatosPieza brazo = Pieza("pieza_brazoizq_militar_1");
            equipo.Equipar(brazo);
            inventario.Agregar(brazo.Municion, 3);
            EventosPelea.PublicarInicio();
            yield return null;
            yield return Tecla(Key.Digit1);
            Assert.Greater(uso.EnfriamientoRestante(RanuraPieza.BrazoIzquierdo), 5f);

            EventosPelea.PublicarFin(new ResultadoPelea(LadoPelea.Jugador, 1f));
            EventosPelea.PublicarInicio();
            yield return null;
            Assert.AreEqual(0f, uso.EnfriamientoRestante(RanuraPieza.BrazoIzquierdo), Tolerancia);
            Assert.AreEqual(MotivoHabilidad.Puede, uso.Motivo(RanuraPieza.BrazoIzquierdo));
        }

        [UnityTest]
        public IEnumerator MechaSinEquipo_Teclas1y2_NoHacenNada_NiDanErrores()
        {
            CrearMecha(RutaJugador);
            CrearInventario();
            yield return Calentar();
            Assert.IsNull(mecha.GetComponent<EquipoMecha>(), "MechaJugador no lleva EquipoMecha");
            EventosPelea.PublicarInicio();
            yield return null;

            yield return Tecla(Key.Digit1);
            yield return Tecla(Key.Digit2);

            Assert.AreEqual(MotivoHabilidad.SinHabilidad, uso.Motivo(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(MotivoHabilidad.SinHabilidad, uso.Motivo(RanuraPieza.Torso));
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator CambiarBrazoAFuegoII_CambiaLaRanura1_YSuMunicion()
        {
            yield return Escenario(false);
            DatosPieza militar = Pieza("pieza_brazoizq_militar_1");
            DatosPieza fuego = Pieza("pieza_brazoizq_fuego_2");
            equipo.Equipar(militar);
            inventario.Agregar(militar.Municion, 2);
            inventario.Agregar(fuego.Municion, 5);
            int avisos = 0;
            uso.Cambio += () => avisos++;
            yield return null;
            Assert.AreEqual(militar.Habilidad, uso.Habilidad(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(2, uso.Municion(RanuraPieza.BrazoIzquierdo));

            equipo.Equipar(fuego);
            yield return null;

            Assert.AreEqual(fuego.Habilidad, uso.Habilidad(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(2, uso.Nivel(RanuraPieza.BrazoIzquierdo));
            Assert.AreEqual(5, uso.Municion(RanuraPieza.BrazoIzquierdo), "La munición ahora es el combustible");
            Assert.Greater(avisos, 0, "Avisó del cambio");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Embestida_CongelaElGiro_MientrasEmbiste()
        {
            yield return Escenario(false);
            DatosPieza torso = Pieza("pieza_torso_cuerpoacuerpo_1");
            equipo.Equipar(torso);
            inventario.Agregar(torso.Municion, 2);
            EventosPelea.PublicarInicio();
            yield return null;

            float inicio = mecha.transform.eulerAngles.y;
            yield return Tecla(Key.Digit2);
            Assert.IsTrue(lanzador.Embistiendo, "Empezó la embestida");
            float maxGiro = 0f;
            float limite = Time.time + EsperaMaxima;
            while (lanzador.Embistiendo && Time.time < limite)
            {
                Set(raton.delta, new Vector2(GiroMouse, 0f));
                yield return null;
                maxGiro = Mathf.Max(maxGiro, Mathf.Abs(Mathf.DeltaAngle(inicio, mecha.transform.eulerAngles.y)));
            }
            Assert.IsFalse(lanzador.Embistiendo, "La embestida terminó");
            Assert.Less(maxGiro, 0.01f, "No giró mientras embestía");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));

            Set(raton.delta, Vector2.zero);
            yield return null;
            Set(raton.delta, new Vector2(GiroMouse, 0f));
            yield return Esperar(0.5f);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(inicio, mecha.transform.eulerAngles.y)), 1f, "Después sí gira");
        }
    }
}
