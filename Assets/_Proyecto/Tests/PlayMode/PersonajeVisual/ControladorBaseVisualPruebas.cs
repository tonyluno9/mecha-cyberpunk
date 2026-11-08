using System.Collections;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-561: el controlador base humanoide jugando, sobre un visual solo (sin el juego): estados de la capa base y de la de arriba
    /// al saltar, cubrirse, apuntar, disparar, recargar, golpear, recibir daño y caer.
    /// </summary>
    public class ControladorBaseVisualPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_man.prefab";
        private GameObject suelo;
        private GameObject raiz;
        private VisualPersonaje visual;
        private Animator animador;
        private int fpsAnterior;
        private int vsyncAnterior;

        [SetUp]
        public void SetUp()
        {
            fpsAnterior = Application.targetFrameRate;
            vsyncAnterior = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(50f, 1f, 50f);
            raiz = new GameObject("PadreVisual");
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            visual = Object.Instantiate(prefab, raiz.transform).GetComponent<VisualPersonaje>();
            visual.LeerDelPadre = false;
            animador = visual.Animador;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = fpsAnterior;
            QualitySettings.vSyncCount = vsyncAnterior;
            Object.Destroy(suelo);
            Object.Destroy(raiz);
        }

        private bool EnEstado(int capa, string nombre)
        {
            AnimatorStateInfo actual = animador.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = animador.GetNextAnimatorStateInfo(capa);
            return animador.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        private bool Base(string nombre) => EnEstado(0, nombre);

        private bool Arriba(string nombre) => EnEstado(1, nombre);

        private IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 3f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator Quieto_EmpiezaEnIdleYTrae3CapasConLaDeArribaVacia()
        {
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(2, animador.layerCount, "Base y Arriba (T-733: sin capa Brazos)");
            Assert.IsTrue(Base("Idle"));
            Assert.IsTrue(Arriba("Vacio"));
            Assert.AreEqual(1f, visual.PesoCapaArriba, 0.01f);
        }

        [UnityTest]
        public IEnumerator Saltar_PasaPorInicioAireYAterrizaje()
        {
            yield return new WaitForSeconds(0.2f);
            visual.EnSuelo = false;
            yield return Hasta(() => Base("SaltoInicio"), "SaltoInicio");
            yield return Hasta(() => Base("SaltoAire"), "SaltoAire");
            visual.EnSuelo = true;
            yield return Hasta(() => Base("SaltoAterrizaje"), "SaltoAterrizaje");
            yield return Hasta(() => Base("Idle"), "Idle al terminar de aterrizar");
        }

        [UnityTest]
        public IEnumerator CaerRapido_MuestraLaCaidaYAlTocarPisoAterriza()
        {
            yield return new WaitForSeconds(0.2f);
            visual.EnSuelo = false;
            for (int i = 0; i < 60 && !Base("Caida"); i++)
            {
                animador.SetFloat(ReglasAnimacion.VelY, -ReglasAnimacion.VelocidadCaidaLibre - 3f);
                yield return null;
            }
            Assert.IsTrue(Base("Caida"));
            visual.EnSuelo = true;
            yield return Hasta(() => Base("SaltoAterrizaje"), "SaltoAterrizaje");
        }

        [UnityTest]
        public IEnumerator Caminar_ConVelocidadYDireccion_EligeCaminarYCorrer_ConLosPiesEnElSitio()
        {
            visual.Velocidad = 3f;
            yield return Hasta(() => Base("Caminar"), "Caminar");
            visual.Velocidad = 8f;
            yield return Hasta(() => Base("Correr"), "Correr");
            // En el sitio: la cadera no se aleja de la raíz mientras corre (el clip no desliza al personaje).
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            float fin = Time.time + 1.6f;
            while (Time.time < fin)
            {
                Vector3 local = visual.transform.InverseTransformPoint(cadera.position);
                minZ = Mathf.Min(minZ, local.z);
                maxZ = Mathf.Max(maxZ, local.z);
                yield return null;
            }
            Assert.Less(maxZ - minZ, 0.6f, "La cadera no avanza con el clip (m)");
            visual.Velocidad = 0f;
            yield return Hasta(() => Base("Idle"), "Idle");
        }

        [UnityTest]
        public IEnumerator Quieto_GirandoRapido_GiraEnElSitio()
        {
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < 60 && !Base("GirarDer"); i++)
            {
                animador.SetFloat(ReglasAnimacion.Giro, 150f);
                yield return null;
            }
            Assert.IsTrue(Base("GirarDer"), "GirarDer");
            animador.SetFloat(ReglasAnimacion.Giro, 0f);
            yield return Hasta(() => Base("Idle"), "Idle al terminar de girar");
            for (int i = 0; i < 300 && !Base("GirarIzq"); i++)
            {
                animador.SetFloat(ReglasAnimacion.Giro, -150f);
                yield return null;
            }
            Assert.IsTrue(Base("GirarIzq"), "GirarIzq");
        }

        [UnityTest]
        public IEnumerator Pistola_IdleApuntarDisparoYRecarga_EnLaCapaDeArriba()
        {
            visual.TipoArma = TipoArmaVisual.Pistola;
            yield return Hasta(() => Arriba("Vacio"), "T-671: sin apuntar, la pistola no usa pose de arma");
            visual.Apuntando = true;
            yield return Hasta(() => Arriba("PistolaApuntar"), "PistolaApuntar");
            visual.Apuntando = false;
            yield return Hasta(() => Arriba("Vacio"), "Vacio otra vez al soltar", 5f);
            visual.Apuntando = true;
            yield return Hasta(() => Arriba("PistolaApuntar"), "PistolaApuntar");
            visual.Disparar();
            yield return Hasta(() => Arriba("PistolaDisparo"), "PistolaDisparo");
            yield return Hasta(() => Arriba("PistolaApuntar"), "vuelve a apuntar");
            visual.Recargar();
            yield return Hasta(() => Arriba("PistolaRecargar"), "PistolaRecargar quieto");
            yield return Hasta(() => Arriba("PistolaApuntar"), "vuelve a apuntar", 5f);
            visual.Velocidad = 4f;
            visual.Recargar();
            yield return Hasta(() => Arriba("PistolaRecargarCaminar"), "PistolaRecargarCaminar caminando");
            visual.Apuntando = false;
            visual.TipoArma = TipoArmaVisual.Ninguna;
            yield return Hasta(() => Arriba("Vacio"), "Vacio sin arma", 5f);
        }

        [UnityTest]
        public IEnumerator ArmaLarga_IdleApuntarDisparoYRecarga()
        {
            visual.TipoArma = TipoArmaVisual.ArmaLarga;
            yield return Hasta(() => Arriba("Vacio"), "T-671: sin apuntar, el arma larga no usa pose de arma");
            visual.Apuntando = true;
            yield return Hasta(() => Arriba("LargaApuntar"), "LargaApuntar");
            visual.Apuntando = false;
            yield return Hasta(() => Arriba("Vacio"), "Vacio otra vez al soltar", 5f);
            visual.Apuntando = true;
            yield return Hasta(() => Arriba("LargaApuntar"), "LargaApuntar");
            visual.Disparar();
            yield return Hasta(() => Arriba("LargaDisparo"), "LargaDisparo");
            yield return Hasta(() => Arriba("LargaApuntar"), "vuelve a apuntar");
            visual.Recargar();
            yield return Hasta(() => Arriba("LargaRecargar"), "LargaRecargar");
        }

        [UnityTest]
        public IEnumerator Punos_AlternanIzquierdaDerechaYFuerte()
        {
            yield return new WaitForSeconds(0.2f);
            visual.Golpear(false);
            yield return Hasta(() => Arriba("GolpeIzq"), "GolpeIzq");
            yield return Hasta(() => Arriba("Vacio"), "Vacio");
            visual.Golpear(false);
            yield return Hasta(() => Arriba("GolpeDer"), "GolpeDer");
            yield return Hasta(() => Arriba("Vacio"), "Vacio");
            visual.Golpear(true);
            yield return Hasta(() => Arriba("GolpeFuerte"), "GolpeFuerte");
        }

        [UnityTest]
        public IEnumerator Dano_DeFrenteDeEspaldaYFuerte_ElijeLaReaccion()
        {
            yield return new WaitForSeconds(0.2f);
            visual.Herir(-visual.transform.forward);
            yield return Hasta(() => Arriba("DanoFrente"), "DanoFrente");
            yield return Hasta(() => Arriba("Vacio"), "Vacio");
            visual.Herir(visual.transform.forward);
            yield return Hasta(() => Arriba("DanoEspalda"), "DanoEspalda");
            yield return Hasta(() => Arriba("Vacio"), "Vacio");
            visual.Herir(visual.transform.forward, true);
            yield return Hasta(() => Arriba("DanoFuerte"), "DanoFuerte");
        }

        [UnityTest]
        public IEnumerator Cubrirse_EntraSeQuedaDisparaDesdeLaCoberturaYSale()
        {
            yield return new WaitForSeconds(0.2f);
            visual.Cubierto = true;
            yield return Hasta(() => Base("CoberturaEntrar"), "CoberturaEntrar");
            yield return Hasta(() => Base("CoberturaMantener"), "CoberturaMantener");
            yield return Hasta(() => visual.PesoCapaArriba < 0.05f, "la capa de arriba se apaga cubierto");
            visual.Disparar();
            yield return Hasta(() => Base("CoberturaDisparo1") || Base("CoberturaDisparo2"), "disparo desde cobertura");
            yield return Hasta(() => Base("CoberturaMantener"), "vuelve a la cobertura");
            visual.Cubierto = false;
            yield return Hasta(() => Base("Idle"), "Idle al salir");
            yield return Hasta(() => visual.PesoCapaArriba > 0.95f, "la capa de arriba vuelve");
        }

        [UnityTest]
        public IEnumerator Agachado_ConRifleCaminando_UsaAgachadoCaminar()
        {
            visual.TipoArma = TipoArmaVisual.ArmaLarga;
            visual.Agachado = true;
            visual.Velocidad = 2f;
            yield return Hasta(() => Base("AgachadoCaminar"), "AgachadoCaminar");
            yield return Hasta(() => visual.PesoCapaArriba < 0.05f, "el torso lo lleva el clip completo");
            visual.Agachado = false;
            yield return Hasta(() => !Base("AgachadoCaminar"), "se levanta");
        }

        [UnityTest]
        public IEnumerator Caer_DeFrenteConAzarBajo_CaeDeEspaldasYSeLevantaBocaArriba()
        {
            return CaerYLevantarse(-1f, 0f, MuerteVisual.Atras, "MuerteAtras", "LevantarseBocaArriba");
        }

        [UnityTest]
        public IEnumerator Caer_DeFrenteConAzarAlto_SeDesmayaYSeLevantaBocaArriba()
        {
            return CaerYLevantarse(-1f, 0.99f, MuerteVisual.Desmayo, "MuerteDesmayo", "LevantarseBocaArriba");
        }

        [UnityTest]
        public IEnumerator Caer_DeEspalda_CaeDeFrenteYSeLevantaBocaAbajo()
        {
            return CaerYLevantarse(1f, 0f, MuerteVisual.Adelante, "MuerteAdelante", "LevantarseBocaAbajo");
        }

        private IEnumerator CaerYLevantarse(float signoGolpe, float azar, MuerteVisual esperada, string estado, string levantar)
        {
            yield return new WaitForSeconds(0.2f);
            visual.InyectarAzar(() => azar);
            visual.RegistrarGolpe(visual.transform.forward * signoGolpe);
            visual.Caido = true;
            Assert.AreEqual(esperada, visual.Muerte);
            yield return Hasta(() => Base(estado), estado);
            yield return Hasta(() => visual.PesoCapaArriba < 0.05f, "la capa de arriba se apaga caído");
            Assert.AreEqual(0f, visual.Tumbado, 0.001f, "Con clips de muerte el cuerpo no se gira a mano");
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(Base(estado), "Se queda en la última pose");
            visual.Caido = false;
            yield return Hasta(() => Base(levantar), levantar);
            yield return Hasta(() => Base("Idle"), "Idle", 12f);
        }

        [UnityTest]
        public IEnumerator IKDePiesYMirada_ApoyaLosPiesYVoltea_LaCabezaAlObjetivo()
        {
            IKPiesYMirada ik = animador.GetComponent<IKPiesYMirada>();
            Assert.IsNotNull(ik);
            GameObject objetivo = new GameObject("ObjetivoMirada");
            objetivo.transform.position = visual.transform.position + visual.transform.right * 3f + Vector3.up * 1.5f + visual.transform.forward * 1f;
            try
            {
                ik.Objetivo = objetivo.transform;
                yield return new WaitForSeconds(1.2f);
                Assert.Greater(ik.PesoPies, 0.95f, "Pies apoyados por IK");
                Assert.Greater(ik.PesoMirada, 0.95f, "Mirando al objetivo");
                Transform cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
                Vector3 haciaObjetivo = (objetivo.transform.position - cabeza.position).normalized;
                Assert.Greater(Vector3.Dot(visual.transform.forward, haciaObjetivo), 0.2f, "El objetivo queda al lado, no al frente");
                Assert.Greater(Vector3.Dot(cabeza.forward, haciaObjetivo), Vector3.Dot(visual.transform.forward, haciaObjetivo), "La cabeza mira más al objetivo que el cuerpo");
                ik.Objetivo = null;
                yield return new WaitForSeconds(1.2f);
                Assert.Less(ik.PesoMirada, 0.05f, "Sin objetivo deja de mirar");
            }
            finally
            {
                Object.Destroy(objetivo);
            }
        }
    }
}
