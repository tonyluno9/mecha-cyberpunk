using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-747 y T-721 en la ciudad de verdad (Ciudad_Prologo), con la entrada simulada: M abre el mapa grande con la captura cenital de la ciudad, la flecha del
    /// jugador cae sobre su lugar de la imagen (en el mapa y en el minimapa), los iconos de los lugares caen sobre la imagen y los nombres no se traslapan
    /// (ni con zoom). Guarda docs/capturas/s1e-T-747-mapa.png, -minimapa.png y -mecanico.png.
    /// </summary>
    public class MapaPlayT747Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";

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
            PuntoMarcadoMapa.Quitar();
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
            CatalogoDestinosMapa.Limpiar();
            yield return null;
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        // Dónde cae (px de pantalla) un punto del mundo dentro de la imagen de un mapa, según su lugar en la imagen (u, v).
        private static Vector2 PuntoDeImagen(RawImage imagen, Vector2 mundo)
        {
            Vector2 uv = MapaImagen.MundoAUv(mundo);
            Rect r = imagen.rectTransform.rect;
            Vector3 local = new Vector3(r.xMin + uv.x * r.width, r.yMin + uv.y * r.height, 0f);
            return imagen.rectTransform.TransformPoint(local);
        }

        private static Rect CajaDelNombre(Text nombre)
        {
            float escala = nombre.canvas.scaleFactor;
            Vector2 tamano = new Vector2(nombre.preferredWidth, nombre.fontSize * 1.2f) * escala;
            return new Rect((Vector2)nombre.rectTransform.position - tamano * 0.5f, tamano);
        }

        // La tecla M con la entrada simulada, en la ciudad real: abre el mapa con la imagen y M o Esc lo cierran.
        [UnityTest]
        public IEnumerator M_AbreElMapa_ConLaImagen_YMOEscLoCierran()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float finCiudad = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < finCiudad)
            {
                DirectorPandillas directorCiudad = Object.FindFirstObjectByType<DirectorPandillas>();
                if (directorCiudad != null && directorCiudad.Iniciado && Registro<IJugador>.Actual != null && Registro<IObjetivos>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            Assert.IsFalse(mapa.Abierto);
            Press(teclado.mKey);
            yield return null;
            Release(teclado.mKey);
            yield return null;
            yield return null;
            Assert.IsTrue(mapa.Abierto, "M abre el mapa grande");
            Assert.IsNotNull(mapa.ImagenMapa, "El mapa grande dibuja la imagen de la ciudad");
            Assert.AreSame(MapaImagen.Cargar(), mapa.ImagenMapa.texture);
            Assert.IsTrue(mapa.ImagenMapa.gameObject.activeInHierarchy);
            Assert.AreEqual(4, mapa.NombresBarrios.Count, "Los cuatro barrios con su nombre");
            yield return Pulsar(teclado.mKey);
            yield return null;
            Assert.IsFalse(mapa.Abierto, "M lo cierra");
            yield return Pulsar(teclado.mKey);
            yield return null;
            Assert.IsTrue(mapa.Abierto);
            yield return Pulsar(teclado.escapeKey);
            yield return null;
            Assert.IsFalse(mapa.Abierto, "Esc lo cierra");
        }

        [UnityTest]
        public IEnumerator FlechaEIconos_CaenSobreSuLugarDeLaImagen_EnElMapaYElMinimapa()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float finCiudad = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < finCiudad)
            {
                DirectorPandillas directorCiudad = Object.FindFirstObjectByType<DirectorPandillas>();
                if (directorCiudad != null && directorCiudad.Iniciado && Registro<IJugador>.Actual != null && Registro<IObjetivos>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
            IJugador jugador = Registro<IJugador>.Actual;
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            Minimapa minimapa = Object.FindFirstObjectByType<Minimapa>();
            Assert.IsNotNull(mapa);
            Assert.IsNotNull(minimapa);
            Assert.IsNotNull(MapaImagen.Cargar(), "La imagen está en Resources/" + MapaImagen.RutaRecurso);

            Vector3 sitio = new Vector3(-62f, 0.5f, -20f);
            jugador.Colocar(new Pose(sitio, Quaternion.LookRotation(Vector3.right)));
            yield return null;
            yield return null;

            yield return Pulsar(teclado.mKey);
            Assert.IsTrue(mapa.Abierto, "M abre el mapa grande");
            Assert.IsNotNull(mapa.ImagenMapa, "El mapa grande dibuja la imagen de la ciudad");
            Assert.AreSame(MapaImagen.Cargar(), mapa.ImagenMapa.texture);
            Assert.IsTrue(mapa.ImagenMapa.gameObject.activeInHierarchy);
            Assert.AreEqual(4, mapa.NombresBarrios.Count, "Los cuatro barrios con su nombre");

            // La flecha del jugador cae exactamente donde la imagen tiene ese punto del mundo.
            Vector2 flecha = mapa.PuntoJugador.position;
            Vector2 enLaImagen = PuntoDeImagen(mapa.ImagenMapa, new Vector2(jugador.Raiz.position.x, jugador.Raiz.position.z));
            Assert.AreEqual(0f, Vector2.Distance(flecha, enLaImagen), 1.5f, "La flecha cae sobre su lugar de la imagen (mapa grande)");
            Assert.AreEqual(0f, Vector2.Distance(flecha, mapa.PantallaDe(jugador.Raiz.position)), 1.5f, "y coincide con PantallaDe");

            // Cada lugar del catálogo cae sobre su punto de la imagen.
            foreach (DestinoMapa d in CatalogoDestinosMapa.Todos)
            {
                Image icono = mapa.IconoDestino(d.Id);
                if (icono == null || !CatalogoDestinosMapa.Posicion(d.Id, out Vector3 donde))
                {
                    continue;
                }
                Assert.AreEqual(0f, Vector2.Distance(icono.rectTransform.position, PuntoDeImagen(mapa.ImagenMapa, new Vector2(donde.x, donde.z))), 1.5f,
                    "El icono de " + d.Id + " cae sobre su lugar de la imagen");
            }
            Camera camara = jugador.Camara;
            CapturaUI.Capturar("docs/capturas/s1e-T-747-mapa.png", camara);

            // Cierra y el minimapa usa la misma imagen, con la flecha en el centro sobre el lugar del jugador.
            mapa.Cerrar();
            Assert.IsFalse(mapa.Abierto);
            yield return null;
            minimapa.Actualizar();
            Assert.IsNotNull(minimapa.ImagenMapa, "El minimapa dibuja la misma imagen");
            Assert.AreSame(MapaImagen.Cargar(), minimapa.ImagenMapa.texture);
            Vector2 centro = minimapa.Flecha.position;
            Vector2 sobreLaImagen = PuntoDeImagen(minimapa.ImagenMapa, new Vector2(jugador.Raiz.position.x, jugador.Raiz.position.z));
            Assert.AreEqual(0f, Vector2.Distance(centro, sobreLaImagen), 1.5f, "La flecha del minimapa cae sobre su lugar de la imagen");
            yield return null;
            CapturaUI.Capturar("docs/capturas/s1e-T-747-minimapa.png", camara);
        }

        [UnityTest]
        public IEnumerator Nombres_NoSeTraslapanEnElMapa_NiConZoomSobreElMecanico()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            float finCiudad = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < finCiudad)
            {
                DirectorPandillas directorCiudad = Object.FindFirstObjectByType<DirectorPandillas>();
                if (directorCiudad != null && directorCiudad.Iniciado && Registro<IJugador>.Actual != null && Registro<IObjetivos>.Actual != null)
                {
                    break;
                }
                yield return null;
            }
            Assert.IsNotNull(Registro<IJugador>.Actual, "La ciudad arrancó");
            yield return new WaitForSecondsRealtime(1f);
            IJugador jugador = Registro<IJugador>.Actual;
            PantallaMapa mapa = Object.FindFirstObjectByType<PantallaMapa>();
            jugador.Colocar(new Pose(new Vector3(-55f, 0.5f, -55f), Quaternion.LookRotation(Vector3.right)));
            yield return null;
            mapa.Abrir();
            yield return null;
            yield return null;
            Assert.IsNotNull(mapa.IconoDestino(IdDestinoMapa.Mecanico), "El mecánico sale en el mapa");
            Assert.IsNotNull(mapa.IconoDestino(IdDestinoMapa.Tianguis), "El tianguis sale en el mapa");
            VerSinTraslape(mapa, "zoom 1");

            // Rueda sobre el icono del mecánico hasta el zoom máximo (misma entrada que un jugador).
            CatalogoDestinosMapa.Posicion(IdDestinoMapa.Mecanico, out Vector3 mecanico);
            Set(Mouse.current.position, mapa.PantallaDe(mecanico));
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                Set(Mouse.current.scroll, new Vector2(0f, 120f));
                yield return null;
                Set(Mouse.current.scroll, Vector2.zero);
                yield return null;
            }
            Assert.Greater(mapa.Zoom, 2.5f, "La rueda acerca el mapa");
            yield return null;
            VerSinTraslape(mapa, "zoom " + mapa.Zoom);
            CapturaUI.Capturar("docs/capturas/s1e-T-747-mecanico.png", jugador.Camara);
        }

        private static void VerSinTraslape(PantallaMapa mapa, string situacion)
        {
            List<(IdDestinoMapa id, Rect caja, Rect icono)> visibles = new List<(IdDestinoMapa, Rect, Rect)>();
            foreach (DestinoMapa d in CatalogoDestinosMapa.Todos)
            {
                Image icono = mapa.IconoDestino(d.Id);
                Text nombre = IconosMapa.NombreDe(icono);
                if (icono == null || nombre == null)
                {
                    continue;
                }
                float escala = nombre.canvas.scaleFactor;
                Vector2 lado = icono.rectTransform.sizeDelta * escala;
                visibles.Add((d.Id, CajaDelNombre(nombre), new Rect((Vector2)icono.rectTransform.position - lado * 0.5f, lado)));
            }
            Assert.GreaterOrEqual(visibles.Count, 2, "Hay nombres de lugares en el mapa (" + situacion + ")");
            for (int i = 0; i < visibles.Count; i++)
            {
                for (int j = 0; j < visibles.Count; j++)
                {
                    if (j > i)
                    {
                        Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(visibles[i].caja, visibles[j].caja),
                            "Los nombres de " + visibles[i].id + " y " + visibles[j].id + " no se traslapan (" + situacion + ")");
                    }
                    if (i != j)
                    {
                        Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(visibles[i].caja, visibles[j].icono),
                            "El nombre de " + visibles[i].id + " no tapa el icono de " + visibles[j].id + " (" + situacion + ")");
                    }
                }
                foreach (IMarcaMapa marca in Registro<IMarcaMapa>.Todos)
                {
                    Image dibujo = mapa.MarcaDe(marca);
                    if (dibujo == null)
                    {
                        continue;
                    }
                    float escalaMarca = dibujo.canvas.scaleFactor;
                    Vector2 ladoMarca = dibujo.rectTransform.sizeDelta * escalaMarca;
                    Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(visibles[i].caja, new Rect((Vector2)dibujo.rectTransform.position - ladoMarca * 0.5f, ladoMarca)),
                        "El nombre de " + visibles[i].id + " no tapa la marca " + marca.Tipo + " (" + situacion + ")");
                }
                foreach (Text barrio in mapa.NombresBarrios)
                {
                    Assert.AreEqual(0f, CalculoEtiquetasMapa.Traslape(visibles[i].caja, CajaDelNombre(barrio)),
                        "El nombre de " + visibles[i].id + " no tapa el nombre del barrio " + barrio.text + " (" + situacion + ")");
                }
            }
        }
    }
}
