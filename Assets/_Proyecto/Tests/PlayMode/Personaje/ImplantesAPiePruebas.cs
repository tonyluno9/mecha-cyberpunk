using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-192: con implantes (doble registrado) correr, saltar y golpear a pie cambian; sin ellos todo es como antes.</summary>
    public class ImplantesAPiePruebas : BaseJugadorAPie
    {
        private const float TolerRel = 0.05f;
        private const float SegundosCalentarCarrera = 2f;
        private const float SegundosMedirCarrera = 1.5f;

        private ImplantesFalsos falsos;
        private GameObject muneco;
        private MunecoAPie vidaMuneco;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            falsos = new ImplantesFalsos();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            if (muneco != null) Object.Destroy(muneco);
            base.TearDown();
        }

        private void Registrar(EspacioImplante espacio, int nivel)
        {
            falsos.Poner(espacio, nivel);
            Registro<IImplantes>.Agregar(falsos);
        }

        // Corre 2 s para llegar a la velocidad, mide la distancia en los 1.5 s siguientes y devuelve distancia y segundos.
        private IEnumerator MedirCarrera(float[] resultado)
        {
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(SegundosCalentarCarrera);
            float z0 = jugador.transform.position.z;
            float t0 = Time.time;
            yield return Esperar(SegundosMedirCarrera);
            resultado[0] = jugador.transform.position.z - z0;
            resultado[1] = Time.time - t0;
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
        }

        // T-403: los puños usan el daño de su nivel en el arsenal; sin arsenal, el de los datos del personaje.
        private static float DanioBase(GolpeAPie golpe)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            DatosArma punos = arsenal != null ? arsenal.Datos(ArmaAPie.Punos) : null;
            return punos != null ? punos.Danio : golpe.Datos.DanioGolpe;
        }

        [UnityTest]
        public IEnumerator Correr_SinImplantes_DistanciaIgualALaBase()
        {
            yield return Calentar();
            var r = new float[2];
            yield return MedirCarrera(r);
            float esperada = datos.VelocidadCorrer * r[1];
            Debug.Log("[Prueba] T-192 correr sin implantes: medido " + r[0] + " m, esperado " + esperada + " m");
            Assert.AreEqual(esperada, r[0], esperada * TolerRel);
        }

        [UnityTest]
        public IEnumerator Correr_ConPiernasI_DistanciaIgualABaseXMultiplicador() => CorrerConPiernas(1);

        [UnityTest]
        public IEnumerator Correr_ConPiernasII_DistanciaIgualABaseXMultiplicador() => CorrerConPiernas(2);

        private IEnumerator CorrerConPiernas(int nivel)
        {
            yield return Calentar();
            Registrar(EspacioImplante.Piernas, nivel);
            yield return null;
            var r = new float[2];
            yield return MedirCarrera(r);
            float mult = falsos.Efectos.multiplicadorCorrer;
            float esperada = datos.VelocidadCorrer * mult * r[1];
            Debug.Log("[Prueba] T-192 correr piernas " + nivel + " (x" + mult + "): medido " + r[0] + " m, esperado " + esperada + " m");
            Assert.AreEqual(esperada, r[0], esperada * TolerRel);
        }

        [UnityTest]
        public IEnumerator Caminar_NoCambiaConPiernas()
        {
            yield return Calentar();
            Registrar(EspacioImplante.Piernas, 2);
            yield return null;
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            float velocidad = controlador.Velocidad.y;
            Release(teclado.wKey);
            Debug.Log("[Prueba] T-192 caminar con piernas II: " + velocidad + " m/s, esperado " + datos.VelocidadCaminar);
            Assert.AreEqual(datos.VelocidadCaminar, velocidad, datos.VelocidadCaminar * TolerRel);
        }

        [UnityTest]
        public IEnumerator Correr_CambioAMediaCarrera_EntraSinSaltosDePosicion()
        {
            yield return Calentar();
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            float vBase = controlador.Velocidad.y;
            Assert.AreEqual(datos.VelocidadCorrer, vBase, datos.VelocidadCorrer * TolerRel);

            Registrar(EspacioImplante.Piernas, 2);
            float maximoPaso = 0f;
            float z = jugador.transform.position.z;
            float fin = Time.time + 1.5f;
            while (Time.time < fin)
            {
                yield return null;
                float nueva = jugador.transform.position.z;
                maximoPaso = Mathf.Max(maximoPaso, (nueva - z) / Mathf.Max(Time.deltaTime, 0.0001f));
                z = nueva;
            }
            float vNueva = controlador.Velocidad.y;
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            float esperada = datos.VelocidadCorrer * falsos.Efectos.multiplicadorCorrer;
            Debug.Log("[Prueba] T-192 cambio a media carrera: " + vBase + " -> " + vNueva + " m/s (esperada " + esperada + "), paso max " + maximoPaso + " m/s");
            Assert.AreEqual(esperada, vNueva, esperada * TolerRel);
            Assert.LessOrEqual(maximoPaso, esperada * 1.2f, "Sin saltos de posición");
        }

        private IEnumerator MedirSalto(float[] alturaMax)
        {
            yield return Esperar(0.3f);
            Assert.IsTrue(controlador.EnPiso, "Debe estar en el piso antes de saltar");
            float pisoY = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float maxima = 0f;
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                maxima = Mathf.Max(maxima, jugador.transform.position.y - pisoY);
                yield return null;
            }
            alturaMax[0] = maxima;
        }

        [UnityTest]
        public IEnumerator Saltar_SinImplantes_AlturaIgualALaBase()
        {
            yield return Calentar();
            var h = new float[1];
            yield return MedirSalto(h);
            Debug.Log("[Prueba] T-192 salto sin implantes: medido " + h[0] + " m, esperado " + datos.AlturaSalto + " m");
            Assert.AreEqual(datos.AlturaSalto, h[0], datos.AlturaSalto * TolerRel);
        }

        [UnityTest]
        public IEnumerator Saltar_ConPiernasI_AlturaIgualABaseXMultiplicador() => SaltarConPiernas(1);

        [UnityTest]
        public IEnumerator Saltar_ConPiernasII_AlturaIgualABaseXMultiplicador() => SaltarConPiernas(2);

        private IEnumerator SaltarConPiernas(int nivel)
        {
            yield return Calentar();
            Registrar(EspacioImplante.Piernas, nivel);
            yield return null;
            var h = new float[1];
            yield return MedirSalto(h);
            float esperada = datos.AlturaSalto * falsos.Efectos.multiplicadorSalto;
            Debug.Log("[Prueba] T-192 salto piernas " + nivel + ": medido " + h[0] + " m, esperado " + esperada + " m");
            Assert.AreEqual(esperada, h[0], esperada * TolerRel);
        }

        private void CrearMuneco(float distancia)
        {
            muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            muneco.transform.position = new Vector3(0f, 0.9f, distancia);
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            vidaMuneco = muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
        }

        [UnityTest]
        public IEnumerator Golpe_SinImplantes_PegaElDanioBase()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            float antes = vidaMuneco.Actual;
            yield return Clic();
            yield return Esperar(0.3f);
            float danio = antes - vidaMuneco.Actual;
            Debug.Log("[Prueba] T-192 golpe sin implantes: medido " + danio + ", esperado " + DanioBase(golpe));
            Assert.AreEqual(DanioBase(golpe), danio, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Golpe_ConBrazosI_PegaDanioXMultiplicador() => GolpeConBrazos(1);

        [UnityTest]
        public IEnumerator Golpe_ConBrazosII_PegaDanioXMultiplicador() => GolpeConBrazos(2);

        private IEnumerator GolpeConBrazos(int nivel)
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            Registrar(EspacioImplante.Brazos, nivel);
            yield return null;
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            float antes = vidaMuneco.Actual;
            yield return Clic();
            yield return Esperar(0.3f);
            float danio = antes - vidaMuneco.Actual;
            float esperado = DanioBase(golpe) * falsos.Efectos.multiplicadorGolpeAPie;
            Debug.Log("[Prueba] T-192 golpe brazos " + nivel + ": medido " + danio + ", esperado " + esperado);
            Assert.AreEqual(esperado, danio, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Golpe_CambiarElDobleAMediaPrueba_CambiaElSiguienteGolpe()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            Registrar(EspacioImplante.Brazos, 1);
            yield return null;
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            float baseDanio = DanioBase(golpe);
            float v0 = vidaMuneco.Actual;
            yield return Clic();
            yield return Esperar(0.3f);
            float primero = v0 - vidaMuneco.Actual;

            falsos.Poner(EspacioImplante.Brazos, 2);
            yield return Esperar(0.8f); // Termina la recarga.
            float v1 = vidaMuneco.Actual;
            yield return Clic();
            yield return Esperar(0.3f);
            float segundo = v1 - vidaMuneco.Actual;
            Debug.Log("[Prueba] T-192 golpe cambio brazos I->II: " + primero + " -> " + segundo);
            Assert.AreEqual(baseDanio * MultiplicadorBrazos(1), primero, 0.0001f);
            Assert.AreEqual(baseDanio * MultiplicadorBrazos(2), segundo, 0.0001f);
            Assert.Greater(segundo, primero);
        }

        private static float MultiplicadorBrazos(int nivel) => ImplantesFalsos.Datos(EspacioImplante.Brazos, nivel).Efectos.multiplicadorGolpeAPie;

        [UnityTest]
        public IEnumerator ServicioQueLlegaDespues_SeSigueAlRegistrarse()
        {
            yield return Calentar();
            CrearMuneco(1.2f);
            yield return null;
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            float antes = vidaMuneco.Actual;
            Registrar(EspacioImplante.Brazos, 2); // Llega después de que el jugador ya estaba activo.
            yield return Clic();
            yield return Esperar(0.3f);
            Assert.AreEqual(DanioBase(golpe) * falsos.Efectos.multiplicadorGolpeAPie, antes - vidaMuneco.Actual, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Puno_ConBrazos_SeVeCromadoYConFranjaEnElNivel2()
        {
            yield return Calentar();
            var puno = jugador.GetComponentInChildren<PunoImplantes>(true);
            if (puno == null)
            {
                Assert.Ignore("T-400: en tercera persona el jugador ya no tiene el puño en primera persona (PunoImplantes); los brazos se ven en el cuerpo.");
            }
            Assert.AreEqual(0, puno.NivelMostrado);
            Assert.IsFalse(puno.Franja.activeSelf);

            Registrar(EspacioImplante.Brazos, 1);
            yield return null;
            Assert.AreEqual(1, puno.NivelMostrado);
            Assert.IsFalse(puno.Franja.activeSelf);
            Assert.Greater(ColorPuno(puno).r, 0.7f, "cromado claro");

            falsos.Poner(EspacioImplante.Brazos, 2);
            Assert.IsTrue(puno.Franja.activeSelf);

            falsos.Poner(EspacioImplante.Brazos, 0);
            Assert.IsFalse(puno.Franja.activeSelf);
            Assert.AreEqual(0, puno.NivelMostrado);
        }

        private static Color ColorPuno(PunoImplantes puno)
        {
            var b = new MaterialPropertyBlock();
            puno.Puno.GetPropertyBlock(b);
            return b.GetColor("_BaseColor");
        }
    }
}
