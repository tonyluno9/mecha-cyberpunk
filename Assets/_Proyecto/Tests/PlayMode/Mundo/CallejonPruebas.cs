using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Callejón de la emboscada (T-088): punto de llamado con cielo abierto, camino a pie, zona prohibida en la casa y sitio libre para los rivales.</summary>
    public class CallejonPruebas
    {
        private const string IdEntrada = "Callejon_Entrada";
        private const string RutaEmboscada = "Assets/_Proyecto/Pelea/Prefabs/Emboscada.prefab";
        private const float RadioLibre = 3f;
        private const float AltoLibre = 6f;
        private const float MargenCapsula = 0.1f;
        private const float AlturaCaminar = 1f;
        private const float RadioCuerpo = 0.4f;
        // Poses de los rivales de DatosEmboscada_Base (T-071): a 14 m del anclaje hacia el fondo, a ±1.5 m del eje.
        private const float MetrosAlFondo = 14f;
        private const float MetrosDeLado = 1.5f;
        private const float AlturaOjos = 1.7f;
        private const float AtrasCaptura = 6f;
        private const float SegundosFisica = 0.3f;

        private GameObject camaraObjeto;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        private static IPuntoLlamadoMecha PuntoDelCallejon()
        {
            IPuntoAparicion entrada = EntradaCallejon();
            IPuntoLlamadoMecha mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (IPuntoLlamadoMecha punto in Registro<IPuntoLlamadoMecha>.Todos)
            {
                float d = Vector3.Distance(punto.Llegada.position, entrada.Pose.position);
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = punto;
                }
            }
            return mejor;
        }

        private static IPuntoAparicion EntradaCallejon()
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto.Id == IdEntrada)
                {
                    return punto;
                }
            }
            Assert.Fail("Falta el punto de aparición " + IdEntrada);
            return null;
        }

        [UnityTest]
        public IEnumerator Callejon_PuntoDeLlamado_TieneCieloAbiertoYSuelo()
        {
            yield return EscenaLugares.CargarCiudad();
            IPuntoLlamadoMecha punto = PuntoDelCallejon();
            Assert.IsNotNull(punto, "Hay un IPuntoLlamadoMecha");
            Assert.IsTrue(punto.Habilitado);
            Assert.Greater(punto.Radio, 0f);
            Vector3 suelo = punto.Llegada.position;
            Vector3 abajo = suelo + Vector3.up * (RadioLibre + MargenCapsula);
            Vector3 arriba = suelo + Vector3.up * (AltoLibre - RadioLibre);
            Assert.IsFalse(Physics.CheckCapsule(abajo, arriba, RadioLibre, ~0, QueryTriggerInteraction.Ignore), "El cilindro de 3 m × 6 m sobre Llegada está libre");
            Assert.IsTrue(Physics.Raycast(suelo + Vector3.up * 0.5f, Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore), "Hay suelo bajo Llegada");
            Assert.IsFalse(Physics.Raycast(suelo + Vector3.up * AltoLibre, Vector3.up, 50f, ~0, QueryTriggerInteraction.Ignore), "Nada cubre el cielo sobre Llegada");
        }

        [UnityTest]
        public IEnumerator Callejon_Radio_CubreLaEntradaYNoLaCasaPorDentro()
        {
            yield return EscenaLugares.CargarCiudad();
            IPuntoLlamadoMecha punto = PuntoDelCallejon();
            Vector3 entrada = EntradaCallejon().Pose.position;
            Assert.LessOrEqual(Plana(punto.Llegada.position, entrada), punto.Radio, "La boca del callejón está dentro del radio");
            foreach (ZonaInterior zona in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                Vector3 centro = zona.GetComponent<BoxCollider>().bounds.center;
                foreach (IPuntoLlamadoMecha candidato in Registro<IPuntoLlamadoMecha>.Todos)
                {
                    Assert.Greater(Plana(candidato.Llegada.position, centro), candidato.Radio, "El interior '" + zona.transform.parent.name + "' es zona prohibida");
                }
            }
        }

        [UnityTest]
        public IEnumerator Callejon_DeLaEntradaALaLlegada_HayCaminoAPie()
        {
            yield return EscenaLugares.CargarCiudad();
            IPuntoLlamadoMecha punto = PuntoDelCallejon();
            Vector3 desde = EntradaCallejon().Pose.position + Vector3.up * AlturaCaminar;
            Vector3 hasta = punto.Llegada.position + Vector3.up * AlturaCaminar;
            Assert.IsFalse(Physics.SphereCast(desde, RadioCuerpo, (hasta - desde).normalized, out RaycastHit golpe, Vector3.Distance(desde, hasta), ~0, QueryTriggerInteraction.Ignore),
                "Camino directo libre; choca con " + (golpe.collider != null ? golpe.collider.name : ""));
        }

        [UnityTest]
        public IEnumerator Callejon_Emboscada_SusRivalesCaenEnPisoLibre()
        {
            yield return EscenaLugares.CargarCiudad();
            Transform anclaje = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "Emboscada_Anclaje")
                {
                    anclaje = t;
                }
            }
            Assert.IsNotNull(anclaje, "El callejón publica Emboscada_Anclaje");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaEmboscada);
            Assert.IsNotNull(prefab, "Existe Emboscada.prefab de Tony");
            GameObject instancia = Object.Instantiate(prefab, anclaje.position, anclaje.rotation);
            yield return null;
            Assert.IsNotNull(instancia);
            foreach (float lado in new[] { -MetrosDeLado, MetrosDeLado })
            {
                Vector3 sitio = instancia.transform.TransformPoint(new Vector3(lado, 0f, MetrosAlFondo));
                Assert.IsTrue(Physics.Raycast(sitio + Vector3.up * 2f, Vector3.down, out RaycastHit suelo, 4f, ~0, QueryTriggerInteraction.Ignore), "Hay piso bajo el rival en " + sitio);
                Assert.AreEqual(sitio.y, suelo.point.y, 0.3f, "El rival pisa el suelo del callejón");
                Assert.IsFalse(Physics.CheckSphere(sitio + Vector3.up * 1.2f, RadioCuerpo, ~0, QueryTriggerInteraction.Ignore), "El rival cabe sin tocar muros ni cajas en " + sitio);
            }
            Object.DestroyImmediate(instancia);
        }

        [UnityTest]
        public IEnumerator Callejon_Capturas_DiaYNoche()
        {
            yield return EscenaLugares.CargarCiudad();
            RelojMundo reloj = EscenaLugares.RelojPausado();
            camaraObjeto = null;
            EscenaLugares.ColocarFrenteA(IdLugar.Casa, out camaraObjeto);
            CamaraLibre camara = camaraObjeto.GetComponent<CamaraLibre>();
            Pose entrada = EntradaCallejon().Pose;
            Vector3 hacia = entrada.rotation * Vector3.forward;
            Vector3 desde = entrada.position - hacia * AtrasCaptura + Vector3.up * AlturaOjos;
            Quaternion mira = Quaternion.LookRotation(hacia + Vector3.down * 0.05f, Vector3.up);
            foreach ((float hora, string ruta) in new[] { (EscenaLugares.HoraMediodia, "docs/capturas/T-088-1.png"), (EscenaLugares.HoraNoche, "docs/capturas/T-088-2.png") })
            {
                reloj.AvanzarHasta(hora);
                camara.Colocar(new Pose(desde, mira));
                yield return EscenaLugares.Calentar();
                yield return EscenaLugares.Esperar(SegundosFisica);
                CapturaPruebas.Guardar(camara.Camara, ruta);
            }
        }

        private static float Plana(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
