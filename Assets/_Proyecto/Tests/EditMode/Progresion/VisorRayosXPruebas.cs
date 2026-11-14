using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Progresion.Apoyo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-866 (playtest 6-nov tarde): con el VISOR (ojos) los enemigos cercanos llevan el material de rayos X; sin él, no. T-992: y solo mientras CABEZA está activa.</summary>
    public class VisorRayosXPruebas
    {
        private sealed class ImplantesDoble : IImplantes
        {
            public int Ojos;
            public int Nivel(EspacioImplante espacio) => espacio == EspacioImplante.Ojos ? Ojos : 0;
            public string Nombre(EspacioImplante espacio) => string.Empty;
            public int Cantidad => Ojos > 0 ? 1 : 0;
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio { add { } remove { } }
            public event Action<EspacioImplante, int> Instalado { add { } remove { } }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        [TearDown]
        public void Limpiar() => HabilidadesProtesis.PonerCabezaActiva(false);

        [Test]
        public void Visor_MarcaAlEnemigoCercano_YAlQuitarloLoDesmarca()
        {
            HabilidadesProtesis.PonerCabezaActiva(true);
            ImplantesDoble implantes = new ImplantesDoble { Ojos = 1 };
            GameObject jugadorGo = new GameObject("Jugador");
            JugadorDoble jugador = new JugadorDoble { Raiz = jugadorGo.transform };
            GameObject enemigo = new GameObject("Pandillero");
            enemigo.SetActive(false);
            enemigo.AddComponent<PandilleroAPie>();
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.transform.SetParent(enemigo.transform, false);
            enemigo.transform.position = new Vector3(0f, 0f, 10f);
            GameObject visorGo = new GameObject("Visor");
            VisorRayosX visor = visorGo.AddComponent<VisorRayosX>();
            Material rayos = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Proyecto/Progresion/Apoyo/Resources/VisorRayosX.mat");
            Assert.IsNotNull(rayos, "el material del visor está en Resources");
            visor.Configurar(rayos);
            Registro<IImplantes>.Agregar(implantes);
            Registro<IJugador>.Agregar(jugador);
            try
            {
                enemigo.SetActive(true);
                Renderer r = cuerpo.GetComponent<Renderer>();
                visor.Actualizar();
                Assert.IsTrue(visor.EstaMarcado(r), "con el visor se ve a través de las cosas");
                Assert.AreSame(rayos, r.sharedMaterials[r.sharedMaterials.Length - 1]);
                implantes.Ojos = 0;
                visor.Actualizar();
                Assert.IsFalse(visor.EstaMarcado(r), "sin el visor, no");
                Assert.AreEqual(1, r.sharedMaterials.Length, "y su material queda como estaba");
                implantes.Ojos = 1;
                visor.Actualizar();
                Assert.IsTrue(visor.EstaMarcado(r), "con el visor otra vez, se marca");
                HabilidadesProtesis.PonerCabezaActiva(false);
                visor.Actualizar();
                Assert.IsFalse(visor.EstaMarcado(r), "con CABEZA apagada, aunque tenga el implante, no se ve a través de las cosas");
            }
            finally
            {
                Registro<IImplantes>.Quitar(implantes);
                Registro<IJugador>.Quitar(jugador);
                Object.DestroyImmediate(visorGo);
                Object.DestroyImmediate(enemigo);
                Object.DestroyImmediate(jugadorGo);
            }
        }
    }
}
