using System.Collections;
using System.IO;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
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
    /// T-1014: las cinco armas nuevas (subfusil W05, revólver W06, rifle de plasma W07, lanzagranadas W08 y granada W09) con su modelo: en la mano (visible, cañón al frente,
    /// empuñadura en la palma, boca en la punta, tamaño real), en el piso (se recogen con E) y el tambor del revólver y del lanzagranadas (gira sobre su eje; el del revólver
    /// avanza una recámara con cada disparo). Capturas con CAPTURAR_S1=1 en docs/capturas/t1014/.
    /// </summary>
    public class ArmasNuevasT1014Pruebas : BaseJugadorAPie
    {
        private const string CarpetaCapturas = "t1014/";
        // Margen del largo del modelo respecto al largo real esperado.
        private const float MargenLargo = 0.25f;
        // Cuánto puede moverse el centro del tambor al girarlo 60° (m): su origen está en el eje.
        private const float MovimientoMaximoTambor = 0.012f;
        private const float GradosPrueba = 60f;

        private static readonly (string codigo, string nombre, string objeto, ArmaAPie arma, string piso, float largo, float distanciaBoca, bool poseDePistola)[] Armas =
        {
            ("W05", "subfusil", "Subfusil", ArmaAPie.Subfusil, "Assets/Arte/Armas/W05_Subfusil/W05_Subfusil_Piso.prefab", 0.6f, 0.4f, false),
            ("W06", "revolver", "Revolver", ArmaAPie.Revolver, "Assets/Arte/Armas/W06_Revolver/W06_Revolver_Piso.prefab", 0.35f, 0.25f, true),
            ("W08", "lanzagranadas", "Lanzagranadas", ArmaAPie.Lanzagranadas, "Assets/Arte/Armas/W08_Lanzagranadas/W08_Lanzagranadas_Piso.prefab", 0.8f, 0.45f, false),
            ("W09", "granada", "Granada", ArmaAPie.Granada, "Assets/Arte/Armas/W09_Granada/W09_Granada_Piso.prefab", 0.1f, 0.1f, true),
            ("W07", "rifle", "RiflePlasma", ArmaAPie.RiflePlasma, "Assets/Arte/Armas/W07_RiflePlasma/W07_RiflePlasma_Piso.prefab", 1.0f, 0.55f, false),
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

        private static Bounds Limites(Transform raiz) =>
            raiz.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });

        private void Fotos(string sufijo, Transform arma, bool granada)
        {
            if (!CapturaS1.Activa)
            {
                return;
            }
            Directory.CreateDirectory("docs/capturas/" + CarpetaCapturas);
            Transform t = jugador.transform;
            Vector3 centro = arma.position + arma.forward * (granada ? 0f : 0.25f);
            float distancia = granada ? 0.6f : 1.3f;
            CapturaS1.Foto(CarpetaCapturas + sufijo + "-cuerpo", t, new Vector3(2.4f, 1.0f, 0.3f), new Vector3(0f, 0.95f, 0.3f), 45f, 700, 700);
            CapturaS1.FotoMira(CarpetaCapturas + sufijo + "-cerca-der", t, centro, new Vector3(1f, 0.25f, 0.2f), distancia, 40f, 700);
            CapturaS1.FotoMira(CarpetaCapturas + sufijo + "-cerca-frente", t, centro, new Vector3(0.35f, 0.5f, 1f), distancia, 40f, 700);
        }

        [UnityTest]
        public IEnumerator EnLaMano_ModeloVisible_CanonAlFrente_EmpunaduraEnLaPalma_BocaEnLaPunta()
        {
            yield return Calentar();
            ConIKDeApuntadoEncendido(); // mide las correcciones de ApuntadoIK, que el jugador ya no usa
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
                Assert.AreSame(objeto.gameObject, armas.Visual(arma.arma), arma.nombre + " es el visual de ArmasAPie");
                if (arma.poseDePistola)
                {
                    // Como la pistola: el revólver y la granada salen al apuntar (evento de Pistola_Sacar).
                    Press(raton.rightButton);
                }
                yield return Esperar(1f);
                Assert.IsTrue(objeto.gameObject.activeInHierarchy, arma.nombre + " activa en la mano");
                Transform modelo = objeto.Find(arma.codigo);
                Assert.IsNotNull(modelo, arma.codigo + " es el modelo en la mano");
                Renderer[] visibles = objeto.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                Assert.IsTrue(visibles.Length > 0 && visibles.All(r => r.transform.IsChildOf(modelo)), arma.nombre + ": solo se ve el modelo");
                Bounds b = Limites(modelo);
                float largo = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                Assert.That(largo, Is.InRange(arma.largo * (1f - MargenLargo), arma.largo * (1f + MargenLargo)), arma.nombre + ": tamaño real (m)");
                Assert.Less(Vector3.Distance(objeto.position, Palma(a)), 0.1f, arma.nombre + ": la empuñadura está en la palma (m)");
                Assert.Greater(Vector3.Dot(objeto.forward, visual.transform.forward), -0.2f, arma.nombre + ": el cañón no apunta hacia atrás");
                Transform boca = objeto.Find("Boca");
                Assert.IsNotNull(boca.GetComponent<Light>(), arma.nombre + ": la luz de la boca");
                Assert.Greater(Vector3.Dot((boca.position - objeto.position).normalized, objeto.forward), 0.9f, arma.nombre + ": la boca está al frente del arma");
                Assert.Greater(Vector3.Distance(boca.position, objeto.position), arma.distanciaBoca, arma.nombre + ": la boca está en la punta (m)");
                if (arma.arma != ArmaAPie.Granada)
                {
                    // La boca coincide con el punto más adelantado del modelo.
                    float punta = visibles.SelectMany(r => new[] { r.bounds.min, r.bounds.max }).Max(p => Vector3.Dot(p - objeto.position, objeto.forward));
                    Assert.Less(Mathf.Abs(Vector3.Dot(boca.position - objeto.position, objeto.forward) - punta), 0.15f, arma.nombre + ": la boca está en la punta del modelo (m)");
                }
                Fotos(arma.nombre + "-mano", objeto, arma.arma == ArmaAPie.Granada);
                if (!arma.poseDePistola)
                {
                    Press(raton.rightButton);
                    yield return Esperar(0.9f);
                }
                if (arma.arma != ArmaAPie.Granada)
                {
                    Assert.Greater(Vector3.Dot(Vector3.ProjectOnPlane(objeto.forward, Vector3.up).normalized, visual.transform.forward), 0.9f, arma.nombre + ": apuntando el cañón mira al frente");
                    Assert.Less(Mathf.Abs(Vector3.Dot(objeto.right, Vector3.up)), 0.35f, arma.nombre + ": no está ladeada");
                }
                Fotos(arma.nombre + "-apuntar", objeto, arma.arma == ArmaAPie.Granada);
                Release(raton.rightButton);
                yield return Esperar(0.6f);
            }
        }

        [UnityTest]
        public IEnumerator EnElPiso_SeRecogeConE_YQuedaEnElArsenal()
        {
            yield return Calentar();
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            float x = -3f;
            foreach (var arma in Armas)
            {
                Assert.IsFalse(arsenal.Tiene(arma.arma), arma.nombre + " todavía no se tiene");
                GameObject piso = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(arma.piso), new Vector3(x, 0.1f, 4f), Quaternion.Euler(0f, 35f, 0f));
                x += 1.5f;
                if (CapturaS1.Activa)
                {
                    Directory.CreateDirectory("docs/capturas/" + CarpetaCapturas);
                    CapturaS1.FotoMira(CarpetaCapturas + arma.nombre + "-piso", jugador.transform, piso.transform.position + Vector3.up * 0.25f, new Vector3(0.3f, 0.6f, -1f),
                        arma.arma == ArmaAPie.Granada ? 0.8f : 1.6f, 40f, 700);
                }
                yield return null;
                ArmaEnPiso enPiso = piso.GetComponent<ArmaEnPiso>();
                Assert.AreEqual(arma.arma, enPiso.Arma);
                Assert.IsTrue(piso.GetComponentsInChildren<Renderer>().Length > 0, "El arma del piso tiene modelo");
                Bounds b = piso.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((p, q) => { p.Encapsulate(q); return p; });
                Assert.Less(b.size.y, Mathf.Max(0.3f, arma.largo * 0.5f), arma.nombre + ": en el piso va acostada (alto, m)");
                Assert.That(Mathf.Max(b.size.x, b.size.z), Is.InRange(arma.largo * 0.7f, arma.largo * 1.9f), arma.nombre + ": largo en el piso");
                Assert.Less(Vector3.Distance(b.center, piso.transform.position + Vector3.up * 0.25f), 0.35f, arma.nombre + ": el modelo flota sobre su punto");
                Assert.AreEqual(0, piso.GetComponentsInChildren<Collider>().Length, arma.nombre + ": sin colliders");
            }
            yield return Esperar(0.3f);
            if (CapturaS1.Activa)
            {
                CapturaS1.Foto(CarpetaCapturas + "armas-piso", jugador.transform, new Vector3(0.6f, 1.9f, 0.6f), new Vector3(0f, 0.2f, 4f), 50f, 1000, 520);
            }
            foreach (ArmaEnPiso enPiso in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None).ToArray())
            {
                ArmaAPie cual = enPiso.Arma;
                enPiso.Usar();
                Assert.IsTrue(enPiso.FueTomada);
                Assert.IsTrue(arsenal.Tiene(cual), cual + " pasó al arsenal al recogerla");
                Assert.IsTrue(armas.Equipar(cual), cual + " se puede equipar");
            }
        }

        [UnityTest]
        public IEnumerator Tambor_ElRevolverYElLanzagranadasGiranSobreSuEje_YElRevolverGiraAlDisparar()
        {
            yield return Calentar();
            Visual().Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate; // sin esto la cámara de las capturas "despierta" la animación y mueve el arma
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            foreach (ArmaAPie tipo in new[] { ArmaAPie.Revolver, ArmaAPie.Lanzagranadas })
            {
                Dar(tipo);
                Assert.IsTrue(armas.Equipar(tipo));
                if (tipo == ArmaAPie.Revolver)
                {
                    Press(raton.rightButton); // la pose de pistola muestra el arma al apuntar
                }
                yield return Esperar(1f);
                GameObject visual = armas.Visual(tipo);
                Transform tambor = visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == TamborArma.NombreTambor);
                Assert.IsNotNull(tambor, tipo + ": tiene un objeto Tambor");
                Assert.IsNotNull(tambor.GetComponent<TamborArma>(), tipo + ": ArmasEspecialesAPie le puso TamborArma");
                MeshFilter filtro = tambor.GetComponent<MeshFilter>();
                Assert.IsNotNull(filtro, tipo + ": el tambor tiene su malla");
                Assert.Less(filtro.sharedMesh.bounds.size.x + filtro.sharedMesh.bounds.size.y + filtro.sharedMesh.bounds.size.z, 0.7f, tipo + ": el tambor es solo una parte del arma (m, sin escala)");
                // Gira 60° sobre el eje del cañón (+Z del arma): el centro del tambor no se mueve (su origen está en el eje del tambor).
                Vector3 CentroDelTambor() => tambor.TransformPoint(filtro.sharedMesh.bounds.center);
                FotoTambor(tipo, visual.transform, "0"); // la foto renderiza una cámara: el arma puede moverse con la animación, por eso se mide después
                Vector3 antes = CentroDelTambor();
                Quaternion reposo = tambor.localRotation;
                Vector3 eje = tambor.parent.InverseTransformDirection(visual.transform.forward).normalized;
                // A 30° la diferencia se ve (un tambor de 6 recámaras a 60° queda igual de lado), por eso la foto intermedia.
                tambor.localRotation = Quaternion.AngleAxis(GradosPrueba / 2f, eje) * reposo;
                FotoTambor(tipo, visual.transform, "30");
                tambor.localRotation = Quaternion.AngleAxis(GradosPrueba, eje) * reposo;
                Assert.Less(Vector3.Distance(antes, CentroDelTambor()), MovimientoMaximoTambor, tipo + ": al girar 60° el tambor sigue en su sitio (m)");
                Assert.AreEqual(GradosPrueba, Quaternion.Angle(reposo, tambor.localRotation), 0.5f, tipo + ": giró 60°");
                FotoTambor(tipo, visual.transform, "60");
                tambor.localRotation = reposo;
                Release(raton.rightButton);
                yield return Esperar(0.5f);
            }

            // El revólver gira una recámara (60° de 6) con cada disparo.
            Assert.IsTrue(armas.Equipar(ArmaAPie.Revolver));
            TamborArma t = armas.Visual(ArmaAPie.Revolver).GetComponentInChildren<TamborArma>(true);
            Press(raton.rightButton);
            yield return Esperar(0.9f);
            float antesObjetivo = t.Objetivo;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.6f);
            Release(raton.rightButton);
            Assert.AreEqual(360f / TamborArma.RecamarasPorOmision, t.Objetivo - antesObjetivo, 0.5f, "Un disparo gira una recámara");
            Assert.AreEqual(t.Objetivo, t.Angulo, 0.5f, "El tambor termina de girar");
        }

        // Foto del tambor de lado (desde el costado del arma), con el giro en grados en el nombre.
        private void FotoTambor(ArmaAPie tipo, Transform arma, string grados)
        {
            if (!CapturaS1.Activa)
            {
                return;
            }
            Directory.CreateDirectory("docs/capturas/" + CarpetaCapturas);
            Vector3 centro = arma.position + arma.forward * 0.12f;
            CapturaS1.FotoMira(CarpetaCapturas + tipo.ToString().ToLowerInvariant() + "-tambor-" + grados, jugador.transform, centro, jugador.transform.InverseTransformDirection(arma.forward * 0.8f + arma.right * 0.6f + arma.up * 0.3f), 0.8f, 30f, 700);
        }
    }
}
