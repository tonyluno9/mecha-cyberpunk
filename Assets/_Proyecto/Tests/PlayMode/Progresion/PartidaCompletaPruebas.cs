using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-111: F5 guarda y F9 carga en Pruebas_Calle; dinero, mecha, perfil, renta y reloj vuelven a como estaban.</summary>
    public class PartidaCompletaPruebas : InputTestFixture
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const int DineroGuardado = 1234;
        private const float VidaGuardada = 40f;
        private const int AdeudoGuardado = 500;
        private const int ProximoCobroGuardado = 15;
        private const int DiaGuardado = 9;
        private const float HoraGuardada = 22.5f;
        private const float Margen = 0.01f;

        private Keyboard teclado;
        private string carpeta;
        private HashSet<GameObject> raicesIniciales;


        public override void Setup()
        {
            raicesIniciales = RaicesDePrueba.Anotar();
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            carpeta = Path.Combine(Application.temporaryCachePath, "partida_completa_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
        }

        public override void TearDown()
        {
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            RaicesDePrueba.DestruirNuevas(raicesIniciales);
            yield return null;
            Registros.LimpiarTodos();
        }

        private IEnumerator Tecla(Key tecla)
        {
            Press(teclado[tecla]);
            yield return null;
            Release(teclado[tecla]);
            yield return null;
        }

        private static IGuardable Buscar(string clave)
        {
            foreach (IGuardable g in Registro<IGuardable>.Todos)
            {
                if (g.ClaveGuardado == clave) return g;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator GuardarYCargar_ConTodoCambiado_DevuelveDineroMechaPerfilRentaYReloj()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaCalle, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }

            ServicioGuardado guardado = Object.FindFirstObjectByType<ServicioGuardado>();
            Assert.IsNotNull(guardado, "Pruebas_Calle trae un ServicioGuardado");
            guardado.FijarRuta(Path.Combine(carpeta, "partida.json"));
            ICartera cartera = Registro<ICartera>.Actual;
            IRenta renta = Registro<IRenta>.Actual;
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            ServicioPerfil perfil = Object.FindFirstObjectByType<ServicioPerfil>();
            MechaPersistente mecha = Object.FindFirstObjectByType<MechaPersistente>();
            Assert.IsNotNull(cartera);
            Assert.IsNotNull(renta);
            Assert.IsNotNull(reloj);
            Assert.IsNotNull(perfil);
            Assert.IsNotNull(mecha);
            VidaMecha vida = mecha.GetComponent<VidaMecha>();
            reloj.Pausar(true);

            // Estado a guardar.
            cartera.Abonar(DineroGuardado - cartera.Dinero, MotivoDinero.Otro);
            vida.FijarVida(VidaGuardada);
            perfil.Fijar("Ana", "LA CHISPA");
            Buscar("renta").Cargar("{\"diaProximoCobro\":" + ProximoCobroGuardado + ",\"adeudo\":" + AdeudoGuardado + "}");
            Buscar("reloj").Cargar("{\"dia\":" + DiaGuardado + ",\"hora\":" + HoraGuardada.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            Assert.AreEqual(DineroGuardado, cartera.Dinero);

            yield return Tecla(Key.F5);
            Assert.IsTrue(guardado.HayPartida, "F5 escribió el archivo");

            // Cambiar todo.
            cartera.Abonar(777, MotivoDinero.Otro);
            mecha.Reparar();
            perfil.Fijar("Beto", "OTRO");
            yield return Tecla(Key.J);
            yield return Tecla(Key.J);
            Assert.AreNotEqual(DiaGuardado, reloj.Dia, "J avanzó el día");

            int cobros = 0;
            void AlCambiarDinero(int dinero, MotivoDinero motivo)
            {
                if (motivo == MotivoDinero.Renta) cobros++;
            }
            cartera.Cambio += AlCambiarDinero;

            yield return Tecla(Key.F9);
            cartera.Cambio -= AlCambiarDinero;

            Assert.AreEqual(DineroGuardado, cartera.Dinero);
            Assert.AreEqual(VidaGuardada, vida.Actual, Margen);
            Assert.AreEqual("Ana", perfil.Nombre);
            Assert.AreEqual("LA CHISPA", perfil.Alias);
            Assert.AreEqual(AdeudoGuardado, renta.Adeudo);
            Assert.AreEqual(ProximoCobroGuardado, renta.DiaProximoCobro);
            Assert.AreEqual(DiaGuardado, reloj.Dia);
            Assert.AreEqual(HoraGuardada, reloj.Hora, Margen);
            Assert.AreEqual(0, cobros, "Cargar no cobra renta");
        }
    }
}
