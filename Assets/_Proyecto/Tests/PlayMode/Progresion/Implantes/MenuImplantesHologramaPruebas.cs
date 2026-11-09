using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Clinica;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-815: la clínica con la presentación del banco de armas. En Pruebas_Implantes, E abre el lienzo holográfico con el cuerpo del personaje
    /// (malla horneada, 4 submallas) y el espacio elegido resaltado con su marcador; ↓ cambia el espacio resaltado y la tabla antes/después;
    /// arrastrar gira el holograma; Enter Enter instala igual que antes (cobra) y el holograma destella y se desvanece; Esc sale.
    /// Con CAPTURAR_S2N_IMPLANTES=1 guarda docs/capturas/s2n-menu-implantes-*.png.
    /// </summary>
    public class MenuImplantesHologramaPruebas : BaseImplantesPruebas
    {
        private const string VariableCapturaS2n = "CAPTURAR_S2N_IMPLANTES";
        private const int AnchoFoto = 1920;
        private const int AltoFoto = 1080;
        private const float SegundosAnimacion = 0.6f;

        private static void Foto(string nombre)
        {
            if (Environment.GetEnvironmentVariable(VariableCapturaS2n) != "1")
            {
                return;
            }
            GameObject go = new GameObject("CamaraCaptura");
            go.transform.position = new Vector3(0f, -1000f, 0f);
            Camera camara = go.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.farClipPlane = 5f;
            var cambiados = new List<Canvas>();
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = camara;
                    c.planeDistance = camara.nearClipPlane + 0.01f + Mathf.Max(0, 100 - Mathf.Min(c.sortingOrder, 100)) * 0.0005f;
                    cambiados.Add(c);
                }
            }
            RenderTexture rt = new RenderTexture(AnchoFoto, AltoFoto, 24, RenderTextureFormat.ARGB32);
            Texture2D foto = new Texture2D(AnchoFoto, AltoFoto, TextureFormat.RGB24, false);
            RenderTexture activa = RenderTexture.active;
            camara.targetTexture = rt;
            camara.aspect = (float)AnchoFoto / AltoFoto;
            Canvas.ForceUpdateCanvases();
            camara.Render();
            RenderTexture.active = rt;
            foto.ReadPixels(new Rect(0, 0, AnchoFoto, AltoFoto), 0, 0);
            foto.Apply();
            RenderTexture.active = activa;
            string ruta = "docs/capturas/" + nombre;
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            camara.targetTexture = null;
            foreach (Canvas c in cambiados)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            Object.Destroy(foto);
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(go);
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (Time.realtimeSinceStartup < fin)
            {
                yield return null;
                Vigilar();
            }
        }

        [UnityTest]
        public IEnumerator Clinica_HologramaDelCuerpo_ResaltaElEspacioGiraEInstalaConDestello()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            CargarImplantes("implante_brazos_1");
            DatosImplante ojos = Datos(EspacioImplante.Ojos, 1);
            DatosImplante piernas = Datos(EspacioImplante.Piernas, 1);

            yield return AbrirClinica();
            Assert.IsTrue(pantalla.LienzoVisible, "se ve el lienzo holográfico");
            VistaHologramaCuerpo holo = pantalla.Holograma;
            Assert.IsTrue(holo.Encendida, "el holograma está encendido");
            Assert.IsTrue(holo.ConCuerpo, "con el cuerpo horneado del personaje");
            Assert.IsNotNull(holo.Textura, "dibuja en su RenderTexture");
            Assert.AreEqual(4, holo.Render.sharedMaterials.Length, "una submalla por espacio");
            Assert.AreEqual(EspacioImplante.Ojos, holo.Elegido, "empieza en OJOS");
            Assert.Greater(holo.Resalte(EspacioImplante.Ojos), 0.5f, "el espacio elegido brilla");
            Assert.AreEqual(0f, holo.Resalte(EspacioImplante.Piernas), "los demás no");
            Assert.AreEqual(ojos.Nombre, pantalla.NombreImplante, "el implante ofrecido");
            Assert.AreEqual(config.NombreEspacio(0), pantalla.Marcador, "el marcador dice el espacio");
            Assert.Greater(pantalla.FilasEfectos, 0, "la tabla antes/después trae los efectos de los ojos");
            StringAssert.Contains("Ƀ" + ojos.Precio.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), pantalla.TextoBoton);
            StringAssert.Contains("Ƀ" + DineroAlto.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), pantalla.DineroHolograma);
            yield return Esperar(SegundosAnimacion);
            Vector2 marcadorOjos = pantalla.PosicionMarcador;
            Foto("s2n-menu-implantes-1.png");

            // ↓ BRAZOS (instalado: verde, la tabla muestra lo instalado) y ↓ PIERNAS: el marcador baja.
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(EspacioImplante.Brazos, holo.Elegido);
            yield return Esperar(SegundosAnimacion);
            Foto("s2n-menu-implantes-2.png");
            yield return Tecla(teclado.downArrowKey);
            Assert.AreEqual(EspacioImplante.Piernas, holo.Elegido);
            Assert.AreEqual(piernas.Nombre, pantalla.NombreImplante);
            StringAssert.StartsWith(config.NombreEfecto((int)EfectoImplante.Correr) + " 100% → 120% +20%", pantalla.TextoEfecto(0));
            yield return Esperar(SegundosAnimacion);
            Assert.Less(pantalla.PosicionMarcador.y, marcadorOjos.y - 100f, "el marcador baja de la cabeza a las piernas");

            // Arrastrar sobre el holograma lo gira.
            float giroAntes = holo.Giro;
            Vector2 centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Set(raton.position, centro);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Set(raton.delta, new Vector2(-200f, 0f));
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(giroAntes, holo.Giro)), 20f, "arrastrar gira el holograma");
            yield return Esperar(SegundosAnimacion);
            Foto("s2n-menu-implantes-3.png");

            // Enter Enter instala como antes; el holograma destella y se desvanece.
            yield return Tecla(teclado.enterKey);
            Assert.IsTrue(pantalla.Confirmando);
            Foto("s2n-menu-implantes-4.png");
            int antes = cartera.Dinero;
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(antes - piernas.Precio, cartera.Dinero, "cobra el implante");
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Piernas), "queda instalado");
            Assert.IsFalse(pantalla.Visible, "la pantalla se cerró");
            Assert.IsTrue(pantalla.Destellando && pantalla.LienzoVisible, "el holograma destella al cerrar");
            Foto("s2n-menu-implantes-5.png");
            yield return Esperar(config.DuracionDestello + config.DuracionEnsamble + 0.3f);
            Assert.IsFalse(pantalla.LienzoVisible, "y se va");
            Assert.IsFalse(holo.Encendida, "el holograma se apaga");
            yield return EsperarOperacion();
            VerificarFinal("T-815 holograma");
        }

        [UnityTest]
        public IEnumerator Clinica_EscSale_ApagaElHolograma()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            yield return AbrirClinica();
            Assert.IsTrue(pantalla.Holograma.Encendida);
            yield return Tecla(teclado.escapeKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantalla.Visible, "Esc sale");
            Assert.IsFalse(pantalla.LienzoVisible, "sin destello si no se instaló");
            Assert.IsFalse(pantalla.Holograma.Encendida, "el holograma se apaga");
            VerificarFinal("T-815 Esc");
        }
    }
}
