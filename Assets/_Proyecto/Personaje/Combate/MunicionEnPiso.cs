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
        private const float RadioRecoger = 1.3f;
        private const float AlturaFlotar = 0.3f;
        private const float AmplitudFlotar = 0.05f;
        private const float GiroPorSegundo = 100f;
        private const float SegundosAntesDeRecoger = 0.6f;
        private static readonly ArmaAPie[] Limitadas = { ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper };

        private Transform modelo;
        private float recogibleDesde;
        private bool tomada;

        public bool FueTomada => tomada;

        /// <summary>Se dispara al recoger: el arma que recibió las balas y cuántas entraron.</summary>
        public static event Action<ArmaAPie, int> Recogida;

        /// <summary>El arma limitada que recibiría las balas ahora (la de la mano si lo es; si no, la primera que tenga), o null si no tiene ninguna.</summary>
        public static ArmaAPie? ArmaDestino()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return null;
            }
            ArmasAPie armas = UnityEngine.Object.FindFirstObjectByType<ArmasAPie>();
            if (armas != null && Array.IndexOf(Limitadas, armas.ArmaActiva) >= 0 && arsenal.Tiene(armas.ArmaActiva))
            {
                return armas.ArmaActiva;
            }
            foreach (ArmaAPie arma in Limitadas)
            {
                if (arsenal.Tiene(arma))
                {
                    return arma;
                }
            }
            return null;
        }

        public static MunicionEnPiso Crear(Vector3 posicionPiso)
        {
            var raiz = new GameObject("MunicionEnPiso");
            raiz.transform.position = posicionPiso;
            var municion = raiz.AddComponent<MunicionEnPiso>();
            municion.recogibleDesde = Time.time + SegundosAntesDeRecoger;
            municion.Armar();
            return municion;
        }

        private void Armar()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MunicionEnPiso" };
            Color naranja = new Color(1f, 0.65f, 0.15f);
            material.SetColor("_BaseColor", naranja * 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", naranja * 1.2f);
            modelo = new GameObject("Caja").transform;
            modelo.SetParent(transform, false);
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
            Light luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = Vector3.up * AlturaFlotar;
            luz.type = LightType.Point;
            luz.color = naranja;
            luz.intensity = 0.5f;
            luz.range = 1.2f;
            luz.shadows = LightShadows.None;
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
            Vector3 d = jugador.Raiz.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= RadioRecoger * RadioRecoger)
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
            ArmaAPie? destino = ArmaDestino();
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            if (destino == null || reserva == null || reserva.Espacio(destino.Value) <= 0)
            {
                return;
            }
            DatosMunicionArma datos = ReservaBalas.Datos(destino.Value);
            int entraron = reserva.Agregar(destino.Value, datos != null ? datos.BalasPorPaquete : 6);
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
