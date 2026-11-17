using System.Collections;
using System.Linq;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1310: clips de Blender de la ronda 2. Necesitan que se haya corrido ImportarClipsRonda2T1310.Aplicar (crea Resources/ConfigAnimacionParkour y reconstruye el controlador).
    /// </summary>
    public class ClipsRonda2T1310Pruebas : BaseJugadorAPie
    {
        private const float ZBarda = 3f;
        private const float AlturaBarda = 1f;
        private const float GrosorBarda = 0.2f;
        private const float AlturaCaida = 8f;
        private const float MaxSegundos = 6f;

        public override void Setup()
        {
            base.Setup();
            MapasDeAccionesDePrueba.Reiniciar(teclado);
            controlador.enabled = false;
            controlador.enabled = true;
        }

        public override void TearDown()
        {
            MapasDeAccionesDePrueba.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDeAccionesDePrueba.DespuesDelTearDown();
        }

        private void Teletransportar(Vector3 posicion)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = posicion;
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        [Test]
        public void Config_TieneLosClipsNuevos()
        {
            ConfigAnimacionParkour config = ConfigAnimacionParkour.Actual;
            Assert.IsNotNull(config, "Falta Resources/ConfigAnimacionParkour (corre ImportarClipsRonda2T1310.Aplicar)");
            Assert.IsNotNull(config.SaltarBarda, "saltarBarda");
            Assert.IsNotNull(config.TreparBorde, "treparBorde");
            Assert.IsNotNull(config.RodarCaida, "rodarCaida");
            Assert.IsNotNull(config.CarroSacarConductor, "carroSacarConductor");
            Assert.IsNotNull(config.ConductorSerSacado, "conductorSerSacado");
            // Los dos del robo empiezan juntos y duran lo mismo (80 cuadros a 30 fps).
            Assert.AreEqual(config.CarroSacarConductor.length, config.ConductorSerSacado.length, 0.05f);
        }

        [Test]
        public void Clips_CoberturaRifleYMotoBajar_ExistenEnElProyecto()
        {
            foreach (string nombre in new[] { "PJ01_CoberturaRifle_Entrar", "PJ01_CoberturaRifle_Idle", "PJ01_CoberturaRifle_Disparo", "PJ01_Moto_Bajar", "PJ01_Loco_Idle", "PJ01_Caminar_SinImplante" })
            {
                string ruta = "Assets/Arte/Personajes/PJ01/Clips/" + nombre + ".fbx";
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().FirstOrDefault(c => c.name == nombre);
                Assert.IsNotNull(clip, "Falta el clip " + nombre);
            }
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Arte/Personajes/PJ01/Clips/Limpios/PJ01_Moto_Bajar.anim"),
                "La copia limpia vieja de Moto_Bajar ya no se usa");
        }

        [UnityTest]
        public IEnumerator Barda_SaltarConEspacio_DejaAlJugadorDelOtroLado()
        {
            yield return Calentar();
            Assert.IsNotNull(ConfigAnimacionParkour.Actual, "Falta ConfigAnimacionParkour");
            Caja(new Vector3(0f, AlturaBarda / 2f, ZBarda), new Vector3(4f, AlturaBarda, GrosorBarda));
            Teletransportar(new Vector3(0f, 0.05f, ZBarda - GrosorBarda / 2f - 0.5f));
            yield return Esperar(0.5f);
            TreparAPie trepar = jugador.GetComponent<TreparAPie>();
            Assert.IsNotNull(trepar);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            bool salto = false;
            float fin = Time.time + MaxSegundos;
            while (Time.time < fin)
            {
                salto |= trepar.SaltandoBarda;
                if (salto && !trepar.Trepando)
                {
                    break;
                }
                yield return null;
            }
            yield return Esperar(0.5f);
            Assert.IsTrue(salto, "Debe haber saltado la barda");
            Assert.Greater(jugador.transform.position.z, ZBarda + GrosorBarda / 2f, "Debe quedar del otro lado de la barda");
            Assert.Less(jugador.transform.position.y, 0.3f, "Debe quedar en el piso, no encima de la barda");
        }

        [UnityTest]
        public IEnumerator Caida_DeOchoMetros_NoMataYRueda()
        {
            yield return Calentar();
            Assert.IsNotNull(ConfigAnimacionParkour.Actual?.RodarCaida, "Falta el clip RodarCaida en la config");
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            CaidaJugador caida = jugador.GetComponent<CaidaJugador>();
            Assert.IsNotNull(vida);
            Assert.IsNotNull(caida, "TreparAPie debe agregar CaidaJugador");
            yield return Esperar(0.3f);
            float antes = vida.Actual;
            Teletransportar(new Vector3(0f, AlturaCaida, 0f));
            bool roto = false;
            string clip = null;
            float fin = Time.time + MaxSegundos;
            while (Time.time < fin)
            {
                if (caida.Rodando)
                {
                    roto = true;
                    ReproductorClipUnico r = jugador.GetComponentInChildren<ReproductorClipUnico>(true);
                    clip = r != null && r.ClipActual != null ? r.ClipActual.name : clip;
                }
                yield return null;
            }
            Assert.GreaterOrEqual(caida.UltimaCaida, AlturaCaida - 1f, "Debe haber medido la caída");
            Assert.IsFalse(vida.EstaKO, "Una caída de 8 m no mata");
            Assert.Greater(vida.Actual, 0f);
            Assert.Less(vida.Actual, antes, "Debe doler un poco");
            Assert.IsTrue(roto, "Debe rodar al aterrizar");
            Assert.AreEqual("PJ01_Parkour_RodarCaida", clip);
        }
    }
}
