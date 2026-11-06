using System.Collections;
using System.IO;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-672: al apuntar el personaje queda centrado en horizontal y la mira (centro de la pantalla) queda arriba de su cabeza.</summary>
    public class CamaraApuntarT672Pruebas : BaseJugadorAPie
    {
        private Camera Camara() => jugador.GetComponentInChildren<CamaraHombro>().GetComponent<Camera>();

        private static void Captura(Camera c, string nombre)
        {
            if (!CapturaS1.Activa) return;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            RenderTexture previa = c.targetTexture;
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            // Cruz de la mira en el centro para revisar la captura.
            for (int i = -12; i <= 12; i++) { foto.SetPixel(640 + i, 360, Color.red); foto.SetPixel(640, 360 + i, Color.red); }
            foto.Apply();
            RenderTexture.active = null;
            c.targetTexture = previa;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator Apuntar_PersonajeCentradoYMiraArribaDeLaCabeza()
        {
            Caja(new Vector3(0f, 1f, 12f), new Vector3(6f, 2f, 0.5f)); // un blanco al frente
            yield return Calentar();
            yield return Esperar(0.5f);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Camera camara = Camara();
            Vector3 normal = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.Greater(normal.x, 0.3f, "Sin apuntar la cámara va al hombro");

            Press(raton.rightButton);
            yield return Esperar(1.2f);
            Vector3 local = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.AreEqual(0f, local.x, 0.05f, "Apuntando la cámara queda centrada detrás del personaje");
            Assert.Less(local.z, -1.5f, "Detrás");
            Assert.Greater(local.y, 1.9f, "Un poco más alta que la cabeza");
            Vector3 pies = camara.WorldToViewportPoint(jugador.transform.position);
            Vector3 cabeza = camara.WorldToViewportPoint(jugador.transform.position + Vector3.up * 1.8f);
            Assert.AreEqual(0.5f, cabeza.x, 0.03f, "El personaje queda centrado en horizontal");
            Assert.Less(cabeza.y, 0.5f, "La mira (centro de la pantalla) queda arriba de la cabeza");
            Assert.Greater(cabeza.y, 0.2f, "El personaje no se sale por abajo");
            Captura(camara, "s1n-T-672-apuntando");

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.08f);
            Captura(camara, "s1n-T-672-disparando");
            Release(raton.rightButton);
            yield return Esperar(1.2f);
            Vector3 suelta = jugador.transform.InverseTransformPoint(camara.transform.position);
            Assert.Greater(suelta.x, 0.3f, "Al soltar vuelve al hombro");
        }
    }
}
