using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-178: los jefes de liga usan una habilidad con aviso. Jefe real de los prefabs a 5 m de MechaEstacionado (con entrada
    /// simulada). Los golpes normales del jefe se apagan con una espera enorme para medir solo la habilidad. La pelea se marca
    /// publicando EventosPelea.PublicarInicio.
    /// </summary>
    public class HabilidadRivalPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaEstacionado.prefab";
        private const string RutaJefe = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga{0}_5.prefab";
        private const string RutaTornillo = "Assets/_Proyecto/IA/Prefabs/Ligas/RivalLiga1_1.prefab";
        private const string RutaTutorial = "Assets/_Proyecto/IA/Prefabs/RivalTutorial.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EscalaTiempo = 3f;
        private const float DistanciaInicial = 5f;
        private const float EsperaEnorme = 1000f;
        private const float Enfriamiento = 10.5f;
        private const float EnfriamientoFuria = 7.35f;
        private const float ToleranciaTiempo = 0.2f;
        private const float SegundosAviso = 0.6f;
        private const float ToleranciaAviso = 0.15f;
        private const float DanioEmbestida = 14f;
        private const float DanioMisil = 30f;
        private const float SegundosAturdimientoDescarga = 1.0f;
        private const float ToleranciaDanio = 0.05f;
        private const float ToleranciaPosicion = 0.05f;
        private const float ToleranciaColor = 0.02f;
        private const float LimiteLargo = 40f;
        private const float DanioTornillo = 8f;
        private const float FraccionVidaFuria = 0.4f;
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private GameObject jefe;
        private VidaMecha vidaJugador;
        private PeleadorMecha peleadorJugador;
        private GuardiaMecha guardia;
        private PeleadorMecha peleadorJefe;
        private VidaMecha vidaJefe;
        private RivalBasico rival;
        private HabilidadRival habilidad;
        private float escalaOriginal;
        private float inicioPelea;
        private readonly List<float> avisos = new List<float>();
        private readonly List<float> lanzamientos = new List<float>();
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            errores.Clear();
            avisos.Clear();
            lanzamientos.Clear();
            escalaOriginal = Time.timeScale;
            Application.logMessageReceived += AlRegistrarLog;
        }

        public override void TearDown()
        {
            Time.timeScale = escalaOriginal;
            Application.logMessageReceived -= AlRegistrarLog;
            if (jefe != null) UnityEngine.Object.DestroyImmediate(jefe);
            if (jugador != null) UnityEngine.Object.DestroyImmediate(jugador);
            if (piso != null) UnityEngine.Object.Destroy(piso);
            foreach (ProyectilHabilidad p in UnityEngine.Object.FindObjectsByType<ProyectilHabilidad>(FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(p.gameObject);
            }
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

        // Espera a que se cumpla la condición; falla si pasa el límite (segundos de juego).
        private static IEnumerator Hasta(Func<bool> condicion, float limite, string que)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) yield return null;
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        // Jugador quieto a 5 m del jefe, los dos sobre el mismo piso. Todavía no empieza la pelea.
        private IEnumerator Escenario(string rutaJefe, bool apagarGolpes = true)
        {
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);

            GameObject padre = new GameObject("PadreApagado");
            padre.SetActive(false);
            jugador = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador),
                new Vector3(0f, 0.05f, 0f), Quaternion.identity, padre.transform);
            SerializedObject so = new SerializedObject(jugador.GetComponent<MontajeMecha>());
            so.FindProperty("arranque").enumValueIndex = (int)ArranqueMontaje.Directo;
            so.ApplyModifiedPropertiesWithoutUndo();
            jugador.transform.SetParent(null);
            UnityEngine.Object.Destroy(padre);
            vidaJugador = jugador.GetComponent<VidaMecha>();
            peleadorJugador = jugador.GetComponent<PeleadorMecha>();
            guardia = jugador.GetComponent<GuardiaMecha>();

            jefe = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(rutaJefe),
                new Vector3(0f, 0.05f, DistanciaInicial), Quaternion.Euler(0f, 180f, 0f));
            peleadorJefe = jefe.GetComponent<PeleadorMecha>();
            vidaJefe = jefe.GetComponent<VidaMecha>();
            rival = jefe.GetComponent<RivalBasico>();
            habilidad = jefe.GetComponent<HabilidadRival>();
            rival.AsignarObjetivo(jugador);
            if (apagarGolpes) ApagarGolpes();
            if (habilidad != null)
            {
                habilidad.AvisoIniciado += () => avisos.Add(Time.time);
                habilidad.Lanzada += () => lanzamientos.Add(Time.time);
            }
            Physics.SyncTransforms();
            yield return Calentar();
            Time.timeScale = EscalaTiempo;
        }

        // La espera del primer golpe ya está elegida: se alarga por reflexión (campo privado del cerebro) además del multiplicador.
        private void ApagarGolpes()
        {
            rival.MultiplicadorEspera = EsperaEnorme;
            object cerebro = typeof(RivalBasico).GetField("cerebro", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rival);
            typeof(CerebroRival).GetField("espera", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cerebro, EsperaEnorme);
        }

        private IEnumerator Iniciar()
        {
            peleadorJefe.ActivarControl(true);
            yield return null;
            EventosPelea.PublicarInicio();
            inicioPelea = Time.time;
            yield return null;
        }

        private static string Ruta(int liga) => string.Format(RutaJefe, liga);

        private Color ColorCuerpo()
        {
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            jefe.GetComponent<PresentacionRival>().Cuerpo.GetPropertyBlock(bloque);
            return bloque.GetColor(IdColorBase);
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest]
        public IEnumerator DonFierro_AvisaALos10s5_SeDetiene_Embiste_YElSiguienteTardaOtro()
        {
            yield return Escenario(Ruta(1));
            yield return Iniciar();
            float vidaAntes = vidaJugador.Actual;

            yield return Hasta(() => habilidad.EnAviso, LimiteLargo, "el primer aviso");
            Assert.AreEqual(Enfriamiento, Time.time - inicioPelea, ToleranciaTiempo, "Primer uso tras 7 x 1.5 s");
            yield return null;
            yield return null;
            Color rama = habilidad.Datos.Habilidad.ColorRama;
            Color color = ColorCuerpo();
            Assert.AreEqual(rama.r, color.r, ToleranciaColor, "Brilla con el color de la rama (r)");
            Assert.AreEqual(rama.g, color.g, ToleranciaColor, "Brilla con el color de la rama (g)");
            Assert.AreEqual(rama.b, color.b, ToleranciaColor, "Brilla con el color de la rama (b)");
            Assert.AreEqual(0f, rival.Velocidad, 1e-4f, "Se detiene");
            Vector3 quieto = Plano(jefe.transform.position);
            float inicioAviso = avisos[0];

            yield return Hasta(() => lanzamientos.Count == 1, 3f, "el lanzamiento");
            Assert.AreEqual(SegundosAviso, lanzamientos[0] - inicioAviso, ToleranciaAviso, "El aviso dura 0.6 s");
            Assert.AreEqual(0f, Vector3.Distance(quieto, Plano(jefe.transform.position)), ToleranciaPosicion, "No se movió durante el aviso");
            yield return Hasta(() => !jefe.GetComponent<LanzadorHabilidades>().Embistiendo, 3f, "el fin de la embestida");
            yield return Esperar(0.3f);
            Assert.AreEqual(DanioEmbestida, vidaAntes - vidaJugador.Actual, ToleranciaDanio, "El jugador pierde 14 si no bloquea");

            yield return Hasta(() => avisos.Count == 2, LimiteLargo, "el segundo aviso");
            Assert.AreEqual(Enfriamiento, avisos[1] - lanzamientos[0], ToleranciaTiempo, "El siguiente uso tarda otros 10.5 s");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator EnFuria_ElEnfriamientoMedidoEs7s35()
        {
            yield return Escenario(Ruta(1));
            vidaJefe.FijarVida(vidaJefe.Maxima * FraccionVidaFuria);
            yield return Iniciar();
            yield return null;
            Assert.IsTrue(jefe.GetComponent<FuriaJefe>().EnFuria, "Entró en furia");

            yield return Hasta(() => avisos.Count == 1, LimiteLargo, "el primer aviso en furia");
            Assert.AreEqual(EnfriamientoFuria, avisos[0] - inicioPelea, ToleranciaTiempo, "Primer uso en furia: 10.5 x 0.7");
            yield return Hasta(() => lanzamientos.Count == 1, 3f, "el primer lanzamiento");
            yield return Hasta(() => avisos.Count == 2, LimiteLargo, "el segundo aviso en furia");
            Assert.AreEqual(EnfriamientoFuria, avisos[1] - lanzamientos[0], ToleranciaTiempo, "Enfriamiento en furia: 7.35 s");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator LaMatraca_LaDescargaAturdeAlJugador1s_YLuegoVuelveTodo()
        {
            yield return Escenario(Ruta(2));
            yield return Iniciar();
            yield return Hasta(() => lanzamientos.Count == 1, LimiteLargo, "la descarga");
            yield return null;

            Assert.IsTrue(peleadorJugador.EstaAturdido, "La descarga aturdió al jugador");
            float inicioStun = Time.time;
            Vector3 antes = Plano(jugador.transform.position);
            Press(raton.rightButton);
            Press(teclado.sKey);
            float maxGuardia = 0f;
            while (peleadorJugador.EstaAturdido && Time.time < inicioStun + 3f)
            {
                maxGuardia = Mathf.Max(maxGuardia, guardia.Progreso);
                yield return null;
            }
            float duracion = Time.time - inicioStun;
            Assert.AreEqual(SegundosAturdimientoDescarga, duracion, 0.2f, "Aturdido 1.0 s");
            Assert.AreEqual(0f, maxGuardia, 1e-4f, "No sube la guardia aturdido");
            Assert.AreEqual(0f, Vector3.Distance(antes, Plano(jugador.transform.position)), 0.1f, "Las teclas de movimiento no hacen nada");

            // Las acciones se apagan con el aturdimiento: hay que soltar y volver a apretar para que cuenten.
            Release(raton.rightButton);
            Release(teclado.sKey);
            yield return null;
            Press(raton.rightButton);
            Press(teclado.sKey);
            yield return Esperar(0.5f);
            Assert.IsTrue(guardia.Levantada, "Pasado el aturdimiento la guardia vuelve a subir");
            Assert.Greater(Vector3.Distance(antes, Plano(jugador.transform.position)), 0.2f, "Pasado el aturdimiento vuelve a caminar");
            Release(raton.rightButton);
            Release(teclado.sKey);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator ElAlcalde_ElMisilContraLaGuardiaEntraReducido()
        {
            yield return Escenario(Ruta(3));
            yield return Iniciar();
            Press(raton.rightButton);
            float vidaAntes = vidaJugador.Actual;
            float esperado = DanioMisil * (1f - vidaJugador.Datos.ReduccionBloqueo);

            yield return Hasta(() => avisos.Count == 1, LimiteLargo, "el aviso del misil");
            Assert.IsTrue(guardia.EstaArriba, "La guardia está arriba");
            yield return Hasta(() => lanzamientos.Count == 1, 3f, "el misil");
            yield return Esperar(1.5f);

            Assert.AreEqual(esperado, vidaAntes - vidaJugador.Actual, ToleranciaDanio, "30 x (1 - reducción del bloqueo)");
            Release(raton.rightButton);
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator ElAlcalde_ElMisilSinGuardiaPegaLos30()
        {
            yield return Escenario(Ruta(3));
            yield return Iniciar();
            float vidaAntes = vidaJugador.Actual;
            yield return Hasta(() => lanzamientos.Count == 1, LimiteLargo, "el misil");
            yield return Esperar(1.5f);
            Assert.AreEqual(DanioMisil, vidaAntes - vidaJugador.Actual, ToleranciaDanio, "Sin guardia entra completo");
        }

        [UnityTest]
        public IEnumerator SiElJefeQuedaAturdidoDuranteElAviso_NoLanza()
        {
            yield return Escenario(Ruta(3));
            yield return Iniciar();
            float vidaAntes = vidaJugador.Actual;
            yield return Hasta(() => habilidad.EnAviso, LimiteLargo, "el aviso");

            peleadorJefe.Aturdir(3f);
            yield return null;
            yield return null;
            Assert.IsFalse(habilidad.EnAviso, "El aviso se cortó");
            yield return Esperar(1.5f);

            Assert.AreEqual(0, lanzamientos.Count, "No lanzó");
            Assert.AreEqual(vidaAntes, vidaJugador.Actual, 1e-4f, "El jugador no recibió nada");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator SiElJefeQuedaEnKODuranteElAviso_NoLanza()
        {
            yield return Escenario(Ruta(3));
            yield return Iniciar();
            float vidaAntes = vidaJugador.Actual;
            yield return Hasta(() => habilidad.EnAviso, LimiteLargo, "el aviso");

            vidaJefe.RecibirDanio(new InfoDanio(vidaJefe.Maxima * 10f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return null;
            yield return null;
            Assert.IsTrue(vidaJefe.EstaKO, "El jefe cayó");
            Assert.IsFalse(habilidad.EnAviso, "El aviso se cortó");
            yield return Esperar(1.5f);

            Assert.AreEqual(0, lanzamientos.Count, "No lanzó");
            Assert.AreEqual(vidaAntes, vidaJugador.Actual, 1e-4f, "El jugador no recibió nada");
        }

        [UnityTest]
        public IEnumerator Tornillo_NoTieneHabilidad_YPeleaIgual()
        {
            GameObject tornillo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTornillo);
            GameObject tutorial = AssetDatabase.LoadAssetAtPath<GameObject>(RutaTutorial);
            Assert.IsNull(tornillo.GetComponent<HabilidadRival>(), "TORNILLO no lleva HabilidadRival");
            Assert.IsNull(tornillo.GetComponent<LanzadorHabilidades>(), "TORNILLO no lleva LanzadorHabilidades");
            Assert.IsNull(tutorial.GetComponent<HabilidadRival>(), "El tutorial no lleva HabilidadRival");
            Assert.IsNull(tutorial.GetComponent<LanzadorHabilidades>(), "El tutorial no lleva LanzadorHabilidades");

            yield return Escenario(RutaTornillo, false);
            yield return Iniciar();
            float vidaAntes = vidaJugador.Actual;
            yield return Hasta(() => vidaJugador.Actual < vidaAntes, LimiteLargo, "su primer golpe");
            Assert.AreEqual(DanioTornillo, vidaAntes - vidaJugador.Actual, ToleranciaDanio, "Pega con su golpe de siempre");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }

        [UnityTest]
        public IEnumerator Revancha_ElPrimerUsoVuelveATardarUnEnfriamientoCompleto()
        {
            yield return Escenario(Ruta(1));
            yield return Iniciar();
            yield return Hasta(() => lanzamientos.Count == 1, LimiteLargo, "el primer uso");
            yield return Hasta(() => !jefe.GetComponent<LanzadorHabilidades>().Embistiendo, 3f, "el fin de la embestida");
            yield return Esperar(5f);
            Assert.AreEqual(1, avisos.Count, "Todavía no hay un segundo aviso");

            EventosPelea.PublicarInicio();
            float segundaPelea = Time.time;
            yield return Hasta(() => avisos.Count == 2, LimiteLargo, "el primer uso de la revancha");

            Assert.AreEqual(Enfriamiento, avisos[1] - segundaPelea, ToleranciaTiempo, "Un enfriamiento completo desde la revancha");
            Assert.AreEqual(0, errores.Count, string.Join("\n", errores));
        }
    }
}
