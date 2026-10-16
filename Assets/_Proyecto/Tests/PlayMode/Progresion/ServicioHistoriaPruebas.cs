using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>ServicioHistoria (T-124): se registra, se guarda por nombre y no se rompe con datos raros.</summary>
    public class ServicioHistoriaPruebas
    {
        private string carpeta;
        private GameObject raiz;
        private ServicioHistoria historia;
        private ServicioGuardado guardado;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "historia_pruebas_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            raiz = new GameObject("ServiciosDePrueba");
            guardado = raiz.AddComponent<ServicioGuardado>();
            guardado.FijarRuta(Path.Combine(carpeta, "partida.json"));
            historia = raiz.AddComponent<ServicioHistoria>();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.Destroy(raiz);
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator Activo_SeRegistraComoHistoriaYGuardable()
        {
            yield return null;
            Assert.AreSame(historia, Registro<IHistoria>.Actual);
            IGuardable encontrado = null;
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == "historia") encontrado = g;
            }
            Assert.AreSame(historia, encontrado);
            Assert.AreEqual(EtapaPrologo.Ninguna, historia.Etapa);
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_ConEtapaCambiada_DevuelveLaGuardada()
        {
            yield return null;
            historia.Avanzar(EtapaPrologo.Emboscada);
            Assert.IsTrue(guardado.Guardar());
            historia.Reiniciar();
            Assert.AreEqual(EtapaPrologo.Ninguna, historia.Etapa);
            int avisos = 0;
            EtapaPrologo ultima = EtapaPrologo.Ninguna;
            historia.EtapaCambio += e =>
            {
                avisos++;
                ultima = e;
            };
            Assert.IsTrue(guardado.Cargar());
            Assert.AreEqual(EtapaPrologo.Emboscada, historia.Etapa);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(EtapaPrologo.Emboscada, ultima);
            Assert.IsTrue(guardado.Cargar());
            Assert.AreEqual(1, avisos, "cargar la misma etapa no vuelve a avisar");
        }

        [UnityTest]
        public IEnumerator Guardar_UsaElNombreDeLaEtapa()
        {
            yield return null;
            historia.Avanzar(EtapaPrologo.Dormir);
            StringAssert.Contains("Dormir", historia.Guardar());
        }

        [UnityTest]
        public IEnumerator Cargar_ConEtapaDesconocida_NoCambiaNadaYAvisaConWarning()
        {
            yield return null;
            historia.Avanzar(EtapaPrologo.Dormir);
            int avisos = 0;
            historia.EtapaCambio += _ => avisos++;
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] La historia"));
            historia.Cargar("{\"etapa\":\"EtapaQueNoExiste\"}");
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] La historia"));
            historia.Cargar("{\"etapa\":\"99\"}");
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Progresion\] La historia"));
            historia.Cargar("no es json");
            Assert.AreEqual(EtapaPrologo.Dormir, historia.Etapa);
            Assert.AreEqual(0, avisos);
        }
    }
}
