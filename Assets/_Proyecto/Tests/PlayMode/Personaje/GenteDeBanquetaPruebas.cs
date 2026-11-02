using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-510: la gente de banqueta ya no está congelada (camina, respira, platica con fases distintas), los peatones salen robot 1 de cada 4 y los personajes nuevos no se quedan en T.</summary>
    public class GenteDeBanquetaPruebas
    {
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_";
        private readonly System.Collections.Generic.List<GameObject> creados = new System.Collections.Generic.List<GameObject>();

        [SetUp]
        public void Preparar()
        {
            Camera camara = new GameObject("CamaraPrueba").AddComponent<Camera>();
            camara.transform.position = new Vector3(3f, 1.4f, -6f);
            camara.transform.LookAt(new Vector3(3f, 0.9f, 0f));
            camara.fieldOfView = 70f;
            creados.Add(camara.gameObject);
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(3f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(40f, 1f, 40f);
            creados.Add(piso);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject g in creados)
            {
                if (g != null) { Object.Destroy(g); }
            }
            creados.Clear();
        }

        private GameObject Visual(string nombre, Transform padre, Vector3 pos)
        {
            GameObject g = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + nombre + ".prefab"), pos, Quaternion.identity, padre);
            // Sin ventana de juego (batchmode) nada "se ve" y el Animator por defecto no actualiza los huesos: se le pide que anime siempre.
            g.GetComponentInChildren<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (padre == null) { creados.Add(g); }
            return g;
        }

        private static string Estado(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(0);
            foreach (string n in ReglasVisual.Estados)
            {
                if (a.IsInTransition(0) ? siguiente.IsName(n) : actual.IsName(n))
                {
                    return n;
                }
            }
            return "?";
        }

        private static AnimationClip ClipDe(string modelo, string nombre)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath("Assets/Arte/Personajes/" + modelo + "/" + modelo + ".fbx"))
            {
                if (o is AnimationClip c && c.name == nombre) { return c; }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Caminante_AvanzaEntreSusDosPuntos_ConLaAnimacionDeCaminar_YNoSeSale()
        {
            GameObject envoltorio = new GameObject("Caminante");
            creados.Add(envoltorio);
            GameObject visual = Visual("woman2", envoltorio.transform, Vector3.zero);
            Vector3 a = new Vector3(0f, 0f, 0f);
            Vector3 b = new Vector3(6f, 0f, 0f);
            GenteDeBanqueta gente = visual.AddComponent<GenteDeBanqueta>();
            gente.Configurar(ModoGente.Caminar, 0f, 1f, null, a, b, 1.2f, 0.4f, envoltorio.transform);
            VisualPersonaje vp = visual.GetComponent<VisualPersonaje>();
            Animator animador = vp.Animador;

            Vector3 inicio = envoltorio.transform.position;
            Assert.AreEqual(a.x, inicio.x, 0.01f, "Arranca en el punto de su fase");
            yield return new WaitForSeconds(1.5f);
            float xAvance = envoltorio.transform.position.x;
            Assert.Greater(xAvance, 0.8f, "Avanza por la banqueta (m)");
            Assert.Greater(vp.Velocidad, 0.4f, "El visual lee la velocidad del movimiento");
            Assert.AreEqual("Caminar", Estado(animador), "Usa la animación de caminar");
            Assert.Less(Mathf.Abs(envoltorio.transform.forward.x - 1f), 0.2f, "Mira hacia donde camina");

            float minimo = float.MaxValue;
            float maximo = float.MinValue;
            bool llegoAlFinal = false;
            for (float t = 0f; t < 10f; t += 0.1f)
            {
                yield return new WaitForSeconds(0.1f);
                float x = envoltorio.transform.position.x;
                minimo = Mathf.Min(minimo, x);
                maximo = Mathf.Max(maximo, x);
                llegoAlFinal |= x > b.x - 0.3f;
            }
            Assert.IsTrue(llegoAlFinal, "Llega al otro extremo");
            Assert.LessOrEqual(maximo, b.x + 0.05f, "No pasa del final");
            Assert.GreaterOrEqual(minimo, a.x - 0.05f, "No pasa del principio al volver");
            Assert.Less(envoltorio.transform.position.z, 0.05f, "No se sale de la banqueta (z)");
            Assert.Greater(envoltorio.transform.position.z, -0.05f);
        }

        [UnityTest]
        public IEnumerator DosCaminantes_ConFaseYRapidezDistintas_NoVanAlMismoPaso()
        {
            var pos = new float[2];
            var raices = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject env = new GameObject("Caminante" + i);
                creados.Add(env);
                env.transform.position = new Vector3(0f, 0f, i * 2f);
                GameObject visual = Visual("woman2", env.transform, Vector3.zero);
                GenteDeBanqueta g = visual.AddComponent<GenteDeBanqueta>();
                g.Configurar(ModoGente.Caminar, i == 0 ? 0.1f : 0.6f, 1f, null, new Vector3(0f, 0f, i * 2f), new Vector3(8f, 0f, i * 2f), i == 0 ? 0.8f : 1.2f, 1f, env.transform);
                raices[i] = env.transform;
            }
            yield return new WaitForSeconds(1.2f);
            pos[0] = raices[0].position.x;
            pos[1] = raices[1].position.x;
            Assert.Greater(Mathf.Abs(pos[0] - pos[1]), 1f, "Cada quien en su punto del recorrido (m)");
        }

        [UnityTest]
        public IEnumerator Quieto_Respira_ElPechoSeMueve()
        {
            GameObject visual = Visual("woman2", null, new Vector3(3f, 0f, 0f));
            visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            GenteDeBanqueta g = visual.AddComponent<GenteDeBanqueta>();
            g.Configurar(ModoGente.Quieto, 0.25f, 1.5f, null, Vector3.zero, Vector3.zero, 1f, 0f, null);
            Animator animador = visual.GetComponentInChildren<Animator>();
            Transform pecho = animador.GetBoneTransform(HumanBodyBones.Chest) != null ? animador.GetBoneTransform(HumanBodyBones.Chest) : animador.GetBoneTransform(HumanBodyBones.Spine);
            yield return new WaitForSeconds(0.5f);
            Quaternion uno = pecho.localRotation;
            float maxima = 0f;
            for (int i = 0; i < 25; i++)
            {
                yield return new WaitForSeconds(0.1f);
                maxima = Mathf.Max(maxima, Quaternion.Angle(uno, pecho.localRotation));
            }
            Assert.Greater(maxima, 0.3f, "El pecho sube y baja al respirar (grados)");
            Assert.Less(maxima, 8f, "Sutil, no un baile (grados)");
            Assert.AreEqual("Idle", Estado(animador), "Sigue en su reposo");
        }

        [UnityTest]
        public IEnumerator Platicar_ReproduceElClipEnBucle_ConFasesDistintasEntreDosPersonas()
        {
            AnimationClip clip = ClipDe("woman2", "AcuerdoCompleto");
            Assert.IsNotNull(clip, "Clip de asentir (agree completo) en woman2");
            Assert.IsTrue(clip.isLooping, "En bucle");
            var cabezas = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject visual = Visual("woman2", null, new Vector3(2f + i * 1.5f, 0f, 0f));
                visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                GenteDeBanqueta g = visual.AddComponent<GenteDeBanqueta>();
                g.Configurar(ModoGente.Platicar, i == 0 ? 0.05f : 0.55f, i == 0 ? 0.8f : 1.2f, clip, Vector3.zero, Vector3.zero, 1f, 0f, null);
                Assert.IsTrue(g.Platicando, "Arranca su clip");
                Assert.IsFalse(visual.GetComponent<VisualPersonaje>().enabled, "El puente deja el Animator al clip");
                cabezas[i] = visual.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
            }
            yield return new WaitForSeconds(0.6f);
            Quaternion a0 = cabezas[0].localRotation;
            Quaternion b0 = cabezas[1].localRotation;
            float cambio = 0f;
            float distintos = 0f;
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(0.1f);
                cambio = Mathf.Max(cambio, Quaternion.Angle(a0, cabezas[0].localRotation));
                distintos = Mathf.Max(distintos, Quaternion.Angle(cabezas[0].localRotation, cabezas[1].localRotation));
            }
            Assert.Greater(cambio, 1f, "La cabeza se mueve (grados)");
            Assert.Greater(distintos, 0.5f, "Las dos personas no van al mismo compás (grados)");
        }

        [UnityTest]
        public IEnumerator PersonajesNuevos_EnReposo_TienenLosBrazosAbajoYNoEnT([ValueSource(nameof(Nuevos))] string nombre)
        {
            GameObject visual = Visual(nombre, null, new Vector3(3f, 0f, 0f));
            yield return new WaitForSeconds(0.4f);
            Animator a = visual.GetComponentInChildren<Animator>();
            Assert.AreEqual("Idle", Estado(a));
            Transform hombro = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform codo = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            float angulo = Vector3.Angle(codo.position - hombro.position, Vector3.down);
            Assert.Less(angulo, 55f, nombre + ": el brazo cuelga (en T serían 90°) (grados)");
        }

        private static readonly string[] Nuevos = { "SkullHombre", "Doctor", "Inventor", "Hacker", "RobotHombre", "GuardiaNova" };

        [UnityTest]
        public IEnumerator PeatonVariado_UnoDeCadaCuatroSaleRobot_YSoloUnaVersionQuedaActiva()
        {
            GameObject plantilla = new GameObject("PlantillaPeaton");
            creados.Add(plantilla);
            plantilla.SetActive(false);
            GameObject persona = new GameObject("Visual");
            GameObject robot = new GameObject("VisualRobot");
            persona.transform.SetParent(plantilla.transform);
            robot.transform.SetParent(plantilla.transform);
            PeatonVariado v = plantilla.AddComponent<PeatonVariado>();
            v.Configurar(persona, robot, 0.25f);
            Random.InitState(20261101);
            int robots = 0;
            const int total = 400;
            for (int i = 0; i < total; i++)
            {
                GameObject copia = Object.Instantiate(plantilla);
                copia.SetActive(true);
                PeatonVariado c = copia.GetComponent<PeatonVariado>();
                Assert.AreNotEqual(c.Persona.activeSelf, c.Robot.activeSelf, "Una sola versión activa");
                robots += c.EsRobot ? 1 : 0;
                Object.Destroy(copia);
            }
            yield return null;
            Assert.AreEqual(0.25f, robots / (float)total, 0.06f, "1 de cada 4 es robot");
            Assert.Greater(robots, 0);
        }

        [UnityTest]
        public IEnumerator PrefabPeaton_ConElRobot_CaminaConLaMismaAnimacion()
        {
            GameObject robot = Visual("RobotHombre", null, new Vector3(3f, 0f, 0f));
            GameObject envoltorio = new GameObject("PeatonRobot");
            creados.Add(envoltorio);
            robot.transform.SetParent(envoltorio.transform, true);
            envoltorio.transform.position = new Vector3(0f, 0f, 0f);
            VisualPersonaje vp = robot.GetComponent<VisualPersonaje>();
            Animator a = vp.Animador;
            yield return null;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                envoltorio.transform.position += Vector3.right * 1.1f * Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual("Caminar", Estado(a), "El robot camina con las animaciones de siempre");
        }
    }
}
