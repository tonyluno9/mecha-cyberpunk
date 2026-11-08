using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-717: el carro del asaltante de la historia del dinero. Usa el pool del tráfico (<see cref="TraficoCalle.EnviarRefuerzo"/>, T-704/T-714): sale fuera de la vista,
    /// llega junto al jugador, espera a que el asaltante suba y huye por las calles hasta un nodo lejano. Se detiene si le ponchan una llanta (disparo a las ruedas o choque
    /// fuerte), si llega al final de su ruta o si se atora (nunca deja la historia trabada). Se comunica con la historia (Progresion, que no ve Trafico) por puntos de historia
    /// de Core (<see cref="IPuntoHistoria"/>): <c>Historia_Asalto</c> (Activar(true) manda el carro; Activar(false) lo hace huir; su Pose es la del carro),
    /// <c>Historia_Asalto_Listo</c> (registrado mientras el carro espera junto al jugador) e <c>Historia_Asalto_Detenido</c> (registrado cuando ya no va a moverse).
    /// Lo agrega <see cref="GestorPersecucion"/> en su Awake.
    /// </summary>
    public class AsaltoMundo : MonoBehaviour
    {
        public const string IdPunto = "Historia_Asalto";
        public const string IdListo = "Historia_Asalto_Listo";
        public const string IdDetenido = "Historia_Asalto_Detenido";

        public enum Fase { Inactivo, Llegando, Esperando, Huyendo, Detenido }

        public const string IdEscena = "Historia_Asalto_Escena";

        public const float VelocidadLlegada = 16f;
        /// <summary>T-728: velocidad real de persecución (m/s, 50 km/h): por encima del tráfico (8 a 12) y un poco por debajo del carro del jugador (16), así se le puede dar alcance.</summary>
        public const float VelocidadHuida = 14f;
        public const float MetrosHuidaMinimos = 200f;
        public const float MetrosHuidaIdeales = 320f;
        public const float MetrosHuidaMaximos = 500f;
        public const float MetrosHuidaDeRespaldo = 100f;
        public const float DistanciaMinimaSalida = 50f;
        public const float SegundosMaximosHuida = 90f;
        public const float SegundosAtorado = 6f;
        private const float SegundosReintento = 0.5f;
        private const float SegundosMaximosBuscandoCarro = 10f;
        private const float RapidezQuieto = 0.3f;

        private sealed class Punto : IPuntoHistoria
        {
            private readonly AsaltoMundo dueno;
            private readonly Action<bool> alActivar;

            public Punto(AsaltoMundo dueno, string id, Action<bool> alActivar)
            {
                this.dueno = dueno;
                Id = id;
                this.alActivar = alActivar;
            }

            public string Id { get; }
            public Pose Pose => dueno.PoseCarro;
            public void Activar(bool activo) => alActivar?.Invoke(activo);
        }

        private Punto puntoPrincipal;
        private Punto puntoListo;
        private Punto puntoDetenido;
        private CarroTrafico carro;
        private float reintento;
        private float inicioBusqueda;
        private float inicioHuida;
        private float quietoDesde = -1f;

        public Fase Estado { get; private set; } = Fase.Inactivo;

        /// <summary>El carro del asaltante (null mientras no se haya mandado).</summary>
        public CarroTrafico Carro => carro;

        /// <summary>Por qué se detuvo: "ponchado", "llego", "atorado" o "perdido". Vacío si no se ha detenido.</summary>
        public string Causa { get; private set; } = "";

        private Pose PoseCarro => carro != null ? new Pose(carro.transform.position, carro.transform.rotation) : default;

        private void Awake()
        {
            puntoPrincipal = new Punto(this, IdPunto, AlActivar);
            puntoListo = new Punto(this, IdListo, null);
            puntoDetenido = new Punto(this, IdDetenido, null);
        }

        private void OnEnable() => Registro<IPuntoHistoria>.Agregar(puntoPrincipal);

        private void OnDisable()
        {
            Registro<IPuntoHistoria>.Quitar(puntoPrincipal);
            Registro<IPuntoHistoria>.Quitar(puntoListo);
            Registro<IPuntoHistoria>.Quitar(puntoDetenido);
            Soltar();
        }

        private void AlActivar(bool activo)
        {
            if (activo)
            {
                Llamar();
            }
            else
            {
                Huir();
            }
        }

        /// <summary>Manda el carro del pool hacia el jugador. Si no hay carro libre, reintenta unos segundos.</summary>
        public void Llamar()
        {
            if (Estado != Fase.Inactivo && Estado != Fase.Detenido)
            {
                return;
            }
            Soltar();
            Registro<IPuntoHistoria>.Quitar(puntoListo);
            Registro<IPuntoHistoria>.Quitar(puntoDetenido);
            carro = null;
            Causa = "";
            Estado = Fase.Llegando;
            inicioBusqueda = Time.time;
            reintento = 0f;
            IntentarMandar();
        }

        private void IntentarMandar()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            TraficoCalle trafico = FindFirstObjectByType<TraficoCalle>();
            if (jugador == null || jugador.Raiz == null || trafico == null)
            {
                return;
            }
            // T-728: el carro llega adonde asaltan a la víctima (el punto de escena que pone la historia); sin él, junto al jugador.
            Vector3 destino = jugador.Raiz.position;
            foreach (IPuntoHistoria punto in Registro<IPuntoHistoria>.Todos)
            {
                if (punto != null && punto.Id == IdEscena)
                {
                    destino = punto.Pose.position;
                }
            }
            CarroTrafico nuevo = trafico.EnviarRefuerzo(destino, VelocidadLlegada, 8f, 5f, DistanciaMinimaSalida);
            if (nuevo == null)
            {
                return;
            }
            carro = nuevo;
            carro.name = "CarroAsaltante";
            carro.AlLlegarRefuerzo += AlLlegar;
            PonerLlantas(carro);
        }

        // La franja baja del carro: lo que pega en las ruedas.
        private static void PonerLlantas(CarroTrafico carro)
        {
            Bounds cuerpo = default;
            bool hay = false;
            foreach (Collider c in carro.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger)
                {
                    continue;
                }
                if (!hay)
                {
                    cuerpo = c.bounds;
                    hay = true;
                }
                else
                {
                    cuerpo.Encapsulate(c.bounds);
                }
            }
            GameObject llantas = new GameObject("LlantasAsalto");
            llantas.transform.SetParent(carro.transform, false);
            BoxCollider caja = llantas.AddComponent<BoxCollider>();
            Vector3 tamano = hay ? cuerpo.size : new Vector3(1.9f, 1.4f, 4.4f);
            Vector3 centroMundo = hay ? new Vector3(cuerpo.center.x, cuerpo.min.y + 0.25f, cuerpo.center.z) : carro.transform.position;
            llantas.transform.position = centroMundo;
            llantas.transform.rotation = carro.transform.rotation;
            // El carro va sin escala: las medidas del collider son metros. Ancho x largo del cuerpo, alto de 0.5 m.
            float ancho = Mathf.Min(tamano.x, tamano.z);
            float largo = Mathf.Max(tamano.x, tamano.z) * 0.9f;
            caja.size = new Vector3(ancho, 0.5f, largo);
            llantas.AddComponent<LlantaPonchable>().Iniciar(carro);
        }

        private void AlLlegar(CarroTrafico llegado)
        {
            if (llegado != carro)
            {
                return;
            }
            if (Estado == Fase.Llegando)
            {
                Estado = Fase.Esperando;
                Registro<IPuntoHistoria>.Agregar(puntoListo);
            }
            else if (Estado == Fase.Huyendo)
            {
                Detener("llego");
            }
        }

        /// <summary>El asaltante ya subió: el carro sale huyendo a un nodo lejano de las calles.</summary>
        public void Huir()
        {
            if (Estado != Fase.Esperando || carro == null)
            {
                return;
            }
            Registro<IPuntoHistoria>.Quitar(puntoListo);
            IJugador jugador = Registro<IJugador>.Actual;
            int destino = ElegirDestino(carro, jugador != null && jugador.Raiz != null ? jugador.Raiz.position : carro.transform.position);
            if (destino < 0 || !carro.IniciarHuida(destino, VelocidadHuida))
            {
                Detener("perdido");
                return;
            }
            Estado = Fase.Huyendo;
            inicioHuida = Time.time;
            quietoDesde = -1f;
        }

        /// <summary>Elige el nodo de la red de calles de la huida: lejos del jugador y con una ruta de metros razonable. -1 si no hay.</summary>
        public static int ElegirDestino(CarroTrafico carro, Vector3 jugador)
        {
            int mejor = -1;
            float error = float.MaxValue;
            for (int i = 0; i < carro.NodosDeCalle; i++)
            {
                float metros = carro.MetrosHasta(i);
                Vector3 d = carro.PosicionNodo(i) - jugador;
                d.y = 0f;
                if (metros == float.MaxValue || metros < MetrosHuidaMinimos || metros > MetrosHuidaMaximos || d.magnitude < MetrosHuidaMinimos * 0.6f)
                {
                    continue;
                }
                float e = Mathf.Abs(metros - MetrosHuidaIdeales);
                if (e < error)
                {
                    error = e;
                    mejor = i;
                }
            }
            if (mejor >= 0)
            {
                return mejor;
            }
            // Respaldo: la red de calles es chica; si ninguna ruta llega a lo ideal, el nodo más lejano por la calle que esté lejos del jugador.
            float masMetros = MetrosHuidaDeRespaldo;
            for (int i = 0; i < carro.NodosDeCalle; i++)
            {
                float metros = carro.MetrosHasta(i);
                Vector3 d = carro.PosicionNodo(i) - jugador;
                d.y = 0f;
                if (metros != float.MaxValue && metros > masMetros && d.magnitude >= MetrosHuidaDeRespaldo * 0.6f)
                {
                    masMetros = metros;
                    mejor = i;
                }
            }
            return mejor;
        }

        private void Detener(string causa)
        {
            if (Estado == Fase.Detenido)
            {
                return;
            }
            Estado = Fase.Detenido;
            Causa = causa;
            Registro<IPuntoHistoria>.Quitar(puntoListo);
            Registro<IPuntoHistoria>.Agregar(puntoDetenido);
        }

        private void Soltar()
        {
            if (carro != null)
            {
                carro.AlLlegarRefuerzo -= AlLlegar;
            }
        }

        private void Update()
        {
            switch (Estado)
            {
                case Fase.Llegando:
                    if (carro == null)
                    {
                        if (Time.time - inicioBusqueda > SegundosMaximosBuscandoCarro)
                        {
                            Detener("perdido");
                        }
                        else if (Time.time >= reintento)
                        {
                            reintento = Time.time + SegundosReintento;
                            IntentarMandar();
                        }
                    }
                    break;
                case Fase.Huyendo:
                    if (carro == null || !carro.gameObject.activeInHierarchy)
                    {
                        Detener("perdido");
                        break;
                    }
                    if (carro.Ponchado)
                    {
                        if (carro.VelocidadActual <= RapidezQuieto)
                        {
                            Detener("ponchado");
                        }
                        break;
                    }
                    if (Time.time - inicioHuida > SegundosMaximosHuida)
                    {
                        Detener("atorado");
                        break;
                    }
                    if (carro.VelocidadActual <= RapidezQuieto)
                    {
                        quietoDesde = quietoDesde < 0f ? Time.time : quietoDesde;
                        if (Time.time - quietoDesde >= SegundosAtorado)
                        {
                            Detener("atorado");
                        }
                    }
                    else
                    {
                        quietoDesde = -1f;
                    }
                    break;
            }
        }
    }
}
