using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-700: capturas del HUD nuevo en la calle de Ciudad_Prologo (frente a la casa), a 1920x1080, para compararlas con H10_HUD.png.
    /// Solo corren con CAPTURAR_S1=1 y dejan docs/capturas/s1c-T-700-juego-*.png (base, busqueda, dano, carro).
    /// </summary>
    public class HudH10CapturasT700 : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";

        private Keyboard teclado;
        private Mouse raton;

        private sealed class ZonaBusquedaFalsa : IZonaBusqueda
        {
            public bool Activa => true;
            public Vector3 Centro => Vector3.zero;
            public float Radio => 30f;
            public bool Visto => false;
        }

        private sealed class CalorFijo : ICalor
        {
            public float Valor { get; set; }
            public float Maximo { get; set; }
            public int Nivel { get; set; }
            public event System.Action<float> ValorCambio { add { } remove { } }
            public event System.Action<int> NivelCambio { add { } remove { } }
            public void Sumar(float cantidad, MotivoCalor motivo) { }
            public void Enfriar(float cantidad) { }
            public void Borrar() { }
        }

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private static IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 4; i++) yield return null;
            yield return new WaitForSeconds(0.5f);
            foreach (MonoBehaviour m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (m != null && m.GetType().Name == "DirectorPandillas") m.enabled = false;
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name.StartsWith("CalaveraPunos")) Object.Destroy(t.gameObject);
            }
            IJugador jugador = Registro<IJugador>.Todos[0];
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == "Casa_Puerta")
                {
                    Vector3 enLaCalle = p.Pose.position + p.Pose.rotation * Vector3.forward * 9f + Vector3.up * 0.05f;
                    jugador.Colocar(new Pose(enLaCalle, Quaternion.Euler(0f, 0f, 0f)));
                }
            }
            yield return new WaitForSeconds(1.2f);
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static void Foto(string nombre)
        {
            IJugador jugador = Registro<IJugador>.Todos[0];
            CapturaUI.Capturar("docs/capturas/" + nombre + ".png", jugador.Camara);
        }

        [UnityTest]
        public IEnumerator Capturas_HudNuevoEnLaCalle()
        {
            if (System.Environment.GetEnvironmentVariable("CAPTURAR_S1") != "1") Assert.Ignore("Sin CAPTURAR_S1");
            yield return Cargar();
            IJugador jugador = Registro<IJugador>.Todos[0];
            HudAPie hud = Object.FindFirstObjectByType<HudAPie>();
            HudH10 h10 = hud.GetComponent<HudH10>();
            VidaAPie vida = jugador.Raiz.GetComponent<VidaAPie>();
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            ICartera cartera = Registro<ICartera>.Actual;
            ServicioObjetivos objetivos = Object.FindFirstObjectByType<ServicioObjetivos>();

            cartera.Abonar(12450 - cartera.Dinero, MotivoDinero.Premio);
            inventario.Agregar((DatosConsumible)inventario.Catalogo.Buscar("bebida_energetica"), 2);
            inventario.Agregar((DatosConsumible)inventario.Catalogo.Buscar("papitas"), 1);
            inventario.Agregar((DatosConsumible)inventario.Catalogo.Buscar("venda"), 3);
            // Vida como en H10: 143 de 204 (el escudo a un tercio).
            IEscudo escudo = Registro<IEscudo>.Actual;
            float danio = escudo.Actual * 0.65f;
            vida.RecibirDanio(new InfoDanio(danio, TipoDanio.Disparo, jugador.Raiz.position + Vector3.up, Vector3.forward, null));
            // La pistola con un tiro menos en el cargador: salen su holograma, las balas y la ayuda de recargar.
            Registro<ArsenalJugador>.Actual.DarMejora(ArmaAPie.Pistola);
            jugador.Raiz.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar != null && lugar.Id != IdLugar.Ninguno && lugar.Id != IdLugar.Calle && objetivos != null)
                {
                    objetivos.Fijar(new Objetivo("Ve con el Doc", lugar.Id));
                    break;
                }
            }
            for (int i = 0; i < 20; i++) yield return null;
            yield return Esperar(0.5f);
            Foto("s1c-T-700-juego-base");

            // Búsqueda activa: 3 estrellas llenas parpadeando (la foto se toma con las llenas encendidas).
            ZonaBusquedaFalsa zona = new ZonaBusquedaFalsa();
            Registro<IZonaBusqueda>.Agregar(zona);
            float limite = Time.unscaledTime + 2f;
            // En la ciudad la persecución borra el calor real cada cuadro: la foto usa un calor fijo (3 de 5 estrellas).
            CalorFijo fijo = new CalorFijo { Nivel = 3, Valor = 60f, Maximo = 100f };
            Registro<ICalor>.Agregar(fijo);
            while (Time.unscaledTime < limite && !(h10.Buscando && h10.Estrella(0).color.a > 0.99f)) yield return null;
            Debug.Log("[T700] llenas " + h10.EstrellasLlenas + " buscando " + h10.Buscando);
            Foto("s1c-T-700-juego-busqueda");
            Registro<IZonaBusqueda>.Quitar(zona);
            Registro<ICalor>.Quitar(fijo);

            // Daño: un tiro al brazo y la vida baja.
            vida.RecibirDanio(new InfoDanio(escudo.Maximo + vida.Maxima * 0.25f, TipoDanio.Disparo, jugador.Raiz.position + Vector3.up * 1.2f + jugador.Raiz.right * 0.4f, Vector3.forward, null));
            yield return null;
            yield return Esperar(0.1f);
            Foto("s1c-T-700-juego-dano");

            // Cerca de un carro: la ayuda cambia a subir.
            GameObject carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), jugador.Raiz.position + jugador.Raiz.right * 2f + jugador.Raiz.forward * 1.1f, jugador.Raiz.rotation);
            yield return Esperar(0.6f);
            Foto("s1c-T-700-juego-carro");
            Object.Destroy(carro);
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin) yield return null;
        }
    }
}
