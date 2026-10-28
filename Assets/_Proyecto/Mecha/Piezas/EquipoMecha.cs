using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha
{
    /// <summary>
    /// Las 5 piezas puestas en el mecha del jugador (T-167). Los valores del mecha son los de serie más lo que suman las piezas
    /// (<see cref="ReglaEstadisticasPiezas"/>); al cambiar una pieza se entregan a <see cref="VidaMecha.CambiarDatos"/>, que conserva
    /// el daño en puntos. Va en el mismo objeto que MechaPersistente (solo en MechaEstacionado.prefab). No toca inventario ni dinero.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    public class EquipoMecha : MonoBehaviour
    {
        private const int NumeroRanuras = 5;

        [SerializeField] private CatalogoPiezas catalogo;

        private VidaMecha vida;
        private ValoresMecha deSerie;
        private DatosMecha datosEjecucion;
        private DatosPieza[] puestas;
        private bool listo;
        private float reaccion = 1f;
        private IImplantes implantesEscuchados;

        /// <summary>Avisa cuando cambia alguna pieza puesta.</summary>
        public event Action Cambio;

        /// <summary>Las 5 piezas puestas, en el orden de <see cref="RanuraPieza"/>.</summary>
        public IReadOnlyList<DatosPieza> Puestas
        {
            get
            {
                Asegurar();
                return puestas ?? Array.Empty<DatosPieza>();
            }
        }

        /// <summary>Los valores compuestos del mecha con las piezas puestas.</summary>
        public ValoresMecha Valores
        {
            get
            {
                Asegurar();
                return puestas == null ? deSerie : Componer();
            }
        }

        public CatalogoPiezas Catalogo => catalogo;

        private void Awake()
        {
            Asegurar();
        }

        // T-194: el sistema nervioso se compone encima de serie + piezas. Se lee al activarse y se sigue por Cambio y por Agregado.
        private void OnEnable()
        {
            Registro<IImplantes>.Agregado += AlAgregarImplantes;
            Registro<IImplantes>.Quitado += AlQuitarImplantes;
            Escuchar(Registro<IImplantes>.Actual);
        }

        private void OnDisable()
        {
            Registro<IImplantes>.Agregado -= AlAgregarImplantes;
            Registro<IImplantes>.Quitado -= AlQuitarImplantes;
            if (implantesEscuchados != null)
            {
                implantesEscuchados.Cambio -= LeerImplantes;
                implantesEscuchados = null;
            }
        }

        private void AlAgregarImplantes(IImplantes nuevos) => Escuchar(nuevos);

        private void AlQuitarImplantes(IImplantes quitados)
        {
            if (quitados == implantesEscuchados)
            {
                Escuchar(Registro<IImplantes>.Actual);
            }
        }

        private void Escuchar(IImplantes implantes)
        {
            if (implantesEscuchados != null)
            {
                implantesEscuchados.Cambio -= LeerImplantes;
            }
            implantesEscuchados = implantes;
            if (implantesEscuchados != null)
            {
                implantesEscuchados.Cambio += LeerImplantes;
            }
            LeerImplantes();
        }

        private void LeerImplantes()
        {
            float nuevo = implantesEscuchados != null ? implantesEscuchados.Efectos.multiplicadorReaccionMecha : 1f;
            if (Mathf.Approximately(nuevo, reaccion))
            {
                return;
            }
            reaccion = nuevo;
            Asegurar();
            if (puestas != null)
            {
                Aplicar();
            }
        }

        private ValoresMecha Componer()
        {
            return ReglaReaccionImplantes.Aplicar(ReglaEstadisticasPiezas.Componer(deSerie, puestas), reaccion);
        }

        private void OnDestroy()
        {
            SoltarDatos();
        }

        // Puede llamarse antes del Awake (MechaPersistente carga en su OnEnable): por eso se arma bajo demanda.
        private void Asegurar()
        {
            if (listo)
            {
                return;
            }
            listo = true;
            vida = GetComponent<VidaMecha>();
            if (catalogo == null)
            {
                Debug.LogError("[Mecha] EquipoMecha sin CatalogoPiezas en " + name, this);
                return;
            }
            if (vida.Datos == null)
            {
                Debug.LogError("[Mecha] EquipoMecha: la VidaMecha no tiene datos en " + name, this);
                return;
            }
            deSerie = vida.Datos.Valores;
            puestas = new DatosPieza[NumeroRanuras];
            for (int i = 0; i < NumeroRanuras; i++)
            {
                puestas[i] = catalogo.DeSerie((RanuraPieza)i);
            }
        }

        /// <summary>La pieza puesta en esa ranura (null si no hay catálogo).</summary>
        public DatosPieza Puesta(RanuraPieza ranura)
        {
            Asegurar();
            int i = (int)ranura;
            return puestas != null && i >= 0 && i < NumeroRanuras ? puestas[i] : null;
        }

        /// <summary>
        /// Pone la pieza en su ranura y devuelve la que se quitó. Devuelve null si no cambió nada (misma pieza o pieza null).
        /// </summary>
        public DatosPieza Equipar(DatosPieza pieza)
        {
            Asegurar();
            if (puestas == null || pieza == null)
            {
                return null;
            }
            int i = (int)pieza.Ranura;
            DatosPieza anterior = puestas[i];
            if (anterior == pieza)
            {
                return null;
            }
            puestas[i] = pieza;
            Aplicar();
            return anterior;
        }

        /// <summary>Deja las 5 piezas de serie.</summary>
        public void PonerDeSerie()
        {
            PonerPorIds(null);
        }

        /// <summary>
        /// Pone las piezas con esos ids (al cargar una partida): lo que no se nombra o no existe en el catálogo queda de serie
        /// (un id desconocido avisa con warning). Compone una sola vez.
        /// </summary>
        public void PonerPorIds(IReadOnlyList<string> ids)
        {
            Asegurar();
            if (puestas == null)
            {
                return;
            }
            var nuevas = new DatosPieza[NumeroRanuras];
            for (int i = 0; i < NumeroRanuras; i++)
            {
                nuevas[i] = catalogo.DeSerie((RanuraPieza)i);
            }
            if (ids != null)
            {
                foreach (string id in ids)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }
                    DatosPieza pieza = catalogo.Buscar(id);
                    if (pieza == null)
                    {
                        Debug.LogWarning("[Mecha] Pieza guardada desconocida (" + id + "): queda la de serie.", this);
                        continue;
                    }
                    nuevas[(int)pieza.Ranura] = pieza;
                }
            }
            bool cambio = false;
            for (int i = 0; i < NumeroRanuras; i++)
            {
                if (puestas[i] != nuevas[i])
                {
                    puestas[i] = nuevas[i];
                    cambio = true;
                }
            }
            if (cambio)
            {
                Aplicar();
            }
        }

        private void Aplicar()
        {
            DatosMecha nuevo = DatosMecha.CrearEnEjecucion(Componer());
            vida.CambiarDatos(nuevo);
            SoltarDatos();
            datosEjecucion = nuevo;
            Cambio?.Invoke();
        }

        private void SoltarDatos()
        {
            if (datosEjecucion != null)
            {
                Destroy(datosEjecucion);
                datosEjecucion = null;
            }
        }
    }
}
