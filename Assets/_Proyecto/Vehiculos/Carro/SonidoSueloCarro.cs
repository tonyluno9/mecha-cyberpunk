using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// T-1534: sonidos del carro que maneja el jugador según lo que hay debajo y el clima. Un rayo corto hacia abajo lee los nombres del piso (material físico, material,
    /// collider y objeto): grava o tierra suena VEH_LLANTAS_GRAVA en bucle mientras rueda, una junta de puente suena VEH_PUENTE al entrar. Con lluvia y el jugador dentro de
    /// un carro (no una moto) suena CLI_LLUVIA_DENTRO_CARRO en bucle (el clima la prende con <see cref="ReglaLluviaDentroCarro.FuenteLluvia"/>).
    /// Lo agrega CarroViejo solo. Los carros de la IA y del tráfico no lo usan: solo hay una voz de cada uno, la del carro que maneja el jugador.
    /// </summary>
    public class SonidoSueloCarro : MonoBehaviour
    {
        [SerializeField, Min(0.05f), Tooltip("Cada cuántos segundos se lee el piso bajo el carro.")] private float segundosEntreLecturas = 0.2f;
        [SerializeField, Min(0.1f), Tooltip("Largo (m) del rayo hacia abajo desde un poco arriba del carro.")] private float alcanceRayo = 2f;

        private CarroViejo carro;
        private bool esMoto;
        private float siguienteLectura;
        private bool pisoGrava;
        private bool pisoPuente;
        private float segundosDesdePuente = ReglaSueloCarro.SegundosEntrePuente;
        private int bucleGrava = Sonidos.SinBucle;
        private int bucleLluvia = Sonidos.SinBucle;

        /// <summary>El bucle de grava suena (para pruebas).</summary>
        public bool GravaSuena => bucleGrava != Sonidos.SinBucle;

        /// <summary>El bucle de la lluvia dentro del carro suena (para pruebas).</summary>
        public bool LluviaSuena => bucleLluvia != Sonidos.SinBucle;

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            esMoto = GetComponent<ManejoMoto>() != null;
            if (carro == null)
            {
                Debug.LogError("[Vehiculos] SonidoSueloCarro en '" + name + "' necesita un CarroViejo.", this);
                enabled = false;
            }
        }

        private void OnDisable() => Detener();

        private void OnDestroy() => Detener();

        private void Update()
        {
            bool jugador = carro.TieneControl && carro.Ocupado;
            segundosDesdePuente += Time.deltaTime;
            if (!jugador)
            {
                Detener();
                return;
            }
            if (Time.time >= siguienteLectura)
            {
                siguienteLectura = Time.time + segundosEntreLecturas;
                LeerPiso();
            }
            bool grava = ReglaSueloCarro.SuenaGrava(pisoGrava, carro.Rapidez, bucleGrava != Sonidos.SinBucle) && carro.RuedasEnPiso > 0;
            Alternar(ref bucleGrava, grava, ReglaSueloCarro.IdLlantasGrava);
            Alternar(ref bucleLluvia, ReglaLluviaDentroCarro.Suena(true, esMoto, ReglaLluviaDentroCarro.Llueve), ReglaLluviaDentroCarro.Id);
        }

        private void LeerPiso()
        {
            bool eraPuente = pisoPuente;
            pisoGrava = false;
            pisoPuente = false;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit golpe, alcanceRayo, ~0, QueryTriggerInteraction.Ignore)
                && !golpe.collider.transform.IsChildOf(transform))
            {
                Renderer r = golpe.collider.GetComponent<Renderer>();
                string nombre = (golpe.collider.sharedMaterial != null ? golpe.collider.sharedMaterial.name : "") + "|"
                    + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "") + "|"
                    + golpe.collider.name + "|" + golpe.collider.transform.root.name;
                pisoGrava = ReglaSueloCarro.EsGrava(nombre);
                pisoPuente = ReglaSueloCarro.EsPuente(nombre);
            }
            if (ReglaSueloCarro.SuenaPuente(eraPuente, pisoPuente, carro.Rapidez, segundosDesdePuente))
            {
                segundosDesdePuente = 0f;
                Sonidos.ReproducirEn(ReglaSueloCarro.IdPuente, transform.position);
            }
        }

        private void Alternar(ref int bucle, bool debeSonar, string id)
        {
            if (debeSonar && bucle == Sonidos.SinBucle)
            {
                bucle = Sonidos.ReproducirBucle(id, transform);
            }
            else if (!debeSonar && bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucle);
                bucle = Sonidos.SinBucle;
            }
        }

        private void Detener()
        {
            Alternar(ref bucleGrava, false, null);
            Alternar(ref bucleLluvia, false, null);
            pisoGrava = false;
            pisoPuente = false;
        }
    }
}
