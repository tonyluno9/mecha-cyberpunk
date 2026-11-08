using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-729: la policía a pie ataca primero a quien comete el delito (un SKULL que dispara al jugador), no al jugador; lo que cae por sus balas no suma búsqueda al jugador.
    /// </summary>
    public class PoliciaCriminalT729Pruebas
    {
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const string RutaSkull = "Assets/_Proyecto/Trafico/Resources/SkullRefuerzo.prefab";
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";

        private sealed class CalorDoble : ICalor
        {
            public float Valor { get; private set; }
            public float Maximo => 100f;
            public int Nivel => Valor >= 25f ? 1 : 0;
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
            public void Sumar(float cantidad, MotivoCalor motivo) { Valor = Mathf.Clamp(Valor + cantidad, 0f, Maximo); ValorCambio?.Invoke(Valor); }
            public void Enfriar(float cantidad) { Valor = Mathf.Max(0f, Valor - cantidad); }
            public void Borrar() { Valor = 0f; }
        }

        private readonly List<GameObject> extras = new List<GameObject>();
        private Func<float> azarAnterior;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            azarAnterior = PistolaPandillero.Azar;
        }

        [TearDown]
        public void Limpiar()
        {
            PistolaPandillero.Azar = azarAnterior;
            foreach (GameObject e in extras)
            {
                if (e != null) Object.DestroyImmediate(e);
            }
            extras.Clear();
            Registros.LimpiarTodos();
        }

        private GameObject Poner(string ruta, Vector3 posicion)
        {
            GameObject g = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), posicion, Quaternion.identity);
            extras.Add(g);
            return g;
        }

        private void Piso()
        {
            GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            extras.Add(piso);
        }

        [UnityTest]
        public IEnumerator Policia_ConUnSkullQueDisparaAlJugador_LeDisparaAlSkullNoAlJugador_YNoSumaBusqueda()
        {
            Piso();
            GameObject jugador = Poner(RutaJugador, new Vector3(0f, 0.05f, 0f));
            CalorDoble calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            GameObject skull = Poner(RutaSkull, new Vector3(0f, 0.05f, 14f));
            GameObject policiaObjeto = Poner(RutaPolicia, new Vector3(10f, 0.05f, -2f));
            PandilleroAPie policia = policiaObjeto.GetComponent<PandilleroAPie>();
            PandilleroAPie criminal = skull.GetComponent<PandilleroAPie>();
            Assert.IsNotNull(policia);
            Assert.IsTrue(policia.Datos.EsPolicia);
            PistolaPandillero.Azar = () => 0f; // aciertan siempre
            yield return null;

            Transform raizJugador = Registro<IJugador>.Actual.Raiz;
            bool apuntoAlSkull = false;
            bool apuntoAlJugadorConSkullVivo = false;
            float inicio = Time.time;
            float fin = inicio + 25f;
            while (Time.time < fin && !criminal.EstaKO)
            {
                // Un instante de arranque: el SKULL todavía no apunta a nadie en sus primeros cuadros.
                if (Time.time - inicio > 0.5f)
                {
                    if (policia.ObjetivoActual == criminal.transform) apuntoAlSkull = true;
                    if (policia.ObjetivoActual == raizJugador) apuntoAlJugadorConSkullVivo = true;
                }
                yield return null;
            }
            Assert.IsTrue(apuntoAlSkull, "La policía apuntó al SKULL que dispara (el que comete el delito)");
            Assert.IsFalse(apuntoAlJugadorConSkullVivo, "Mientras el SKULL siguió en pie, la policía no le apuntó al jugador");
            Assert.IsTrue(criminal.EstaKO, "La policía derribó al SKULL");
            Assert.IsTrue(criminal.CayoPorLaPolicia, "Cayó por la policía");
            Assert.AreEqual(0f, calor.Valor, 0.001f, "Lo que hace la policía no le suma búsqueda al jugador");
            Assert.IsNotNull(jugador);
        }

        [UnityTest]
        public IEnumerator Policia_SinNingunCriminal_SigueATacandoAlJugador()
        {
            Piso();
            Poner(RutaJugador, new Vector3(0f, 0.05f, 0f));
            Registro<ICalor>.Agregar(new CalorDoble());
            PandilleroAPie policia = Poner(RutaPolicia, new Vector3(10f, 0.05f, -2f)).GetComponent<PandilleroAPie>();
            PistolaPandillero.Azar = () => 1f; // no acierta: solo se mira a quién apunta
            yield return null;
            Transform raizJugador = Registro<IJugador>.Actual.Raiz;
            float fin = Time.time + 3f;
            bool alJugador = false;
            while (Time.time < fin)
            {
                alJugador |= policia.ObjetivoActual == raizJugador;
                yield return null;
            }
            Assert.IsTrue(alJugador, "Sin criminales a la vista, la policía va por el jugador como antes");
        }
    }
}
