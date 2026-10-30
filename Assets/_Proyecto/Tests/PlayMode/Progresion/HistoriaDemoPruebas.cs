using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-418: la historia de la demo desde el paso Chato, como un jugador: E recluta al CHATO, E con EL DOC (vida 50) cura gratis, E con EL INVENTOR
    /// da la PISTOLA I gratis, F6 salta al jefe (vencido por daño directo), E con EL HACKER saca los tres subtítulos de NOVA y luego CONTINUARÁ;
    /// Enter pasa al juego libre, se prenden los SNAKES y Neón Alto se conquista con su jefe. Atajos anotados: al jugador se le coloca frente a cada
    /// profe con Colocar; el respeto de cada zona se sube con RegistrarVencido y los jefes caen por IRecibeDanio.
    /// Con CAPTURAR_T418=1 y el editor abierto (sin -batchmode) deja docs/capturas/T-418-1.png (EL HACKER con el subtítulo de NOVA) y T-418-2.png (CONTINUARÁ), a 1920x1080.
    /// </summary>
    public class HistoriaDemoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string VariableCaptura = "CAPTURAR_T418";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const float DanoMatar = 100000f;
        private const float DistanciaFrente = 1.6f;
        private const int VencidosParaJefe = 7;

        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static string Objetivo => Registro<IObjetivos>.Actual.Actual.Texto;

        private static PersonajeApoyo Profe(string id)
        {
            foreach (PersonajeApoyo p in Object.FindObjectsByType<PersonajeApoyo>(FindObjectsSortMode.None))
            {
                if (p.Datos.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        private static JefePandilla Jefe(string zona)
        {
            foreach (JefePandilla j in Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    return j;
                }
            }
            return null;
        }

        private static int SnakesPrendidos()
        {
            int total = 0;
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                if (g.Pandilla == IdPandilla.Viboras)
                {
                    total += g.Presentes;
                }
            }
            JefePandilla jefeNeon = Jefe("neon");
            return total + (jefeNeon != null && jefeNeon.Vivo ? 1 : 0);
        }

        private static int GruposSnakesPrendidos()
        {
            int total = 0;
            foreach (PandillaAPie g in Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
            {
                total += g.Pandilla == IdPandilla.Viboras ? 1 : 0;
            }
            return total;
        }

        // El jugador aparece frente al personaje (donde mira el personaje), mirándolo, y espera un momento.
        private static IEnumerator PonerFrente(IJugador jugador, Transform p, float distancia = DistanciaFrente)
        {
            Vector3 frente = p.position + p.forward * distancia;
            if (Physics.Raycast(frente + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f))
            {
                frente = piso.point;
            }
            frente.y += 0.1f;
            Vector3 hacia = p.position - frente;
            hacia.y = 0f;
            jugador.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // Foto de la vista Game (con los textos OnGUI) sin el muñeco del jugador, que tapa la vista. OnGUI no corre en batchmode: las capturas se
        // toman corriendo esta prueba con el editor abierto (sin -batchmode), que deja la vista Game fija en 1920x1080.
        private static IEnumerator Capturar(IJugador jugador, string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                yield break;
            }
            if (Application.isBatchMode)
            {
                Debug.LogWarning("[T-418] En batchmode no hay OnGUI: la captura " + nombre + " se toma con el editor abierto.");
                yield break;
            }
            List<Renderer> ocultos = new List<Renderer>();
            foreach (Renderer r in jugador.Raiz.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    ocultos.Add(r);
                }
            }
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D foto = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine("docs", "capturas", nombre), foto.EncodeToPNG());
            Debug.Log("[T-418] Captura " + nombre + " de " + foto.width + "x" + foto.height);
            Object.Destroy(foto);
            foreach (Renderer r in ocultos)
            {
                r.enabled = true;
            }
        }

        // Vista Game fija en 1920x1080 (API interna del editor, por reflexión; solo para las capturas).
        private static void FijarVistaGame(int ancho, int alto)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1" || Application.isBatchMode)
            {
                return;
            }
            const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            Type tipoTamanos = editor.GetType("UnityEditor.GameViewSizes");
            object tamanos = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(tipoTamanos).GetProperty("instance", Todo).GetValue(null);
            object grupoActual = tipoTamanos.GetProperty("currentGroupType", Todo).GetValue(tamanos);
            object grupo = tipoTamanos.GetMethod("GetGroup", Todo).Invoke(tamanos, new[] { grupoActual });
            Type tipoTamano = editor.GetType("UnityEditor.GameViewSize");
            Type tipoClase = editor.GetType("UnityEditor.GameViewSizeType");
            object tamano = tipoTamano.GetConstructor(new[] { tipoClase, typeof(int), typeof(int), typeof(string) })
                .Invoke(new[] { Enum.Parse(tipoClase, "FixedResolution"), ancho, alto, "T-418" });
            grupo.GetType().GetMethod("AddCustomSize", Todo).Invoke(grupo, new[] { tamano });
            int indice = (int)grupo.GetType().GetMethod("GetTotalCount", Todo).Invoke(grupo, null) - 1;
            Type tipoVista = editor.GetType("UnityEditor.GameView");
            UnityEditor.EditorWindow vista = UnityEditor.EditorWindow.GetWindow(tipoVista);
            tipoVista.GetMethod("SizeSelectionCallback", Todo).Invoke(vista, new object[] { indice, null });
        }

        [UnityTest]
        public IEnumerator RecorridoDocInventorHacker()
        {
            FijarVistaGame(Ancho, Alto);
            yield return null;
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null
                    && Registro<ICartera>.Actual != null && Registro<ArsenalJugador>.Actual != null && Registro<ServicioInventario>.Actual != null;
            }, "el director arrancó", 30f);
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            VidaAPie vida = jugador.Raiz.GetComponentInChildren<VidaAPie>();
            PantallaHistoriaPandillas pantalla = director.Pantalla;
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso);
            Assert.AreEqual(DirectorPandillas.AyudaInicio, pantalla.Ayuda, "Al despertar: MEGALOPOLIS · SUBURBIOS");

            // Como después de la primera pelea: ya traes la pistola y cargas el paso Chato.
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Pistola, 0), 1);
            director.Cargar("{\"paso\":\"Chato\"}");
            Assert.AreEqual(PasoHistoria.Chato, director.Paso);
            Assert.AreEqual(0, SnakesPrendidos(), "Antes de Libre no hay SNAKES");
            cartera.Abonar(500 - cartera.Dinero, MotivoDinero.Otro);

            // a. El CHATO: E lo recluta y toca ir con EL DOC.
            Reclutable chato = null;
            foreach (Reclutable r in Object.FindObjectsByType<Reclutable>(FindObjectsSortMode.None))
            {
                chato = r.Nombre == DirectorPandillas.NombreChato ? r : chato;
            }
            Assert.IsNotNull(chato);
            yield return PonerFrente(jugador, chato.transform, 1.0f);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Doc, "reclutar al CHATO", 5f);
            Assert.AreEqual("VE CON EL DOC A I+D+I", Objetivo);

            // b. EL DOC con vida 50: E te deja al máximo sin cobrar.
            PersonajeApoyo doc = Profe(DirectorPandillas.IdDoc);
            Assert.IsNotNull(doc);
            vida.RecibirDanio(new InfoDanio(vida.Actual - 50f, TipoDanio.Disparo, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual(50f, vida.Actual, 0.5f, "Vida 50");
            yield return PonerFrente(jugador, doc.transform);
            Assert.IsTrue(doc.TieneRegalo, "En el paso Doc, EL DOC regala la curación");
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Inventor, "usar a EL DOC", 5f);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.5f, "EL DOC te deja la vida al máximo");
            Assert.AreEqual(500, cartera.Dinero, "EL DOC no cobra en la historia");
            StringAssert.Contains("Aguanta, mijo", pantalla.Subtitulo);
            Assert.AreEqual("VE CON EL INVENTOR AL CIC", Objetivo);
            Assert.IsFalse(doc.TieneRegalo, "Fuera del paso Doc vuelve a cobrar");

            // c. EL INVENTOR: E cambia la pistola a PISTOLA I (cargador 10) sin cobrar.
            PersonajeApoyo inventor = Profe(DirectorPandillas.IdInventor);
            Assert.IsNotNull(inventor);
            Assert.AreEqual(0, arsenal.Nivel(ArmaAPie.Pistola));
            yield return PonerFrente(jugador, inventor.transform, 2.2f);
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Tianguis, "usar a EL INVENTOR", 5f);
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Pistola), "PISTOLA I");
            Assert.AreEqual(10, arsenal.Datos(ArmaAPie.Pistola).Cargador, "La PISTOLA I trae 10 balas");
            Assert.AreEqual(500, cartera.Dinero, "EL INVENTOR no cobra en la historia");
            Assert.AreEqual("PISTOLA I: MÁS DAÑO Y MÁS BALAS", pantalla.Ayuda);
            Assert.IsFalse(inventor.TieneRegalo);
            Assert.AreEqual(DirectorPandillas.TextoTianguis(0, VencidosParaJefe), Objetivo);
            yield return EsperarHasta(() => pantalla.Subtitulo == DirectorPandillas.SubtituloTianguis, "los textos del Tianguis, después de los del INVENTOR", 10f);
            Assert.AreEqual(DirectorPandillas.AyudaTianguis, pantalla.Ayuda);
            Assert.AreEqual(0, SnakesPrendidos(), "Antes de Libre no hay SNAKES");

            // d. F6 lleva al paso del jefe; el respeto llega a 70, sale el jefe y cae por daño directo.
            yield return Pulsar(teclado.f6Key);
            Assert.AreEqual(PasoHistoria.Jefe, director.Paso);
            JefePandilla jefeTianguis = Jefe("tianguis");
            Assert.IsNotNull(jefeTianguis);
            for (int i = 0; i < VencidosParaJefe; i++)
            {
                territorio.RegistrarVencido(jefeTianguis.transform.position, IdPandilla.Calaveras, false);
            }
            yield return EsperarHasta(() => jefeTianguis.Vivo, "aparece el jefe de los SKULLS", 10f);
            jefeTianguis.Jefe.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, jefeTianguis.Jefe.transform.position, Vector3.forward, null));
            yield return EsperarHasta(() => director.Paso == PasoHistoria.NeonAlto, "conquistar El Tianguis", 5f);
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("tianguis"));
            Assert.AreEqual("VE CON EL HACKER A NEÓN ALTO", Objetivo);
            StringAssert.StartsWith(DirectorPandillas.PrefijoTianguisTuyo, pantalla.Ayuda);
            StringAssert.Contains(Registro<IPerfilJugador>.Actual.NombrePandilla.ToUpperInvariant(), pantalla.Ayuda, "Con el nombre de tu pandilla");
            Assert.AreEqual(0, SnakesPrendidos(), "Llegar con EL HACKER no es una balacera");

            // e. EL HACKER: E saca los tres subtítulos (captura 1) y después CONTINUARÁ (captura 2).
            PersonajeApoyo hacker = Profe(DirectorPandillas.IdHacker);
            Assert.IsNotNull(hacker);
            Assert.AreEqual("neon", territorio.ZonaEn(hacker.transform.position), "EL HACKER está en Neón Alto");
            yield return PonerFrente(jugador, hacker.transform);
            yield return Pulsar(teclado.eKey);
            Assert.AreEqual(PasoHistoria.Continuara, director.Paso);
            HashSet<string> dichos = new HashSet<string>();
            bool capturado = false;
            float fin = Time.realtimeSinceStartup + 20f;
            while (!pantalla.ContinuaraVisible)
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "CONTINUARÁ no salió");
                if (pantalla.Subtitulo.Length > 0)
                {
                    dichos.Add(pantalla.Subtitulo);
                }
                if (!capturado && pantalla.Subtitulo == DirectorPandillas.SubtitulosHacker[1])
                {
                    capturado = true;
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capturar(jugador, "T-418-1.png");
                }
                Assert.AreEqual(0, SnakesPrendidos(), "Antes de Libre no hay SNAKES");
                yield return null;
            }
            CollectionAssert.AreEquivalent(DirectorPandillas.SubtitulosHacker, dichos, "Los tres subtítulos de EL HACKER, y nada más");
            Assert.IsFalse(Registro<IObjetivos>.Actual.HayObjetivo, "En CONTINUARÁ no hay objetivo");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capturar(jugador, "T-418-2.png");
            Assert.AreEqual("NOVA ARMA A LOS SNAKES. NEÓN ALTO TE ESPERA.", PantallaHistoriaPandillas.LineaContinuara);

            // f. Enter: juego libre y se prenden los SNAKES.
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreEqual(0, GruposSnakesPrendidos());
            yield return Pulsar(teclado.enterKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Libre, "cerrar CONTINUARÁ", 5f);
            Assert.AreEqual(2, GruposSnakesPrendidos(), "En Libre se prenden los dos grupos de SNAKES");

            // g. Neón Alto se conquista como El Tianguis: 70 de respeto, su jefe sale y al vencerlo la zona es tuya.
            JefePandilla jefeNeon = Jefe("neon");
            Assert.IsNotNull(jefeNeon);
            Assert.AreEqual("neon", territorio.ZonaEn(jefeNeon.transform.position));
            Assert.AreNotEqual(IdPandilla.Jugador, territorio.Dueno("neon"));
            for (int i = 0; i < VencidosParaJefe; i++)
            {
                territorio.RegistrarVencido(jefeNeon.transform.position, IdPandilla.Viboras, false);
            }
            Assert.GreaterOrEqual(territorio.Respeto("neon"), 70);
            yield return EsperarHasta(() => jefeNeon.Vivo, "aparece el jefe de los SNAKES", 10f);
            jefeNeon.Jefe.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, jefeNeon.Jefe.transform.position, Vector3.forward, null));
            yield return EsperarHasta(() => territorio.Dueno("neon") == IdPandilla.Jugador, "conquistar Neón Alto", 5f);
            Assert.AreEqual(PasoHistoria.Libre, director.Paso);

            // h. Solo con capturas: la calle principal de noche con los peatones (D-tony-255: 6 de día y 8 de noche), mirando al más cercano.
            if (Environment.GetEnvironmentVariable(VariableCaptura) == "1" && !Application.isBatchMode)
            {
                Registro<IRelojMundo>.Actual.AvanzarHasta(22f);
                yield return new WaitForSecondsRealtime(3f);
                Vector3 calle = new Vector3(-30f, 0.2f, -2f);
                Transform cercano = null;
                foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb.GetType().Name == "PeatonMundo" && mb.gameObject.activeInHierarchy)
                    {
                        Debug.Log("[T-418] Peatón en " + mb.transform.position + " a " + Vector3.Distance(mb.transform.position, calle).ToString("0") + " m de la calle principal");
                        if (cercano == null || (mb.transform.position - calle).sqrMagnitude < (cercano.position - calle).sqrMagnitude)
                        {
                            cercano = mb.transform;
                        }
                    }
                }
                Assert.IsNotNull(cercano, "Hay peatones de noche");
                // Desde la calle principal, a unos 7 m del peatón, mirándolo (se mueve: se apunta justo antes de la foto).
                Vector3 desdeCalle = cercano.position - calle;
                desdeCalle.y = 0f;
                Vector3 punto = cercano.position - desdeCalle.normalized * 7f;
                punto.y = calle.y;
                jugador.Colocar(new Pose(punto, Quaternion.LookRotation(desdeCalle.normalized, Vector3.up)));
                yield return new WaitForSecondsRealtime(1f);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 mira = cercano.position - jugador.Raiz.position;
                    mira.y = 0f;
                    jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(mira.normalized, Vector3.up)));
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                Debug.Log("[T-418] Foto de la calle: peatón a " + Vector3.Distance(cercano.position, jugador.Raiz.position).ToString("0.0") + " m, ángulo "
                    + Vector3.Angle(jugador.Camara.transform.forward, cercano.position - jugador.Camara.transform.position).ToString("0") + "°");
                yield return Capturar(jugador, "T-418-3.png");
            }
        }
    }
}
