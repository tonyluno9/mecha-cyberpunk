using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-130: la cama y el espejo de la casa en Pruebas_Prologo. Se juega con entrada simulada (W para caminar, E y Esc);
    /// el cambio de escena se intercepta para ver a dónde iría.
    /// </summary>
    public class CamaEspejoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity";
        private const string RutaDatosPruebas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";
        private const string ClaveReloj = "reloj";
        private const string TextoDormir = "E · Dormir · hasta las 08:00";
        private const string TextoPrimeroCasera = "Primero tu pelea casera";
        private const string TextoAhoraNo = "Ahora no puedes dormir";
        private const string TextoEspejo = "E · Mirarte al espejo";
        private const string AliasPerfil = "EL RAYO";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const int FramesParaElHud = 5;
        private const float DeltaTimeEstable = 0.05f;
        private const float MaxSegundosDormir = 15f;
        private const float SegundosCaminando = 0.5f;
        private const float SegundosSinEfecto = 1.5f;
        private const float DistanciaInicial = 2f;
        private const float AlturaInicio = 0.05f;
        private const float DistanciaMinimaCaminada = 0.2f;
        private const float DistanciaQuieto = 0.02f;
        private const float HoraInicial = 21f;
        private const float HoraDespertar = 8f;
        private const float ToleranciaHora = 0.05f;
        private const float DistanciaMaximaDeLaCama = 2.5f;
        private const string VariableCaptura = "CAPTURAR_T130";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;

        [Serializable]
        private class DatosReloj
        {
            public int dia;
            public float hora;
        }

        [Serializable]
        private class DatosHistoria
        {
            public string etapa;
        }

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private JugadorAPie jugador;
        private ControladorAPie controlador;
        private CamaJugador cama;
        private EspejoJugador espejo;
        private PantallaEspejo pantalla;
        private VistaPreviaApariencia vista;
        private HudAPie hud;
        private IRelojMundo reloj;
        private readonly List<string> errores = new List<string>();
        private HashSet<GameObject> raicesIniciales;


        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            // Otras pruebas (sin InputTestFixture) dejan acciones prendidas; se apagan antes de que el fixture reinicie el Input System.
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "cama_espejo_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            errores.Clear();
            Application.logMessageReceived += RegistrarError;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarError;
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta))
            {
                Directory.Delete(carpeta, true);
            }
            Registros.LimpiarTodos();
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado, raton);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private void RegistrarError(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
        }

        // ---- Preparación y utilidades ----

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            // El director del prólogo (T-132) arrancaría la pelea del campeonato solo; esta prueba maneja la escena a mano.
            DirectorPrologo prologo = Object.FindFirstObjectByType<DirectorPrologo>();
            if (prologo != null)
            {
                Object.Destroy(prologo);
            }
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            jugador = Object.FindFirstObjectByType<JugadorAPie>();
            cama = Object.FindFirstObjectByType<CamaJugador>();
            espejo = Object.FindFirstObjectByType<EspejoJugador>();
            hud = Object.FindFirstObjectByType<HudAPie>();
            Assert.IsNotNull(jugador, "Pruebas_Prologo trae al jugador");
            Assert.IsNotNull(cama, "Pruebas_Prologo trae la cama");
            Assert.IsNotNull(espejo, "Pruebas_Prologo trae el espejo");
            Assert.IsNotNull(hud, "El jugador trae el HUD a pie");
            controlador = jugador.GetComponent<ControladorAPie>();
            pantalla = espejo.GetComponentInChildren<PantallaEspejo>(true);
            vista = espejo.GetComponentInChildren<VistaPreviaApariencia>(true);
            reloj = Registro<IRelojMundo>.Actual;
            Assert.IsNotNull(reloj, "Hay reloj");
            Assert.IsNotNull(pantalla, "El espejo trae su pantalla");
            Assert.IsNotNull(vista, "El espejo trae su vista previa");
            reloj.Pausar(true);
            Assert.GreaterOrEqual(Vector3.Distance(cama.Posicion, espejo.Posicion), DistanciaMaximaDeLaCama,
                "La cama y el espejo están a más de 2.5 m entre sí");
            yield return null;
        }

        private static void PonerEtapa(EtapaPrologo etapa)
        {
            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            Assert.IsNotNull(historia, "La escena trae el ServicioHistoria");
            Assert.IsTrue(historia.Avanzar(etapa) || historia.Etapa == etapa, "La historia llega a " + etapa);
        }

        // Pone al jugador a 2 m al sur del punto de uso, mirando hacia él, y deja pasar unos cuadros para que el HUD se actualice.
        private IEnumerator IrA(Vector3 punto)
        {
            jugador.Colocar(new Pose(punto + new Vector3(0f, AlturaInicio, -DistanciaInicial), Quaternion.identity));
            for (int i = 0; i < FramesParaElHud; i++)
            {
                yield return null;
            }
        }

        private IEnumerator PulsarE()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
        }

        // Camina hacia adelante con la W simulada y devuelve cuánto se movió el jugador (m).
        private IEnumerator CaminarYMedir(Action<float> resultado)
        {
            Vector3 antes = jugador.Raiz.position;
            Press(teclado.wKey);
            float fin = Time.time + SegundosCaminando;
            while (Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            resultado(Vector3.Distance(antes, jugador.Raiz.position));
        }

        private static int OyentesActivos()
        {
            int oyentes = 0;
            foreach (AudioListener oyente in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (oyente.enabled && oyente.gameObject.activeInHierarchy)
                {
                    oyentes++;
                }
            }
            return oyentes;
        }

        private IEnumerator EsperarHasta(Func<bool> condicion, float maxSegundos, string que)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo mientras se espera: " + que);
                yield return null;
            }
            Assert.IsTrue(condicion(), "Se esperaba: " + que);
        }

        private static string PiezaGuardada(string clave)
        {
            string json = File.ReadAllText(ServicioGuardado.Actual.RutaArchivo);
            Assert.IsTrue(ArchivoPartida.TryLeer(json, out List<PiezaGuardada> piezas, out _), "El archivo guardado se entiende");
            foreach (PiezaGuardada pieza in piezas)
            {
                if (pieza.clave == clave)
                {
                    return pieza.datos;
                }
            }
            return null;
        }

        // Captura de lo que ve el jugador (solo con CAPTURAR_T130=1). Los lienzos en Overlay no salen en una cámara a textura:
        // se pasan un instante a ScreenSpaceCamera con esa cámara y se devuelven como estaban.
        private void Capturar(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = jugador.Camara;
            List<Canvas> movidos = new List<Canvas>();
            foreach (Canvas lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (lienzo.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lienzo.renderMode = RenderMode.ScreenSpaceCamera;
                    lienzo.worldCamera = camara;
                    lienzo.planeDistance = DistanciaPlanoCanvas;
                    movidos.Add(lienzo);
                }
            }
            RenderTexture rt = new RenderTexture(AnchoCaptura, AltoCaptura, ProfundidadCaptura);
            Texture2D imagen = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            imagen.ReadPixels(new Rect(0f, 0f, AnchoCaptura, AltoCaptura), 0, 0);
            imagen.Apply();
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas lienzo in movidos)
            {
                lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            float minimo = 1f;
            float maximo = 0f;
            Color32[] pixeles = imagen.GetPixels32();
            for (int i = 0; i < pixeles.Length; i += SaltoDePixeles)
            {
                float brillo = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, brillo);
                maximo = Mathf.Max(maximo, brillo);
            }
            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(imagen);
            Assert.Greater(maximo - minimo, VariacionMinima, "La captura es plana: no se ve nada");
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Cama_SinEtapa_DuermeHastaLas8_GuardaYDevuelveElControl()
        {
            yield return CargarEscena();
            Assert.AreEqual(1, reloj.Dia, "Empieza el día 1");
            Assert.AreEqual(HoraInicial, reloj.Hora, ToleranciaHora, "A las 21:00");
            yield return IrA(cama.Posicion);
            Assert.AreEqual(TextoDormir, hud.TextoAvisoInteraccion.text, "El HUD ofrece dormir hasta las 08:00");
            Capturar("docs/capturas/T-130-2.png");

            yield return PulsarE();
            Assert.IsTrue(cama.Durmiendo, "E empieza a dormir");
            Assert.IsFalse(controlador.ControlActivo, "Mientras duerme el jugador no tiene control");
            yield return EsperarHasta(() => !cama.Durmiendo, MaxSegundosDormir, "termina de dormir");
            yield return null;

            Assert.AreEqual(2, reloj.Dia, "Ahora es el día 2");
            Assert.AreEqual(HoraDespertar, reloj.Hora, ToleranciaHora, "A las 08:00");
            DatosReloj guardado = JsonUtility.FromJson<DatosReloj>(PiezaGuardada(ClaveReloj));
            Assert.AreEqual(2, guardado.dia, "El archivo trae el día 2");
            Assert.AreEqual(HoraDespertar, guardado.hora, ToleranciaHora, "El archivo trae las 08:00");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");
            Assert.Less(Vector3.Distance(jugador.Raiz.position, cama.Posicion), DistanciaMaximaDeLaCama, "Sigue junto a la cama");
            Assert.IsNotNull(PantallaFundido.Actual, "Hay pantalla de fundido");
            Assert.AreEqual(0f, PantallaFundido.Actual.Alfa, 0.001f, "El fundido volvió a transparente");

            float movido = 0f;
            yield return CaminarYMedir(d => movido = d);
            Assert.Greater(movido, DistanciaMinimaCaminada, "Con W vuelve a caminar");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo al final");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Cama_EnLaEtapaDormir_GuardaFlashbackYPideLaEscenaDelFlashback()
        {
            yield return CargarEscena();
            string rutaPedida = null;
            CambioEscena.Interceptar(ruta => rutaPedida = ruta);
            PonerEtapa(EtapaPrologo.Dormir);
            yield return IrA(cama.Posicion);
            Assert.AreEqual("E · Dormir", hud.TextoAvisoInteraccion.text, "En la etapa Dormir el HUD ofrece dormir");

            yield return PulsarE();
            yield return EsperarHasta(() => rutaPedida != null, MaxSegundosDormir, "se pide la escena del flashback");

            DatosEscenas datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosPruebas);
            Assert.AreEqual(datos.RutaFlashback, rutaPedida, "Va a la escena del flashback de DatosEscenas_Pruebas");
            Assert.AreEqual(EtapaPrologo.Flashback, Registro<IHistoria>.Actual.Etapa, "La historia está en Flashback");
            DatosHistoria guardada = JsonUtility.FromJson<DatosHistoria>(PiezaGuardada(ServicioHistoria.Clave));
            Assert.AreEqual(EtapaPrologo.Flashback.ToString(), guardada.etapa, "El archivo trae la etapa Flashback");
            Assert.AreEqual(HoraInicial, reloj.Hora, ToleranciaHora, "Dormir hacia el flashback no adelanta el reloj");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Cama_ConLaPeleaCasera_AvisaYNoCambiaLaHora()
        {
            yield return CargarEscena();
            string rutaPedida = null;
            CambioEscena.Interceptar(ruta => rutaPedida = ruta);

            PonerEtapa(EtapaPrologo.PeleaTutorial);
            yield return IrA(cama.Posicion);
            Assert.AreEqual(TextoAhoraNo, hud.TextoAvisoInteraccion.text, "Antes de llegar a casa no se puede dormir");
            Assert.IsFalse(cama.PuedeUsar);

            PonerEtapa(EtapaPrologo.PeleaCasera);
            yield return IrA(cama.Posicion);
            Assert.AreEqual(TextoPrimeroCasera, hud.TextoAvisoInteraccion.text, "El HUD avisa de la pelea casera");
            StringAssert.DoesNotContain("E · ", hud.TextoAvisoInteraccion.text, "Es un aviso, no una acción");
            Assert.IsFalse(cama.PuedeUsar, "No se puede usar");

            float horaAntes = reloj.Hora;
            yield return PulsarE();
            yield return new WaitForSeconds(SegundosSinEfecto);
            Assert.IsFalse(cama.Durmiendo, "E no duerme");
            Assert.AreEqual(1, reloj.Dia, "Sigue el día 1");
            Assert.AreEqual(horaAntes, reloj.Hora, ToleranciaHora, "E no cambia la hora");
            Assert.IsNull(rutaPedida, "Y no pide otra escena");
            Assert.IsTrue(controlador.ControlActivo, "El jugador conserva el control");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Espejo_AbreConAliasYVistaPrevia_QuitaElControlYSeCierraConE()
        {
            yield return CargarEscena();
            ServicioPerfil perfil = Registro<IPerfilJugador>.Actual as ServicioPerfil;
            Assert.IsNotNull(perfil, "Hay perfil");
            perfil.Fijar("Tony", AliasPerfil);
            yield return IrA(espejo.Posicion);
            Assert.AreEqual(TextoEspejo, hud.TextoAvisoInteraccion.text, "El HUD ofrece mirarse");
            Assert.IsFalse(pantalla.Visible, "La pantalla empieza cerrada");
            Assert.IsFalse(vista.EstaEncendida, "La vista previa empieza apagada");

            yield return PulsarE();
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla del espejo");
            Assert.AreEqual(AliasPerfil, pantalla.Alias, "Muestra el alias del perfil");
            Assert.AreEqual("E · salir", pantalla.TextoSalir, "Dice cómo salir");
            Assert.IsTrue(vista.EstaEncendida && vista.Camara.enabled, "La cámara de la vista previa está activa");
            Assert.IsFalse(controlador.ControlActivo, "El jugador no tiene control");
            for (int i = 0; i < FramesParaElHud; i++)
            {
                yield return null;
            }
            Capturar("docs/capturas/T-130-1.png");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener con la vista previa encendida");

            // Con la pantalla abierta, W no mueve al jugador.
            float movido = -1f;
            yield return CaminarYMedir(d => movido = d);
            Assert.Less(movido, DistanciaQuieto, "W no mueve al jugador con el espejo abierto");

            yield return PulsarE();
            yield return null;
            Assert.IsFalse(pantalla.Visible, "E cierra la pantalla");
            Assert.IsFalse(vista.EstaEncendida, "La vista previa queda apagada");
            Assert.IsFalse(vista.Camara.enabled, "No queda cámara de vista previa activa");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");
            yield return CaminarYMedir(d => movido = d);
            Assert.Greater(movido, DistanciaMinimaCaminada, "W vuelve a mover al jugador");
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener al final");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }

        [UnityTest]
        public IEnumerator Espejo_ConEsc_TambienCierraYDevuelveElControl()
        {
            yield return CargarEscena();
            yield return IrA(espejo.Posicion);
            yield return PulsarE();
            Assert.IsTrue(pantalla.Visible, "E abre la pantalla");
            yield return null;

            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            yield return null;
            yield return null;
            Assert.IsFalse(pantalla.Visible, "Esc cierra la pantalla");
            Assert.IsFalse(vista.EstaEncendida, "La vista previa queda apagada");
            Assert.IsTrue(controlador.ControlActivo, "El jugador recupera el control");
            Assert.IsEmpty(errores, "Consola sin errores: " + string.Join(" | ", errores));
        }
    }
}
