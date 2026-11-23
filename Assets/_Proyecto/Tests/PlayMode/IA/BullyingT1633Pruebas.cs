using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Barrio;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Tests.Personaje;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-1633 (T12): el bullying de los SKULLS ya encuentra pandilleros en un GrupoAcoso (antes Acosando los descartaba y nunca salía). Escenario chico: un grupo
    /// de acoso de 3 SKULLS con su civil, otro civil del barrio a 12 m y el jugador lejos. Al forzar la escena el grupo presta 2, empujan al civil y este reacciona;
    /// al terminar vuelven al acoso; si el jugador golpea a uno, su grupo y ellos van contra él.
    /// </summary>
    public class BullyingT1633Pruebas : BaseJugadorAPie
    {
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaDatosAcoso = "Assets/_Proyecto/IA/Datos/DatosAcoso_Base.asset";
        private const string RutaVisualHombre = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab";
        private const string RutaVisualCivil = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_NP07_Pescador_a.prefab";
        private static readonly Vector3 CentroGrupo = new Vector3(45f, 0f, 0f);
        private static readonly Vector3 PosicionVictima = new Vector3(45f, 0f, 12f);
        private const float SegundosMaximosEscena = 60f;

        private GrupoAcoso grupo;
        private PandilleroAPie[] pandilleros;
        private PeatonMundo victima;
        private ConfigPeatones configPeatones;
        private EscenaBarrio escena;
        private readonly List<GameObject> objetos = new List<GameObject>();
        private readonly List<Subtitulos.Linea> subtitulos = new List<Subtitulos.Linea>();
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            EventosRuido.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 1f; // nadie acierta: se mide la IA, no la puntería
            DirectorEscenasBarrio.Habilitado = false; // solo las escenas que forzamos
            subtitulos.Clear();
            Subtitulos.Publicada += subtitulos.Add;
        }

        public override void TearDown()
        {
            Subtitulos.Publicada -= subtitulos.Add;
            DirectorEscenasBarrio.Habilitado = true;
            PistolaPandillero.Azar = azarAnterior;
            foreach (GameObject o in objetos)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }
            objetos.Clear();
            if (escena != null)
            {
                Object.DestroyImmediate(escena);
            }
            if (configPeatones != null)
            {
                Object.DestroyImmediate(configPeatones);
            }
            EventosRuido.LimpiarSuscriptores();
            base.TearDown();
        }

        private static void Asignar(Object objetivo, string campo, Object valor)
        {
            SerializedObject so = new SerializedObject(objetivo);
            so.FindProperty(campo).objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CrearEscenario(int cuantos)
        {
            DatosAcoso acoso = AssetDatabase.LoadAssetAtPath<DatosAcoso>(RutaDatosAcoso);
            DatosPandilleroAPie calaveras = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            Assert.IsNotNull(acoso);
            Assert.IsNotNull(calaveras);
            GameObject raiz = new GameObject("GrupoBullying");
            objetos.Add(raiz);
            raiz.SetActive(false);
            raiz.transform.position = CentroGrupo;
            GameObject civilObj = new GameObject("CivilDelGrupo");
            civilObj.transform.SetParent(raiz.transform, false);
            civilObj.transform.position = CentroGrupo;
            CharacterController cc = civilObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualHombre), civilObj.transform);
            visual.transform.localPosition = Vector3.zero;
            CivilAcosado civil = civilObj.AddComponent<CivilAcosado>();
            Asignar(civil, "datos", acoso);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            for (int i = 0; i < cuantos; i++)
            {
                Vector3 pos = CentroGrupo + Quaternion.Euler(0f, 360f * i / cuantos, 0f) * Vector3.forward * 2f;
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
                Vector3 mira = CentroGrupo - pos;
                mira.y = 0f;
                p.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(mira.normalized, Vector3.up));
                Asignar(p.GetComponent<PandilleroAPie>(), "datos", calaveras);
            }
            PandillaAPie pandilla = raiz.AddComponent<PandillaAPie>();
            Asignar(pandilla, "datos", calaveras);
            SerializedObject soPandilla = new SerializedObject(pandilla);
            soPandilla.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            soPandilla.ApplyModifiedPropertiesWithoutUndo();
            grupo = raiz.AddComponent<GrupoAcoso>();
            Asignar(grupo, "datos", acoso);
            Asignar(grupo, "civil", civil);
            raiz.SetActive(true);
            pandilleros = raiz.GetComponentsInChildren<PandilleroAPie>();

            // Otro civil del barrio (el que va a sufrir el bullying) con su paseo, que la escena le quita y devuelve.
            configPeatones = ScriptableObject.CreateInstance<ConfigPeatones>();
            GameObject c = new GameObject("CivilBarrio");
            objetos.Add(c);
            c.SetActive(false);
            c.transform.position = PosicionVictima;
            c.AddComponent<AnimacionCivilMundo>();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaVisualCivil), c.transform);
            victima = c.AddComponent<PeatonMundo>();
            victima.Preparar(new[] { PosicionVictima, PosicionVictima + new Vector3(200f, 0f, 0f) }, configPeatones, new System.Random(3), () => (Vector3?)null, 0f);
            c.SetActive(true);

            escena = ScriptableObject.CreateInstance<EscenaBarrio>();
            escena.Definir("prueba_t1633", TipoEscenaBarrio.Bullying, IdPandilla.Calaveras, 2, "prueba", true, new[]
            {
                new LineaBarrio(RolBarrio.Agresor1, "SKULL", "¿Y la cuota, jefe?", GestoBarrio.Ninguno, "SKU_INSULTO"),
                new LineaBarrio(RolBarrio.Victima, "CIVIL", "Ya pagué ayer...", GestoBarrio.Suplicar),
                new LineaBarrio(RolBarrio.Agresor2, "SKULL", "Pues hoy pagas doble.", GestoBarrio.Empujar, "SKU_PROVOCAR"),
                new LineaBarrio(RolBarrio.Agresor1, "SKULL", "Jaja.", GestoBarrio.Tumbar, "SKU_RISA_BURLA")
            });
        }

        private int Prestados()
        {
            int n = 0;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (grupo.EsPrestado(p))
                {
                    n++;
                }
            }
            return n;
        }

        private static bool Reacciona(PeatonMundo v) => v.Huyendo || v.Sobresaltado || v.Agachado || v.LlamandoPolicia || v.Nervioso;

        private bool reaccionVista;

        private IEnumerator EsperarHasta(System.Func<bool> condicion, float segundos)
        {
            float fin = Time.time + segundos;
            while (!condicion() && Time.time < fin)
            {
                reaccionVista |= victima != null && Reacciona(victima);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Bullying_ConGrupoDeAcoso_PrestaDosEmpujanAlCivilYRegresan()
        {
            CrearEscenario(3);
            yield return null;
            yield return null;
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, grupo.Fase);
            Assert.AreEqual(2, grupo.CupoPrestable, "3 en pie: se pueden prestar 2 (queda 1)");
            string r = DirectorEscenasBarrio.ForzarEscena(escena);
            Assert.IsTrue(r.StartsWith("ok"), "el bullying encuentra pandilleros del grupo de acoso: " + r);
            yield return null;
            Assert.AreEqual(2, Prestados(), "el grupo presta 2 pandilleros");
            Assert.IsTrue(DirectorEscenasBarrio.HayEscenaEnCurso);
            bool civilSujeto = false;
            bool reacciono = false;
            float fin = Time.time + SegundosMaximosEscena;
            while (DirectorEscenasBarrio.HayEscenaEnCurso && Time.time < fin)
            {
                civilSujeto |= !victima.enabled;
                reacciono |= Reacciona(victima);
                yield return null;
            }
            yield return EsperarHasta(() => Reacciona(victima), 3f);
            reacciono |= Reacciona(victima);
            Assert.IsFalse(DirectorEscenasBarrio.HayEscenaEnCurso, "la escena terminó sola");
            Assert.IsTrue(civilSujeto, "durante la escena el civil estaba sujeto (sin paseo)");
            Assert.IsTrue(victima.enabled, "al terminar recupera su paseo");
            Assert.IsTrue(reacciono, "el civil reacciona y huye al soltarlo");
            // T-1655 (T32): los SKULLS ya no hablan ni ponen subtítulo; solo queda la línea de la víctima.
            Assert.IsFalse(subtitulos.Exists(s => s.Hablante == "SKULL"), "los SKULLS no ponen subtítulo");
            Assert.GreaterOrEqual(subtitulos.Count, 1, "la víctima sí habla");
            Assert.AreEqual(0, Prestados(), "los pandilleros regresan al grupo");
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, grupo.Fase, "el grupo sigue acosando a su civil");
            foreach (PandilleroAPie p in pandilleros)
            {
                Assert.IsTrue(p.Acosando, "vuelven a Acosando");
                Assert.IsFalse(p.EsHostil);
            }
        }

        [UnityTest]
        public IEnumerator Bullying_ConSoloUnoLibre_NoSeArmaPorqueElGrupoConservaAlguien()
        {
            CrearEscenario(1);
            yield return null;
            string r = DirectorEscenasBarrio.ForzarEscena(escena);
            Assert.IsFalse(r.StartsWith("ok"), "un grupo de 1 no presta a su único pandillero: " + r);
            Assert.AreEqual(0, Prestados());
        }

        [UnityTest]
        public IEnumerator Bullying_SiElJugadorGolpeaAUnPrestado_ElGrupoYElVanContraElJugadorYElCivilHuye()
        {
            CrearEscenario(3);
            yield return null;
            yield return null;
            string r = DirectorEscenasBarrio.ForzarEscena(escena);
            Assert.IsTrue(r.StartsWith("ok"), r);
            yield return null;
            PandilleroAPie agresor = null;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (grupo.EsPrestado(p))
                {
                    agresor = p;
                    break;
                }
            }
            Assert.IsNotNull(agresor);
            // Que ya estén cerca de la víctima (empezó el diálogo) antes de pegarles.
            yield return EsperarHasta(() => subtitulos.Count > 0, 30f);
            Assert.Greater(subtitulos.Count, 0, "empezaron a hablar");
            // El jugador se acerca a pegar (queda a ~6 m del agresor y a menos de 20 m del civil).
            jugador.transform.position = agresor.transform.position + new Vector3(-6f, 0.05f, 0f);
            Physics.SyncTransforms();
            agresor.RecibirDanio(new InfoDanio(5f, TipoDanio.Golpe, agresor.transform.position + Vector3.up, Vector3.forward, null));
            yield return EsperarHasta(() => !DirectorEscenasBarrio.HayEscenaEnCurso, 3f);
            Assert.IsFalse(DirectorEscenasBarrio.HayEscenaEnCurso, "la escena se corta al intervenir el jugador");
            Assert.IsTrue(agresor.EsHostil || agresor.Alerta, "el pandillero va contra el jugador");
            Assert.IsFalse(agresor.Acosando);
            yield return EsperarHasta(() => grupo.Fase == ReglaAcoso.Fase.Alerta, 3f);
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, grupo.Fase, "su grupo pasa a alerta");
            yield return EsperarHasta(() => Reacciona(victima), 3f);
            Assert.IsTrue(victima.enabled, "el civil recupera su paseo");
            reaccionVista |= Reacciona(victima);
            Assert.IsTrue(reaccionVista, "el civil huye");
        }
    }
}
