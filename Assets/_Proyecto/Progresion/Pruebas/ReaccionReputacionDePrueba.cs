using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Doble del mundo de Alan, solo para escenas de Tony: un letrero que lee <see cref="IReputacion"/> y el alias del perfil.
    /// Con barrio en nivel 1 o más dice "GRAFITI: ALIAS"; con corporaciones en -1 o menos, "ANUNCIO: ALIAS ES UN PELIGRO"; si no, vacío.
    /// Se actualiza con <c>Cambio</c>, no en cada cuadro.
    /// </summary>
    public class ReaccionReputacionDePrueba : MonoBehaviour
    {
        [Tooltip("Letrero opcional (TextMesh) donde se escribe el texto.")]
        [SerializeField] private TextMesh letrero;

        private IReputacion reputacion;

        public string Texto { get; private set; } = "";

        private void OnEnable()
        {
            reputacion = Registro<IReputacion>.Actual;
            if (reputacion != null) reputacion.Cambio += Actualizar;
            Actualizar();
        }

        private void OnDisable()
        {
            if (reputacion != null) reputacion.Cambio -= Actualizar;
            reputacion = null;
        }

        public void Actualizar()
        {
            string texto = "";
            if (reputacion != null)
            {
                IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
                string alias = perfil != null ? perfil.Alias : "";
                if (reputacion.Nivel(TipoReputacion.Barrio) >= 1) texto = "GRAFITI: " + alias;
                else if (reputacion.Nivel(TipoReputacion.Corporaciones) <= -1) texto = "ANUNCIO: " + alias + " ES UN PELIGRO";
            }
            Texto = texto;
            if (letrero != null) letrero.text = texto;
        }
    }
}
