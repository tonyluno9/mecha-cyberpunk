using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-933: con la búsqueda en nivel 0 un policía a pie no ataca al jugador; con la policía ya encima sí.</summary>
    public class PoliciaNivelCeroT933Pruebas
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private GameObject jugador;
        private GameObject piso;
        private System.Func<float> azarAnterior;
        private CalorDoble calor;

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 10f ? 1 : 0;
            public event System.Action<float> ValorCambio;
            public event System.Action<int> NivelCambio;
            private void Poner(float valor)
            {
                int antes = Nivel;
                Valor = Mathf.Clamp(valor, 0f, Maximo);
                ValorCambio?.Invoke(Valor);
                if (Nivel != antes) NivelCambio?.Invoke(Nivel);
            }
            public void Sumar(float cantidad, MotivoCalor motivo) => Poner(Valor + cantidad);
            public void Enfriar(float cantidad) => Poner(Valor - cantidad);
            public void Borrar() => Poner(0f);
        }

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f; // siempre acierta si dispara
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(300f, 1f, 300f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor); // después del jugador: el último registrado es el que usa todo
        }

        [TearDown]
        public void Limpiar()
        {
            PistolaPandillero.Azar = azarAnterior;
            foreach (PandilleroAPie p in Object.FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(p.gameObject);
            }
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
        }

        private PistolaPandillero PonerPolicia()
        {
            GameObject policia = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPolicia), new Vector3(0f, 0.05f, 12f), Quaternion.Euler(0f, 180f, 0f));
            Assert.IsTrue(policia.GetComponent<PandilleroAPie>().Datos.EsPolicia, "Control: es un policía");
            return policia.GetComponent<PistolaPandillero>();
        }

        [UnityTest]
        public IEnumerator ConNivelCero_UnPoliciaAPie_NoAtacaAlJugador()
        {
            PistolaPandillero pistola = PonerPolicia();
            float fin = Time.time + 5f;
            while (Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(0, pistola.Disparos, "Sin búsqueda el policía no dispara al jugador");
        }

        [UnityTest]
        public IEnumerator ConLaPoliciaYaEncima_ElPoliciaAPieSiAtaca()
        {
            PistolaPandillero pistola = PonerPolicia();
            calor.Sumar(15f, MotivoCalor.Mision);
            float fin = Time.time + 15f;
            while (Time.time < fin && pistola.Disparos == 0)
            {
                yield return null;
            }
            Assert.Greater(pistola.Disparos, 0, "Con búsqueda sí dispara");
        }
    }
}
