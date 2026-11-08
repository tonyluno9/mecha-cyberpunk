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

        // Excepcion documentada (T-688): el Doc (Visual_Doctor) mide 1.10 veces al jugador a proposito (T-812, sesion 2).
        // Alturas relativas al jugador de cada personaje que comparte escala con él (ColocarS2.Personajes): nadie enano ni gigante.
        private static readonly (string prefab, float relativa)[] Personajes =
        {
            ("Visual_man", 1.00f), ("Visual_woman", 1.00f), ("Visual_PJ01", 1.00f), ("Visual_Doctor", 1.10f), ("Visual_Inventor", 0.98f), ("Visual_Hacker", 1.00f),
            ("Visual_RobotHombre", 1.00f), ("Visual_SkullHombre", 1.00f), ("Visual_woman2", 0.97f), ("Visual_GuardiaNova", 1.25f),
        };

        [UnityTest]
        public IEnumerator Personajes_NPCyJugador_QuedanParejosContraLos180()
        {
            yield return Calentar();
            string fallos = "";
            for (int i = 0; i < Personajes.Length; i++)
            {
                GameObject go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/" + Personajes[i].prefab + ".prefab"), new Vector3(3f + i * 2f, 0.05f, 0f), Quaternion.identity);
                go.GetComponentInChildren<Animator>(true).cullingMode = AnimatorCullingMode.AlwaysAnimate;
                yield return Esperar(0.8f);
                float altura = AlturaAnimada(go);
                Debug.Log("T670 altura " + Personajes[i].prefab + " " + altura.ToString("F3"));
                Object.Destroy(go);
                if (Mathf.Abs(altura - AlturaEsperada * Personajes[i].relativa) > Tolerancia) { fallos += Personajes[i].prefab + " " + altura.ToString("F2") + "; "; }
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
