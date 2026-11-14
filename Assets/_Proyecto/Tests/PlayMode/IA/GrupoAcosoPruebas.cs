using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Tests.Personaje;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// S2 pandillas (14-nov): un grupo de SKULLS acosa a un civil con el JugadorAPie real. (a) te ven de frente y atacan en menos de 0,5 s;
    /// (b) les pegas por la espalda desde arriba: buscan durante el retraso largo y después atacan; (c) los vences y el civil huye y deja el bono;
    /// más el empujón, la distancia sin pelea y el reparto de la ciudad.
    /// </summary>
    public class GrupoAcosoPruebas : BaseJugadorAPie
    {
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaDatosAcoso = "Assets/_Proyecto/IA/Datos/DatosAcoso_Base.asset";
        private const string RutaVisual = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab";
        private const float RadioCirculo = 2f;
        private const float DistanciaDeFrente = 10f;
        private const float SegundosDeteccion = 0.5f;
        private const float DistanciaEspalda = 8f;
        private const float AlturaAzotea = 4f;
        private const float DanioPrueba = 5f;
        private const float DanioMatar = 1000f;
        private const float MinimoAlejado = 8f;
        private const float SegundosHuida = 4f;
        private const float Margen = 0.35f;

        private DatosAcoso acoso;
        private readonly List<InfoBotin> botines = new List<InfoBotin>();
        private readonly List<GameObject> grupos = new List<GameObject>();
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            EventosBotin.LimpiarSuscriptores();
            botines.Clear();
            EventosBotin.Soltado += AlSoltar;
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // nadie acierta (acierta si Azar <= probabilidad): la prueba mide la IA, no la puntería
            acoso = AssetDatabase.LoadAssetAtPath<DatosAcoso>(RutaDatosAcoso);
            Assert.IsNotNull(acoso, "Falta DatosAcoso_Base.asset");
        }

        public override void TearDown()
        {
            EventosBotin.Soltado -= AlSoltar;
            foreach (GameObject grupo in grupos)
            {
                if (grupo != null)
                {
                    Object.DestroyImmediate(grupo);
                }
            }
            grupos.Clear();
            PistolaPandillero.Azar = azarAnterior;
            EventosBotin.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void AlSoltar(InfoBotin info) => botines.Add(info);

        private void Teletransportar(Vector3 posicion)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = posicion;
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        // Grupo con civil en 'centro'. En fila (todos mirando a +X, hacia el civil) o en círculo.
        private GrupoAcoso CrearGrupo(Vector3 centro, int cuantos, bool enFila)
        {
            GameObject raiz = new GameObject("GrupoPrueba");
            raiz.SetActive(false);
            raiz.transform.position = centro;
            GameObject civilObj = new GameObject("Civil");
            civilObj.transform.SetParent(raiz.transform, false);
            civilObj.transform.position = centro;
            CharacterController cc = civilObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisual), civilObj.transform);
            visual.transform.localPosition = Vector3.zero;
            CivilAcosado civil = civilObj.AddComponent<CivilAcosado>();
            Asignar(civil, "datos", acoso);

            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            for (int i = 0; i < cuantos; i++)
            {
                Vector3 pos;
                if (enFila)
                {
                    pos = centro + new Vector3(-RadioCirculo, 0f, (i - (cuantos - 1) * 0.5f) * 1.2f);
                }
                else
                {
                    pos = centro + Quaternion.Euler(0f, 360f * i / cuantos, 0f) * Vector3.forward * RadioCirculo;
                }
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                Vector3 mira = centro - pos;
                mira.y = 0f;
                p.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira.normalized, Vector3.up));
                Asignar(p.GetComponent<PandilleroAPie>(), "datos", calaveras);
            }
            PandillaAPie pandilla = raiz.AddComponent<PandillaAPie>();
            Asignar(pandilla, "datos", calaveras);
            GrupoAcoso grupo = raiz.AddComponent<GrupoAcoso>();
            Asignar(grupo, "datos", acoso);
            Asignar(grupo, "civil", civil);
            grupos.Add(raiz);
            raiz.SetActive(true);
            return grupo;
        }

        private static void Asignar(Object objetivo, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(objetivo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static IEnumerable<PandilleroAPie> Vivos(GrupoAcoso g)
        {
            foreach (PandilleroAPie p in g.GetComponent<PandillaAPie>().Pandilleros)
            {
                if (p.gameObject.activeSelf && !p.EstaKO)
                {
                    yield return p;
                }
            }
        }

        private static bool AlguienAtaca(GrupoAcoso g)
        {
            foreach (PandilleroAPie p in Vivos(g))
            {
                if (p.Alerta || p.ObjetivoActual != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static InfoDanio GolpeDelJugador(PandilleroAPie a) => new InfoDanio(DanioPrueba, TipoDanio.Disparo, a.transform.position + Vector3.up, Vector3.forward, null);

        [UnityTest]
        public IEnumerator Acosando_ElJugadorSeAcercaDeFrente_AlertaEnMenosDeMedioSegundo()
        {
            yield return Calentar();
            Teletransportar(new Vector3(0f, 0.05f, acoso.DistanciaVista + 20f));
            GrupoAcoso g = CrearGrupo(new Vector3(0f, 0.05f, 0f), 3, false);
            yield return Esperar(0.5f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, "Antes de que el jugador esté a la vista acosan");
            Assert.IsFalse(AlguienAtaca(g), "Acosando no persiguen a nadie");
            // El jugador queda frente al que está en +Z (mira hacia -Z).
            Teletransportar(new Vector3(0f, 0.05f, -DistanciaDeFrente));
            float inicio = Time.time;
            while (Time.time - inicio < SegundosDeteccion && !AlguienAtaca(g))
            {
                yield return null;
            }
            Assert.IsTrue(AlguienAtaca(g), "Debe haber alerta y alguien que lo persigue en menos de " + SegundosDeteccion + " s");
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, g.Fase);
            yield return Esperar(0.3f);
            bool persigue = false;
            foreach (PandilleroAPie p in Vivos(g))
            {
                persigue |= p.ObjetivoActual == jugador.transform;
            }
            Assert.IsTrue(persigue, "Alguno apunta al jugador");
        }

        [UnityTest]
        public IEnumerator Acosando_DanioPorLaEspaldaDesdeArriba_BuscanElRetrasoLargoYDespuesAtacan()
        {
            yield return Calentar();
            Vector3 centro = new Vector3(0f, 0.05f, 0f);
            // Una azotea baja a sus espaldas (los pandilleros miran a +X).
            float xAzotea = -RadioCirculo - DistanciaEspalda;
            Caja(new Vector3(xAzotea, AlturaAzotea * 0.5f, 0f), new Vector3(6f, AlturaAzotea, 6f));
            Teletransportar(new Vector3(xAzotea, AlturaAzotea + 0.05f, 0f));
            GrupoAcoso g = CrearGrupo(centro, 3, true);
            yield return Esperar(0.6f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, "A sus espaldas y a 8 m no lo ven");
            Assert.IsFalse(AlguienAtaca(g));

            PandilleroAPie golpeado = null;
            foreach (PandilleroAPie p in Vivos(g)) { golpeado = p; break; }
            float antesVida = golpeado.Actual;
            golpeado.RecibirDanio(GolpeDelJugador(golpeado));
            Assert.Less(golpeado.Actual, antesVida, "El daño de ese golpe cuenta");
            float inicio = Time.time;
            yield return null;
            Assert.AreEqual(ReglaAcoso.Fase.Sorpresa, g.Fase, "Sin verte: sorpresa");
            Assert.AreEqual(acoso.GritoSorpresa, GritoDeAlguno(g), "Gritan buscando");

            // Durante el retraso largo (desde arriba) no atacan.
            while (Time.time - inicio < acoso.RetrasoEmboscadaDesdeArriba - 0.3f)
            {
                Assert.IsFalse(AlguienAtaca(g), "No atacan durante el retraso (" + (Time.time - inicio).ToString("F2") + " s)");
                yield return null;
            }
            Assert.Greater(Time.time - inicio, acoso.RetrasoEmboscada + 0.2f, "Pasó el retraso normal y siguen buscando porque está arriba");
            while (Time.time - inicio < acoso.RetrasoEmboscadaDesdeArriba + 0.5f && !AlguienAtaca(g))
            {
                yield return null;
            }
            Assert.IsTrue(AlguienAtaca(g), "Después del retraso atacan");
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, g.Fase);
        }

        private static string GritoDeAlguno(GrupoAcoso g)
        {
            foreach (PandilleroAPie p in Vivos(g))
            {
                if (!string.IsNullOrEmpty(p.GritoActual)) { return p.GritoActual; }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator Acosando_DanioPorLaEspaldaEnElPiso_AtacanDespuesDelRetrasoNormal()
        {
            yield return Calentar();
            Teletransportar(new Vector3(-RadioCirculo - DistanciaEspalda, 0.05f, 0f));
            GrupoAcoso g = CrearGrupo(new Vector3(0f, 0.05f, 0f), 3, true);
            yield return Esperar(0.6f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase);
            PandilleroAPie golpeado = null;
            foreach (PandilleroAPie p in Vivos(g)) { golpeado = p; break; }
            golpeado.RecibirDanio(GolpeDelJugador(golpeado));
            float inicio = Time.time;
            while (Time.time - inicio < acoso.RetrasoEmboscada - 0.3f)
            {
                yield return null;
                Assert.IsFalse(AlguienAtaca(g), "No atacan durante el retraso normal");
            }
            while (Time.time - inicio < acoso.RetrasoEmboscada + 0.5f && !AlguienAtaca(g))
            {
                yield return null;
            }
            Assert.IsTrue(AlguienAtaca(g));
            Assert.Less(Time.time - inicio, acoso.RetrasoEmboscadaDesdeArriba, "Sin altura el retraso es el normal, menor que el de desde arriba");
        }

        [UnityTest]
        public IEnumerator Vencidos_TodosKO_ElCivilSeAlejaYSueltaElBono()
        {
            yield return Calentar();
            Vector3 centro = new Vector3(0f, 0.05f, 0f);
            Teletransportar(new Vector3(0f, 0.05f, -DistanciaDeFrente));
            GrupoAcoso g = CrearGrupo(centro, 3, false);
            yield return Esperar(0.5f);
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, g.Fase);
            Vector3 inicioCivil = g.Civil.transform.position;
            List<PandilleroAPie> todos = new List<PandilleroAPie>(Vivos(g));
            foreach (PandilleroAPie p in todos)
            {
                p.RecibirDanio(new InfoDanio(DanioMatar, TipoDanio.Golpe, p.transform.position, Vector3.forward, null));
            }
            yield return null;
            Assert.IsTrue(g.GetComponent<PandillaAPie>().TodosVencidos, "Todos del grupo en KO");
            yield return null;
            Assert.AreEqual(ReglaAcoso.Fase.Vencidos, g.Fase);
            int bonos = 0;
            foreach (InfoBotin b in botines)
            {
                if (b.Dinero == acoso.BonoCivil && Vector3.Distance(b.Posicion, inicioCivil) < 0.05f)
                {
                    bonos++;
                }
            }
            Assert.AreEqual(1, bonos, "Un bono de " + acoso.BonoCivil + " donde estaba el civil");
            Assert.AreEqual(todos.Count, botines.Count - bonos, "Y el dinero de cada pandillero vencido");

            float inicio = Time.time;
            float maxima = 0f;
            while (Time.time - inicio < SegundosHuida)
            {
                yield return null;
                if (g.Civil != null && g.Civil.gameObject.activeSelf)
                {
                    maxima = Mathf.Max(maxima, Vector3.Distance(g.Civil.transform.position, inicioCivil));
                }
            }
            Assert.GreaterOrEqual(maxima, MinimoAlejado, "El civil corre lejos en " + SegundosHuida + " s");
            Assert.IsTrue(g.Civil.Huyendo || g.Civil.Terminado);
        }

        [UnityTest]
        public IEnumerator Acosando_ConAzarBajo_UnEmpujonTiraAlCivilYSeLevanta()
        {
            yield return Calentar();
            Vector3 centro = new Vector3(0f, 0.05f, 0f);
            Teletransportar(new Vector3(0f, 0.05f, 60f));
            GrupoAcoso g = CrearGrupo(centro, 4, false);
            g.InyectarAzar(() => 0f); // periodo mínimo, empujón y caída
            Vector3 inicioCivil = g.Civil.transform.position;
            float inicio = Time.time;
            bool cayo = false;
            float maxDesvio = 0f;
            while (Time.time - inicio < acoso.PeriodoGestoMaximo + 1f)
            {
                yield return null;
                maxDesvio = Mathf.Max(maxDesvio, Vector3.Distance(g.Civil.transform.position, inicioCivil));
                cayo |= g.Civil.Estado == CivilAcosado.Fase.Caido;
            }
            Assert.IsTrue(cayo, "El empujón tira al civil");
            Assert.GreaterOrEqual(maxDesvio, acoso.RetrocesoEmpujon - Margen, "Retrocede unos " + acoso.RetrocesoEmpujon + " m");
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, "Sigue el acoso");
            yield return Esperar(acoso.SegundosCaido + 0.3f);
            Assert.AreNotEqual(CivilAcosado.Fase.Huyendo, g.Civil.Estado);
        }

        [UnityTest]
        public IEnumerator Acosando_ElJugadorSeVaLejos_SigueAcosandoSinPelear()
        {
            yield return Calentar();
            Teletransportar(new Vector3(0f, 0.05f, acoso.DistanciaVista + 20f));
            GrupoAcoso g = CrearGrupo(new Vector3(0f, 0.05f, 0f), 3, false);
            yield return Esperar(1.5f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase);
            Assert.IsFalse(AlguienAtaca(g), "El territorio de la pandilla no los pone en alerta mientras acosan");
        }

        [UnityTest]
        public IEnumerator Acosando_ElJugadorEstaTrasUnMuro_NoLoVen()
        {
            yield return Calentar();
            Caja(new Vector3(0f, 2f, -6f), new Vector3(8f, 4f, 0.5f));
            Physics.SyncTransforms(); // el muro recién creado debe estar en la física antes del primer sondeo del grupo
            Teletransportar(new Vector3(0f, 0.05f, -DistanciaDeFrente));
            GrupoAcoso g = CrearGrupo(new Vector3(0f, 0.05f, 0f), 3, false);
            yield return Esperar(0.8f);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, g.Fase, "La línea de vista está tapada");
        }

        [UnityTest]
        public IEnumerator Civil_SiElJugadorLoGolpea_Huye()
        {
            yield return Calentar();
            Teletransportar(new Vector3(0f, 0.05f, 60f));
            GrupoAcoso g = CrearGrupo(new Vector3(0f, 0.05f, 0f), 3, false);
            ResultadoDanio r = g.Civil.RecibirDanio(new InfoDanio(DanioPrueba, TipoDanio.Golpe, g.Civil.transform.position, Vector3.forward, null));
            Assert.AreEqual(0f, r.DanioAplicado, "No recibe daño");
            Assert.IsTrue(g.Civil.Huyendo);
            yield return Esperar(0.2f);
        }
    }
}
