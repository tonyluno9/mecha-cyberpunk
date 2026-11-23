using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-1528: un maleante de calle (SKULL con el dato "provoca antes de atacar") se acerca, empuja con 1 punto de daño o menos y no dispara; cuando el jugador
    /// le hace daño se vuelve hostil y ataca con el daño limitado del inicio.
    /// </summary>
    public class ProvocacionCalleT1528Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private DatosPandilleroAPie datos;
        private PandillaAPie pandilla;
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f; // toda bala acierta: si dispararan, se notaría
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            CrearGrupo(6f); // T-1659: a menos de 8 m cuenta como estar cerca
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.DestroyImmediate(grupo);
            Object.DestroyImmediate(datos);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void CrearGrupo(float distancia)
        {
            // Una copia de los datos con el dato activo (no depende de haber corrido AplicarMaleantesCalleT1528 ni toca el asset).
            datos = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos));
            SerializedObject soD = new SerializedObject(datos);
            soD.FindProperty("provocaAntesDeAtacar").boolValue = true;
            soD.ApplyModifiedPropertiesWithoutUndo();
            grupo = new GameObject("GrupoProvoca");
            grupo.SetActive(false);
            grupo.transform.position = new Vector3(0f, 0f, distancia);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            for (int i = 0; i < 2; i++)
            {
                GameObject p = Object.Instantiate(prefab, grupo.transform);
                p.transform.position = new Vector3(i * 2f - 1f, 0.05f, distancia);
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }
            pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
        }

        private int Disparos()
        {
            int total = 0;
            foreach (PistolaPandillero pistola in grupo.GetComponentsInChildren<PistolaPandillero>(true))
            {
                total += pistola.Disparos;
            }
            return total;
        }

        [UnityTest]
        public IEnumerator SinResponder_SeAcercanEmpujanYNoDisparan()
        {
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float anterior = vida.Actual;
            float mayorCaida = 0f;
            float menorDistancia = float.MaxValue;
            float fin = Time.time + 40f; // T-1659: te miran, a los 20 s cerca uno se acerca caminando lento (~1.2 m/s) y empuja
            while (Time.time < fin)
            {
                yield return null;
                mayorCaida = Mathf.Max(mayorCaida, anterior - vida.Actual);
                anterior = vida.Actual;
                foreach (PandilleroAPie p in pandilla.Pandilleros)
                {
                    if (p != null && p.isActiveAndEnabled)
                    {
                        Vector3 d = p.transform.position - jugador.transform.position;
                        d.y = 0f;
                        menorDistancia = Mathf.Min(menorDistancia, d.magnitude);
                    }
                }
            }
            Assert.AreEqual(0, Disparos(), "provocando no disparan");
            Assert.LessOrEqual(mayorCaida, ReglaProvocacionCalle.DanioEmpujon + 0.01f, "cada empujón hace 1 punto de daño o menos");
            Assert.LessOrEqual(menorDistancia, ReglaProvocacionCalle.DanioEmpujon + ReglaProvocacionCalle.DistanciaEmpujon, "se acercan al jugador");
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                Assert.IsTrue(p.EsMaleanteDeCalle, p.name + " es maleante de calle");
                Assert.IsFalse(p.EsHostil, p.name + " no es hostil si el jugador no respondió");
            }
        }

        [UnityTest]
        public IEnumerator AlRecibirDanioDelJugador_SeVuelveHostilYDisparaConDanioLimitado()
        {
            yield return null;
            yield return null;
            PandilleroAPie golpeado = pandilla.Pandilleros[0];
            Assert.IsFalse(golpeado.EsHostil);
            golpeado.RecibirDanio(new InfoDanio(1f, TipoDanio.Disparo, golpeado.transform.position + Vector3.up, Vector3.forward, null));
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                Assert.IsTrue(p.EsHostil, p.name + " pasa a hostil: todo el grupo responde");
            }
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float anterior = vida.Actual;
            float mayorCaida = 0f;
            float fin = Time.time + 10f;
            while (Time.time < fin && Disparos() == 0)
            {
                yield return null;
                mayorCaida = Mathf.Max(mayorCaida, anterior - vida.Actual);
                anterior = vida.Actual;
            }
            Assert.Greater(Disparos(), 0, "hostiles: disparan");
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                mayorCaida = Mathf.Max(mayorCaida, anterior - vida.Actual);
                anterior = vida.Actual;
            }
            Assert.Less(mayorCaida, datos.DanioDisparo, "el daño de los primeros tiros es menor que el completo");
        }
    }
}
