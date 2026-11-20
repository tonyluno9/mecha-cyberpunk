using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-670: el jugador animado mide 1.80 m, como su collider, y conserva esa escala al agacharse y rodar.</summary>
    public class EscalaJugadorT670Pruebas : BaseJugadorAPie
    {
        public const float AlturaEsperada = 1.8f;
        private const float Tolerancia = 0.06f;

        /// <summary>Altura (m) de la malla animada horneada (BakeMesh): de la planta del pie al punto más alto de las mallas con piel.</summary>
        public static float AlturaAnimada(GameObject raiz)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            Mesh malla = new Mesh();
            foreach (SkinnedMeshRenderer r in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!r.enabled || r.sharedMesh == null)
                {
                    continue;
                }
                r.BakeMesh(malla, true);
                foreach (Vector3 v in malla.vertices)
                {
                    float y = r.transform.TransformPoint(v).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            Object.Destroy(malla);
            return max - min;
        }

        [UnityTest]
        public IEnumerator Jugador_AnimadoDePie_Mide180()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(1.5f);
            Assert.AreEqual(AlturaEsperada, AlturaAnimada(visual.gameObject), Tolerancia, "El jugador animado mide 1.80 m");
        }

        // T-1521: la altura de los humanos sale de EscalasHumanos (el Doc ya no es 1.09 veces el jugador). Los que no están en esa tabla (el jugador, AL10, AL11 y los
        // guardias de Nova, que son más altos a propósito) llevan aquí su altura relativa al jugador: nadie enano ni gigante.
        private static readonly (string prefab, float relativa)[] Personajes =
        {
            ("Visual_man", EscalasHumanos.Relativa("Visual_man")), ("Visual_woman", EscalasHumanos.Relativa("Visual_woman")), ("Visual_PJ01", 1.00f),
            // El Hacker no va: su idle es sentado tecleando (su altura de pie la mide EscalaHumanosT1521Pruebas en EditMode).
            ("Visual_Doctor", EscalasHumanos.Relativa("Visual_Doctor")), ("Visual_Inventor", EscalasHumanos.Relativa("Visual_Inventor")),
            ("Visual_RobotHombre", EscalasHumanos.Relativa("Visual_RobotHombre")), ("Visual_SkullHombre", EscalasHumanos.Relativa("Visual_SkullHombre")),
            ("Visual_woman2", EscalasHumanos.Relativa("Visual_woman2")), ("Visual_AL10", 1.00f), ("Visual_AL11", 1.00f), ("Visual_GuardiaNova", 1.34f),
        };

        [UnityTest]
        public IEnumerator Personajes_NPCyJugador_QuedanParejosContraLos180()
        {
            yield return Calentar();
            string fallos = "";
            // Se compara contra el prefab del jugador medido igual (la pose de estas instancias sueltas da unos centímetros más que en la escena).
            float referencia = AlturaEsperada;
            for (int i = 0; i < Personajes.Length; i++)
            {
                if (Personajes[i].prefab == "Visual_PJ01")
                {
                    GameObject pj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_PJ01.prefab"), new Vector3(-3f, 0.05f, 0f), Quaternion.identity);
                    pj.GetComponentInChildren<Animator>(true).cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    yield return Esperar(0.8f);
                    referencia = AlturaAnimada(pj);
                    Object.Destroy(pj);
                }
            }
            Assert.AreEqual(AlturaEsperada, referencia, 0.1f, "El prefab del jugador mide cerca de 1.80 m");
            for (int i = 0; i < Personajes.Length; i++)
            {
                GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + Personajes[i].prefab + ".prefab"), new Vector3(3f + i * 2f, 0.05f, 0f), Quaternion.identity);
                go.GetComponentInChildren<Animator>(true).cullingMode = AnimatorCullingMode.AlwaysAnimate;
                yield return Esperar(0.8f);
                float altura = AlturaAnimada(go);
                Debug.Log("T670 altura " + Personajes[i].prefab + " " + altura.ToString("F3"));
                Object.Destroy(go);
                if (Mathf.Abs(altura - referencia * Personajes[i].relativa) > Tolerancia) { fallos += Personajes[i].prefab + " " + altura.ToString("F2") + "; "; }
            }
            Assert.AreEqual("", fallos, "Personajes fuera de escala contra los 1.80 m del jugador");
        }

        [UnityTest]
        public IEnumerator Jugador_AlRodarYLevantarse_ConservaSuEscala()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            MechaCyberpunk.Personaje.Movimiento.CubiertaYRodada cubierta = jugador.GetComponent<MechaCyberpunk.Personaje.Movimiento.CubiertaYRodada>();
            float escala = visual.Animador.transform.localScale.y;
            Assert.Greater(escala, 0.9f, "El Modelo del jugador conserva su escala (PJ01 la trae del import, T-732)");
            cubierta.AlternarAgachado(); // sin agacharse no se rueda (T-673)
            Assert.IsTrue(cubierta.Rodar());
            yield return Esperar(cubierta.Datos.DuracionRodada + 0.3f);
            Assert.AreEqual(escala, visual.Animador.transform.localScale.y, 0.001f, "Tras rodar vuelve a su escala, no a 1");
        }
    }
}
