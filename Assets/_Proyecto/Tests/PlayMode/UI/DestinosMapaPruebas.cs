using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>
    /// T-667 y T-668: el mapa grande y el minimapa traen un icono por cada lugar al que se puede ir (casa, El Doc, Tino, mecánico, tianguis, casino, mercado y
    /// misiones); un clic sobre el icono lo marca, el GPS naranja traza la ruta hasta él en los dos mapas, otro clic lo quita y la marca se queda en el
    /// minimapa hasta que llegas.
    /// </summary>
    public class DestinosMapaPruebas : InputTestFixture
    {
        // T-727: el minimapa quita los iconos que caen a menos de 35 px del centro (los taparían la flecha del jugador, T-721): a 2.17 px/m son 16 m.
        // "Cerca" ya no es 2 m (ahí el icono queda bajo la flecha a propósito) sino 20 m, dentro del radio de 60 m del minimapa.
        private const float Cerca = 20f;
        private const float ToleranciaMetros = 1.5f;

        private sealed class JugadorFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara { get; set; }
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { Raiz.position = pose.position; }
        }

        private sealed class LugarFalso : ILugar
        {
            public IdLugar Id { get; set; }
            public string Nombre => Id.ToString();
            public HorarioLugar Horario => HorarioLugar.Siempre;
            public bool EstaAbierto => true;
            public bool JugadorDentro => false;
            public Vector3 PosicionEntrada { get; set; }
            public event Action<bool> EstadoCambio { add { } remove { } }
        }

        private sealed class RutaEnLFalsa : IRutaCalles
        {
            public List<Vector3> Calcular(Vector3 origen, Vector3 destino)
            {
                return new List<Vector3> { origen, new Vector3(origen.x, 0f, destino.z), destino };
            }
        }

        private static readonly Dictionary<IdDestinoMapa, Vector3> Sitios = new Dictionary<IdDestinoMapa, Vector3>
        {
            { IdDestinoMapa.Casa, new Vector3(-100f, 0f, -45f) },
            { IdDestinoMapa.ElDoc, new Vector3(-50f, 0f, 55f) },
            { IdDestinoMapa.Tino, new Vector3(-5f, 0f, 20f) },
            { IdDestinoMapa.Mecanico, new Vector3(-100f, 0f, 20f) },
            { IdDestinoMapa.Tianguis, new Vector3(60f, 0f, -40f) },
            { IdDestinoMapa.Casino, new Vector3(110f, 0f, 55f) },
            { IdDestinoMapa.Mercado, new Vector3(10f, 0f, -55f) },
            { IdDestinoMapa.Misiones, new Vector3(100f, 0f, -10f) },
        };

        private Keyboard teclado;
        private Mouse raton;
        private readonly List<GameObject> extras = new List<GameObject>();
        private GameObject camaraObjeto;
        private PantallaMapa mapa;
        private Minimapa minimapa;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            Registros.LimpiarTodos();
            PuntoMarcadoMapa.Quitar();
            CatalogoDestinosMapa.Limpiar();
            camaraObjeto = new GameObject("Camara", typeof(Camera));
            Camera camara = camaraObjeto.GetComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camara.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camaraObjeto.transform.position = new Vector3(0f, 0f, 0f);
            Registro<IJugador>.Agregar(new JugadorFalso { Raiz = camaraObjeto.transform, Camara = camara });
            Registro<IRutaCalles>.Agregar(new RutaEnLFalsa());
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Casa, PosicionEntrada = Sitios[IdDestinoMapa.Casa] });
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Clinica, PosicionEntrada = Sitios[IdDestinoMapa.ElDoc] });
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Casino, PosicionEntrada = Sitios[IdDestinoMapa.Casino] });
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.MercadoNegro, PosicionEntrada = Sitios[IdDestinoMapa.Mercado] });
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Bar, PosicionEntrada = Sitios[IdDestinoMapa.Misiones] });
            Objeto("TallerTino", Sitios[IdDestinoMapa.Tino]);
            Objeto("Lugar_TallerMecanico", Sitios[IdDestinoMapa.Mecanico]);
            Objeto("Tianguis", Sitios[IdDestinoMapa.Tianguis]);
            GameObject mapaObjeto = new GameObject("PantallaMapa");
            extras.Add(mapaObjeto);
            mapa = mapaObjeto.AddComponent<PantallaMapa>();
            GameObject minimapaObjeto = new GameObject("Minimapa");
            extras.Add(minimapaObjeto);
            minimapa = minimapaObjeto.AddComponent<Minimapa>();
        }

        public override void TearDown()
        {
            if (mapa != null && mapa.Abierto)
            {
                mapa.Cerrar();
            }
            PuntoMarcadoMapa.Quitar();
            foreach (GameObject extra in extras)
            {
                UnityEngine.Object.DestroyImmediate(extra);
            }
            extras.Clear();
            UnityEngine.Object.DestroyImmediate(camaraObjeto);
            CatalogoDestinosMapa.Limpiar();
            Registros.LimpiarTodos();
            base.TearDown();
        }

        private void Objeto(string nombre, Vector3 donde)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.transform.position = donde;
            extras.Add(objeto);
        }

        private void Colocar(Vector3 donde)
        {
            camaraObjeto.transform.position = donde;
        }

        private IEnumerator Clic(Vector2 pantalla)
        {
            Set(raton.position, pantalla);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Minimapa_MuestraElIconoDeCadaLugar_CuandoEstasCerca()
        {
            yield return null;
            foreach (KeyValuePair<IdDestinoMapa, Vector3> sitio in Sitios)
            {
                Colocar(sitio.Value + new Vector3(Cerca, 0f, 0f));
                minimapa.Actualizar();
                Assert.IsNotNull(minimapa.IconoDestino(sitio.Key), "El minimapa muestra el icono de " + sitio.Key + " cuando estás junto a él");
            }
            Colocar(Sitios[IdDestinoMapa.Casa]);
            minimapa.Actualizar();
            Assert.IsNull(minimapa.IconoDestino(IdDestinoMapa.Casino), "El casino queda lejos: no sale en el minimapa");
        }

        [UnityTest]
        public IEnumerator MapaGrande_MuestraElIconoYElNombreDeTodosLosLugares()
        {
            yield return null;
            mapa.Abrir();
            yield return null;
            yield return null;
            foreach (DestinoMapa destino in CatalogoDestinosMapa.Todos)
            {
                UnityEngine.UI.Image icono = mapa.IconoDestino(destino.Id);
                Assert.IsNotNull(icono, "El mapa grande muestra " + destino.Id);
                Vector2 esperado = new Vector2(Sitios[destino.Id].x, Sitios[destino.Id].z) * mapa.PixelesPorMetro;
                Assert.AreEqual(esperado.x, icono.rectTransform.anchoredPosition.x, 0.01f, destino.Id + " en su sitio (x)");
                Assert.AreEqual(esperado.y, icono.rectTransform.anchoredPosition.y, 0.01f, destino.Id + " en su sitio (z)");
                UnityEngine.UI.Text nombre = icono.transform.Find("Nombre").GetComponent<UnityEngine.UI.Text>();
                Assert.IsNotNull(nombre, "Cada icono lleva su nombre");
                Assert.AreEqual(destino.Nombre, nombre.text);
            }
            Assert.AreEqual(8, CatalogoDestinosMapa.Todos.Count, "Casa, El Doc, Tino, mecánico, tianguis, casino, mercado y misiones");
        }

        [UnityTest]
        public IEnumerator ClicEnElIcono_MarcaElLugar_YElGpsTrazaLaRutaEnLosDosMapas_ConOtroClicSeQuita()
        {
            yield return null;
            Colocar(new Vector3(-20f, 0f, -20f));
            mapa.Abrir();
            yield return null;
            yield return null;
            Vector3 tino = Sitios[IdDestinoMapa.Tino];
            Assert.AreEqual(0, mapa.SegmentosRutaVisibles, "Sin nada marcado no hay ruta");
            yield return Clic(mapa.PantallaDe(tino));
            Assert.IsTrue(PuntoMarcadoMapa.Hay, "El clic en el icono marca el lugar");
            Assert.AreEqual(IdDestinoMapa.Tino, PuntoMarcadoMapa.Destino);
            Assert.AreEqual(tino.x, PuntoMarcadoMapa.Posicion.x, 0.01f);
            Assert.AreEqual(tino.z, PuntoMarcadoMapa.Posicion.z, 0.01f);
            yield return null;
            Assert.GreaterOrEqual(mapa.Ruta.Count, 2, "El mapa grande traza la ruta hasta Tino");
            Assert.AreEqual(mapa.Ruta.Count - 1, mapa.SegmentosRutaVisibles);
            Assert.Less(Vector3.Distance(mapa.Ruta[mapa.Ruta.Count - 1], new Vector3(tino.x, 0f, tino.z)), ToleranciaMetros, "La ruta termina en el lugar");
            Assert.Less(Vector3.Distance(mapa.Ruta[0], new Vector3(-20f, 0f, -20f)), ToleranciaMetros, "y sale de donde estás");

            mapa.Cerrar();
            yield return null;
            minimapa.Actualizar();
            Assert.GreaterOrEqual(minimapa.Ruta.Count, 2, "El minimapa también traza la ruta GPS");
            Assert.Less(Vector3.Distance(minimapa.Ruta[minimapa.Ruta.Count - 1], new Vector3(tino.x, 0f, tino.z)), ToleranciaMetros);
            Assert.AreEqual(minimapa.Ruta.Count - 1, minimapa.SegmentosRutaVisibles);
            Assert.IsTrue(minimapa.PuntoVisible, "La marca sale en el minimapa");

            // Otro clic en el mismo icono lo quita y la ruta desaparece de los dos mapas.
            mapa.Abrir();
            yield return null;
            yield return null;
            yield return Clic(mapa.PantallaDe(tino));
            Assert.IsFalse(PuntoMarcadoMapa.Hay, "Otro clic sobre el lugar marcado lo quita");
            yield return null;
            Assert.AreEqual(0, mapa.SegmentosRutaVisibles, "Sin marca no hay ruta en el mapa grande");
            mapa.Cerrar();
            yield return null;
            minimapa.Actualizar();
            Assert.AreEqual(0, minimapa.SegmentosRutaVisibles, "ni en el minimapa");
            Assert.IsFalse(minimapa.PuntoVisible);
        }

        [UnityTest]
        public IEnumerator MarcarOtroLugar_CambiaLaRuta()
        {
            yield return null;
            Colocar(new Vector3(0f, 0f, 0f));
            mapa.Abrir();
            yield return null;
            yield return Clic(mapa.PantallaDe(Sitios[IdDestinoMapa.Tianguis]));
            Assert.AreEqual(IdDestinoMapa.Tianguis, PuntoMarcadoMapa.Destino);
            yield return Clic(mapa.PantallaDe(Sitios[IdDestinoMapa.Casino]));
            Assert.AreEqual(IdDestinoMapa.Casino, PuntoMarcadoMapa.Destino, "Marcar otro lugar reemplaza al anterior");
            yield return null;
            Vector3 casino = Sitios[IdDestinoMapa.Casino];
            Assert.Less(Vector3.Distance(mapa.Ruta[mapa.Ruta.Count - 1], new Vector3(casino.x, 0f, casino.z)), ToleranciaMetros);
        }

        [UnityTest]
        public IEnumerator LaMarca_SeQuedaEnElMinimapaHastaQueLlegas()
        {
            yield return null;
            Vector3 mercado = Sitios[IdDestinoMapa.Mercado];
            PuntoMarcadoMapa.MarcarDestino(IdDestinoMapa.Mercado, mercado);
            Colocar(mercado + new Vector3(0f, 0f, 55f));
            minimapa.Actualizar();
            Assert.IsTrue(minimapa.PuntoVisible, "Lejos: la marca sale pegada al borde");
            Assert.Greater(minimapa.SegmentosRutaVisibles, 0);
            Colocar(mercado + new Vector3(0f, 0f, 25f));
            minimapa.Actualizar();
            Assert.IsTrue(PuntoMarcadoMapa.Hay, "Al acercarte la marca sigue ahí");
            Assert.IsTrue(minimapa.PuntoVisible);
            Colocar(mercado + new Vector3(0f, 0f, PuntoMarcadoMapa.MetrosParaLlegar + 3f));
            minimapa.Actualizar();
            Assert.IsTrue(PuntoMarcadoMapa.Hay, "A punto de llegar todavía está");
            Colocar(mercado + new Vector3(0f, 0f, PuntoMarcadoMapa.MetrosParaLlegar - 2f));
            minimapa.Actualizar();
            Assert.IsFalse(PuntoMarcadoMapa.Hay, "Al llegar la marca se quita sola");
            Assert.IsFalse(minimapa.PuntoVisible);
            Assert.AreEqual(0, minimapa.SegmentosRutaVisibles, "y la ruta desaparece");
        }

        [Test]
        public void ElCatalogo_ResuelveLaPosicionDeCadaLugar_PorLugarDeCoreOPorNombreDeObjeto()
        {
            foreach (DestinoMapa destino in CatalogoDestinosMapa.Todos)
            {
                Assert.IsTrue(CatalogoDestinosMapa.Posicion(destino.Id, out Vector3 p), destino.Id + " se encuentra");
                Assert.AreEqual(Sitios[destino.Id].x, p.x, 0.01f);
                Assert.AreEqual(Sitios[destino.Id].z, p.z, 0.01f);
            }
            Assert.AreEqual(IdDestinoMapa.Tino, CatalogoDestinosMapa.MasCercano(Sitios[IdDestinoMapa.Tino] + new Vector3(1f, 0f, 1f), 5f).Id);
            Assert.IsNull(CatalogoDestinosMapa.MasCercano(new Vector3(500f, 0f, 500f), 5f));
        }
    }
}
