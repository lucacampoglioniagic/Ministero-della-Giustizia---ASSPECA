using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgicAsspeca.Plugins
{
    /// <summary>
    /// Logica di selezione del magistrato (Livello 2 del motore di assegnazione ASSPECA),
    /// condivisa tra la Custom API <see cref="AssegnaFascicoloAppelloPlugin"/> (prima assegnazione)
    /// e <see cref="VariazioneFascicoloPlugin"/> (ricalcolo automatico in caso di Variazione
    /// Sezionale/Magistrato, v. Analisi Comparativa ASSPECA-vs-ASPEN par. su "Variazione").
    /// </summary>
    internal static class MotoreAssegnazione
    {
        // Valore scelta "Presidente (P)" sul campo contact.agc_tipomagistrato.
        public const int TipoMagistratoPresidente = 10000;

        /// <summary>
        /// Individua, tra i magistrati della sezione data, quello con meno fascicoli nella
        /// categoria indicata (tie-break: anzianità di nomina). Presidenti esclusi se richiesto
        /// dalla categoria; magistrati con esonero totale (100% astensione) sempre esclusi; un
        /// magistrato "nuovo" (nessun fascicolo mai assegnato in nessuna categoria) è trattato come
        /// se avesse il MASSIMO dei conteggi ("messo in coda"), NON il minimo come farebbe ASPEN.
        /// </summary>
        /// <param name="escludiMagistratoId">
        /// Magistrato da escludere esplicitamente dai candidati (usato dalla Variazione Magistrato
        /// per garantire che il ricalcolo automatico non riproponga lo stesso magistrato uscente).
        /// </param>
        public static EntityReference SelezionaMagistrato(IOrganizationService service, ITracingService tracer,
            EntityReference categoriaRef, Guid sezioneId, bool esclusaPresidenti, Guid? escludiMagistratoId = null)
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
                if (escludiMagistratoId.HasValue && m.Id == escludiMagistratoId.Value)
                    return false;

                if (esclusaPresidenti && m.GetAttributeValue<OptionSetValue>("agc_tipomagistrato")?.Value == TipoMagistratoPresidente)
                    return false;

                var astensione = m.Contains("agc_percentualeastensione") ? m.GetAttributeValue<decimal>("agc_percentualeastensione") : 0m;
                if (astensione >= 100m)
                    return false;

                return true;
            }).ToList();

            if (candidati.Count == 0)
                throw new InvalidPluginExecutionException("Nessun magistrato disponibile nella sezione assegnata (verificare esclusioni Presidente/astensione totale/magistrato uscente).");

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

        public static int ContaFascicoliCategoria(IOrganizationService service, EntityReference categoriaRef, string lookupField, Guid lookupId)
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

        public static int ContaFascicoliTotali(IOrganizationService service, string lookupField, Guid lookupId)
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
