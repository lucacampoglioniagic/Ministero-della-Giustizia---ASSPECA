using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace AgicAsspeca.Plugins
{
    /// <summary>
    /// Plugin registrato su <c>Create</c> (PostOperation, sincrono) della tabella
    /// <c>agc_variazione</c> (v. Analisi Comparativa ASSPECA-vs-ASPEN, sezione "Variazione
    /// Sezionale / Variazione Magistrato"): applica la rettifica motivata di un'assegnazione già
    /// effettuata sul fascicolo (<c>agc_fascicoloappello</c>) collegato.
    ///
    /// Due tipi di variazione (campo <c>agc_tipovariazione</c>):
    ///  - <b>Sezionale</b> (10000): il Presidente/coordinatore sceglie manualmente la nuova
    ///    Sezione (<c>agc_sezionedopo</c>, obbligatoria in input); il Magistrato nella nuova
    ///    sezione viene invece SEMPRE ricalcolato automaticamente dal motore (Livello 2), come da
    ///    F1 p.17 ("il magistrato è ricalcolato automaticamente nella nuova sezione").
    ///  - <b>Magistrato</b> (10001): la Sezione resta invariata; il nuovo Magistrato
    ///    (<c>agc_magistratodopo</c>) può essere scelto manualmente dall'utente (se valorizzato in
    ///    input) oppure, se lasciato vuoto, viene selezionato automaticamente dal motore
    ///    (Livello 2), escludendo esplicitamente il magistrato uscente per garantire un reale
    ///    cambio.
    ///
    /// I valori "prima" (<c>agc_sezioneprima</c>, <c>agc_magistratoprima</c>) sono sempre
    /// ricavati automaticamente dallo stato corrente del fascicolo al momento della variazione
    /// (snapshot per l'audit "vista prima/dopo"), a prescindere da eventuali valori indicati in
    /// input, per garantire la coerenza dello storico.
    ///
    /// Precondizione: il fascicolo deve essere già assegnato (Sezione e Magistrato non nulli),
    /// altrimenti la variazione non ha senso (si usa la Custom API
    /// <c>agc_AssegnaFascicoloAppello</c> per la prima assegnazione).
    /// </summary>
    public class VariazioneFascicoloPlugin : PluginBase
    {
        private const int TipoVariazioneSezionale = 10000;
        private const int TipoVariazioneMagistrato = 10001;

        public VariazioneFascicoloPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(VariazioneFascicoloPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
                throw new ArgumentNullException(nameof(localPluginContext));

            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.PluginUserService;
            var tracer = localPluginContext.TracingService;

            if (context.MessageName != "Create" || context.PrimaryEntityName != "agc_variazione")
                return;

            if (context.Stage != 40) // PostOperation
                return;

            if (!(context.InputParameters["Target"] is Entity targetEntity))
                return;

            // Rileggiamo il record appena creato per avere tutti i campi (il Target in
            // PostOperation potrebbe non contenere attributi calcolati/di default).
            var variazione = service.Retrieve("agc_variazione", targetEntity.Id, new ColumnSet(
                "agc_fascicolo", "agc_tipovariazione", "agc_sezionedopo", "agc_magistratodopo"));

            if (!variazione.Contains("agc_fascicolo"))
                throw new InvalidPluginExecutionException("La Variazione deve indicare il Fascicolo a cui si riferisce.");

            if (!variazione.Contains("agc_tipovariazione"))
                throw new InvalidPluginExecutionException("La Variazione deve indicare il Tipo (Sezionale o Magistrato).");

            var fascicoloRef = variazione.GetAttributeValue<EntityReference>("agc_fascicolo");
            var tipoVariazione = variazione.GetAttributeValue<OptionSetValue>("agc_tipovariazione").Value;

            var fascicolo = service.Retrieve("agc_fascicoloappello", fascicoloRef.Id, new ColumnSet(
                "agc_categoria", "agc_sezioneassegnata", "agc_magistratoassegnato"));

            if (!fascicolo.Contains("agc_sezioneassegnata") || !fascicolo.Contains("agc_magistratoassegnato"))
                throw new InvalidPluginExecutionException("Il fascicolo non risulta ancora assegnato: usare prima la Custom API di assegnazione (agc_AssegnaFascicoloAppello).");

            var categoriaRef = fascicolo.GetAttributeValue<EntityReference>("agc_categoria");
            var sezionePrimaRef = fascicolo.GetAttributeValue<EntityReference>("agc_sezioneassegnata");
            var magistratoPrimaRef = fascicolo.GetAttributeValue<EntityReference>("agc_magistratoassegnato");

            var categoria = service.Retrieve("agc_categoria", categoriaRef.Id, new ColumnSet("agc_esclusapresidenti"));
            var esclusaPresidenti = categoria.GetAttributeValue<bool>("agc_esclusapresidenti");

            tracer.Trace($"VariazioneFascicoloPlugin: variazione={targetEntity.Id} fascicolo={fascicoloRef.Id} tipo={tipoVariazione}");

            EntityReference sezioneDopoRef;
            EntityReference magistratoDopoRef;

            if (tipoVariazione == TipoVariazioneSezionale)
            {
                sezioneDopoRef = variazione.GetAttributeValue<EntityReference>("agc_sezionedopo");
                if (sezioneDopoRef == null)
                    throw new InvalidPluginExecutionException("La Variazione Sezionale richiede di indicare la nuova Sezione (agc_sezionedopo).");

                // Il magistrato nella nuova sezione è SEMPRE ricalcolato automaticamente
                // dall'algoritmo (Livello 2), a prescindere da eventuali valori indicati in input.
                magistratoDopoRef = MotoreAssegnazione.SelezionaMagistrato(service, tracer, categoriaRef, sezioneDopoRef.Id, esclusaPresidenti);
            }
            else if (tipoVariazione == TipoVariazioneMagistrato)
            {
                // Stessa sezione: la Variazione Magistrato non cambia la sezione assegnata.
                sezioneDopoRef = sezionePrimaRef;

                var magistratoDopoInput = variazione.GetAttributeValue<EntityReference>("agc_magistratodopo");
                magistratoDopoRef = magistratoDopoInput
                    ?? MotoreAssegnazione.SelezionaMagistrato(service, tracer, categoriaRef, sezioneDopoRef.Id, esclusaPresidenti, escludiMagistratoId: magistratoPrimaRef.Id);
            }
            else
            {
                throw new InvalidPluginExecutionException($"Tipo variazione non gestito: {tipoVariazione}.");
            }

            // Aggiorna il fascicolo con il nuovo stato (Sezione/Magistrato "dopo").
            var updateFascicolo = new Entity("agc_fascicoloappello", fascicoloRef.Id)
            {
                ["agc_sezioneassegnata"] = sezioneDopoRef,
                ["agc_magistratoassegnato"] = magistratoDopoRef
            };
            service.Update(updateFascicolo);

            // Aggiorna la Variazione con lo snapshot "prima" (sempre dal fascicolo, per audit
            // affidabile) e il "dopo" effettivo (compreso il ricalcolo automatico, se avvenuto).
            var updateVariazione = new Entity("agc_variazione", targetEntity.Id)
            {
                ["agc_sezioneprima"] = sezionePrimaRef,
                ["agc_magistratoprima"] = magistratoPrimaRef,
                ["agc_sezionedopo"] = sezioneDopoRef,
                ["agc_magistratodopo"] = magistratoDopoRef
            };
            service.Update(updateVariazione);

            tracer.Trace($"VariazioneFascicoloPlugin: applicata. SezionePrima={sezionePrimaRef.Id} SezioneDopo={sezioneDopoRef.Id} MagistratoPrima={magistratoPrimaRef.Id} MagistratoDopo={magistratoDopoRef.Id}");
        }
    }
}
