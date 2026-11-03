using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-533 y T-534: la gente de la banqueta te voltea a ver si te quedas cerca y quieto un segundo; los guardias de Nova te vigilan desde
    /// más lejos y, si sacas un arma o hay disparos, se detienen, miran hacia allá y suben el calor.
    /// </summary>
    public class GenteMiraPruebas
    {
        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
#pragma warning disable 67
            public event Action<ModoJugador> ModoCambio;
#pragma warning restore 67
            public void Colocar(Pose pose) => Raiz.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private sealed class CalorDoble : ICalor
        {
            public float Sumado;
            public float Valor => Sumado;
            public float Maximo => 100f;
            public int Nivel => 0;
#pragma warning disable 67
            public event Action<float> ValorCambio;
            public event Action<int> NivelCambio;
#pragma warning restore 67
            public void Sumar(float cantidad, MotivoCalor motivo) => Sumado += cantidad;
            public void Enfriar(float cantidad) => Sumado -= cantidad;
            public void Borrar() => Sumado = 0f;
        }

        private GameObject jugador;
        private GameObject persona;
        private JugadorDoble doble;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
            jugador = new GameObject("JugadorDePrueba");
            jugador.transform.position = new Vector3(0f, 0f, 20f);
            doble = new JugadorDoble { Raiz = jugador.transform };
            Registro<IJugador>.Agregar(doble);
            persona = new GameObject("Persona");
            persona.transform.rotation = Quaternion.LookRotation(Vector3.right);
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(persona);
            Object.DestroyImmediate(jugador);
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
        }

        [UnityTest]
        public IEnumerator Gente_JugadorCercaYQuieto_LoVoltearAVerTrasUnSegundo()
        {
            GenteDeBanqueta gente = persona.AddComponent<GenteDeBanqueta>();
            yield return null;
            jugador.transform.position = new Vector3(0f, 0f, 2.5f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(gente.Mirando, "Todavía no: espera a que te quedes quieto un segundo");
            yield return new WaitForSeconds(1.8f);
            Assert.IsTrue(gente.Mirando);
            Assert.Less(Vector3.Angle(persona.transform.forward, Vector3.forward), 15f, "Se volteó hacia el jugador");
            jugador.transform.position = new Vector3(0f, 0f, 30f);
            yield return new WaitForSeconds(4f);
            Assert.IsFalse(gente.Mirando, "Al irte vuelve a lo suyo");
            Assert.Less(Vector3.Angle(persona.transform.forward, Vector3.right), 10f, "y regresa a como estaba");
        }

        [UnityTest]
        public IEnumerator Guardia_TeVigilaDeLejos_YAlSacarArmaSubeElCalor()
        {
            CalorDoble calor = new CalorDoble();
            Registro<ICalor>.Agregar(calor);
            persona.AddComponent<GenteDeBanqueta>();
            GuardiaNova guardia = persona.AddComponent<GuardiaNova>();
            yield return null;
            jugador.transform.position = new Vector3(0f, 0f, 7f);
            yield return new WaitForSeconds(1.2f);
            Assert.Less(Vector3.Angle(persona.transform.forward, Vector3.forward), 15f, "Te vigila a 7 m sin esperar");
            EventosRuido.PublicarArmaSacada(jugador.transform.position);
            Assert.IsTrue(guardia.Alerta);
            Assert.Greater(calor.Sumado, 0f, "Llama a la policía");
            float primero = calor.Sumado;
            EventosRuido.PublicarDisparo(jugador.transform.position);
            Assert.AreEqual(primero, calor.Sumado, 0.001f, "Una sola llamada por alerta");
            EventosRuido.PublicarArmaSacada(new Vector3(0f, 0f, 200f));
            Assert.AreEqual(primero, calor.Sumado, 0.001f);
        }
    }
}
