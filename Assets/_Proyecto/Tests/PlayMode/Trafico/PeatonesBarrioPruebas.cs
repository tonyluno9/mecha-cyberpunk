using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// Peatones sobre Barrio.prefab (T-331): las rutas de banqueta no cruzan ningún collider del barrio, la cuenta viva cambia con la franja
    /// (reloj de prueba en el registro de Core) y un peatón se detiene a 2 m del jugador (jugador de prueba en el registro). Instancia el prefab, no lo escribe.
    /// </summary>
    public class PeatonesBarrioPruebas
    {
        private const string RutaBarrio = "Assets/_Proyecto/Mundo/Prefabs/Barrio.prefab";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        private const string RutaPrefabPeaton = "Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab";
        private const float MitadManzana = 40f;
        private const float AlturaBanqueta = 0.2f;
        private const float PasoMuestreo = 0.5f;
        private const float RadioCuerpo = 0.25f;
        private const float AlturaInferior = 0.4f;
        private const float AlturaSuperior = 1.7f;
        private const int FramesDeEspera = 3;
        private const float MargenParada = 0.3f;

        private sealed class RelojDoble : IRelojMundo
        {
            public bool Noche;
            public float Hora => Noche ? 22f : 12f;
            public int Dia => 1;
            public FranjaDia Franja => Noche ? FranjaDia.Noche : FranjaDia.Dia;
            public bool EsDeNoche => Noche;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private sealed class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject barrio;
        private RelojDoble reloj;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            reloj = new RelojDoble();
            Registro<IRelojMundo>.Agregar(reloj);
            config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            Assert.IsNotNull(config, "Falta ConfigPeatones_Trafico.asset");
            barrio = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaBarrio));
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(barrio);
            Registros.LimpiarTodos();
        }

        private List<Vector3[]> RutasDeLasManzanas()
        {
            List<Vector3> centros = barrio.transform.Cast<Transform>().Where(t => t.name.StartsWith("Manzana_")).Select(t => t.localPosition).ToList();
            return RutasBanqueta.DeManzanas(centros, MitadManzana - config.MargenBanqueta, AlturaBanqueta, config.RecorteCierres);
        }

        private GrupoPeatones ColgarGrupo()
        {
            GameObject objeto = new GameObject("Peatones");
            objeto.transform.SetParent(barrio.transform, false);
            GrupoPeatones grupo = objeto.AddComponent<GrupoPeatones>();
            grupo.Configurar(config, AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPeaton).GetComponent<PeatonMundo>(), RutasDeLasManzanas());
            return grupo;
        }

        [Test]
        public void Rutas_NoAtraviesanNingunCollider_DelBarrio()
        {
            List<Vector3[]> rutas = RutasDeLasManzanas();
            Assert.AreEqual(6, rutas.Count);
            List<string> problemas = new List<string>();
            foreach (Vector3[] ruta in rutas)
            {
                for (int i = 0; i < ruta.Length - 1; i++)
                {
                    float largo = Vector3.Distance(ruta[i], ruta[i + 1]);
                    for (float d = 0f; d <= largo; d += PasoMuestreo)
                    {
                        Vector3 punto = barrio.transform.TransformPoint(Vector3.Lerp(ruta[i], ruta[i + 1], d / largo));
                        Collider[] choques = Physics.OverlapCapsule(punto + Vector3.up * AlturaInferior, punto + Vector3.up * AlturaSuperior, RadioCuerpo);
                        foreach (Collider c in choques) { problemas.Add(c.name + "@" + punto.x.ToString("F0") + "," + punto.z.ToString("F0")); }
                    }
                }
            }
            Assert.IsEmpty(problemas.Distinct().ToArray(), "Puntos de ruta que chocan");
        }

        [UnityTest]
        public IEnumerator Cuenta_CambiaConLaFranja()
        {
            GrupoPeatones grupo = ColgarGrupo();
            for (int i = 0; i < FramesDeEspera; i++)
            {
                yield return null;
            }
            Assert.AreEqual(config.Cantidad(false), grupo.Vivos);
            reloj.Noche = true;
            yield return null;
            Assert.AreEqual(config.Cantidad(true), grupo.Vivos);
            Assert.Greater(config.Cantidad(true), config.Cantidad(false));
        }

        [UnityTest]
        public IEnumerator Peaton_SeDetieneAlAcercarseElJugador_YSigueCuandoSeAleja()
        {
            reloj.Noche = true;
            GrupoPeatones grupo = ColgarGrupo();
            yield return null;
            yield return null;
            PeatonMundo peaton = grupo.Peatones[0];
            GameObject jugadorObjeto = new GameObject("JugadorDePrueba");
            JugadorDoble jugador = new JugadorDoble { Raiz = jugadorObjeto.transform };
            Registro<IJugador>.Agregar(jugador);
            jugadorObjeto.transform.position = peaton.transform.position + Vector3.right * (config.DistanciaParada - MargenParada);
            yield return null;
            yield return null;
            Vector3 quieto = peaton.transform.position;
            yield return null;
            Assert.IsTrue(peaton.Detenido);
            Assert.AreEqual(quieto, peaton.transform.position);
            jugadorObjeto.transform.position += Vector3.right * 20f;
            yield return null;
            Assert.IsFalse(peaton.Detenido);
            Object.Destroy(jugadorObjeto);
        }
    }
}
