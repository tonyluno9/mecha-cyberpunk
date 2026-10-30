using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Un lugar fijo en el minimapa y el mapa (T-409): El Mercado, I+D+I, el CIC o la casa. Va en el punto del lugar y se registra como
    /// <see cref="IMarcaMapa"/> mientras está activo. Los pone CreadorCiudadPandillas.T409 bajo GTA_T409.
    /// </summary>
    public class MarcaLugar : MonoBehaviour, IMarcaMapa
    {
        [SerializeField] private TipoMarcaMapa tipo = TipoMarcaMapa.Mercado;

        public TipoMarcaMapa Tipo => tipo;
        public Vector3 Posicion => transform.position;
        public bool Visible => isActiveAndEnabled;

        private void OnEnable() => Registro<IMarcaMapa>.Agregar(this);

        private void OnDisable() => Registro<IMarcaMapa>.Quitar(this);
    }
}
