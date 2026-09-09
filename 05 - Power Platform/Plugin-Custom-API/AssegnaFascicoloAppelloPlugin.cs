using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgicAsspeca.Plugins
{
    /// <summary>
    /// Plugin che implementa la Custom API <c>agc_AssegnaFascicoloAppello</c>, bound sulla tabella
    /// dedicata <c>agc_fascicoloappello</c> (v. Analisi Comparativa ASSPECA-vs-ASPEN, par. 6.4,
    /// per il motivo della tabella non condivisa con ASPEN).
    ///
    /// Implementa il motore di assegnazione a DUE LIVELLI richiesto da ASSPECA (diverso da ASPEN,
    /// che assegna direttamente al magistrato):
    ///   1) Livello SEZIONE: tra le sezioni attive compatibili con la categoria/specializzazione
    ///      del fascicolo, si calcola PERC = (fascicoli già assegnati in quella categoria) /
    ///      (numero magistrati della sezione). Si ordina per PERC crescente (tie-break: numero
    ///      sezione) e si sceglie la sezione con PERC minimo tra le prime N ammesse a turno.
    ///   2) Livello MAGISTRATO: tra i magistrati della sezione assegnata, si sceglie quello con
    ///      MENO fascicoli nella stessa categoria (tie-break: anzianità di nomina). I Presidenti
    ///      sono esclusi dalle categorie marcate <c>agc_esclusapresidenti</c>. Un magistrato "nuovo"
    ///      (nessun fascicolo mai assegnato in nessuna categoria) viene trattato come se avesse il
    ///      MASSIMO dei conteggi ("messo in coda"), NON il minimo come farebbe ASPEN.
    ///
    /// ASSUNZIONI adottate per la POC (da validare con il cliente, v. Analisi Comparativa par. 6.1):
    ///  - "prime N sezioni in turno" semplificato a: tutte le sezioni attive compatibili, prendendo
    ///    le prime 4 per PERC crescente (il concetto di turno/rotazione storica non è ancora
    ///    modellato in questa POC).
    ///  - Tie-break anzianità magistrato: ordinamento per data di nomina crescente (il magistrato
    ///    più anziano, cioè con la data più vecchia, viene preferito). Ambiguità nota (F1 p.15 vs
    ///    F3 30:03) - da confermare con il cliente.
    ///  - L'abbinamento per "trimestre/mese di nascita del 1° imputato" e le "specializzate a
    ///    sezione fissa per semestre" non sono ancora implementati in questa prima versione: la
    ///    Specializzazione fissa di sezione (<c>agc_sezione.agc_specializzazionefissa</c>) è già
    ///    usata per includere/escludere le sezioni compatibili, ma la rotazione temporale (semestre)
    ///    è rimandata a una versione successiva.
    ///
    /// Parametri Custom API:
    /// - Target (EntityReference, obbligatorio, implicito da bound action): il fascicolo
    ///   (agc_fascicoloappello) da assegnare.
    ///
    /// Output:
    /// - SezioneAssegnataId (Guid)
    /// - MagistratoAssegnatoId (Guid)
    /// - Messaggio (String): riepilogo leggibile del calcolo, utile per il dialog UI.
    /// </summary>
    public class AssegnaFascicoloAppelloPlugin : PluginBase
    {
        // Valore scelta "Presidente (P)" sul campo contact.agc_tipomagistrato.
        private const int TipoMagistratoPresidente = 10000;

        // Valore scelta "Proposto" sul campo agc_fascicoloappello.agc_stato.
        private const int StatoProposto = 10000;

        public AssegnaFascicoloAppelloPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AssegnaFascicoloAppelloPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
                throw new ArgumentNullException(nameof(localPluginContext));

            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.PluginUserService;
            var tracer = localPluginContext.TracingService;

            if (context.MessageName != "agc_AssegnaFascicoloAppello")
                return;

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is EntityReference target))
                throw new InvalidPluginExecutionException("Parametro Target (fascicolo) mancante o non valido.");

            if (target.LogicalName != "agc_fascicoloappello")
                throw new InvalidPluginExecutionException("La Custom API agc_AssegnaFascicoloAppello è utilizzabile solo su record di tipo agc_fascicoloappello.");

            var fascicolo = service.Retrieve("agc_fascicoloappello", target.Id, new ColumnSet(
                "agc_categoria", "agc_specializzazione", "agc_sezioneassegnata", "agc_magistratoassegnato", "agc_stato"));

            if (fascicolo.Contains("agc_sezioneassegnata") || fascicolo.Contains("agc_magistratoassegnato"))
                throw new InvalidPluginExecutionException("Il fascicolo risulta già assegnato: usare una Variazione per modificare sezione/magistrato.");

            if (!fascicolo.Contains("agc_categoria"))
                throw new InvalidPluginExecutionException("Il fascicolo non ha una Categoria impostata: impossibile calcolare l'assegnazione.");

            var categoriaRef = fascicolo.GetAttributeValue<EntityReference>("agc_categoria");
            var specializzazioneRef = fascicolo.GetAttributeValue<EntityReference>("agc_specializzazione");

            var categoria = service.Retrieve("agc_categoria", categoriaRef.Id, new ColumnSet("agc_name", "agc_esclusapresidenti"));
            var esclusaPresidenti = categoria.GetAttributeValue<bool>("agc_esclusapresidenti");

            tracer.Trace($"AssegnaFascicoloAppelloPlugin: fascicolo={target.Id} categoria={categoria.GetAttributeValue<string>("agc_name")} specializzazione={specializzazioneRef?.Id}");

            // ---------- LIVELLO 1: SEZIONE ----------
            var sezioneAssegnata = SelezionaSezione(service, tracer, categoriaRef, specializzazioneRef);

            // ---------- LIVELLO 2: MAGISTRATO ----------
            var magistratoAssegnato = SelezionaMagistrato(service, tracer, categoriaRef, sezioneAssegnata.Id, esclusaPresidenti);

            // ---------- Aggiornamento fascicolo ----------
            var update = new Entity("agc_fascicoloappello", target.Id)
            {
                ["agc_sezioneassegnata"] = sezioneAssegnata,
                ["agc_magistratoassegnato"] = magistratoAssegnato,
                ["agc_stato"] = new OptionSetValue(StatoProposto)
            };
            service.Update(update);

            // .Name su EntityReference non è popolato in modo affidabile da RetrieveMultiple/ToEntityReference:
            // recuperiamo i nomi con una lettura mirata per un messaggio leggibile nel dialog UI.
            var sezioneNome = service.Retrieve("agc_sezione", sezioneAssegnata.Id, new ColumnSet("agc_name")).GetAttributeValue<string>("agc_name");
            var magistratoNome = service.Retrieve("contact", magistratoAssegnato.Id, new ColumnSet("fullname")).GetAttributeValue<string>("fullname");
            var messaggio = $"Proposta: Sezione '{sezioneNome}' -> Magistrato '{magistratoNome}' (categoria {categoria.GetAttributeValue<string>("agc_name")}).";
            tracer.Trace($"AssegnaFascicoloAppelloPlugin: {messaggio}");

            context.OutputParameters["SezioneAssegnataId"] = sezioneAssegnata.Id;
            context.OutputParameters["MagistratoAssegnatoId"] = magistratoAssegnato.Id;
            context.OutputParameters["Messaggio"] = messaggio;
        }

        /// <summary>
        /// Livello 1: individua la sezione con PERC (fascicoli/magistrati) minimo per la categoria
        /// data, tra le sezioni attive compatibili con la specializzazione del fascicolo.
        /// </summary>
        private static EntityReference SelezionaSezione(IOrganizationService service, ITracingService tracer,
            EntityReference categoriaRef, EntityReference specializzazioneRef)
        {
            var sezioni = service.RetrieveMultiple(new QueryExpression("agc_sezione")
            {
                ColumnSet = new ColumnSet("agc_name", "agc_numero", "agc_numeromagistrati", "agc_specializzazionefissa"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("agc_attiva", ConditionOperator.Equal, true),
                        new ConditionExpression("agc_numeromagistrati", ConditionOperator.GreaterThan, 0)
                    }
                }
            }).Entities;

            // Compatibilità specializzazione: se il fascicolo richiede una specializzazione, solo le
            // sezioni con quella specializzazione fissa sono ammesse; le sezioni con una
            // specializzazione fissa DIVERSA (o comunque impostata) sono escluse per i fascicoli
            // ordinari (senza specializzazione richiesta), perché riservate.
            var candidate = sezioni.Where(s =>
            {
                var specFissa = s.GetAttributeValue<EntityReference>("agc_specializzazionefissa");
                if (specializzazioneRef != null)
                    return specFissa != null && specFissa.Id == specializzazioneRef.Id;
                return specFissa == null;
            }).ToList();

            if (candidate.Count == 0)
            {
                tracer.Trace("SelezionaSezione: nessuna sezione compatibile con la specializzazione richiesta, ripiego su tutte le sezioni attive.");
                candidate = sezioni.ToList();
            }

            if (candidate.Count == 0)
                throw new InvalidPluginExecutionException("Nessuna sezione attiva disponibile per l'assegnazione.");

            var classifica = candidate.Select(s =>
            {
                var numeroMagistrati = s.GetAttributeValue<decimal>("agc_numeromagistrati");
                var conteggioFascicoli = ContaFascicoliCategoria(service, categoriaRef, "agc_sezioneassegnata", s.Id);
                var perc = numeroMagistrati > 0 ? conteggioFascicoli / numeroMagistrati : decimal.MaxValue;
                return new
                {
                    Sezione = s,
                    Perc = perc,
                    Numero = s.GetAttributeValue<int>("agc_numero")
                };
            })
            .OrderBy(x => x.Perc)
            .ThenBy(x => x.Numero)
            .ToList();

            // "Prime 4 su 6 in turno" semplificato: tra le sezioni ammesse si considerano solo le
            // prime 4 per PERC crescente, e si sceglie la prima (PERC minimo). V. nota assunzioni.
            var primeInTurno = classifica.Take(4).ToList();
            var scelta = primeInTurno.First();

            tracer.Trace($"SelezionaSezione: classifica=[{string.Join(", ", classifica.Select(x => $"{x.Sezione.GetAttributeValue<string>("agc_name")}:{x.Perc:N2}"))}] scelta={scelta.Sezione.GetAttributeValue<string>("agc_name")}");

            return scelta.Sezione.ToEntityReference();
        }

        /// <summary>
        /// Livello 2: individua, tra i magistrati della sezione assegnata, quello con meno
        /// fascicoli nella categoria data. Presidenti esclusi se la categoria lo richiede;
        /// magistrati con esonero totale (100% astensione) esclusi; nuovi magistrati (0 fascicoli
        /// mai assegnati) trattati come "in coda" (massimo, non minimo).
        /// </summary>
        private static EntityReference SelezionaMagistrato(IOrganizationService service, ITracingService tracer,
            EntityReference categoriaRef, Guid sezioneId, bool esclusaPresidenti)
        {
            var filtro = new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression("agc_ismagistrato", ConditionOperator.Equal, true),
                    new ConditionExpression("agc_sezionemagistrato", ConditionOperator.Equal, sezioneId),
                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                }
            };

            var magistrati = service.RetrieveMultiple(new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "agc_tipomagistrato", "agc_datanominamagistrato", "agc_percentualeastensione"),
                Criteria = filtro
            }).Entities;

            var candidati = magistrati.Where(m =>
            {
                if (esclusaPresidenti && m.GetAttributeValue<OptionSetValue>("agc_tipomagistrato")?.Value == TipoMagistratoPresidente)
                    return false;

                var astensione = m.Contains("agc_percentualeastensione") ? m.GetAttributeValue<decimal>("agc_percentualeastensione") : 0m;
                if (astensione >= 100m)
                    return false;

                return true;
            }).ToList();

            if (candidati.Count == 0)
                throw new InvalidPluginExecutionException("Nessun magistrato disponibile nella sezione assegnata (verificare esclusioni Presidente/astensione totale).");

            var conteggi = candidati.Select(m => new
            {
                Magistrato = m,
                Conteggio = ContaFascicoliCategoria(service, categoriaRef, "agc_magistratoassegnato", m.Id),
                DataNomina = m.Contains("agc_datanominamagistrato") ? m.GetAttributeValue<DateTime>("agc_datanominamagistrato") : (DateTime?)null,
                TotaleFascicoliAssegnati = ContaFascicoliTotali(service, "agc_magistratoassegnato", m.Id)
            }).ToList();

            var massimo = conteggi.Count > 0 ? conteggi.Max(x => x.Conteggio) : 0;

            // Nuovo magistrato (nessun fascicolo mai assegnato in nessuna categoria) => "in coda":
            // il suo conteggio effettivo per l'ordinamento diventa il massimo+1, cosi non riceve
            // automaticamente il primo fascicolo disponibile (comportamento opposto ad ASPEN).
            var classifica = conteggi.Select(x => new
            {
                x.Magistrato,
                ConteggioEffettivo = x.TotaleFascicoliAssegnati == 0 ? massimo + 1 : x.Conteggio,
                x.DataNomina
            })
            .OrderBy(x => x.ConteggioEffettivo)
            .ThenBy(x => x.DataNomina ?? DateTime.MaxValue)
            .ToList();

            var scelto = classifica.First();

            tracer.Trace($"SelezionaMagistrato: classifica=[{string.Join(", ", classifica.Select(x => $"{x.Magistrato.GetAttributeValue<string>("fullname")}:{x.ConteggioEffettivo}"))}] scelto={scelto.Magistrato.GetAttributeValue<string>("fullname")}");

            return scelto.Magistrato.ToEntityReference();
        }

        private static int ContaFascicoliCategoria(IOrganizationService service, EntityReference categoriaRef, string lookupField, Guid lookupId)
        {
            var query = new QueryExpression("agc_fascicoloappello")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("agc_categoria", ConditionOperator.Equal, categoriaRef.Id),
                        new ConditionExpression(lookupField, ConditionOperator.Equal, lookupId)
                    }
                }
            };
            return service.RetrieveMultiple(query).Entities.Count;
        }

        private static int ContaFascicoliTotali(IOrganizationService service, string lookupField, Guid lookupId)
        {
            var query = new QueryExpression("agc_fascicoloappello")
            {
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = { new ConditionExpression(lookupField, ConditionOperator.Equal, lookupId) }
                }
            };
            return service.RetrieveMultiple(query).Entities.Count;
        }
    }
}
