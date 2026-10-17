using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.UI.Creacion;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>Título y creación de personaje de T-126, con teclado simulado y la escena real.</summary>
    public class CreacionPersonajePruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Tony/CreacionPersonaje.unity";
        private const string RutaDatosEscenas = "Assets/_Proyecto/Progresion/Datos/DatosEscenas_Demo.asset";
        private const string RutaCaptura1 = "docs/capturas/T-126-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-126-2.png";
        private const int FramesEspera = 5;
        private const float MaxSegundosFundido = 4f;
        private const int AnchoCaptura = 1280;
        private const int AltoCaptura = 720;

        private Keyboard teclado;
        private string carpeta;
        private string rutaPartida;
        private DatosEscenas datos;
        private string rutaPedida;
        private PantallaCreacion pantalla;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            carpeta = Path.Combine(Application.temporaryCachePath, "creacion_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            rutaPartida = Path.Combine(carpeta, "partida.json");
            ServicioGuardado.RutaForzada = rutaPartida;
            CambioEscena.ConsumirTransicion();
            rutaPedida = null;
            CambioEscena.Interceptar(ruta => rutaPedida = ruta);
            datos = AssetDatabase.LoadAssetAtPath<DatosEscenas>(RutaDatosEscenas);
        }

        public override void TearDown()
        {
            CambioEscena.Interceptar(null);
            CambioEscena.ConsumirTransicion();
            ServicioGuardado.RutaForzada = null;
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            foreach (GameObject objeto in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(objeto);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < FramesEspera; i++) yield return null;
            pantalla = Object.FindFirstObjectByType<PantallaCreacion>();
            Assert.IsNotNull(pantalla, "la escena trae PantallaCreacion");
        }

        // El catálogo trae Ninguno + 3 implantes (4 opciones): la vuelta completa tarda tantos pasos como opciones.
        private static MechaCyberpunk.Personaje.CatalogoApariencia CatalogoDeLaEscena()
        {
            return AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Personaje.CatalogoApariencia>("Assets/_Proyecto/Personaje/Datos/CatalogoApariencia_Base.asset");
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Escribir(string texto)
        {
            foreach (char letra in texto)
            {
                InputSystem.QueueTextEvent(teclado, letra);
            }
            yield return null;
            yield return null;
        }

        private IEnumerator Veces(KeyControl tecla, int veces)
        {
            for (int i = 0; i < veces; i++) yield return Tecla(tecla);
        }

        // Escribe una partida en el archivo forzado, con la etapa pedida, sin pasar por la escena.
        private void EscribirPartida(EtapaPrologo etapa)
        {
            var piezas = new List<PiezaGuardada>
            {
                new PiezaGuardada { clave = ServicioHistoria.Clave, datos = "{\"etapa\":\"" + etapa + "\"}" },
                new PiezaGuardada { clave = ServicioPerfil.Clave, datos = "{\"nombre\":\"Ana\",\"alias\":\"EL RAYO\",\"cara\":1,\"ropa\":1,\"implante\":1}" }
            };
            File.WriteAllText(rutaPartida, ArchivoPartida.Escribir(piezas, "2026-01-01T00:00:00Z"));
        }

        private IEnumerator EsperarRuta()
        {
            float fin = Time.unscaledTime + MaxSegundosFundido;
            while (rutaPedida == null && Time.unscaledTime < fin) yield return null;
        }

        private Texture2D Capturar()
        {
            GameObject camObjeto = new GameObject("CamCaptura", typeof(Camera));
            Camera camara = camObjeto.GetComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            Canvas canvas = pantalla.Canvas;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camara;
            canvas.planeDistance = 1f;
            RenderTexture destino = RenderTexture.GetTemporary(AnchoCaptura, AltoCaptura, 24);
            camara.targetTexture = destino;
            camara.Render();
            RenderTexture previa = RenderTexture.active;
            RenderTexture.active = destino;
            Texture2D textura = new Texture2D(AnchoCaptura, AltoCaptura, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, AnchoCaptura, AltoCaptura), 0, 0);
            textura.Apply();
            RenderTexture.active = previa;
            camara.targetTexture = null;
            RenderTexture.ReleaseTemporary(destino);
            Object.Destroy(camObjeto);
            return textura;
        }

        [UnityTest]
        public IEnumerator Escena_TieneUnAudioListenerYUnaCamaraActivaAdemasDeLaVistaPrevia()
        {
            yield return CargarEscena();
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, "un solo AudioListener");
            int otras = 0;
            foreach (Camera camara in Camera.allCameras)
            {
                if (camara != pantalla.Vista.Camara) otras++;
            }
            Assert.AreEqual(1, otras, "una sola cámara activa además de la vista previa");
            Assert.IsTrue(pantalla.Vista.Camara.enabled, "la vista previa dibuja en la creación");
        }

        [UnityTest]
        public IEnumerator SinPartida_ArrancaEnLaCreacion_Y_EmpezarGuardaPerfilHistoriaYPideLaCiudad()
        {
            yield return CargarEscena();
            Assert.AreEqual(PantallaCreacion.Panel.Creacion, pantalla.PanelActual);
            Assert.AreEqual(PantallaCreacion.FilaNombre, pantalla.Fila);

            yield return Escribir("Ana");
            yield return Tecla(teclado.tabKey);
            Assert.AreEqual(PantallaCreacion.FilaAlias, pantalla.Fila);
            yield return Escribir("El Rayo");
            yield return Tecla(teclado.tabKey);
            Assert.AreEqual(PantallaCreacion.FilaCara, pantalla.Fila);
            yield return Veces(teclado.rightArrowKey, 2);
            Assert.AreEqual(2, pantalla.Cara);
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            yield return Tecla(teclado.downArrowKey);
            yield return Veces(teclado.rightArrowKey, CatalogoDeLaEscena().CantidadImplantes);
            Assert.AreEqual(0, pantalla.Implante, "una vuelta completa por los implantes vuelve a Ninguno");
            Assert.AreEqual("◄ Ninguno ►", pantalla.TextoValor(PantallaCreacion.FilaImplante));
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(PantallaCreacion.FilaEmpezar, pantalla.Fila);
            yield return Tecla(teclado.enterKey);
            yield return EsperarRuta();

            Assert.AreEqual(datos.RutaPara(EtapaPrologo.PeleaTutorial), rutaPedida);
            Assert.AreEqual(datos.RutaCiudad, rutaPedida);
            Assert.AreEqual("Ana", Registro<IPerfilJugador>.Actual.Nombre);
            Assert.AreEqual("El Rayo", Registro<IPerfilJugador>.Actual.Alias);
            Assert.AreEqual(new AparienciaJugador(2, 1, 0), Registro<IPerfilJugador>.Actual.Apariencia);
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, Registro<IHistoria>.Actual.Etapa);
            Assert.IsTrue(File.Exists(rutaPartida), "Ir guardó la partida");
            Assert.IsTrue(ArchivoPartida.TryLeer(File.ReadAllText(rutaPartida), out List<PiezaGuardada> piezas, out _));
            var claves = new List<string>();
            foreach (PiezaGuardada pieza in piezas) claves.Add(pieza.clave);
            CollectionAssert.Contains(claves, ServicioPerfil.Clave);
            CollectionAssert.Contains(claves, ServicioHistoria.Clave);
        }

        [UnityTest]
        public IEnumerator Creacion_CambiarOpcion_CambiaElCuerpoDeLaVistaPreviaAlInstante()
        {
            yield return CargarEscena();
            yield return Veces(teclado.downArrowKey, 2);
            yield return Tecla(teclado.rightArrowKey);
            Assert.AreEqual(1, pantalla.Vista.Cuerpo.AparienciaAplicada.Cara);
            yield return Tecla(teclado.leftArrowKey);
            yield return Tecla(teclado.leftArrowKey);
            Assert.AreEqual(3, pantalla.Vista.Cuerpo.AparienciaAplicada.Cara, "da la vuelta");
        }

        [UnityTest]
        public IEnumerator Empezar_ConAliasVacio_NoPideEscenaYMuestraElAviso()
        {
            yield return CargarEscena();
            yield return Escribir("Ana");
            yield return Veces(teclado.upArrowKey, 1);
            Assert.AreEqual(PantallaCreacion.FilaEmpezar, pantalla.Fila, "arriba desde Nombre da la vuelta a EMPEZAR");
            yield return Tecla(teclado.enterKey);
            yield return Esperar(1f);
            Assert.IsNull(rutaPedida);
            Assert.IsFalse(pantalla.Transicionando);
            Assert.AreEqual("Escribe tu nombre y tu alias", pantalla.Aviso);
        }

        [UnityTest]
        public IEnumerator Campo_BackspaceBorra_Y_UnAliasDeVeinteLetrasQuedaEnDieciseis()
        {
            yield return CargarEscena();
            yield return Escribir("Ana");
            yield return Tecla(teclado.backspaceKey);
            Assert.AreEqual("An", pantalla.Nombre);
            yield return Tecla(teclado.tabKey);
            yield return Escribir("ABCDEFGHIJKLMNOPQRST");
            Assert.AreEqual(ReglaAlias.LargoMaximo, pantalla.Alias.Length);
            Assert.AreEqual("ABCDEFGHIJKLMNOP", pantalla.Alias);
        }

        [UnityTest]
        public IEnumerator ConPartida_MuestraElTitulo_Y_ContinuarPideLaCiudad()
        {
            EscribirPartida(EtapaPrologo.PeleaCasera);
            yield return CargarEscena();
            Assert.AreEqual(PantallaCreacion.Panel.Titulo, pantalla.PanelActual);
            Assert.IsFalse(pantalla.Vista.Camara.enabled, "en el título la vista previa está apagada");
            yield return Tecla(teclado.enterKey);
            yield return EsperarRuta();
            Assert.AreEqual(datos.RutaCiudad, rutaPedida);
            Assert.AreEqual(EtapaPrologo.PeleaCasera, Registro<IHistoria>.Actual.Etapa);
            Assert.AreEqual("EL RAYO", Registro<IPerfilJugador>.Actual.Alias);
        }

        [UnityTest]
        public IEnumerator ConPartida_EnFlashback_ContinuarPideElFlashback()
        {
            EscribirPartida(EtapaPrologo.Flashback);
            yield return CargarEscena();
            yield return Tecla(teclado.enterKey);
            yield return EsperarRuta();
            Assert.AreEqual(datos.RutaFlashback, rutaPedida);
        }

        [UnityTest]
        public IEnumerator ConPartida_NuevaPartida_PideConfirmarYAbreLaCreacionVacia()
        {
            EscribirPartida(EtapaPrologo.PeleaCasera);
            yield return CargarEscena();
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(1, pantalla.OpcionTitulo);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(PantallaCreacion.Panel.Confirmar, pantalla.PanelActual);
            StringAssert.Contains("Se borrará tu partida", pantalla.Confirmacion);
            Assert.IsTrue(File.Exists(rutaPartida), "todavía no se borra");

            yield return Tecla(teclado.escapeKey);
            Assert.AreEqual(PantallaCreacion.Panel.Titulo, pantalla.PanelActual, "ESC vuelve");
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.enterKey);
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(PantallaCreacion.Panel.Creacion, pantalla.PanelActual);
            Assert.AreEqual(string.Empty, pantalla.Nombre);
            Assert.AreEqual(string.Empty, pantalla.Alias);
            Assert.AreEqual(0, pantalla.Cara);
            Assert.IsFalse(File.Exists(rutaPartida), "la partida anterior se borró");
            Assert.IsNull(rutaPedida);
        }

        [UnityTest]
        public IEnumerator Capturas_CreacionConCuerpoYTitulo_GuardanPng()
        {
            yield return CargarEscena();
            yield return Escribir("Ana");
            yield return Tecla(teclado.tabKey);
            yield return Escribir("El Rayo");
            yield return Tecla(teclado.tabKey);
            yield return Veces(teclado.rightArrowKey, 2);
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            yield return Tecla(teclado.downArrowKey);
            yield return Tecla(teclado.rightArrowKey);
            for (int i = 0; i < FramesEspera; i++) yield return null;
            Texture2D textura = Capturar();
            Assert.IsTrue(HayNeon(textura), "se dibuja el neón de la fila elegida");
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", RutaCaptura1), textura.EncodeToPNG());
            Object.Destroy(textura);

            foreach (GameObject objeto in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(objeto);
            yield return null;
            Registros.LimpiarTodos();
            EscribirPartida(EtapaPrologo.PeleaCasera);
            yield return CargarEscena();
            Assert.AreEqual(PantallaCreacion.Panel.Titulo, pantalla.PanelActual);
            textura = Capturar();
            Assert.IsTrue(HayNeon(textura), "se dibuja el título neón");
            File.WriteAllBytes(Path.Combine(Application.dataPath, "..", RutaCaptura2), textura.EncodeToPNG());
            Object.Destroy(textura);
        }

        private static bool HayNeon(Texture2D textura)
        {
            for (int y = 0; y < AltoCaptura; y += 4)
            {
                for (int x = 0; x < AnchoCaptura; x += 4)
                {
                    Color pixel = textura.GetPixel(x, y);
                    if (pixel.g > 0.7f && pixel.b > 0.7f && pixel.r < 0.3f) return true;
                }
            }
            return false;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.unscaledTime + segundos;
            while (Time.unscaledTime < fin) yield return null;
        }
    }
}
