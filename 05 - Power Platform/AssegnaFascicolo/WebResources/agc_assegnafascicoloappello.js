"use strict";
// eslint-disable-next-line no-var
var AgicAsspeca = window.AgicAsspeca || {};

/* ── Tasto "Assegna Fascicolo" (form agc_fascicoloappello) ──
   Invoca la Custom API server-side agc_AssegnaFascicoloAppello che calcola e
   propone Sezione + Magistrato relatore con il motore a due livelli (v.
   AssegnaFascicoloAppelloPlugin.cs). Il tasto è visibile solo se il fascicolo
   non è già assegnato (agc_sezioneassegnata/agc_magistratoassegnato vuoti):
   il controllo è client-side per la UX, il plugin applica comunque la stessa
   regola server-side per difesa in profondità (idempotenza). */
AgicAsspeca.AssegnaFascicolo = (function () {

    /* ── Display/Enable rule per la command bar dal form ── */
    function nonAssegnatoDaForm(primaryControl) {
        try {
            var sezione = primaryControl.getAttribute("agc_sezioneassegnata");
            var magistrato = primaryControl.getAttribute("agc_magistratoassegnato");
            var sezioneVal = sezione ? sezione.getValue() : null;
            var magistratoVal = magistrato ? magistrato.getValue() : null;
            return !sezioneVal && !magistratoVal;
        } catch (e) {
            return true;
        }
    }

    /* ── Display/Enable rule per la command bar dalla griglia (un solo record selezionato) ── */
    function nonAssegnatoDaGriglia(selectedControl) {
        try {
            var rows = selectedControl.getGrid().getSelectedRows();
            if (!rows || rows.getLength() !== 1) return false;
            var row = rows.get(0);
            var sezione = row.getData().entity.getAttributes().getByName("agc_sezioneassegnata");
            var magistrato = row.getData().entity.getAttributes().getByName("agc_magistratoassegnato");
            var sezioneVal = sezione ? sezione.getValue() : null;
            var magistratoVal = magistrato ? magistrato.getValue() : null;
            return !sezioneVal && !magistratoVal;
        } catch (e) {
            return true;
        }
    }

    /* ── Esecuzione della Custom API su un id di fascicolo ── */
    function eseguiAssegnazione(fascicoloId, onSuccessRefresh) {
        var req = {
            entity: { entityType: "agc_fascicoloappello", id: fascicoloId },
            getMetadata: function () {
                return {
                    boundParameter: "entity",
                    parameterTypes: {
                        entity: { typeName: "mscrm.agc_fascicoloappello", structuralProperty: 5 }
                    },
                    operationType: 0, // Action
                    operationName: "agc_AssegnaFascicoloAppello"
                };
            }
        };

        Xrm.WebApi.online.execute(req).then(
            function (response) {
                if (!response.ok) {
                    Xrm.Navigation.openErrorDialog({ message: "Errore imprevisto durante l'assegnazione del fascicolo." });
                    return;
                }
                response.json().then(function (result) {
                    Xrm.Navigation.openAlertDialog({
                        title: "Assegna Fascicolo",
                        text: result.Messaggio || "Fascicolo assegnato."
                    }).then(function () {
                        if (onSuccessRefresh) onSuccessRefresh();
                    });
                }).catch(function () {
                    // Nessun body (204) o body non JSON: comunque successo.
                    Xrm.Navigation.openAlertDialog({ title: "Assegna Fascicolo", text: "Fascicolo assegnato." })
                        .then(function () { if (onSuccessRefresh) onSuccessRefresh(); });
                });
            },
            function (error) {
                Xrm.Navigation.openErrorDialog({
                    message: (error && error.message) || "Errore durante la chiamata alla Custom API di assegnazione."
                });
            }
        );
    }

    /* ── Comando dal form ── */
    function eseguiDaForm(primaryControl) {
        var id = primaryControl.data.entity.getId().replace(/[{}]/g, "");
        eseguiAssegnazione(id, function () { primaryControl.data.refresh(false); });
    }

    /* ── Comando dalla griglia (vista) ── */
    function eseguiDaGriglia(selectedControl) {
        var rows = selectedControl.getGrid().getSelectedRows();
        if (!rows || rows.getLength() !== 1) return;
        var id = rows.get(0).getData().entity.getId().replace(/[{}]/g, "");
        eseguiAssegnazione(id, function () { selectedControl.refresh(); });
    }

    return {
        nonAssegnatoDaForm: nonAssegnatoDaForm,
        nonAssegnatoDaGriglia: nonAssegnatoDaGriglia,
        eseguiDaForm: eseguiDaForm,
        eseguiDaGriglia: eseguiDaGriglia
    };
})();
