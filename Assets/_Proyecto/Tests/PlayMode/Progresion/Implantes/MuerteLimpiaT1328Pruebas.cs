using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1328, en la ciudad de verdad: el jugador con 5 estrellas y un policía y un pandillero cerca muere y despierta con el Doc. Al despertar:
    /// 0 estrellas, el policía ya no existe y nadie de los que estaban en pelea lo sigue persiguiendo.
    /// </summary>
    public class MuerteLimpiaT1328Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroAPie.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const float DanioPorGolpe = 60f;
        private const int MaxGolpes = 20;
        private const float MaxSegundosSecuencia = 40f;

        [TearDown]
        public void Limpiar() => EventosPelea.LimpiarSuscriptores();

        [UnityTest]
        public IEnumerator MorirConEstrellas_Despertar_CeroEstrellasYNadiePersigue()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float espera = Time.time + 3f;
            while (Time.time < espera)
            {
                yield return null;
            }
            JugadorAPie jugador = Object.FindFirstObjectByType<JugadorAPie>();
            DoctorClinica doctor = Object.FindFirstObjectByType<DoctorClinica>();
            Assert.IsNotNull(jugador);
            Assert.IsNotNull(doctor);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(calor);

            calor.Sumar(calor.Maximo, MotivoCalor.Pelea);
            Assert.Greater(EstrellasBusqueda.De(calor), 0, "antes de morir hay estrellas");

            Vector3 cerca = jugador.transform.position;
            GameObject policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), cerca + jugador.transform.forward * 6f, Quaternion.identity);
            GameObject pandillero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), cerca - jugador.transform.forward * 6f, Quaternion.identity);
            yield return null;

            for (int i = 0; i < MaxGolpes && !vida.EstaKO; i++)
            {
                jugador.GetComponent<IRecibeDanio>().RecibirDanio(new InfoDanio(DanioPorGolpe, TipoDanio.Disparo, jugador.transform.position + Vector3.up, Vector3.forward, null));
                yield return null;
            }
            Assert.IsTrue(vida.EstaKO, "murió");
            float fin = Time.time + MaxSegundosSecuencia;
            while ((doctor.Ocupado || doctor.Caidas == 0) && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsFalse(doctor.Ocupado, "la secuencia terminó");
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(0, EstrellasBusqueda.De(Registro<ICalor>.Actual), "al despertar no hay estrellas");
            Assert.IsTrue(policia == null, "el policía se fue");
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                Assert.IsFalse(p.EnPelea, p.name + " no debe seguir persiguiendo");
                Assert.IsFalse(p.Datos != null && p.Datos.EsPolicia, "no queda policía a pie");
            }
            Assert.IsFalse(pandillero != null && pandillero.GetComponent<PandilleroAPie>().EnPelea, "el pandillero soltó al jugador");
        }
    }
}
