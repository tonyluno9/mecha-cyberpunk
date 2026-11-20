using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-1511: un SKULL con una cobertura CB cerca termina detrás de ella respecto al jugador (el lado contrario), se agacha ahí,
    /// y prefiere el CB bajo a un muro alto más cercano.
    /// </summary>
    public class CoberturaCbT1511Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaCb = "Assets/Arte/Zonas/CB/CB01_BarreraConcreto_a.prefab";
        private const float ZCobertura = 8f;
        private const float ZSkull = 15f;

        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private GameObject cobertura;
        private GameObject muro;
        private Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f;
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.DestroyImmediate(grupo);
            Object.DestroyImmediate(cobertura);
            Object.DestroyImmediate(muro);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private PandilleroAPie Armar(bool conMuroAlto)
        {
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCb);
            Assert.IsNotNull(molde, "Falta el prefab " + RutaCb);
            cobertura = (GameObject)PrefabUtility.InstantiatePrefab(molde);
            cobertura.transform.SetPositionAndRotation(new Vector3(0f, 0f, ZCobertura), Quaternion.identity);
            if (conMuroAlto)
            {
                // Un muro de 2 m, más cerca del SKULL y fuera de la línea jugador-cobertura.
                muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
                muro.name = "MuroAlto";
                muro.transform.position = new Vector3(2.5f, 1f, 12f);
                muro.transform.localScale = new Vector3(3f, 2f, 0.6f);
            }
            grupo = new GameObject("GrupoSkullsCb");
            grupo.SetActive(false);
            grupo.transform.position = new Vector3(0f, 0f, ZSkull);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull), grupo.transform);
            p.transform.position = new Vector3(0f, 0.05f, ZSkull);
            SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
            soP.FindProperty("datos").objectReferenceValue = datos;
            soP.ApplyModifiedPropertiesWithoutUndo();
            PandillaAPie pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            grupo.SetActive(true);
            return p.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Hasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.time + segundos;
            while (!condicion())
            {
                Assert.Less(Time.time, fin, "Tiempo agotado: " + que);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Skull_ConUnCbCerca_TerminaDetrasDeEl_ConRespectoAlJugador()
        {
            PandilleroAPie skull = Armar(false);
            Assert.IsNotNull(cobertura.GetComponent<CoberturaCB>(), "El prefab CB está marcado como cobertura");
            yield return Hasta(() => skull.Alerta && skull.TieneCubierta, "ve al jugador y elige la cobertura CB", 15f);
            Vector3 punto = skull.PuntoCubierta;
            Assert.Greater(punto.z, ZCobertura, "Detrás del CB: el lado contrario al jugador");
            Assert.Less(Vector3.Distance(new Vector3(punto.x, 0f, punto.z), new Vector3(0f, 0f, ZCobertura)), 4f, "Y pegado al CB");

            yield return Hasta(() => skull.EstaAgachado, "llega detrás del CB y se agacha", 40f);
            Vector3 pos = skull.transform.position;
            Assert.Greater(pos.z, ZCobertura, "Agachado, el SKULL está del lado opuesto al jugador");
            Assert.Less(pos.z, ZCobertura + 4f, "y junto al CB");
        }

        [UnityTest]
        public IEnumerator Skull_PrefiereElCbBajoAUnMuroAltoMasCercano()
        {
            PandilleroAPie skull = Armar(true);
            yield return Hasta(() => skull.Alerta && skull.TieneCubierta, "elige cobertura", 15f);
            Assert.Less(skull.PuntoCubierta.z, 11f, "Eligió el CB (z 8) y no el muro alto (z 12)");
            Assert.Greater(skull.PuntoCubierta.z, ZCobertura, "Del lado opuesto al jugador");
        }
    }
}
