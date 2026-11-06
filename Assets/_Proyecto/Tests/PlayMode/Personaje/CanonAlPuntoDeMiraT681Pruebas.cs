using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-681: al apuntar (y al disparar) el eje del cañón de las cuatro armas apunta al punto de mira (el que marca el centro de la cámara de juego),
    /// con la corredera arriba (sin voltear ni chueca). Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-681-*.png.
    /// </summary>
    public class CanonAlPuntoDeMiraT681Pruebas : BaseJugadorAPie
    {
        private const float DistanciaMira = 30f;
        private const float MaximoAnguloCanon = 4f;
        private const float MaximoLadeo = 0.2f;

        private static readonly (string nombre, string objeto, ArmaAPie arma)[] Armas =
        {
            ("pistola", "Pistola", ArmaAPie.Pistola),
            ("escopeta", "Escopeta", ArmaAPie.Escopeta),
            ("fusil", "Fusil", ArmaAPie.Fusil),
            ("francotirador", "Sniper", ArmaAPie.Sniper),
        };

        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);
        private Camera Camara() => jugador.GetComponentInChildren<CamaraHombro>().GetComponent<Camera>();

        private void Dar(ArmaAPie arma)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
        }

        private static void CapturaJuego(Camera c, string nombre)
        {
            if (!CapturaS1.Activa) return;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            RenderTexture previa = c.targetTexture;
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            for (int i = -12; i <= 12; i++) { foto.SetPixel(640 + i, 360, Color.red); foto.SetPixel(640, 360 + i, Color.red); }
            foto.Apply();
            RenderTexture.active = null;
            c.targetTexture = previa;
            Directory.CreateDirectory("docs/capturas");
            File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }

        // Ángulo (°) entre el cañón y la línea de la boca al punto de mira; el punto es donde apunta el centro de la cámara de juego.
        private float AnguloAlPunto(Transform arma)
        {
            Camera camara = Camara();
            Vector3 punto = camara.transform.position + camara.transform.forward * DistanciaMira;
            Transform boca = arma.Find("Boca");
            Vector3 origen = boca != null ? boca.position : arma.position;
            return Vector3.Angle(arma.forward, punto - origen);
        }

        private void Fotos(string nombre, string estado, Transform arma)
        {
            Transform t = jugador.transform;
            Vector3 centro = arma.position + arma.forward * 0.25f;
            CapturaS1.Foto("s1t-T-681-" + nombre + "-" + estado + "-lado", t, new Vector3(2.6f, 1.0f, 0.3f), new Vector3(0f, 1.1f, 0.3f), 40f, 700, 700);
            CapturaS1.FotoMira("s1t-T-681-" + nombre + "-" + estado + "-cerca", t, centro, new Vector3(1f, 0.25f, 0.2f), 1.4f, 40f, 700);
            CapturaJuego(Camara(), "s1t-T-681-" + nombre + "-" + estado + "-camara");
            CapturaS1.Foto("s1t-T-681-" + nombre + "-" + estado + "-atras", t, new Vector3(0f, 1.6f, -1.6f), new Vector3(0f, 1.35f, 0.6f), 45f, 700, 700);
            CapturaS1.Foto("s1t-T-681-" + nombre + "-" + estado + "-arriba", t, new Vector3(0f, 3.2f, -0.3f), new Vector3(0f, 1.3f, 0.5f), 45f, 700, 700);
        }

        [UnityTest]
        public IEnumerator CuatroArmas_AlApuntarYDisparar_ElCanonApuntaAlPuntoDeMira_ConLaCorrederaArriba()
        {
            Caja(new Vector3(0f, 3f, 40f), new Vector3(30f, 6f, 0.5f));
            yield return Calentar();
            VisualPersonaje visual = Visual();
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            foreach (var arma in Armas)
            {
                Dar(arma.arma);
                Assert.IsTrue(armas.Equipar(arma.arma), arma.nombre + " se equipa");
                Transform objeto = visual.GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find(arma.objeto);
                yield return Esperar(0.5f);
                foreach (float inclinacion in new[] { 0f, -15f, 12f })
                {
                    controlador.Empujar(0f, inclinacion - controlador.Inclinacion);
                    Press(raton.rightButton);
                    yield return Esperar(1.0f);
                    float angulo = AnguloAlPunto(objeto);
                    Debug.Log("T681 " + arma.nombre + " apuntando inclinación " + inclinacion + ": ángulo al punto " + angulo.ToString("F1") + " ladeo " + Mathf.Abs(Vector3.Dot(objeto.right, Vector3.up)).ToString("F2"));
                    if (inclinacion == 0f) { Fotos(arma.nombre, "apuntando", objeto); }
                    Assert.Less(angulo, MaximoAnguloCanon, arma.nombre + ": el cañón apunta al punto de mira con la cámara inclinada " + inclinacion + "° (°)");
                    Assert.Less(Mathf.Abs(Vector3.Dot(objeto.right, Vector3.up)), MaximoLadeo, arma.nombre + ": la corredera queda arriba, sin chueca");
                    Assert.Greater(Vector3.Dot(objeto.up, Vector3.up), 0.5f, arma.nombre + ": no está volteada");
                    if (inclinacion == 0f)
                    {
                        Press(raton.leftButton);
                        yield return null;
                        Release(raton.leftButton);
                        yield return Esperar(0.06f);
                        float disparo = AnguloAlPunto(objeto);
                        Debug.Log("T681 " + arma.nombre + " disparando: ángulo " + disparo.ToString("F1"));
                        Fotos(arma.nombre, "disparo", objeto);
                        Assert.Less(disparo, MaximoAnguloCanon, arma.nombre + ": disparando el cañón sigue hacia el punto de mira (el retroceso solo lo levanta) (°)");
                        Assert.Greater(Vector3.Dot(objeto.up, Vector3.up), 0.3f, arma.nombre + ": disparando no está volteada");
                    }
                    Release(raton.rightButton);
                    yield return Esperar(0.5f);
                }
            }
        }
    }
}
