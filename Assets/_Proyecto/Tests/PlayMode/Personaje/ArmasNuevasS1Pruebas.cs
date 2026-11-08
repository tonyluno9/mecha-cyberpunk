using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-679: la escopeta W02, el fusil W03 y el francotirador W04 con su modelo: en la mano (cañón al frente, empuñadura en la palma), en el piso
    /// (se recogen con E y quedan en el arsenal) y con sus estadísticas. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-679-*.png.
    /// </summary>
    public class ArmasNuevasS1Pruebas : BaseJugadorAPie
    {
        private static readonly (string codigo, string nombre, string objeto, ArmaAPie arma, string pisoPrefab)[] Armas =
        {
            ("W02", "escopeta", "Escopeta", ArmaAPie.Escopeta, "Assets/Arte/Armas/W02_Escopeta/W02_Escopeta_Piso.prefab"),
            ("W03", "fusil", "Fusil", ArmaAPie.Fusil, "Assets/Arte/Armas/W03_Fusil/W03_Fusil_Piso.prefab"),
            ("W04", "francotirador", "Sniper", ArmaAPie.Sniper, "Assets/Arte/Armas/W04_Francotirador/W04_Francotirador_Piso.prefab"),
        };

        private VisualPersonaje Visual() => jugador.GetComponentInChildren<VisualPersonaje>(true);

        private Transform Arma(string objeto) => Visual().GetComponentInChildren<ApuntadoIK>(true).SocketArma.Find(objeto);

        private static Vector3 Palma(Animator a) =>
            Vector3.Lerp(a.GetBoneTransform(HumanBodyBones.RightHand).position, a.GetBoneTransform(HumanBodyBones.RightMiddleProximal).position, 0.5f);

        private void Dar(ArmaAPie arma)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
        }

        private void Fotos(string sufijo, Transform arma)
        {
            Transform t = jugador.transform;
            Vector3 centro = arma.position + arma.forward * 0.25f;
            CapturaS1.Foto("s1t-T-679-" + sufijo + "-cuerpo", t, new Vector3(2.4f, 1.0f, 0.3f), new Vector3(0f, 0.95f, 0.3f), 45f, 700, 700);
            CapturaS1.FotoMira("s1t-T-679-" + sufijo + "-der", t, centro, new Vector3(1f, 0.25f, 0.2f), 1.3f, 40f, 700);
            CapturaS1.FotoMira("s1t-T-679-" + sufijo + "-izq", t, centro, new Vector3(-1f, 0.25f, 0.2f), 1.3f, 40f, 700);
            CapturaS1.FotoMira("s1t-T-679-" + sufijo + "-frente", t, centro, new Vector3(0.35f, 0.5f, 1f), 1.3f, 40f, 700);
        }

        [UnityTest]
        public IEnumerator EnLaMano_ModeloVisible_CanonAlFrente_EmpunaduraEnLaPalma_BocaEnLaPunta()
        {
            yield return Calentar();
            ConIKDeApuntadoEncendido(); // T-737: mide las correcciones de ApuntadoIK, que el jugador ya no usa
            VisualPersonaje visual = Visual();
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            foreach (var arma in Armas)
            {
                Dar(arma.arma);
                Assert.IsTrue(armas.Equipar(arma.arma), arma.nombre + " se equipa");
                Transform objeto = Arma(arma.objeto);
                Assert.IsNotNull(objeto, arma.nombre + " cuelga del socket de la mano");
                Assert.IsTrue(objeto.gameObject.activeInHierarchy, arma.nombre + " activa");
                Transform modelo = objeto.Find(arma.codigo);
                Assert.IsNotNull(modelo, arma.codigo + " es el modelo en la mano");
                Renderer[] visibles = objeto.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                Assert.IsTrue(visibles.Length > 0 && visibles.All(r => r.transform.IsChildOf(modelo)), "Solo se ve el modelo nuevo, no las cajas");
                yield return Esperar(1f);
                Assert.Less(Vector3.Distance(objeto.position, Palma(a)), 0.1f, arma.nombre + ": la empuñadura está en la palma (m)");
                Assert.Greater(Vector3.Dot(objeto.forward, visual.transform.forward), -0.2f, arma.nombre + ": el cañón no apunta hacia atrás");
                Transform boca = objeto.Find("Boca");
                Assert.Greater(Vector3.Dot((boca.position - objeto.position).normalized, objeto.forward), 0.9f, arma.nombre + ": la boca está al frente del arma");
                Assert.Greater(Vector3.Distance(boca.position, objeto.position), 0.5f, arma.nombre + ": la boca está en la punta del cañón (m)");
                Fotos(arma.nombre + "-mano", objeto);
                Press(raton.rightButton);
                yield return Esperar(0.9f);
                Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(objeto.forward, Vector3.up).normalized, visual.transform.forward), 0.9f, arma.nombre + ": apuntando el cañón mira al frente");
                Assert.Less(Mathf.Abs(Vector3.Dot(objeto.right, Vector3.up)), 0.35f, arma.nombre + ": no está ladeada");
                Fotos(arma.nombre + "-apuntar", objeto);
                Release(raton.rightButton);
                yield return Esperar(0.6f);
            }
        }

        [UnityTest]
        public IEnumerator Pistola_ParaComparar_EnLaManoYEnElPiso_Capturas()
        {
            yield return Calentar();
            Visual().Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola));
            Transform pistola = Arma("Pistola");
            yield return Esperar(1f);
            Fotos("pistola-mano", pistola);
            Press(raton.rightButton);
            yield return Esperar(0.9f);
            Fotos("pistola-apuntar", pistola);
            Release(raton.rightButton);
            // La pistola del piso la arma DirectorPandillas con el modelo W01 de lado.
            GameObject piso = new GameObject("PistolaEnPiso");
            piso.transform.position = new Vector3(0f, 0.1f, 4f);
            piso.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            GameObject w = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arte/Armas/W01_Pistola/W01_Pistola.prefab"), piso.transform);
            w.transform.localPosition = Vector3.up * 0.03f;
            w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            piso.AddComponent<ArmaEnPiso>();
            yield return Esperar(0.3f);
            CapturaS1.FotoMira("s1t-T-679-pistola-piso", jugador.transform, piso.transform.position + Vector3.up * 0.25f, new Vector3(0.3f, 0.6f, -1f), 1.0f, 40f, 700);
            Object.Destroy(piso);
        }

        [UnityTest]
        public IEnumerator EnElPiso_SeRecogeConE_YQuedaEnElArsenal()
        {
            yield return Calentar();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            float x = -2f;
            foreach (var arma in Armas)
            {
                Assert.IsFalse(arsenal.Tiene(arma.arma), arma.nombre + " todavía no se tiene");
                GameObject piso = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(arma.pisoPrefab), new Vector3(x, 0.1f, 4f), Quaternion.Euler(0f, 35f, 0f));
                x += 2f;
                CapturaS1.FotoMira("s1t-T-679-" + arma.nombre + "-piso", jugador.transform, piso.transform.position + Vector3.up * 0.25f, new Vector3(0.3f, 0.6f, -1f), 1.6f, 40f, 700);
                yield return null;
                ArmaEnPiso enPiso = piso.GetComponent<ArmaEnPiso>();
                Assert.AreEqual(arma.arma, enPiso.Arma);
                Assert.IsTrue(piso.GetComponentsInChildren<Renderer>().Length > 0, "El arma del piso tiene modelo");
                Bounds b = piso.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                Assert.Less(b.size.y, 0.3f, arma.nombre + ": en el piso va acostada (alto, m)");
                Assert.That(Mathf.Max(b.size.x, b.size.z), Is.InRange(0.7f, 1.2f), arma.nombre + ": largo real en el piso");
                Assert.Less(Vector3.Distance(b.center, piso.transform.position + Vector3.up * 0.25f), 0.35f, arma.nombre + ": el modelo flota sobre su punto");
            }
            yield return Esperar(0.3f);
            CapturaS1.Foto("s1t-T-679-armas-piso", jugador.transform, new Vector3(0.6f, 1.9f, 0.4f), new Vector3(0f, 0.2f, 4f), 50f, 1000, 520);
            foreach (ArmaEnPiso enPiso in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None).Where(p => p.Arma != ArmaAPie.Pistola).ToArray())
            {
                ArmaAPie cual = enPiso.Arma;
                enPiso.Usar();
                Assert.IsTrue(enPiso.FueTomada);
                Assert.IsTrue(arsenal.Tiene(cual), cual + " pasó al arsenal al recogerla");
                Assert.IsTrue(armas.Equipar(cual), cual + " se puede equipar");
            }
        }

        [UnityTest]
        public IEnumerator Sniper_ConZoom_AlApuntar()
        {
            yield return Calentar();
            Dar(ArmaAPie.Sniper);
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Sniper));
            ZoomSniper zoom = jugador.GetComponent<ZoomSniper>();
            Assert.IsFalse(zoom.Activo);
            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.IsTrue(zoom.Activo, "Con el francotirador, apuntar hace zoom");
            Release(raton.rightButton);
        }

        [Test]
        public void Estadisticas_EscopetaPerdigonesPocoAlcance_FusilAutomatico_SniperMuchoDanio()
        {
            ArsenalJugador arsenal = jugador.GetComponent<ArsenalJugador>();
            DatosArma pistola = arsenal.Catalogo.Buscar(ArmaAPie.Pistola, 0);
            DatosArma escopeta = arsenal.Catalogo.Buscar(ArmaAPie.Escopeta, 0);
            DatosArma fusil = arsenal.Catalogo.Buscar(ArmaAPie.Fusil, 0);
            DatosArma sniper = arsenal.Catalogo.Buscar(ArmaAPie.Sniper, 0);
            Assert.Greater(escopeta.Perdigones, 1, "La escopeta dispara perdigones");
            Assert.Less(escopeta.Alcance, pistola.Alcance, "La escopeta alcanza poco");
            Assert.Greater(escopeta.Dispersion, 0f, "Los perdigones se abren");
            Assert.IsTrue(fusil.Automatica, "El fusil es automático");
            Assert.Less(fusil.TiempoEntreDisparos, pistola.TiempoEntreDisparos, "El fusil dispara más rápido que la pistola");
            Assert.IsFalse(sniper.Automatica);
            Assert.GreaterOrEqual(sniper.Danio, pistola.Danio * 2.5f, "El francotirador pega muy fuerte");
            Assert.Greater(sniper.Alcance, fusil.Alcance, "El francotirador llega más lejos");
        }

        [Test]
        public void Sonidos_CadaArmaTieneSuPropioDisparo()
        {
            foreach (string id in new[] { "ARM_ESC_DISPARO_01", "ARM_FUS_DISPARO_01", "ARM_SNI_DISPARO_01" })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Arte/Audio/ARM/" + id + ".wav"), id);
            }
        }
    }
}
