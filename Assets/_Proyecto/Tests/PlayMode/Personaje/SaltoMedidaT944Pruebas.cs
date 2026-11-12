using System.Collections;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-944: en un salto real se ven las tres fases en orden y el aterrizaje no empieza en el aire (antes empezaba en la cima, donde la velocidad vertical es casi cero).</summary>
    public class SaltoMedidaT944Pruebas : BaseJugadorAPie
    {
        private const float AlturaAire = 0.2f;
        private const float UmbralDespegue = 0.02f;
        private const float AlturaSaltoT961 = 0.45f;
        private const float SegundosAireT961 = 0.47f;

        private static bool Es(Animator a, string estado)
        {
            return a.IsInTransition(0) ? a.GetNextAnimatorStateInfo(0).IsName(estado) : a.GetCurrentAnimatorStateInfo(0).IsName(estado);
        }

        private static void Foto(string nombre)
        {
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_UI") == "1" && Camera.main != null)
            {
                Camera camara = Camera.main;
                RenderTexture rt = new RenderTexture(1280, 720, 24);
                Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                RenderTexture antes = RenderTexture.active;
                camara.targetTexture = rt;
                camara.Render();
                RenderTexture.active = rt;
                foto.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                foto.Apply();
                camara.targetTexture = null;
                RenderTexture.active = antes;
                System.IO.File.WriteAllBytes("docs/capturas/" + nombre + ".png", foto.EncodeToPNG());
                Object.Destroy(foto);
                Object.Destroy(rt);
            }
        }

        [UnityTest]
        public IEnumerator Salto_TresFasesEnOrden_YAterrizaEnElPiso()
        {
            yield return Calentar();
            yield return Esperar(0.5f);
            Animator animador = jugador.GetComponentInChildren<Animator>();
            float piso = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            bool vioInicio = false;
            bool vioAire = false;
            bool vioAterrizaje = false;
            bool fotoInicio = false;
            float alturaAlAterrizar = -1f;
            float fin = Time.time + 1.6f;
            string rastro = string.Empty;
            string ultimo = string.Empty;
            // T-961: el salto de la muestra de Blender sube 0.45 m y pasa 0.47 s en el aire.
            float maxima = 0f;
            float despegue = -1f;
            float aterrizaje = -1f;
            while (Time.time < fin)
            {
                float altura = jugador.transform.position.y - piso;
                maxima = Mathf.Max(maxima, altura);
                if (despegue < 0f && altura > UmbralDespegue) { despegue = Time.time; }
                if (despegue >= 0f && aterrizaje < 0f && altura <= UmbralDespegue && Time.time > despegue + 0.1f) { aterrizaje = Time.time; }
                foreach (string e in new[] { "Idle", "SaltoInicio", "SaltoAire", "Caida", "SaltoAterrizaje" })
                {
                    if (Es(animador, e) && e != ultimo)
                    {
                        ultimo = e;
                        rastro += e + "@" + altura.ToString("0.00") + " ";
                    }
                }
                vioInicio |= Es(animador, "SaltoInicio");
                vioAire |= Es(animador, "SaltoAire") && altura > AlturaAire;
                if (Es(animador, "SaltoInicio") && !fotoInicio && altura > AlturaAire)
                {
                    fotoInicio = true;
                    Foto("ui-salto-1-inicio");
                }
                if (Es(animador, "SaltoAterrizaje") && !vioAterrizaje)
                {
                    vioAterrizaje = true;
                    alturaAlAterrizar = altura;
                    Foto("ui-salto-3-aterrizaje");
                }
                yield return null;
            }
            Assert.IsTrue(vioInicio, "Se ve el inicio del salto");
            Assert.IsTrue(vioAire, "Se ve la fase en el aire, ya despegado: " + rastro);
            Assert.IsTrue(vioAterrizaje, "Se ve el aterrizaje");
            Assert.Less(alturaAlAterrizar, 0.35f, "El aterrizaje empieza cerca del piso, no en el aire");
            Assert.AreEqual(AlturaSaltoT961, maxima, 0.05f, "Altura del salto (m)");
            Assert.Greater(aterrizaje, 0f, "Vuelve al piso");
            Assert.AreEqual(SegundosAireT961, aterrizaje - despegue, 0.08f, "Tiempo en el aire (s)");
            Assert.IsNotNull(jugador.GetComponent<ControladorAPie>());
        }
    }
}
