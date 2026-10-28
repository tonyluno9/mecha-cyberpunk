using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion
{
    /// <summary>
    /// Qué implantes tiene el jugador (T-191): implementa <see cref="IImplantes"/> y se guarda con clave <c>implantes</c>.
    /// Va en ServiciosProgresion.prefab. Cobra al instalar con <c>IntentarPagar</c> (no fiado). No sabe de horarios ni de la clínica.
    /// </summary>
    public class ServicioImplantes : MonoBehaviour, IImplantes, IGuardable
    {
        public const string Clave = "implantes";
        private static readonly int CantidadEspacios = Enum.GetValues(typeof(EspacioImplante)).Length;

        [SerializeField] private CatalogoImplantes catalogo;

        [Serializable]
        private class DatosGuardados
        {
            public string[] ids = Array.Empty<string>();
        }

        private DatosImplante[] instalados;
        private bool avisoSinCartera;

        public string ClaveGuardado => Clave;

        public event Action Cambio;
        public event Action<EspacioImplante, int> Instalado;

        private DatosImplante[] Lista => instalados ??= new DatosImplante[CantidadEspacios];

        private CatalogoImplantes Catalogo
        {
            get
            {
                if (catalogo == null)
                {
                    Debug.LogError("[Progresion] ServicioImplantes en " + name + " no tiene CatalogoImplantes; se usa uno vacío.", this);
                    catalogo = ScriptableObject.CreateInstance<CatalogoImplantes>();
                }
                return catalogo;
            }
        }

        private void OnEnable()
        {
            Registro<IImplantes>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            Registro<ServicioImplantes>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IImplantes>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            Registro<ServicioImplantes>.Quitar(this);
        }

        public int Nivel(EspacioImplante espacio) => Lista[(int)espacio] == null ? 0 : Lista[(int)espacio].Nivel;

        public string Nombre(EspacioImplante espacio) => Lista[(int)espacio] == null ? string.Empty : Lista[(int)espacio].Nombre;

        public int Cantidad
        {
            get
            {
                int n = 0;
                foreach (DatosImplante d in Lista)
                {
                    if (d != null) n++;
                }
                return n;
            }
        }

        public EfectosImplantes Efectos => ReglaImplantes.Combinar(Lista);

        /// <summary>El implante instalado en el espacio, o null. (No se llama Instalado: choca con el evento de IImplantes.)</summary>
        public DatosImplante ImplanteInstalado(EspacioImplante espacio) => Lista[(int)espacio];

        /// <summary>La opción que se ofrece en el espacio ahora (liga y dinero actuales).</summary>
        public OfertaImplante Opcion(EspacioImplante espacio)
        {
            return ReglaImplantes.Oferta(Catalogo, espacio, Nivel(espacio), LigaActual(), Registro<ICartera>.Actual?.Dinero ?? 0);
        }

        private static int LigaActual() => Registro<IProgresoLigas>.Actual?.LigaActual ?? 1;

        /// <summary>Instala y cobra (la diferencia si ya tienes el nivel anterior). Sin dinero, sin liga o sin mejora no cambia nada.</summary>
        public ResultadoImplante Instalar(DatosImplante implante)
        {
            if (implante == null) return ResultadoImplante.NoMejora;
            int actual = Nivel(implante.Espacio);
            if (actual == implante.Nivel) return ResultadoImplante.YaInstalado;
            if (actual > implante.Nivel) return ResultadoImplante.NoMejora;
            if (LigaActual() < implante.LigaMinima) return ResultadoImplante.LigaBaja;

            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null)
            {
                if (!avisoSinCartera)
                {
                    avisoSinCartera = true;
                    Debug.LogWarning("[Progresion] ServicioImplantes: no hay ICartera registrada; no se puede instalar.");
                }
                return ResultadoImplante.SinDinero;
            }
            int costo = ReglaImplantes.Costo(Catalogo, implante, actual);
            if (!cartera.IntentarPagar(costo, MotivoDinero.Clinica)) return ResultadoImplante.SinDinero;

            Lista[(int)implante.Espacio] = implante;
            Instalado?.Invoke(implante.Espacio, implante.Nivel);
            Cambio?.Invoke();
            return ResultadoImplante.Instalado;
        }

        /// <summary>Solo para pruebas y herramientas: quita todo sin devolver dinero.</summary>
        public void QuitarTodos()
        {
            Array.Clear(Lista, 0, Lista.Length);
            Cambio?.Invoke();
        }

        public string Guardar()
        {
            var ids = new List<string>();
            foreach (DatosImplante d in Lista)
            {
                if (d != null) ids.Add(d.Id);
            }
            return JsonUtility.ToJson(new DatosGuardados { ids = ids.ToArray() });
        }

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || leidos.ids == null)
            {
                Debug.LogWarning("[Progresion] Los implantes no entendieron lo guardado; no se cambia nada.");
                return;
            }
            var nuevos = new DatosImplante[CantidadEspacios];
            foreach (string id in leidos.ids)
            {
                DatosImplante d = Catalogo.Buscar(id);
                if (d == null)
                {
                    Debug.LogWarning("[Progresion] Implante guardado desconocido, se ignora: " + id);
                    continue;
                }
                nuevos[(int)d.Espacio] = d;
            }
            instalados = nuevos;
            Cambio?.Invoke();
        }
    }
}
