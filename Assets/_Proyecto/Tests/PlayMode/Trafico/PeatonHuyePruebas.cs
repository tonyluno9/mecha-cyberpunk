using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-443: un peatón sobre una banqueta recta oye un disparo (EventosRuido) y sale corriendo al lado contrario; uno lejano no lo oye;
    /// pasados los segundos de huida vuelve a caminar normal.
    /// </summary>
    public class PeatonHuyePruebas
    {
        private const float Tolerancia = 0.01f;
        private const float SegundosHuidaDePrueba = 0.6f;
        private static readonly Vector3[] Ruta = { new Vector3(0f, 0f, 0f), new Vector3(200f, 0f, 0f) };

        private GameObject raiz;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            EventosRuido.LimpiarSuscriptores();
            config = ScriptableObject.CreateInstance<ConfigPeatones>();
            typeof(ConfigPeatones).GetField("segundosHuida", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, SegundosHuidaDePrueba);
            raiz = new GameObject("Peatones");
        }

        [TearDown]
        public void Limpiar()
        {
            EventosRuido.LimpiarSuscriptores();
            Object.Destroy(raiz);
            Object.Destroy(config);
        }

        // Un peatón a 'metros' del inicio de la ruta, sin esperas (empieza caminando hacia +X).
        private PeatonMundo CrearPeaton(float metros)
        {
            GameObject objeto = new GameObject("Peaton");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            peaton.Preparar(Ruta, config, new System.Random(1), () => (Vector3?)null, metros / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        [UnityTest]
        public IEnumerator Muro_EnSuCamino_SeDaLaVueltaSinAtravesarlo()
        {
            // T-534: los peatones ya no atraviesan ni se quedan pegados a lo que les tapa la banqueta.
            PeatonMundo peaton = CrearPeaton(50f);
            GameObject muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.transform.SetParent(raiz.transform, false);
            muro.transform.position = new Vector3(54f, 1f, 0f);
            muro.transform.localScale = new Vector3(0.5f, 2f, 3f);
            yield return new WaitForSeconds(6f);
            Assert.GreaterOrEqual(peaton.Vueltas, 1, "Se dio la vuelta (está en " + peaton.transform.position + ", mira " + peaton.transform.forward + ")");
            Assert.Less(peaton.transform.position.x, 53.75f, "No atravesó el muro");
        }

        [UnityTest]
        public IEnumerator Disparo_DetrasDelPeaton_LoHaceCorrerHaciaDelanteMasRapido()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            yield return null;
            Vector3 inicio = peaton.transform.position;
            // El disparo suena atrás (en x = 40): el peatón ya iba hacia +X y acelera.
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            Assert.IsTrue(peaton.Huyendo, "Un disparo a 10 m lo hace huir");
            yield return new WaitForSeconds(0.3f);
            float recorrido = peaton.transform.position.x - inicio.x;
            Assert.Greater(recorrido, 0.3f * config.Velocidad * 2f, "Corre más rápido que caminando, alejándose del disparo");
        }

        [UnityTest]
        public IEnumerator Disparo_EnfrenteDelPeaton_LoHaceDarLaVuelta()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            yield return null;
            // El disparo suena adelante (en x = 70): da la vuelta y huye hacia -X. Se mide después de que decidió (el primer cuadro puede tardar).
            EventosRuido.PublicarDisparo(new Vector3(70f, 0f, 0f));
            yield return null;
            yield return null;
            float inicio = peaton.transform.position.x;
            yield return new WaitForSeconds(0.3f);
            Assert.Less(peaton.transform.position.x, inicio - 0.3f, "Huye alejándose del disparo");
        }

        [UnityTest]
        public IEnumerator Disparo_Lejano_NoLoOyen()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            yield return null;
            EventosRuido.PublicarDisparo(new Vector3(50f + config.RadioOirDisparo + 5f, 0f, 0f));
            Assert.IsFalse(peaton.Huyendo, "Fuera del radio no lo oye");
        }

        [UnityTest]
        public IEnumerator Huida_Termina_YVuelveAlPasoNormal()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            yield return null;
            EventosRuido.PublicarDisparo(new Vector3(40f, 0f, 0f));
            yield return new WaitForSeconds(SegundosHuidaDePrueba + 0.3f);
            Assert.IsFalse(peaton.Huyendo, "Pasados los segundos de huida se calma");
            float x = peaton.transform.position.x;
            yield return new WaitForSeconds(0.5f);
            float recorrido = peaton.transform.position.x - x;
            Assert.AreEqual(0.5f * config.Velocidad, recorrido, 0.15f, "Vuelve a caminar a su velocidad");
        }

        // T-631: un peatón que alcanza a otro quieto, delante y en el mismo sentido, se frena sin atravesarlo.
        [UnityTest]
        public IEnumerator Peaton_AlcanzaAOtroQueVaDelante_SeFrenaSinAtravesarlo()
        {
            // El de adelante camina muy despacio (0,05 m/s): el de atrás lo alcanza.
            ConfigPeatones lenta = ScriptableObject.CreateInstance<ConfigPeatones>();
            typeof(ConfigPeatones).GetField("velocidad", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(lenta, 0.05f);
            GameObject objetoLento = new GameObject("PeatonLento");
            objetoLento.transform.SetParent(raiz.transform, false);
            objetoLento.SetActive(false);
            PeatonMundo adelante = objetoLento.AddComponent<PeatonMundo>();
            adelante.Preparar(Ruta, lenta, new System.Random(1), () => (Vector3?)null, 52f / 0.05f);
            objetoLento.SetActive(true);
            PeatonMundo atras = CrearPeaton(48f);
            yield return new WaitForSeconds(3.5f);
            Assert.IsTrue(atras.Frenado, "Se frena detrás del otro");
            float separacion = Mathf.Abs(adelante.transform.position.x - atras.transform.position.x);
            Assert.Greater(separacion, 0.5f, "No lo atraviesa");
            Assert.Less(separacion, 1.2f, "Se quedó cerca pero sin chocar");
            Object.Destroy(lenta);
        }

        // T-631: dos peatones que se cruzan en sentidos opuestos pasan cada uno por su derecha, sin encimarse.
        [UnityTest]
        public IEnumerator Peatones_EnSentidosOpuestos_SeCruzanSinEncimarse()
        {
            PeatonMundo ida = CrearPeaton(50f);
            GameObject objeto = new GameObject("PeatonVuelta");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            PeatonMundo vuelta = objeto.AddComponent<PeatonMundo>();
            Vector3[] reves = { Ruta[1], Ruta[0] };
            vuelta.Preparar(reves, config, new System.Random(2), () => (Vector3?)null, 140f / config.Velocidad);
            objeto.SetActive(true);
            float minima = float.MaxValue;
            float fin = Time.time + 6f;
            bool seCruzaron = false;
            while (Time.time < fin)
            {
                yield return null;
                Vector3 d = ida.transform.position - vuelta.transform.position;
                minima = Mathf.Min(minima, d.magnitude);
                seCruzaron |= ida.transform.position.x > vuelta.transform.position.x;
            }
            Assert.IsTrue(seCruzaron, "Se cruzaron");
            Assert.Greater(minima, 0.45f, "Nunca se encimaron (distancia mínima " + minima + ")");
            Assert.IsFalse(ida.Frenado || vuelta.Frenado, "Los que van en sentidos opuestos no se frenan");
        }
    }
}
