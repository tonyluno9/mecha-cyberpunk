using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>El bar de T-083: props de Blender en vez de cubos, recorrido a pie entre ellos sin atorarse y luces dentro del tope.</summary>
    public class BarPropsPruebas : InputTestFixture
    {
        private const int MinimoBarras = 5;
        private const int MinimoMesas = 6;
        private const int MaximoMesas = 8;
        private const int CantidadLetreros = 2;
        private const int MaximoLuces = 12;
        private const float SegundosFisica = 0.3f;
        private const float AlturaApoyo = 0.05f;
        private const float DistanciaLejos = 40f;
        private const float SegundosMaximosRecorrido = 30f;
        private const float MetrosMinimosPorSegundo = 0.5f;
        private const float SegundoVentana = 1f;
        private const float MetrosLlegada = 0.4f;
        private const float MetrosDeLaMesa = 3f;
        private const float GradosAlineado = 3f;
        private const float GradosPorGolpeDeRaton = 0.1f;   // ConfigCamaraLibre.Sensibilidad
        private const float RatonMaximoPorFrame = 300f;
        // Puntos del recorrido, en metros desde el muro del fondo del interior (d) y desde el centro (x).
        private const float DInterior = 13f;
        private const float DBarra = 4.3f;
        private const float XFrenteDelEscenario = 5.4f;
        private const float DDelEscenario = 9.2f;
        private const float DPasilloMesas = 8.75f;
        private const float XJuntoALaMesa = 1.4f;

        private static readonly string[] CubosGreybox = { "Barra", "BarraTapa", "BarraNeon", "Banco", "Mesa", "BancoMesa", "CabinaDJ", "CabinaDJNeon", "Bocina" };

        private Keyboard teclado;
        private Mouse raton;
        private RelojMundo reloj;
        private GameObject camaraObjeto;
        private CamaraLibre camara;
        private ZonaInterior zona;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            EventosLugares.LimpiarSuscriptores();
        }

        public override void TearDown()
        {
            EventosLugares.LimpiarSuscriptores();
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
            base.TearDown();
        }

        private IEnumerator Preparar()
        {
            yield return EscenaLugares.CargarCiudad();
            reloj = EscenaLugares.RelojPausado();
            reloj.AvanzarHasta(EscenaLugares.HoraNoche);
            zona = Object.FindFirstObjectByType<ZonaInterior>();
            Assert.IsNotNull(zona, "Ciudad debe traer la ZonaInterior del bar");
        }

        private List<Transform> Interior(string nombre)
        {
            List<Transform> lista = new List<Transform>();
            foreach (Transform t in zona.transform.parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    lista.Add(t);
                }
            }
            return lista;
        }

        // Punto del piso del interior: x desde el centro y d metros desde el muro del fondo hacia la puerta.
        private Vector3 Punto(float x, float d)
        {
            Bounds caja = zona.GetComponent<BoxCollider>().bounds;
            return new Vector3(caja.center.x + x, caja.min.y + AlturaApoyo, caja.min.z + d);
        }

        private float DistanciaPlana(Vector3 punto)
        {
            Vector3 d = punto - camara.transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        // Gira con el ratón hacia el punto y camina con W hasta llegar; anota los metros avanzados en cada segundo.
        private IEnumerator IrA(Vector3 destino, List<float> avancePorSegundo, float[] tiempo)
        {
            Vector3 inicioVentana = camara.transform.position;
            float tiempoVentana = tiempo[0];
            while (DistanciaPlana(destino) > MetrosLlegada && tiempo[0] < SegundosMaximosRecorrido)
            {
                Vector3 haciaDestino = destino - camara.transform.position;
                float objetivo = Mathf.Atan2(haciaDestino.x, haciaDestino.z) * Mathf.Rad2Deg;
                float error = Mathf.DeltaAngle(camara.Yaw, objetivo);
                float golpe = Mathf.Clamp(error / GradosPorGolpeDeRaton, -RatonMaximoPorFrame, RatonMaximoPorFrame);
                Set(raton.delta, new Vector2(golpe, 0f));
                if (Mathf.Abs(error) < GradosAlineado)
                {
                    Press(teclado.wKey);
                }
                else
                {
                    Release(teclado.wKey);
                }
                yield return null;
                tiempo[0] += Time.deltaTime;
                if (tiempo[0] - tiempoVentana >= SegundoVentana)
                {
                    avancePorSegundo.Add(Vector3.Distance(inicioVentana, camara.transform.position));
                    inicioVentana = camara.transform.position;
                    tiempoVentana = tiempo[0];
                }
            }
            Set(raton.delta, Vector2.zero);
            Release(teclado.wKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bar_Props_TieneBarrasMesasDjYLetrerosYNingunCuboGreybox()
        {
            yield return Preparar();
            int barras = Interior("Prop_Barra").Count;
            int mesas = Interior("Prop_Mesa").Count;
            Assert.GreaterOrEqual(barras, MinimoBarras, "Barra de 5 Prop_Barra o más");
            Assert.AreEqual(1, Interior("Prop_DJ").Count, "Una cabina Prop_DJ");
            Assert.GreaterOrEqual(mesas, MinimoMesas, "Entre 6 y 8 Prop_Mesa");
            Assert.LessOrEqual(mesas, MaximoMesas, "Entre 6 y 8 Prop_Mesa");
            Assert.AreEqual(CantidadLetreros, Interior("Prop_Letrero").Count, "Dos Prop_Letrero");
            foreach (string nombre in CubosGreybox)
            {
                Assert.AreEqual(0, Interior(nombre).Count, "No debe quedar el cubo greybox '" + nombre + "'");
            }
            foreach (Transform prop in Interior("Prop_Barra"))
            {
                Assert.IsNotNull(prop.GetComponent<Collider>(), "El collider de T-076 se conserva en " + prop.name);
            }
        }

        [UnityTest]
        public IEnumerator Bar_Luces_SonDoceOMenosYSeApaganYPrendenPorPresencia()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            Light[] todas = zona.transform.parent.GetComponentsInChildren<Light>(true);
            Assert.LessOrEqual(todas.Length, MaximoLuces, "El interior no pasa de 12 luces");
            Assert.AreEqual(todas.Length, zona.Luces.Length, "Todas las luces del interior están bajo ZonaInterior");

            Vector3 lejos = EscenaLugares.Lugar(IdLugar.Bar).PosicionEntrada + EscenaLugares.HaciaLaPuerta(IdLugar.Bar) * -DistanciaLejos;
            camara.Colocar(new Pose(lejos + Vector3.up * AlturaApoyo, Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsFalse(luz.enabled, "Sin nadie dentro '" + luz.name + "' debe estar apagada");
            }

            camara.Colocar(new Pose(Punto(0f, DBarra), Quaternion.identity));
            yield return EscenaLugares.Esperar(SegundosFisica);
            foreach (Light luz in zona.Luces)
            {
                Assert.IsTrue(luz.enabled, "Al entrar '" + luz.name + "' debe estar prendida");
            }
        }

        [UnityTest]
        public IEnumerator Bar_A22_DeLaPuertaALaBarraAlDjYALaMesaMasLejana_SinAtorarse()
        {
            yield return Preparar();
            camara = EscenaLugares.ColocarFrenteA(IdLugar.Bar, out camaraObjeto);
            yield return EscenaLugares.Calentar();
            Assert.AreEqual(EscenaLugares.HoraNoche, reloj.Hora, 0.01f);
            PressAndRelease(teclado.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(ResultadoPuerta.Abierta, camara.UltimoResultadoPuerta, "La puerta abre a las 22:00");

            Transform dj = Interior("Prop_DJ")[0];
            Transform lejana = null;
            float mayor = -1f;
            foreach (Transform mesa in Interior("Prop_Mesa"))
            {
                float distancia = Vector3.Distance(mesa.position, dj.position);
                if (distancia > mayor)
                {
                    mayor = distancia;
                    lejana = mesa;
                }
            }
            Bounds caja = zona.GetComponent<BoxCollider>().bounds;
            float xLejana = lejana.position.x - caja.center.x;
            Vector3 mesaLejana = lejana.position;
            mesaLejana.y = 0f;

            Vector3[] ruta =
            {
                Punto(0f, DInterior),
                Punto(0f, DBarra),
                Punto(XFrenteDelEscenario, DBarra),
                Punto(XFrenteDelEscenario, DDelEscenario),
                Punto(XFrenteDelEscenario, DPasilloMesas),
                Punto(xLejana + XJuntoALaMesa, DPasilloMesas)
            };
            List<float> avances = new List<float>();
            float[] tiempo = { 0f };
            Vector3 inicio = camara.transform.position;
            foreach (Vector3 punto in ruta)
            {
                yield return IrA(punto, avances, tiempo);
                Assert.LessOrEqual(DistanciaPlana(punto), MetrosLlegada + 0.1f, "No llegó al punto " + punto + " (tiempo " + tiempo[0] + " s, está en " + camara.transform.position + ")");
            }

            Assert.Less(tiempo[0], SegundosMaximosRecorrido, "El recorrido debe tomar menos de 30 s");
            Assert.Greater(avances.Count, 0, "El recorrido debe durar al menos un segundo");
            for (int i = 0; i < avances.Count; i++)
            {
                Assert.Greater(avances[i], MetrosMinimosPorSegundo, "En el segundo " + i + " avanzó solo " + avances[i] + " m: se atoró");
            }
            Vector3 camaraPlana = camara.transform.position;
            camaraPlana.y = 0f;
            Assert.Less(Vector3.Distance(camaraPlana, mesaLejana), MetrosDeLaMesa, "Termina junto a la mesa más lejana del DJ");
            Assert.Greater(Vector3.Distance(inicio, camara.transform.position), 1f);
        }
    }
}
