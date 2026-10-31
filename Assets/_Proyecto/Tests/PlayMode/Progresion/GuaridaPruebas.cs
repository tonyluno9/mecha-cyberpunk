using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-448: la guarida sube de nivel en Ciudad_Prologo, como la usaría el jugador (F6 salta la historia; Usar() es lo que hace E en cada punto):
    /// los tres puntos están dentro de la casa; el armero da más daño a las armas de fuego; el garaje pone el carro (y con más nivel lo hace
    /// más resistente); la cama cura y baja el calor al dormir; sin dinero no se compra, al máximo no hay más y todo se guarda y se carga.
    /// </summary>
    public class GuaridaPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float Tolerancia = 0.0001f;
        private Keyboard teclado;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator DescargarEscena()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            yield return null;
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static GuaridaPunto Punto(string id)
        {
            foreach (GuaridaPunto p in Object.FindObjectsByType<GuaridaPunto>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.IdMejora == id)
                {
                    return p;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ArmeroGarajeYCama_SeCompranCuranYSeGuardan()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas historia = null;
            yield return EsperarHasta(() =>
            {
                historia = Object.FindFirstObjectByType<DirectorPandillas>();
                return historia != null && historia.Iniciado && Registro<IJugador>.Actual != null && Registro<ICartera>.Actual != null
                    && Registro<ServicioGuarida>.Actual != null && Registro<IRelojMundo>.Actual != null;
            }, "el director arrancó con la guarida", 30f);
            for (int i = 0; i < 12 && historia.Paso != PasoHistoria.Libre; i++)
            {
                yield return Pulsar(teclado.f6Key);
            }
            Assert.AreEqual(PasoHistoria.Libre, historia.Paso);
            ServicioGuarida guarida = Registro<ServicioGuarida>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.AreSame(guarida, Registro<IGuarida>.Actual, "El servicio es el IGuarida");
            GuaridaPunto armero = Punto(DatosGuarida.IdArmero);
            GuaridaPunto garaje = Punto(DatosGuarida.IdGaraje);
            GuaridaPunto cama = Punto(DatosGuarida.IdCama);
            Assert.IsNotNull(armero);
            Assert.IsNotNull(garaje);
            Assert.IsNotNull(cama);
            CamaJugador camaJugador = Object.FindFirstObjectByType<CamaJugador>();
            Assert.Less(Vector3.Distance(armero.Posicion, camaJugador.transform.position), 4f, "Los puntos están junto a la cama, dentro de la casa");
            Assert.Less(Vector3.Distance(garaje.Posicion, camaJugador.transform.position), 4f);
            Assert.Less(Vector3.Distance(cama.Posicion, camaJugador.transform.position), 4f);
            Assert.AreEqual(0f, guarida.BonoDanioArmas, Tolerancia);
            Assert.AreEqual(0, guarida.NivelGaraje);
            Assert.IsFalse(guarida.CamaCura);

            // Sin dinero no se compra.
            cartera.CargarDeuda(cartera.Dinero + 1, MotivoDinero.Otro);
            Assert.IsFalse(guarida.Comprar(DatosGuarida.IdArmero));
            Assert.AreEqual(0, guarida.Nivel(DatosGuarida.IdArmero));
            cartera.Abonar(20000, MotivoDinero.Premio);

            // ARMERO: sube el daño de las armas de fuego y el texto cambia de ESTRENAR a MEJORAR; al máximo ya no hay más.
            StringAssert.StartsWith("ESTRENAR", armero.Texto);
            int dinero = cartera.Dinero;
            int precio = guarida.PrecioSiguiente(DatosGuarida.IdArmero);
            Assert.Greater(precio, 0);
            armero.Usar();
            Assert.AreEqual(1, guarida.Nivel(DatosGuarida.IdArmero));
            Assert.AreEqual(dinero - precio, cartera.Dinero, "Pagó el nivel 1");
            Assert.AreEqual(guarida.Datos.BonoDanioArmero[0], guarida.BonoDanioArmas, Tolerancia);
            StringAssert.StartsWith("MEJORAR", armero.Texto);
            armero.Usar();
            armero.Usar();
            Assert.AreEqual(3, guarida.Nivel(DatosGuarida.IdArmero));
            Assert.AreEqual(guarida.Datos.BonoDanioArmero[2], guarida.BonoDanioArmas, Tolerancia, "El nivel 3 da el mayor bono");
            Assert.IsFalse(armero.PuedeUsar, "Al máximo ya no hay más");
            StringAssert.Contains("MÁX", armero.Texto);
            Assert.IsFalse(guarida.Comprar(DatosGuarida.IdArmero));

            // GARAJE: el carro de la casa aparece al comprar y con más nivel aguanta más.
            GarajeCarroEnEscena(out GameObject carro);
            Assert.IsFalse(carro.activeSelf, "Sin garaje no hay carro");
            garaje.Usar();
            yield return EsperarHasta(() => carro.activeSelf, "el carro del garaje", 3f);
            DanioCarroEnEscena(carro, out float vidaNivel1);
            garaje.Usar();
            yield return EsperarHasta(() => DanioVida(carro) > vidaNivel1 + 1f, "la vida del nivel 2", 3f);
            Assert.AreEqual(vidaNivel1 * guarida.Datos.FactorVidaGaraje[1], DanioVida(carro), 1f, "Nivel 2: más resistente");

            // CAMA: dormir cura y baja el calor.
            cama.Usar();
            Assert.IsTrue(guarida.CamaCura);
            Assert.AreEqual(guarida.Datos.CalorBorradoCama[0], guarida.CamaBajaCalor, Tolerancia);
            cama.Usar();
            Assert.AreEqual(0.5f, guarida.CamaBajaCalor, Tolerancia, "Nivel 2 de la cama: borra la mitad del calor");
            VidaAPie vida = jugador.Raiz.GetComponentInChildren<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(vida.Maxima * 0.6f, TipoDanio.Disparo, jugador.Raiz.position, Vector3.forward, null));
            Assert.Less(vida.Actual, vida.Maxima - 1f);
            ICalor calor = Registro<ICalor>.Actual;
            calor.Sumar(40f, MotivoCalor.Pelea);
            float calorAntes = calor.Valor;
            Assert.Greater(calorAntes, 0f);
            Assert.IsTrue(camaJugador.PuedeUsar, "Libre: se puede dormir");
            camaJugador.Usar();
            yield return EsperarHasta(() => !camaJugador.Durmiendo, "despertar", 20f);
            Assert.AreEqual(vida.Maxima, vida.Actual, 0.01f, "Dormir cura la vida");
            Assert.Less(calor.Valor, calorAntes, "Dormir baja el calor");
            Debug.Log("[Prueba] T-448 calor " + calorAntes + " -> " + calor.Valor);

            // Se guarda y se carga.
            string texto = guarida.Guardar();
            StringAssert.Contains(DatosGuarida.IdArmero, texto);
            guarida.Cargar("{}");
            Assert.AreEqual(0, guarida.Nivel(DatosGuarida.IdArmero), "Cargar una partida sin guarida la deja en cero");
            guarida.Cargar(texto);
            Assert.AreEqual(3, guarida.Nivel(DatosGuarida.IdArmero));
            Assert.AreEqual(2, guarida.Nivel(DatosGuarida.IdGaraje));
            Assert.AreEqual(2, guarida.Nivel(DatosGuarida.IdCama));
        }

        private static void GarajeCarroEnEscena(out GameObject carro)
        {
            GameObject garaje = null;
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (m.GetType().Name == "GarajeCarro")
                {
                    garaje = m.gameObject;
                    break;
                }
            }
            Assert.IsNotNull(garaje, "La escena trae el garaje de la guarida");
            carro = garaje.transform.Find("CarroDeLaGuarida").gameObject;
        }

        private static void DanioCarroEnEscena(GameObject carro, out float vida)
        {
            vida = DanioVida(carro);
            Assert.Greater(vida, 0f, "El carro del garaje tiene vida");
        }

        // La vida máxima del DanioCarro del carro (por reflexión sobre la propiedad, sin referenciar Vehiculos).
        private static float DanioVida(GameObject carro)
        {
            foreach (MonoBehaviour m in carro.GetComponents<MonoBehaviour>())
            {
                if (m.GetType().Name == "DanioCarro")
                {
                    return (float)m.GetType().GetProperty("VidaMaxima").GetValue(m);
                }
            }
            return 0f;
        }
    }
}
