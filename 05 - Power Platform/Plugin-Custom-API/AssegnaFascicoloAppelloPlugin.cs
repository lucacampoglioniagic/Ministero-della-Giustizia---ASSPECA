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
    ///      del fascicolo, si individua la finestra "in turno" (le 4 sezioni, su un massimo di 6,
    ///      il cui turno di rotazione è corrente, v. sotto), si calcola PERC = (fascicoli già
    ///      assegnati in quella categoria) / (numero magistrati della sezione) SOLO tra le sezioni
    ///      in turno, si ordina per PERC crescente (tie-break: numero sezione) e si sceglie la
    ///      sezione con PERC minimo.
    ///   2) Livello MAGISTRATO: tra i magistrati della sezione assegnata, si sceglie quello con
    ///      MENO fascicoli nella stessa categoria (tie-break: anzianità di nomina). I Presidenti
    ///      sono esclusi dalle categorie marcate <c>agc_esclusapresidenti</c>. Un magistrato "nuovo"
    ///      (nessun fascicolo mai assegnato in nessuna categoria) viene trattato come se avesse il
    ///      MASSIMO dei conteggi ("messo in coda"), NON il minimo come farebbe ASPEN.
    ///
    /// ROTAZIONE "prime 4 su 6 a turno" (v. Analisi Comparativa par. 6.1): la finestra delle
    /// sezioni "in turno" NON è fissa (altrimenti, a parità di PERC = 0 su categorie nuove, il
    /// tie-break per numero sezione concentrerebbe sempre le assegnazioni sulla sezione più bassa,
    /// come osservato nel test su dati demo del 2026-09-10). Lo stato della rotazione è persistito
    /// nella tabella chiave/valore <c>agc_configurazione</c> (record <c>agc_nome</c> =
    /// <see cref="ConfigurazioneIndiceTurno"/>, <c>agc_valore</c> = indice 0..5 della sezione di
    /// partenza della finestra). Ad ogni assegnazione:
    ///  - si ordinano le sezioni candidate per <c>agc_numero</c> crescente;
    ///  - si prende una finestra circolare di 4 sezioni a partire dall'indice corrente;
    ///  - tra queste si sceglie quella con PERC minimo (tie-break: numero sezione);
    ///  - l'indice viene poi incrementato di 1 (modulo il numero di sezioni candidate) e
    ///    ripersistito, cosi la sezione di partenza della finestra ruota ad ogni assegnazione
    ///    successiva, garantendo nel tempo pari opportunità a tutte le sezioni.
    /// ASSUNZIONI adottate per la POC (da validare con il cliente, v. Analisi Comparativa par. 6.1):
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

        // Chiave del record agc_configurazione che persiste l'indice (0-based) della sezione di
        // partenza della finestra "in turno" per la rotazione del Livello 1.
        private const string ConfigurazioneIndiceTurno = "IndiceTurnoSezione";

        // Dimensione della finestra "in turno" (prime N su totale sezioni compatibili).
        private const int DimensioneFinestraTurno = 4;

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

            // Ordinamento stabile per numero sezione: definisce la sequenza su cui ruota la
            // finestra "in turno".
            var ordinateXNumero = candidate.OrderBy(s => s.GetAttributeValue<int>("agc_numero")).ToList();
            var totaleCandidate = ordinateXNumero.Count;
            var dimensioneFinestra = Math.Min(DimensioneFinestraTurno, totaleCandidate);

            var indiceTurno = LeggiIndiceTurno(service, tracer) % totaleCandidate;

            // Finestra circolare di N sezioni a partire dall'indice corrente (rotante).
            var inTurno = Enumerable.Range(0, dimensioneFinestra)
                .Select(offset => ordinateXNumero[(indiceTurno + offset) % totaleCandidate])
                .ToList();

            var classifica = inTurno.Select(s =>
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

            var scelta = classifica.First();

            tracer.Trace($"SelezionaSezione: indiceTurno={indiceTurno} inTurno=[{string.Join(", ", inTurno.Select(s => s.GetAttributeValue<string>("agc_name")))}] classifica=[{string.Join(", ", classifica.Select(x => $"{x.Sezione.GetAttributeValue<string>("agc_name")}:{x.Perc:N2}"))}] scelta={scelta.Sezione.GetAttributeValue<string>("agc_name")}");

            // Ruota la finestra: la prossima assegnazione parte dalla sezione successiva, cosi nel
            // tempo tutte le sezioni compatibili entrano ed escono dalla finestra "in turno" a
            // parità di opportunità, evitando la concentrazione osservata quando la finestra era
            // fissa sulle prime N per numero.
            ScriviIndiceTurno(service, tracer, (indiceTurno + 1) % totaleCandidate);

            return scelta.Sezione.ToEntityReference();
        }

        /// <summary>
        /// Legge l'indice corrente di rotazione dal record <c>agc_configurazione</c> con
        /// <c>agc_nome</c> = <see cref="ConfigurazioneIndiceTurno"/>. Se il record non esiste
        /// ancora (prima esecuzione), lo crea con indice 0.
        /// </summary>
        private static int LeggiIndiceTurno(IOrganizationService service, ITracingService tracer)
        {
            var record = TrovaConfigurazione(service);
            if (record == null)
            {
                tracer.Trace($"LeggiIndiceTurno: record di configurazione '{ConfigurazioneIndiceTurno}' non trovato, creazione con indice 0.");
                var nuovo = new Entity("agc_configurazione")
                {
                    ["agc_nome"] = ConfigurazioneIndiceTurno,
                    ["agc_valore"] = 0m
                };
                service.Create(nuovo);
                return 0;
            }

            var valore = record.GetAttributeValue<decimal>("agc_valore");
            return (int)valore;
        }

        /// <summary>
        /// Persiste il nuovo indice di rotazione nel record <c>agc_configurazione</c>.
        /// </summary>
        private static void ScriviIndiceTurno(IOrganizationService service, ITracingService tracer, int nuovoIndice)
        {
            var record = TrovaConfigurazione(service);
            if (record == null)
            {
                var nuovo = new Entity("agc_configurazione")
                {
                    ["agc_nome"] = ConfigurazioneIndiceTurno,
                    ["agc_valore"] = (decimal)nuovoIndice
                };
                service.Create(nuovo);
                return;
            }

            var update = new Entity("agc_configurazione", record.Id)
            {
                ["agc_valore"] = (decimal)nuovoIndice
            };
            service.Update(update);
            tracer.Trace($"ScriviIndiceTurno: nuovo indice={nuovoIndice}");
        }

        private static Entity TrovaConfigurazione(IOrganizationService service)
        {
            var query = new QueryExpression("agc_configurazione")
            {
                ColumnSet = new ColumnSet("agc_valore"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions = { new ConditionExpression("agc_nome", ConditionOperator.Equal, ConfigurazioneIndiceTurno) }
                },
                TopCount = 1
            };
            return service.RetrieveMultiple(query).Entities.FirstOrDefault();
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
