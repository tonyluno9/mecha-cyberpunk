using System.Collections;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-943: si disparas hacia atrás o de lado sin apuntar, el cuerpo gira hacia donde disparas; quieto y sin disparar se queda como estaba.</summary>
    public class GiroAlDispararT943Pruebas : BaseJugadorAPie
    {
        private const float MinimoGiroCamara = 120f;
        private const float SegundosGirando = 5f;

        private ControladorAPie Controlador => jugador.GetComponent<ControladorAPie>();

        private IEnumerator GirarCamaraHaciaAtras()
        {
            float fin = Time.realtimeSinceStartup + SegundosGirando;
            while (Mathf.Abs(Mathf.DeltaAngle(jugador.transform.eulerAngles.y, Controlador.GiroCamara)) < MinimoGiroCamara && Time.realtimeSinceStartup < fin)
            {
                Set(raton.delta, new Vector2(40f, 0f));
                yield return null;
            }
            Set(raton.delta, Vector2.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DispararHaciaAtras_ElCuerpoGiraHaciaDondeDispara()
        {
            yield return Calentar();
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            yield return Esperar(0.8f);
            yield return GirarCamaraHaciaAtras();
            float separacion = Mathf.Abs(Mathf.DeltaAngle(jugador.transform.eulerAngles.y, Controlador.GiroCamara));
            Assert.GreaterOrEqual(separacion, MinimoGiroCamara, "Control: la cámara mira hacia atrás y el cuerpo no giró solo");
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.45f);
            float despues = Mathf.Abs(Mathf.DeltaAngle(jugador.transform.eulerAngles.y, Controlador.GiroCamara));
            Assert.Less(despues, 15f, "Tras disparar, el cuerpo quedó mirando hacia donde dispara");
        }

        [UnityTest]
        public IEnumerator SinDisparar_ElCuerpoNoGiraConLaCamara()
        {
            yield return Calentar();
            yield return GirarCamaraHaciaAtras();
            yield return Esperar(0.5f);
            float separacion = Mathf.Abs(Mathf.DeltaAngle(jugador.transform.eulerAngles.y, Controlador.GiroCamara));
            Assert.GreaterOrEqual(separacion, MinimoGiroCamara, "Sin disparar el cuerpo se queda como estaba");
        }
    }
}
