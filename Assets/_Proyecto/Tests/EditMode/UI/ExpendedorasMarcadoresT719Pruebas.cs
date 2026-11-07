using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Datos;
using MechaCyberpunk.UI.Tiendas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-719: las expendedoras leen su posición de un marcador de la escena o de un lugar de Core, no de coordenadas fijas.</summary>
    public class ExpendedorasMarcadoresT719Pruebas
    {
        private sealed class LugarFalso : ILugar
        {
            public IdLugar Id { get; set; }
            public string Nombre => Id.ToString();
            public HorarioLugar Horario => default;
            public bool EstaAbierto => true;
            public bool JugadorDentro => false;
            public Vector3 PosicionEntrada { get; set; }
            public event System.Action<bool> EstadoCambio { add { } remove { } }
        }

        private Scene escena;
        private readonly List<ILugar> puestos = new List<ILugar>();

        [SetUp]
        public void Preparar()
        {
            escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (ILugar l in puestos) Registro<ILugar>.Quitar(l);
            puestos.Clear();
        }

        private void PonerLugar(IdLugar id, Vector3 entrada)
        {
            LugarFalso l = new LugarFalso { Id = id, PosicionEntrada = entrada };
            puestos.Add(l);
            Registro<ILugar>.Agregar(l);
        }

        private static DatosExpendedoras.Posicion Posicion(string marcador, IdLugar lugar, Vector3 desplazamiento, Vector3 fija)
        {
            return new DatosExpendedoras.Posicion { escena = "x", marcador = marcador, lugar = lugar, desplazamiento = desplazamiento, posicion = fija };
        }

        [Test]
        public void Lugar_LaPosicionEsLaEntradaDelLugarMasElDesplazamiento()
        {
            PonerLugar(IdLugar.Bar, new Vector3(0f, 0.2f, -8.49f));
            Assert.IsTrue(ColocadorExpendedoras.ResolverPosicion(Posicion("", IdLugar.Bar, new Vector3(-36f, 0f, 0.49f), Vector3.zero), escena, out Vector3 p));
            Assert.AreEqual(new Vector3(-36f, 0.2f, -8f), p);
        }

        [Test]
        public void Lugar_SiElLugarSeMueveLaMaquinaSeMueveConEl()
        {
            PonerLugar(IdLugar.Casa, new Vector3(100f, 0.2f, 50f));
            ColocadorExpendedoras.ResolverPosicion(Posicion("", IdLugar.Casa, new Vector3(0f, 0f, 6f), Vector3.zero), escena, out Vector3 p);
            Assert.AreEqual(new Vector3(100f, 0.2f, 56f), p);
        }

        [Test]
        public void Marcador_SiExisteEnLaEscenaManda()
        {
            PonerLugar(IdLugar.Bar, Vector3.zero);
            GameObject marcador = new GameObject("Marcador_Expendedora_Tianguis");
            SceneManager.MoveGameObjectToScene(marcador, escena);
            marcador.transform.position = new Vector3(7f, 0.2f, 9f);
            Assert.IsTrue(ColocadorExpendedoras.ResolverPosicion(Posicion("Marcador_Expendedora_Tianguis", IdLugar.Bar, new Vector3(-36f, 0f, 0f), Vector3.zero), escena, out Vector3 p));
            Assert.AreEqual(new Vector3(7f, 0.2f, 9f), p, "El marcador de la escena gana al lugar");
        }

        [Test]
        public void Marcador_SiFaltaSeUsaElLugar()
        {
            PonerLugar(IdLugar.Bar, new Vector3(0f, 0.2f, -8f));
            Assert.IsTrue(ColocadorExpendedoras.ResolverPosicion(Posicion("NoExiste", IdLugar.Bar, new Vector3(5f, 0f, 0f), Vector3.zero), escena, out Vector3 p));
            Assert.AreEqual(new Vector3(5f, 0.2f, -8f), p);
        }

        [Test]
        public void SinMarcadorNiLugarEnLaEscena_NoSeColoca_YNoCaeACoordenadasFijas()
        {
            Assert.IsFalse(ColocadorExpendedoras.ResolverPosicion(Posicion("", IdLugar.Casino, Vector3.zero, new Vector3(1f, 2f, 3f)), escena, out _), "Pidió un lugar que no está: no se coloca");
            Assert.IsFalse(ColocadorExpendedoras.ResolverPosicion(Posicion("NoExiste", IdLugar.Ninguno, Vector3.zero, new Vector3(1f, 2f, 3f)), escena, out _), "Pidió un marcador que no está: no se coloca");
        }

        [Test]
        public void SoloSinLugarNiMarcador_SeUsaLaPosicionFija()
        {
            Assert.IsTrue(ColocadorExpendedoras.ResolverPosicion(Posicion("", IdLugar.Ninguno, Vector3.zero, new Vector3(1f, 2f, 3f)), escena, out Vector3 p));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), p, "Las pruebas y escenas sin lugares siguen con la posición fija");
        }

        [Test]
        public void LosDatosDeLaCiudad_NoTienenCoordenadasFijas()
        {
            DatosExpendedoras datos = Resources.Load<DatosExpendedoras>(ColocadorExpendedoras.NombreDatos);
            Assert.IsNotNull(datos);
            Assert.GreaterOrEqual(datos.Posiciones.Count, 3);
            foreach (DatosExpendedoras.Posicion p in datos.Posiciones)
            {
                Assert.IsTrue(p.lugar != IdLugar.Ninguno || !string.IsNullOrEmpty(p.marcador), p.nota + ": sin lugar ni marcador");
                Assert.AreEqual(Vector3.zero, p.posicion, p.nota + ": quedó una coordenada fija");
            }
        }
    }
}
