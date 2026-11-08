using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-742: al subir y bajar con la E, la puerta del lado por donde subes se abre y se cierra sincronizada con Carro_Entrar y Carro_Salir
    /// (V01a, V01b y V05 por piezas). Entrada simulada, jugador real y capturas con CAPTURAR_S1=1 (docs/capturas/s1e-T-742-*.png).
    /// </summary>
    public class PuertaCarroT742Pruebas : BaseJugadorAPie
    {
        /// <summary>Lado de la puerta (mismo orden que PuertasCarro.Lado de Vehiculos).</summary>
        private enum Lado
        {
            Izquierda,
            Derecha,
        }

        /// <summary>
        /// PuertasCarro es de Vehiculos y las áreas de dueños distintos no se referencian (Core/ReglasEnsamblados): la prueba la lee por reflexión,
        /// que es lo mismo que hace AnimacionCarro con SendMessage.
        /// </summary>
        private sealed class PuertasDelCarro
        {
            private readonly Component componente;
            private readonly MethodInfo apertura;
            private readonly MethodInfo abierta;
            private readonly System.Type tipoLado;

            public PuertasDelCarro(GameObject carro)
            {
                componente = carro.GetComponent("PuertasCarro");
                Assert.IsNotNull(componente, carro.name + " lleva PuertasCarro");
                System.Type tipo = componente.GetType();
                apertura = tipo.GetMethod("AperturaActual");
                abierta = tipo.GetMethod("EstaAbierta");
                tipoLado = apertura.GetParameters()[0].ParameterType;
                AnguloApertura = (float)tipo.GetProperty("AnguloApertura").GetValue(componente);
            }

            public float AnguloApertura { get; }

            public float AperturaActual(Lado lado) => (float)apertura.Invoke(componente, new[] { System.Enum.ToObject(tipoLado, (int)lado) });

            public bool EstaAbierta(Lado lado) => (bool)abierta.Invoke(componente, new[] { System.Enum.ToObject(tipoLado, (int)lado) });
        }

        private static readonly string[] Codigos = { "V01a", "V01b", "V05" };
        private const float AperturaMinima = 55f;
        private const float ApagadaMaxima = 1f;
        private const float SegundosLimite = 6f;
        private IMontable carro;
        private GameObject carroObj;
        private PuertasDelCarro puertas;

        public override void TearDown()
        {
            if (carroObj != null)
            {
                Object.Destroy(carroObj);
            }
            base.TearDown();
        }

        private IEnumerator PrepararCarro(string codigo)
        {
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            carroObj = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 6f), Quaternion.identity);
            puertas = new PuertasDelCarro(carroObj);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == carroObj) { carro = m; }
            }
            Assert.IsNotNull(carro, "El carro se registra");
            yield return Esperar(1f);
        }

        private IEnumerator Tecla()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
        }

        private float Abierta(Lado lado) => puertas.AperturaActual(lado);

        private void PonerJugadorAfuera(bool izquierda)
        {
            Vector3 punto = carro.PuntoSubida;
            if (!izquierda)
            {
                punto.x = 2f * carroObj.transform.position.x - punto.x;
            }
            Vector3 hacia = izquierda ? Vector3.right : Vector3.left;
            Vector3 inicio = punto - hacia * 1f;
            inicio.y = 0.05f;
            jugadorAPie.Colocar(new Pose(inicio, Quaternion.LookRotation(hacia)));
        }

        private void FotoLado(string nombre, bool izquierda)
        {
            float lado = izquierda ? -1f : 1f;
            Vector3 c = carroObj.transform.position;
            CapturaMundoS1.Foto(nombre, c + new Vector3(lado * 5.5f, 1.7f, -3.2f), c + new Vector3(lado * 0.4f, 1.0f, 0.1f), 42f);
        }

        // Sube con la E desde un lado y devuelve lo máximo que se abrió cada puerta mientras entraba.
        private IEnumerator SubirDesde(bool izquierda, string codigo, System.Action<float, float> alTerminar)
        {
            PonerJugadorAfuera(izquierda);
            yield return Esperar(0.3f);
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.AreEqual(0f, Abierta(Lado.Izquierda), ApagadaMaxima, "Antes de subir las puertas están cerradas");
            Assert.AreEqual(0f, Abierta(Lado.Derecha), ApagadaMaxima);
            FotoLado("s1e-T-742-" + codigo + "-0-antes", izquierda);

            yield return Tecla();
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "La E sube");
            float maxIzq = 0f;
            float maxDer = 0f;
            bool foto = false;
            float limite = Time.time + SegundosLimite;
            while (anim.Fase == AnimacionCarro.FaseCarro.Entrando && Time.time < limite)
            {
                yield return null;
                maxIzq = Mathf.Max(maxIzq, Abierta(Lado.Izquierda));
                maxDer = Mathf.Max(maxDer, Abierta(Lado.Derecha));
                float propia = Abierta(izquierda ? Lado.Izquierda : Lado.Derecha);
                if (!foto && propia >= puertas.AnguloApertura - 2f)
                {
                    foto = true;
                    FotoLado("s1e-T-742-" + codigo + "-1-puerta-abierta-al-subir", izquierda);
                }
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase, "Terminó de entrar");
            alTerminar(maxIzq, maxDer);
        }

        [UnityTest]
        public IEnumerator Subir_PorLaIzquierda_SeAbreLaPuertaIzquierdaYSeCierraAlSentarse([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return PrepararCarro(codigo);
            float maxIzq = 0f;
            float maxDer = 0f;
            yield return SubirDesde(true, codigo, (i, d) => { maxIzq = i; maxDer = d; });
            Assert.GreaterOrEqual(maxIzq, AperturaMinima, codigo + ": la puerta izquierda se abrió al subir (" + maxIzq + "°)");
            Assert.LessOrEqual(maxDer, ApagadaMaxima, codigo + ": la derecha no se mueve");
            yield return Esperar(0.9f);
            Assert.AreEqual(0f, Abierta(Lado.Izquierda), ApagadaMaxima, codigo + ": ya sentado, la puerta se cerró");
            Assert.IsFalse(puertas.EstaAbierta(Lado.Izquierda));
            FotoLado("s1e-T-742-" + codigo + "-2-conductor-visible", true);
        }

        [UnityTest]
        public IEnumerator Subir_PorLaDerecha_SeAbreLaPuertaDerecha()
        {
            yield return PrepararCarro("V01a");
            float maxIzq = 0f;
            float maxDer = 0f;
            yield return SubirDesde(false, "V01a-der", (i, d) => { maxIzq = i; maxDer = d; });
            Assert.GreaterOrEqual(maxDer, AperturaMinima, "La puerta derecha se abrió al subir por ese lado (" + maxDer + "°)");
            Assert.LessOrEqual(maxIzq, ApagadaMaxima, "La izquierda no se mueve");
            yield return Esperar(0.9f);
            Assert.AreEqual(0f, Abierta(Lado.Derecha), ApagadaMaxima, "Ya sentado, se cerró");
        }

        [UnityTest]
        public IEnumerator Bajar_SeAbreLaPuertaDelConductorYSeCierraAlSalir([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return PrepararCarro(codigo);
            yield return SubirDesde(true, codigo, (i, d) => { });
            yield return Esperar(1.2f);
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.AreEqual(0f, Abierta(Lado.Izquierda), ApagadaMaxima, "Manejando, la puerta va cerrada");

            yield return Tecla();
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "La E baja");
            float maxIzq = 0f;
            float maxDer = 0f;
            bool foto = false;
            float limite = Time.time + SegundosLimite;
            while (anim.Fase == AnimacionCarro.FaseCarro.Saliendo && Time.time < limite)
            {
                yield return null;
                maxIzq = Mathf.Max(maxIzq, Abierta(Lado.Izquierda));
                maxDer = Mathf.Max(maxDer, Abierta(Lado.Derecha));
                if (!foto && Abierta(Lado.Izquierda) >= puertas.AnguloApertura - 2f)
                {
                    foto = true;
                    FotoLado("s1e-T-742-" + codigo + "-3-puerta-abierta-al-bajar", true);
                }
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Fuera, anim.Fase, "Terminó de bajar");
            Assert.GreaterOrEqual(maxIzq, AperturaMinima, codigo + ": la puerta del conductor se abrió al bajar (" + maxIzq + "°)");
            Assert.LessOrEqual(maxDer, ApagadaMaxima, "La del copiloto no se mueve");
            yield return Esperar(0.6f);
            Assert.AreEqual(0f, Abierta(Lado.Izquierda), ApagadaMaxima, codigo + ": al terminar de bajar la puerta quedó cerrada");
        }

        [UnityTest]
        public IEnumerator Subir_ElJugadorVaDentroSinVidrio_LaCabezaQuedaDentroDeLaSilueta()
        {
            // Sin vidrio: el conductor sentado queda entre el techo y la línea de la carrocería y nada tapa su modelo.
            yield return PrepararCarro("V01a");
            yield return SubirDesde(true, "V01a-sin-vidrio", (i, d) => { });
            yield return Esperar(0.9f);
            MechaCyberpunk.Personaje.Visual.VisualPersonaje visual = jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true);
            Assert.IsTrue(visual.gameObject.activeInHierarchy, "El modelo del conductor se ve");
            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            Bounds todo = new Bounds(carroObj.transform.position, Vector3.zero);
            foreach (Renderer r in carroObj.GetComponentsInChildren<Renderer>())
            {
                todo.Encapsulate(r.bounds);
            }
            Assert.IsTrue(todo.Contains(cabeza.position), "La cabeza del conductor está dentro del volumen del carro: " + cabeza.position + " vs " + todo);
            foreach (Renderer r in carroObj.GetComponentsInChildren<Renderer>())
            {
                foreach (Material m in r.sharedMaterials)
                {
                    StringAssert.DoesNotContain("Vidrio", m.name, "Ninguna pieza del carro es de vidrio");
                }
            }
        }
    }
}
