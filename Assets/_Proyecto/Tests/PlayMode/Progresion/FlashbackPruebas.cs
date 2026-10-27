using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.UI.Historia;
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
    /// T-131: el flashback. La escena Pruebas_Flashback corre completa con los datos reales; el salto, el toque corto, el punto que
    /// falta y la etapa equivocada se prueban con el mismo prefab y datos cortos. El cambio de escena se intercepta.
    /// </summary>
    public class FlashbackPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/Pruebas_Flashback.unity";
        private const string RutaPrefab = "Assets/_Proyecto/Progresion/Prologo/Prefabs/Flashback.prefab";
        private const string RutaDatosEscenasPruebas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Pruebas.asset";
        private const string RutaDatosBase = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosFlashback_Base.asset";
        private const string PrefijoPunto = "Flashback_Plano_";
        private const string VariableCaptura = "CAPTURAR_T131";
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;
        private const int ProfundidadCaptura = 24;
        private const int SaltoDePixeles = 97;
        private const float VariacionMinima = 0.05f;
        private const float DistanciaPlanoCanvas = 0.35f;
        private const int CuadrosEntreRevisiones = 30;
        private const float TolerPose = 0.01f;
        private const float TolerDuracion = 0.5f;
        private const float SegundosPlanoCorto = 0.3f;
        private const float SegundosFundidoCorto = 0.05f;
        private const float SegundosPlanoLargo = 5f;
        private const float SegundosFundidoSalto = 0.1f;
        private const float SegundosSaltar = 1f;
        private const float MaxSegundosSalto = 3f;
        private const float SegundosToque = 0.2f;
        private const float SegundosTrasToque = 1.5f;
        private const float MaxSegundosCortos = 15f;
        private const int CantidadPlanos = 6;

        [Serializable]
        private class DatosHistoria
        {
            public string etapa;
        }

        private Mouse raton;
        private Keyboard teclado;
        private string carpeta;
        private DatosEscenas datosPruebas;
        private readonly List<string> rutasPedidas = new List<string>();
        private readonly List<string> errores = new List<string>();
        private readonly List<string> avisos = new List<string>();
        private DirectorFlashback director;
        private readonly List<PuntoHistoriaDePrueba> puntos = new List<PuntoHistoriaDePrueba>();
        private HashSet<GameObject> raicesIniciales;


        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            InputSystem.DisableAllEnabledActions();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            MapasDeAccionesDePrueba.Reiniciar(teclado, raton);
            carpeta = Path.Combine(Application.temporaryCachePath, "flashback_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            ServicioGuardado.RutaForzada = Path.Combine(carpeta, "partida.json");
            datosPruebas = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenasPruebas);
            rutasPedidas.Clear();
            errores.Clear();
            avisos.Clear();
            puntos.Clear();
            director = null;
            CambioEscena.Interceptar(ruta => rutasPedidas.Add(ruta));
            CambioEscena.ConsumirTransicion();
            Application.logMessageReceived += RegistrarLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= RegistrarLog;
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

        private void RegistrarLog(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
            {
                errores.Add(mensaje);
            }
            else if (tipo == LogType.Warning)
            {
                avisos.Add(mensaje);
            }
        }

        // ---- Preparación ----

        // Deja en el archivo de partida la etapa Flashback y marca una transición, para que ServicioGuardado la cargue al arrancar.
        private void PrepararPartidaEnFlashback()
        {
            DatosHistoria historia = new DatosHistoria { etapa = EtapaPrologo.Flashback.ToString() };
            string json = ArchivoPartida.Escribir(
                new List<PiezaGuardada> { new PiezaGuardada { clave = ServicioHistoria.Clave, datos = JsonUtility.ToJson(historia) } },
                DateTime.UtcNow.ToString("o"));
            File.WriteAllText(ServicioGuardado.RutaForzada, json);
            CambioEscena.Ir("preparacion");
            rutasPedidas.Clear();
        }

        private IEnumerator CargarEscena()
        {
            PrepararPartidaEnFlashback();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            director = Object.FindFirstObjectByType<DirectorFlashback>();
            Assert.IsNotNull(director, "Pruebas_Flashback trae el director");
            foreach (PuntoHistoriaDePrueba punto in Object.FindObjectsByType<PuntoHistoriaDePrueba>(FindObjectsSortMode.None))
            {
                puntos.Add(punto);
            }
            Assert.AreEqual(CantidadPlanos, puntos.Count, "Pruebas_Flashback trae seis puntos");
        }

        private static DatosFlashback CrearDatosCortos(float segundosPlano, float fundido)
        {
            List<PlanoFlashback> planos = new List<PlanoFlashback>();
            for (int i = 1; i <= CantidadPlanos; i++)
            {
                planos.Add(new PlanoFlashback { idPunto = PrefijoPunto + i, segundos = segundosPlano, subtitulo = "Texto " + i });
            }
            DatosFlashback datos = ScriptableObject.CreateInstance<DatosFlashback>();
            datos.Fijar(planos, fundido, "Mantén ESPACIO para saltar", SegundosSaltar);
            return datos;
        }

        // Instancia el prefab con datos cortos y puntos propios (todos menos el que se pida omitir). Se llama en el mismo cuadro que
        // el Instantiate: el director arranca un cuadro después, así que ya lee los datos nuevos.
        private void ArmarConDatosCortos(float segundosPlano, float fundido, EtapaPrologo etapa, int omitirPunto)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            director = instancia.GetComponent<DirectorFlashback>();
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("datos").objectReferenceValue = CrearDatosCortos(segundosPlano, fundido);
            so.FindProperty("datosEscenas").objectReferenceValue = datosPruebas;
            so.ApplyModifiedPropertiesWithoutUndo();

            ServicioHistoria historia = Registro<IHistoria>.Actual as ServicioHistoria;
            Assert.IsNotNull(historia, "El prefab trae el ServicioHistoria");
            if (etapa != EtapaPrologo.Ninguna)
            {
                Assert.IsTrue(historia.Avanzar(etapa), "La historia llega a " + etapa);
            }

            for (int i = 1; i <= CantidadPlanos; i++)
            {
                if (i == omitirPunto)
                {
                    continue;
                }
                GameObject objeto = new GameObject("Punto " + i);
                objeto.transform.position = new Vector3(i * 3f, 1f, i);
                PuntoHistoriaDePrueba punto = objeto.AddComponent<PuntoHistoriaDePrueba>();
                SerializedObject soPunto = new SerializedObject(punto);
                soPunto.FindProperty("id").stringValue = PrefijoPunto + i;
                soPunto.ApplyModifiedPropertiesWithoutUndo();
                puntos.Add(punto);
            }
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

        private static int CamarasActivas()
        {
            int camaras = 0;
            foreach (Camera camara in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camara.enabled && camara.gameObject.activeInHierarchy)
                {
                    camaras++;
                }
            }
            return camaras;
        }

        private static void AfirmarUnaCamaraYUnOyente(string cuando)
        {
            Assert.AreEqual(1, OyentesActivos(), "Un solo AudioListener activo " + cuando);
            Assert.AreEqual(1, CamarasActivas(), "Una sola cámara activa " + cuando);
        }

        private IEnumerator EsperarRuta(float maxSegundos, string que)
        {
            float fin = Time.realtimeSinceStartup + maxSegundos;
            int cuadros = 0;
            while (rutasPedidas.Count == 0 && Time.realtimeSinceStartup < fin)
            {
                if (cuadros++ % CuadrosEntreRevisiones == 0)
                {
                    AfirmarUnaCamaraYUnOyente("mientras se espera: " + que);
                }
                yield return null;
            }
            Assert.AreEqual(1, rutasPedidas.Count, "Se pidió un cambio de escena: " + que);
        }

        private IEnumerator EsperarPlano(float maxSegundos)
        {
            float fin = Time.realtimeSinceStartup + maxSegundos;
            while (director.IndicePlano < 0 && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(director.IndicePlano, 0, "El primer plano empezó");
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

        // Captura de lo que ve el jugador (solo con CAPTURAR_T131=1). Los lienzos en Overlay no salen en una cámara a textura:
        // se pasan un instante a ScreenSpaceCamera con esa cámara y se devuelven como estaban.
        private void Capturar(string ruta)
        {
            if (Environment.GetEnvironmentVariable(VariableCaptura) != "1")
            {
                return;
            }
            Camera camara = director.Camara;
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
                float luz = pixeles[i].g / 255f;
                minimo = Mathf.Min(minimo, luz);
                maximo = Mathf.Max(maximo, luz);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ruta)));
            File.WriteAllBytes(ruta, imagen.EncodeToPNG());
            Object.DestroyImmediate(imagen);
            rt.Release();
            Object.DestroyImmediate(rt);
            Assert.GreaterOrEqual(maximo - minimo, VariacionMinima, "La captura no es un solo color: " + ruta);
        }

        // ---- Pruebas ----

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Escena_CorreLosSeisPlanos_PoseSubtituloYActivacionesYTerminaEnDespertar()
        {
            yield return CargarEscena();
            DatosFlashback datos = AssetDatabase.LoadAssetAtPath<DatosFlashback>(RutaDatosBase);
            Assert.AreSame(datos, director.Datos, "El prefab usa DatosFlashback_Base");

            float inicio = -1f;
            float final = -1f;
            int siguiente = 0;
            int capturaPendiente = -1;
            List<string> fallos = new List<string>();
            director.PlanoIniciado += indice =>
            {
                if (indice == 0)
                {
                    inicio = Time.realtimeSinceStartup;
                }
                PlanoFlashback plano = datos.Planos[indice];
                PuntoHistoriaDePrueba punto = puntos.Find(p => p.Id == plano.idPunto);
                if (indice != siguiente)
                {
                    fallos.Add("plano fuera de orden: " + indice);
                }
                siguiente = indice + 1;
                if (punto == null)
                {
                    fallos.Add("no hay punto " + plano.idPunto);
                    return;
                }
                Pose pose = punto.Pose;
                Transform camara = director.Camara.transform;
                if (Vector3.Distance(camara.position, pose.position) > TolerPose
                    || Quaternion.Angle(camara.rotation, pose.rotation) > 1f)
                {
                    fallos.Add("la cámara no está en la pose del plano " + (indice + 1));
                }
                if (director.Subtitulos.Subtitulo != plano.subtitulo || !director.Subtitulos.SubtituloVisible)
                {
                    fallos.Add("subtítulo del plano " + (indice + 1) + ": '" + director.Subtitulos.Subtitulo + "'");
                }
                if (punto.LlamadasActivar != 1 || !punto.Activo)
                {
                    fallos.Add("el punto " + plano.idPunto + " no recibió un solo Activar(true)");
                }
                if (indice == 0 || indice == 3)
                {
                    capturaPendiente = indice;
                }
            };
            director.Finalizado += () => final = Time.realtimeSinceStartup;

            float limite = Time.realtimeSinceStartup + datos.SegundosTotales + 20f;
            int cuadros = 0;
            while (rutasPedidas.Count == 0 && Time.realtimeSinceStartup < limite)
            {
                if (capturaPendiente >= 0 && PantallaFundido.Actual != null && PantallaFundido.Actual.Alfa < 0.001f)
                {
                    Capturar("docs/capturas/T-131-" + (capturaPendiente == 0 ? 1 : 2) + ".png");
                    capturaPendiente = -1;
                }
                if (cuadros++ % CuadrosEntreRevisiones == 0)
                {
                    AfirmarUnaCamaraYUnOyente("durante el flashback");
                }
                yield return null;
            }

            Assert.AreEqual(1, rutasPedidas.Count, "Al terminar pide un solo cambio de escena");
            CollectionAssert.IsEmpty(fallos, string.Join("; ", fallos));
            Assert.AreEqual(CantidadPlanos, siguiente, "Corrieron los seis planos");
            Assert.Greater(final, 0f, "Avisó que terminó");
            Assert.AreEqual(datos.SegundosTotales, final - inicio, TolerDuracion, "Dura la suma de planos y fundidos");
            foreach (PuntoHistoriaDePrueba punto in puntos)
            {
                Assert.AreEqual(2, punto.LlamadasActivar, punto.Id + " recibió Activar(true) y Activar(false) una vez");
                Assert.IsFalse(punto.Activo, punto.Id + " quedó desactivado");
            }
            Assert.AreEqual(EtapaPrologo.Despertar, Registro<IHistoria>.Actual.Etapa, "La etapa es Despertar");
            StringAssert.Contains("Despertar", PiezaGuardada(ServicioHistoria.Clave), "La etapa Despertar quedó guardada");
            Assert.AreEqual(datosPruebas.RutaCiudad, rutasPedidas[0], "Va a la ciudad de DatosEscenas_Pruebas");
            CollectionAssert.IsEmpty(errores, "Sin errores en consola");
        }

        [UnityTest]
        public IEnumerator Saltar_MantenerEspacioUnSegundo_TerminaRapidoConElMismoFinal()
        {
            ArmarConDatosCortos(SegundosPlanoLargo, SegundosFundidoSalto, EtapaPrologo.Flashback, 0);
            yield return EsperarPlano(MaxSegundosCortos);
            yield return null;
            PuntoHistoriaDePrueba actual = puntos[director.IndicePlano];
            Assert.IsTrue(actual.Activo, "El punto del plano en curso está activo");

            float inicio = Time.realtimeSinceStartup;
            Press(teclado.spaceKey);
            yield return EsperarRuta(MaxSegundosSalto, "saltar con ESPACIO");
            float duracion = Time.realtimeSinceStartup - inicio;
            Release(teclado.spaceKey);

            Assert.GreaterOrEqual(duracion, SegundosSaltar - TolerPose, "No salta antes de mantener un segundo");
            Assert.Less(duracion, MaxSegundosSalto, "Salta en menos de tres segundos");
            Assert.IsTrue(director.Saltado, "Quedó marcado como saltado");
            Assert.IsFalse(actual.Activo, "El punto en curso se desactivó al saltar");
            Assert.AreEqual(2, actual.LlamadasActivar, "Activar(true) y Activar(false) una vez");
            Assert.AreEqual(EtapaPrologo.Despertar, Registro<IHistoria>.Actual.Etapa, "La etapa es Despertar");
            StringAssert.Contains("Despertar", PiezaGuardada(ServicioHistoria.Clave), "La etapa Despertar quedó guardada");
            Assert.AreEqual(datosPruebas.RutaCiudad, rutasPedidas[0], "Va a la ciudad de DatosEscenas_Pruebas");
            CollectionAssert.IsEmpty(errores, "Sin errores en consola");
        }

        [UnityTest]
        public IEnumerator Saltar_ToqueCorto_NoSalta()
        {
            ArmarConDatosCortos(SegundosPlanoLargo, SegundosFundidoSalto, EtapaPrologo.Flashback, 0);
            yield return EsperarPlano(MaxSegundosCortos);
            yield return null;

            Press(teclado.spaceKey);
            float fin = Time.realtimeSinceStartup + SegundosToque;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            Release(teclado.spaceKey);
            fin = Time.realtimeSinceStartup + SegundosTrasToque;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }

            Assert.IsFalse(director.Saltado, "Un toque de 0.2 s no salta");
            Assert.IsTrue(director.Corriendo, "El flashback sigue corriendo");
            Assert.AreEqual(0, rutasPedidas.Count, "No se pidió cambiar de escena");
            Assert.AreEqual(EtapaPrologo.Flashback, Registro<IHistoria>.Actual.Etapa, "La etapa sigue siendo Flashback");
            CollectionAssert.IsEmpty(errores, "Sin errores en consola");
        }

        [UnityTest]
        public IEnumerator FaltaUnPunto_CorreCompletoConUnWarningYSinErrores()
        {
            const int omitido = 3;
            ArmarConDatosCortos(SegundosPlanoCorto, SegundosFundidoCorto, EtapaPrologo.Flashback, omitido);
            yield return EsperarRuta(MaxSegundosCortos, "el flashback sin el punto 3");

            int avisosDelPunto = avisos.FindAll(a => a.Contains(PrefijoPunto + omitido)).Count;
            Assert.AreEqual(1, avisosDelPunto, "Un warning por el punto que falta");
            Assert.IsFalse(director.Saltado, "No se saltó");
            Assert.IsTrue(director.Terminado, "Corrió completo");
            foreach (PuntoHistoriaDePrueba punto in puntos)
            {
                Assert.AreEqual(2, punto.LlamadasActivar, punto.Id + " recibió Activar(true) y Activar(false) una vez");
            }
            Assert.AreEqual(EtapaPrologo.Despertar, Registro<IHistoria>.Actual.Etapa, "La etapa es Despertar");
            Assert.AreEqual(datosPruebas.RutaCiudad, rutasPedidas[0], "Va a la ciudad de DatosEscenas_Pruebas");
            CollectionAssert.IsEmpty(errores, "Sin errores en consola");
        }

        [UnityTest]
        public IEnumerator FueraDeLaEtapaFlashback_CorreIgualYSoloAvisa()
        {
            ArmarConDatosCortos(SegundosPlanoCorto, SegundosFundidoCorto, EtapaPrologo.Ninguna, 0);
            float fin = Time.realtimeSinceStartup + MaxSegundosCortos;
            while (!director.Terminado && Time.realtimeSinceStartup < fin)
            {
                yield return null;
            }
            yield return null;

            Assert.IsTrue(director.Terminado, "Corrió los seis planos");
            Assert.AreEqual(0, rutasPedidas.Count, "No cambia de escena");
            Assert.AreEqual(EtapaPrologo.Ninguna, Registro<IHistoria>.Actual.Etapa, "No toca la etapa");
            Assert.AreEqual(1, avisos.FindAll(a => a.Contains("fuera de la etapa Flashback")).Count, "Avisa con un warning");
            CollectionAssert.IsEmpty(errores, "Sin errores en consola");
        }
    }
}
