using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Balas soltadas por un enemigo o policía (T-746): una cajita que gira y flota; se recoge al pasar cerca y suma a la
    /// <see cref="ReservaBalas"/> del arma limitada que tiene el jugador (la que lleva en la mano primero). El aviso en pantalla
    /// lo pone el HUD escuchando <see cref="Recogida"/>. Modelo provisional hecho en código.
    /// </summary>
    public class MunicionEnPiso : MonoBehaviour
    {
        /// <summary>Prefab en Resources (lo crea ModelosPickupsT1523 en Personaje/Editor).</summary>
        public const string RecursoModelo = "MunicionEnPiso_Modelo";
        /// <summary>T-drops-24nov: cargador real (pieza Cargador de W05) a escala de pickup; lo crea RecompensasEnemigoT24nov. Si falta, la caja CB03.</summary>
        public const string RecursoCargador = "RecompensaCargador_Modelo";
        private const float AlturaFlotar = 0.3f;
        private const float AmplitudFlotar = 0.05f;
        private const float GiroPorSegundo = 100f;
        private const float SegundosAntesDeRecoger = 0.6f;
        private const int BalasSinDatos = 12; // T92: sin datos del arma, un paquete de 12 (antes 6)
        private Transform modelo;
        private ArmaAPie? preferida;
        private float recogibleDesde;
        private bool tomada;

        public bool FueTomada => tomada;

        /// <summary>Se dispara al recoger: el arma que recibió las balas y cuántas entraron.</summary>
        public static event Action<ArmaAPie, int> Recogida;

        /// <summary>El arma limitada que recibiría las balas ahora (la de la mano si lo es; si no, la primera que tenga), o null si no tiene ninguna.</summary>
        public static ArmaAPie? ArmaDestino() => ArmaDestino(null);

        /// <summary>
        /// T-drops-24nov: como <see cref="ArmaDestino()"/>, pero antes que la de la mano prueba <paramref name="delEnemigo"/> (el arma del enemigo
        /// que soltó las balas) si el jugador la tiene. Ver <see cref="ReglaRecompensasEnemigo.ElegirArma"/>.
        /// </summary>
        public static ArmaAPie? ArmaDestino(ArmaAPie? delEnemigo)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return null;
            }
            ArmasAPie armas = UnityEngine.Object.FindFirstObjectByType<ArmasAPie>();
            return ReglaRecompensasEnemigo.ElegirArma(delEnemigo, armas != null ? armas.ArmaActiva : ArmaAPie.Punos, arsenal.Tiene);
        }

        public static MunicionEnPiso Crear(Vector3 posicionPiso) => Crear(posicionPiso, null);

        /// <summary>T-drops-24nov: balas que prefieren ir al arma del enemigo que las soltó (si el jugador la tiene).</summary>
        public static MunicionEnPiso Crear(Vector3 posicionPiso, ArmaAPie? delEnemigo)
        {
            var raiz = new GameObject("MunicionEnPiso");
            raiz.transform.position = posicionPiso;
            var municion = raiz.AddComponent<MunicionEnPiso>();
            municion.preferida = delEnemigo;
            municion.recogibleDesde = Time.time + SegundosAntesDeRecoger;
            municion.Armar();
            return municion;
        }

        private void Armar()
        {
            ConfigRecompensasEnemigo config = ConfigRecompensasEnemigo.Actual;
            Color naranja = config.ColorMunicion;
            modelo = new GameObject("Caja").transform;
            modelo.SetParent(transform, false);
            GameObject prefab = Resources.Load<GameObject>(RecursoCargador);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(RecursoModelo);
            }
            if (prefab != null)
            {
                // T-1523: modelo real (la caja de metal CB03 a escala de pickup; no hay un modelo de cargador o caja de balas en Assets/Arte).
                GameObject instancia = Instantiate(prefab, modelo, false);
                instancia.name = "CajaMunicion";
                foreach (Collider c in instancia.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
            }
            else
            {
                ArmarRespaldo(naranja);
            }
            Light luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = Vector3.up * AlturaFlotar;
            luz.type = LightType.Point;
            luz.color = naranja;
            luz.intensity = config.IntensidadBrillo;
            luz.range = config.AlcanceBrillo;
            luz.shadows = LightShadows.None;
        }

        /// <summary>Respaldo si falta el prefab en Resources: tres cápsulas (solo debería verse si alguien borró el recurso).</summary>
        private void ArmarRespaldo(Color naranja)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MunicionEnPiso" };
            material.SetColor("_BaseColor", naranja * 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", naranja * 1.2f);
            for (int i = 0; i < 3; i++)
            {
                GameObject bala = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bala.name = "Bala" + i;
                Destroy(bala.GetComponent<Collider>());
                bala.transform.SetParent(modelo, false);
                bala.transform.localPosition = new Vector3((i - 1) * 0.1f, 0f, 0f);
                bala.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                bala.transform.localScale = new Vector3(0.06f, 0.09f, 0.06f);
                bala.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private void Update()
        {
            if (tomada)
            {
                return;
            }
            if (modelo != null)
            {
                modelo.localPosition = Vector3.up * (AlturaFlotar + Mathf.Sin(Time.time * 4f) * AmplitudFlotar);
                modelo.Rotate(0f, GiroPorSegundo * Time.deltaTime, 0f, Space.Self);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (Time.time < recogibleDesde || jugador == null || jugador.Raiz == null)
            {
                return;
            }
            // T-drops-24nov: al pasar con el cuerpo o, en VR, al acercar una mano.
            ConfigRecompensasEnemigo config = ConfigRecompensasEnemigo.Actual;
            int n = ManosRecogida.Posiciones(out Vector3[] manos);
            if (ReglaRecompensasEnemigo.Cerca(transform.position, jugador.Raiz.position, config.RadioRecoger, manos, n, config.RadioMano))
            {
                Recoger();
            }
        }

        /// <summary>Suma las balas a la reserva y desaparece. Si el jugador no tiene un arma que las use, se queda en el piso.</summary>
        public void Recoger()
        {
            if (tomada)
            {
                return;
            }
            ArmaAPie? destino = ArmaDestino(preferida);
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            if (destino == null || reserva == null || reserva.Espacio(destino.Value) <= 0)
            {
                return;
            }
            DatosMunicionArma datos = ReservaBalas.Datos(destino.Value);
            int entraron = reserva.Agregar(destino.Value, datos != null ? datos.BalasPorPaquete : BalasSinDatos);
            if (entraron <= 0)
            {
                return;
            }
            tomada = true;
            Sonidos.ReproducirEn("ECO_ARMA_RECOGER", transform.position);
            Recogida?.Invoke(destino.Value, entraron);
            Destroy(gameObject);
        }
    }
}
