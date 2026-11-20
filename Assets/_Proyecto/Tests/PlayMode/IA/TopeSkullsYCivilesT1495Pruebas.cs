using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-1495: (1) los SKULLS no salen de golpe: tope por punto y por zona; (2) el civil herido reacciona y huye, y el que muere cae con una
    /// animación de muerte (MuerteAdelante, MuerteAtras o MuerteDesmayo del controlador base).
    /// </summary>
    public class TopeSkullsYCivilesT1495Pruebas
    {
        private const string RutaPandilla = "Assets/_Proyecto/IA/Prefabs/PandillaAPie.prefab";
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Doctor.prefab";
        private const int GruposEnElMismoPunto = 4;
        private static readonly Vector3[] Ruta = { new Vector3(0f, 0f, 0f), new Vector3(200f, 0f, 0f) };

        private sealed class RelojNoche : IRelojMundo
        {
            public float Hora => 22f;
            public int Dia => 1;
            public FranjaDia Franja => FranjaDia.Noche;
            public bool EsDeNoche => true;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado => true;
            public event Action<int> HoraEnteraCambio { add { } remove { } }
            public event Action<FranjaDia> FranjaCambio { add { } remove { } }
            public event Action<int> DiaCambio { add { } remove { } }
            public void Pausar(bool pausado) { }
            public void AvanzarHasta(float hora) { }
        }

        private readonly List<UnityEngine.Object> basura = new List<UnityEngine.Object>();

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (UnityEngine.Object o in basura)
            {
                if (o != null)
                {
                    UnityEngine.Object.DestroyImmediate(o);
                }
            }
            basura.Clear();
            Registros.LimpiarTodos();
            EventosRuido.LimpiarSuscriptores();
        }

        [UnityTest]
        public IEnumerator Skulls_VariosGruposEnElMismoPunto_NoPasanDelTope()
        {
            Registro<IRelojMundo>.Agregar(new RelojNoche());
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandilla);
            Assert.IsNotNull(prefab, "Falta " + RutaPandilla);
            List<PandillaAPie> grupos = new List<PandillaAPie>();
            for (int i = 0; i < GruposEnElMismoPunto; i++)
            {
                GameObject inactiva = new GameObject("Inactiva" + i);
                inactiva.SetActive(false);
                basura.Add(inactiva);
                GameObject g = UnityEngine.Object.Instantiate(prefab, inactiva.transform);
                basura.Add(g);
                PandillaAPie pandilla = g.GetComponent<PandillaAPie>();
                DatosPandilleroAPie datos = UnityEngine.Object.Instantiate(pandilla.Datos);
                basura.Add(datos);
                SerializedObject so = new SerializedObject(datos);
                so.FindProperty("siempre").boolValue = true;
                so.FindProperty("tamanoPandilla").intValue = Mathf.Max(datos.TamanoPandilla, 3);
                so.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject soGrupo = new SerializedObject(pandilla);
                soGrupo.FindProperty("datos").objectReferenceValue = datos;
                soGrupo.ApplyModifiedPropertiesWithoutUndo();
                g.transform.SetParent(null, true);
                g.transform.position = Vector3.zero;
                grupos.Add(pandilla);
                yield return null;
            }
            yield return null;
            int total = 0;
            foreach (PandillaAPie p in grupos)
            {
                Assert.LessOrEqual(p.Presentes, ReglaTopePandilleros.PorPunto, "un grupo no pasa del tope por punto");
                total += p.Presentes;
            }
            Assert.LessOrEqual(total, ReglaTopePandilleros.EnZona, "juntos en 40 m no pasan del tope de la zona");
            Assert.Greater(total, 0, "pero sí sale gente");
        }

        private PeatonMundo CrearCivilConVisual(ConfigPeatones config)
        {
            GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual);
            Assert.IsNotNull(visualPrefab, "Falta " + RutaVisual);
            GameObject objeto = new GameObject("CivilT1495");
            basura.Add(objeto);
            objeto.SetActive(false);
            UnityEngine.Object.Instantiate(visualPrefab, objeto.transform);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            objeto.AddComponent<AnimacionCivilMundo>();
            peaton.Preparar(Ruta, config, new System.Random(1), () => (Vector3?)new Vector3(50f, 0f, 30f), 50f / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        private static bool EnMuerte(Animator a)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(0);
            foreach (string n in new[] { "MuerteAdelante", "MuerteAtras", "MuerteDesmayo" })
            {
                if (actual.IsName(n) || (a.IsInTransition(0) && siguiente.IsName(n)))
                {
                    return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator Civil_RecibeUnBalazoSinMorir_ReaccionaAlDanioYHuye()
        {
            ConfigPeatones config = ScriptableObject.CreateInstance<ConfigPeatones>();
            basura.Add(config);
            PeatonMundo civil = CrearCivilConVisual(config);
            yield return null;
            yield return null;
            Animator a = civil.GetComponentInChildren<Animator>();
            int capa = a.GetLayerIndex(ReglasAnimacion.CapaDano);
            Assert.GreaterOrEqual(capa, 0, "el controlador tiene la capa de daño");
            civil.RecibirDanio(new InfoDanio(config.VidaPeaton / 4f, TipoDanio.Disparo, civil.transform.position, Vector3.forward, null));
            bool reacciono = false;
            for (int i = 0; i < 20 && !reacciono; i++)
            {
                yield return null;
                AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
                AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
                reacciono = actual.IsName("DanoFrente") || actual.IsName("DanoEspalda") || actual.IsName("DanoFuerte")
                    || siguiente.IsName("DanoFrente") || siguiente.IsName("DanoEspalda") || siguiente.IsName("DanoFuerte");
            }
            Assert.IsTrue(reacciono, "entra a la reacción de daño");
            Assert.IsFalse(civil.Muerto);
            Assert.IsTrue(civil.Huyendo, "y luego huye (el jugador está lejos)");
        }

        [UnityTest]
        public IEnumerator Civil_Muere_EntraAUnEstadoDeMuerteAnimado()
        {
            ConfigPeatones config = ScriptableObject.CreateInstance<ConfigPeatones>();
            basura.Add(config);
            PeatonMundo civil = CrearCivilConVisual(config);
            yield return null;
            yield return null;
            Assert.IsTrue(civil.TieneMuerteAnimada(), "el visual tiene el parámetro Caer");
            civil.RecibirDanio(new InfoDanio(config.VidaPeaton + 1f, TipoDanio.Disparo, civil.transform.position, Vector3.forward, null));
            Assert.IsTrue(civil.Muerto);
            Animator a = civil.GetComponentInChildren<Animator>();
            bool murio = false;
            for (int i = 0; i < 30 && !murio; i++)
            {
                yield return null;
                murio = EnMuerte(a);
            }
            Assert.IsTrue(murio, "entra a MuerteAdelante, MuerteAtras o MuerteDesmayo");
            Assert.Greater(civil.transform.up.y, 0.9f, "no lo tumba girando el cuerpo: cae con la animación");
        }
    }
}
