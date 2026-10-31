using System.Collections;
using MechaCyberpunk.Personaje;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Pruebas jugables del jugador a pie: prefab real, obstáculos creados en la prueba, entrada simulada.</summary>
    public class ControladorAPiePruebas : BaseJugadorAPie
    {
        [UnityTest]
        public IEnumerator APie_CaminarConW2Segundos_AvanzaEntre75Y95Metros()
        {
            yield return Calentar();
            Vector3 inicio = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            float avance = jugador.transform.position.z - inicio.z;
            Assert.GreaterOrEqual(avance, 7.5f);
            Assert.LessOrEqual(avance, 9.5f);
            Assert.Less(Mathf.Abs(jugador.transform.position.x - inicio.x), 0.01f);
        }

        [UnityTest]
        public IEnumerator APie_CorrerConShiftYW2Segundos_AvanzaEntre125Y155Metros()
        {
            yield return Calentar();
            Vector3 inicio = jugador.transform.position;
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            yield return null; // Soltar dos teclas en el mismo frame deja la primera pulsada en el teclado simulado.
            Release(teclado.leftShiftKey);
            float avance = jugador.transform.position.z - inicio.z;
            Assert.GreaterOrEqual(avance, 12.5f);
            Assert.LessOrEqual(avance, 15.5f);
        }

        [UnityTest]
        public IEnumerator APie_SoltarW_SeDetieneEnMenosDeMedioSegundo()
        {
            yield return Calentar();
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            Release(teclado.wKey);
            yield return null; // Soltar dos teclas en el mismo frame deja la primera pulsada en el teclado simulado.
            Release(teclado.leftShiftKey);
            yield return Esperar(0.5f);
            Assert.AreEqual(0f, controlador.Velocidad.magnitude, 0.001f);
            Vector3 posicion = jugador.transform.position;
            yield return Esperar(0.3f);
            Assert.Less((jugador.transform.position - posicion).magnitude, 0.01f);
        }

        [UnityTest]
        public IEnumerator APie_EspacioEnElPiso_SubeLosPiesEntre09Y13Metros()
        {
            yield return Calentar();
            yield return Esperar(0.2f);
            Assert.IsTrue(controlador.EnPiso, "Debe estar en el piso antes de saltar");
            float pisoY = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float maxima = 0f;
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                maxima = Mathf.Max(maxima, jugador.transform.position.y - pisoY);
                yield return null;
            }
            Assert.GreaterOrEqual(maxima, 0.9f);
            Assert.LessOrEqual(maxima, 1.3f);
            Assert.IsTrue(controlador.EnPiso, "Debe volver al piso");
        }

        [UnityTest]
        public IEnumerator APie_EspacioEnElAire_NoHaceNada()
        {
            yield return Calentar();
            yield return Esperar(0.2f);
            float pisoY = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return Esperar(0.2f);
            Assert.IsFalse(controlador.EnPiso, "Debe estar en el aire");
            float velocidadAntes = controlador.VelocidadVertical;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            Assert.Less(controlador.VelocidadVertical, velocidadAntes, "No debe recibir otro impulso");

            float maxima = 0f;
            float t = 0f;
            while (t < 1.5f)
            {
                t += Time.deltaTime;
                maxima = Mathf.Max(maxima, jugador.transform.position.y - pisoY);
                yield return null;
            }
            Assert.LessOrEqual(maxima, 1.3f, "Un segundo salto en el aire llegaría mucho más alto");
        }

        [UnityTest]
        public IEnumerator APie_EscalonDe03_SeSubeCaminando()
        {
            Caja(new Vector3(0f, 0.15f, 12f), new Vector3(10f, 0.3f, 20f));
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(3f);
            Release(teclado.wKey);
            Assert.Greater(jugador.transform.position.z, 5f, "Debe pasar el borde del escalón en z = 2");
            // El CharacterController flota su skinWidth (0.08 m) sobre la superficie: en el piso queda en 0.08.
            Assert.AreEqual(0.38f, jugador.transform.position.y, 0.06f);
        }

        [UnityTest]
        public IEnumerator APie_RampaDe30Grados_SeSubeCaminando()
        {
            Rampa(3f, 30f, 30f);
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            Assert.Greater(jugador.transform.position.y, 2f);
        }

        [UnityTest]
        public IEnumerator APie_MuroDe12_NoSeSube()
        {
            Caja(new Vector3(0f, 0.6f, 6f), new Vector3(20f, 1.2f, 1f));
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            Assert.Less(jugador.transform.position.y, 0.3f);
            Assert.Less(jugador.transform.position.z, 5.5f);
        }

        [UnityTest]
        public IEnumerator APie_RampaDe50Grados_NoSeSube()
        {
            Rampa(3f, 50f, 20f);
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            Assert.Less(jugador.transform.position.y, 0.5f);
        }

        [UnityTest]
        public IEnumerator APie_RatonEnX_GiraLaCamaraNoElCuerpo()
        {
            // T-400 (tercera persona): el ratón gira la cámara (GiroCamara); el cuerpo gira solo al caminar o apuntar.
            yield return Calentar();
            float cuerpo = jugador.transform.eulerAngles.y;
            float inicio = controlador.GiroCamara;
            Set(raton.delta, new Vector2(200f, 0f));
            yield return null;
            yield return null;
            float giro = Mathf.DeltaAngle(inicio, controlador.GiroCamara);
            Assert.Greater(giro, 5f);
            Assert.AreEqual(cuerpo, jugador.transform.eulerAngles.y, 0.001f, "El cuerpo no gira con el ratón");
            Assert.AreEqual(0f, controlador.Inclinacion, 0.001f, "Un delta en X no inclina la cámara");
        }

        [UnityTest]
        public IEnumerator APie_RatonEnY_InclinaLaCamara()
        {
            yield return Calentar();
            float giroInicial = jugador.transform.eulerAngles.y;
            Set(raton.delta, new Vector2(0f, -200f));
            yield return null;
            yield return null;
            Assert.Greater(controlador.Inclinacion, 5f, "Delta Y negativo mira hacia abajo");
            float inclinacionCamara = Mathf.DeltaAngle(0f, controlador.Camara.localEulerAngles.x);
            Assert.AreEqual(controlador.Inclinacion, inclinacionCamara, 0.01f);
            Assert.AreEqual(giroInicial, jugador.transform.eulerAngles.y, 0.001f, "Un delta en Y no gira el cuerpo");
        }

        [UnityTest]
        public IEnumerator APie_RatonEnormeEnY_SeQuedaEnLosLimitesDeLaCamaraDeHombro()
        {
            // T-400: los límites vienen de ConfigCamaraHombro (-40 a 60), no los 85 de la primera persona.
            ConfigCamaraHombro hombro = AssetDatabase.LoadAssetAtPath<ConfigCamaraHombro>("Assets/_Proyecto/Personaje/Datos/ConfigCamaraHombro_Base.asset");
            yield return Calentar();
            Set(raton.delta, new Vector2(0f, 1000000f));
            yield return null;
            yield return null;
            Assert.AreEqual(hombro.InclinacionMinima, controlador.Inclinacion, 0.01f);
            Set(raton.delta, new Vector2(0f, -1000000f));
            yield return null;
            yield return null;
            Assert.AreEqual(hombro.InclinacionMaxima, controlador.Inclinacion, 0.01f);
        }

        [UnityTest]
        public IEnumerator APie_ActivarControlFalse_CongelaAlJugador()
        {
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(0.5f);
            controlador.ActivarControl(false);
            Vector3 posicion = jugador.transform.position;
            yield return Esperar(0.5f);
            Release(teclado.wKey);
            Assert.AreEqual(0f, controlador.Velocidad.magnitude, 0.001f);
            Assert.AreEqual(posicion.z, jugador.transform.position.z, 0.001f);
            controlador.ActivarControl(true);
            Press(teclado.wKey);
            yield return Esperar(0.5f);
            Release(teclado.wKey);
            Assert.Greater(jugador.transform.position.z, posicion.z + 0.5f);
        }

        [UnityTest]
        public IEnumerator APie_Caminando_LaCamaraNoSeMeceEnTerceraPersona()
        {
            // T-400: el balanceo de la vista era de primera persona; la cámara del jugador ya no se mece.
            yield return Calentar();
            float alturaInicial = controlador.Camara.localPosition.y;
            Press(teclado.wKey);
            float minimo = float.MaxValue;
            float maximo = float.MinValue;
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                float y = controlador.Camara.localPosition.y;
                minimo = Mathf.Min(minimo, y);
                maximo = Mathf.Max(maximo, y);
                yield return null;
            }
            Release(teclado.wKey);
            Assert.Less(maximo - minimo, 0.02f);
            Assert.AreEqual(alturaInicial, controlador.Camara.localPosition.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator APie_Escape_NoSueltaElCursor()
        {
            // T-410: Esc ya no suelta el cursor; lo abre la pausa.
            yield return Calentar();
            Assert.IsTrue(controlador.CursorBloqueado);
            Press(teclado.escapeKey);
            yield return null;
            Release(teclado.escapeKey);
            yield return null;
            Assert.IsTrue(controlador.CursorBloqueado);
        }
    }
}
