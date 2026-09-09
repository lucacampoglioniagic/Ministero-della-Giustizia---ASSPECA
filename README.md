# Ministero della Giustizia – ASSPECA

**Cliente:** Ministero della Giustizia
**Fornitore:** AGIC Technology
**Data avvio POC:** Settembre 2026
**Scadenza POC:** 15/09/2026

---

## Obiettivo del progetto

Reingegnerizzazione dell'applicativo legacy **ASSPECA** (VB6/Access, Corte di Appello di Napoli – Settore Penale, estensione di ASPENCA) su **Microsoft Power Platform** in logica model-driven (Dataverse), sul modello architetturale già validato con il progetto gemello **[ASPEN](../Ministero%20della%20Giustizia%20-%20ASPEN)** (assegnazione fascicoli GIP/GUP di primo grado).

A differenza di ASPEN, ASSPECA gestisce l'assegnazione dei procedimenti penali d'appello con un algoritmo **a due livelli** (prima la **Sezione**, poi il **Magistrato relatore**) e un modello di carico basato su **categorie di complessità** anziché su un peso numerico sommato. Vedi l'analisi completa: [`02 - Analisi Comparativa ASSPECA-vs-ASPEN.md`](02%20-%20Analisi%20Comparativa%20ASSPECA-vs-ASPEN.md).

---

## Relazione con ASPEN

| Aspetto | ASPEN | ASSPECA |
|---|---|---|
| Ufficio | Tribunale – GIP/GUP (1° grado) | Corte di Appello di Napoli – Settore Penale |
| Livello di assegnazione | Diretto al magistrato | Sezione → Magistrato relatore |
| Modello di carico | Peso numerico sommato (canestro + fasce) | Conteggio fascicoli per categoria (20–22 categorie) |
| Ambiente Dataverse | `LCC-MINISTEROGIUSTIZIA-DEMO` | **Stesso ambiente**, solution separata |

**Decisione architetturale (09/09/2026):** ASSPECA vive nello stesso ambiente Dataverse di ASPEN (`https://lccministerogiustiziademo.crm4.dynamics.com/`) ma in una **solution separata** (`ASSPECAPOC`), con:
- una **Business Unit dedicata** ("Corte di Appello di Napoli");
- una **tabella fascicoli dedicata e non condivisa** (`agc_fascicoloappello`, distinta da `agc_fascicolo2` di ASPEN) — la sola segregazione per BU su una tabella condivisa non garantisce l'isolamento dei dati tra i due applicativi (v. dettaglio in §6.4 del documento di analisi comparativa);
- riuso delle sole tabelle/componenti realmente trasversali: `contact` (magistrato), `agc_configurazione`, `agc_esonero`, plugin `SetOwnerTeamPlugin`.

---

## Struttura della repository

```
📁 Documentazione/                   # Materiale ricevuto dal cliente sul nuovo applicativo ASSPECA
│   ├── ASPECA _ Incontro con AGIC e Microsoft.docx
│   ├── Link Video_Trascrizione_Sintesi_Overview ASPECA.txt
│   ├── MANUALE ASSPECA_DOC.pdf
│   ├── Meeting ASPEN-ASPENCA - 2026-08-04 - Sintesi meeting.DOCX
│   ├── Meeting ASPEN-ASPENCA - 2026-08-04 - Transcript.DOCX
│   ├── STAMPA ASSPECA.pdf
│   ├── Screen ASPECA.pptx
│   ├── criteri assegnazione.docx
│   └── tabelle seconda sezione penale.pdf

📄 02 - Analisi Comparativa ASSPECA-vs-ASPEN.md   # Analisi funzionale, confronto con ASPEN, componenti riutilizzabili, piano POC

📁 03 - Documentazione Prodotta/
│   ├── Funzionale/                  # Specifiche funzionali ASSPECA
│   ├── Tecnica/                     # Specifiche tecniche, modello dati, integrazioni
│   └── Architettura/                # Diagrammi architetturali

📁 04 - Riunioni e Call/              # Verbali, riassunti call, interlocutori

📁 05 - Power Platform/
│   ├── Dataverse/                   # Schema tabelle, relazioni, choice columns (Sezione, Categoria, Fascicolo Appello, Variazione)
│   ├── Model-Driven-App/            # File app model-driven ASSPECA
│   ├── Power-Automate/              # Flussi (es. stampa PDF giornaliera)
│   ├── Plugin-Custom-API/           # Plugin/Custom API motore di assegnazione a 2 livelli
│   ├── PCF/                         # Componenti grafici carico sezione/magistrato per categoria
│   ├── AssegnaFascicolo/            # Ribbon button + dialog assegnazione (sezione + magistrato)
│   └── Mockup/                      # Mockup UI

📁 06 - Riferimenti Normativi e Tecnici/   # Normativa, riferimenti tecnici
```

---

## Documenti di riferimento

| Documento | Percorso | Note |
|---|---|---|
| Analisi comparativa ASSPECA vs ASPEN | [`02 - Analisi Comparativa ASSPECA-vs-ASPEN.md`](02%20-%20Analisi%20Comparativa%20ASSPECA-vs-ASPEN.md) | Documento principale di analisi e progettazione: requisiti, confronto, componenti riutilizzabili, rischi, piano POC |
| Manuale ASSPECA | [`Documentazione/MANUALE ASSPECA_DOC.pdf`](Documentazione/MANUALE%20ASSPECA_DOC.pdf) | Manuale funzionale dell'applicativo legacy (v.5.a, 2018) |
| Criteri di assegnazione | [`Documentazione/criteri assegnazione.docx`](Documentazione/criteri%20assegnazione.docx) | Regole di calcolo PERC, categorie, tie-break |
| Repo di riferimento ASPEN | [`../Ministero della Giustizia - ASPEN`](../Ministero%20della%20Giustizia%20-%20ASPEN) | Progetto gemello, sola lettura — non modificare |

---

## Architettura target

- **Piattaforma:** Microsoft Power Platform – Model-Driven App su Dataverse
- **Ambiente:** `LCC-MINISTEROGIUSTIZIA-DEMO` (https://lccministerogiustiziademo.crm4.dynamics.com), stesso ambiente di ASPEN
- **Solution:** `ASSPECAPOC` (separata da quella di ASPEN)
- **Publisher prefix:** `agc_` (condiviso con ASPEN)
- **Sicurezza:** Business Unit dedicata "Corte di Appello di Napoli", ruoli di sicurezza nativi, Entra ID (`luca.campoglioni@agic.it`)
- **Logica assegnazione:** Plugin Dataverse / Custom API — motore a due livelli (Sezione → Magistrato)
- **Reporting:** Cruscotto model-driven (PCF) + stampa ufficiale PDF giornaliera

## Modello dati POC (in progettazione)

| Tabella Dataverse | Descrizione | Stato |
|---|---|---|
| `agc_fascicoloappello` | Fascicoli d'appello (nuova, dedicata — non condivisa con ASPEN) | ✅ Creata (09/09/2026) — schema base, lookup a Categoria/Specializzazione/Sezione/Magistrato |
| `agc_sezione` | Sezioni della Corte d'Appello, quote magistrati, specializzazione fissa | ✅ Creata (09/09/2026) |
| `agc_categoria` | Categorie di complessità (D/L, ~20–22 codici) | ✅ Creata (09/09/2026) — dati (22 record) da popolare |
| `agc_specializzazione` | Materie S1/S2/S3 e regola di destinazione sezione | ✅ Creata (09/09/2026) |
| `agc_variazione` + `agc_motivo` | Variazione Sezionale/Magistrato con motivo tabellato | ✅ Creata (09/09/2026) — motivi (Errore materiale, Incompatibilità, Scardinamento, Stralcio, Verifica) da popolare |
| `contact` (standard, riusata) | Magistrati — estesa con `agc_sezionemagistrato`, `agc_tipomagistrato`, `agc_datanominamagistrato`, `agc_percentualeastensione` | ✅ Estesa (09/09/2026) |
| `agc_configurazione` (riusata) | Parametri di sistema per ufficio | Da estendere con chiavi ASSPECA |
| `agc_esonero` (riusata) | Esoneri/astensioni magistrati | Da valutare mappatura astensione % |

**Business Unit:** `Corte di Appello di Napoli` creata come figlia di `Ministero della Giustizia` (09/09/2026).

**Dati di riferimento popolati (09/09/2026):** 6 Sezioni (quote 8/9/8/8/5/5), 22 Categorie (D01-D10/L01-L12, demo/TBD), 3 Specializzazioni (S1/S2/S3), 5 Motivi variazione.

**Motore di assegnazione a 2 livelli — ✅ implementato e testato (09/09/2026):** Custom API `agc_AssegnaFascicoloAppello` (bound su `agc_fascicoloappello`), plugin C# in [`05 - Power Platform/Plugin-Custom-API/AssegnaFascicoloAppelloPlugin.cs`](05%20-%20Power%20Platform/Plugin-Custom-API/AssegnaFascicoloAppelloPlugin.cs). Assembly Dataverse dedicato `ASSPECA-Plugin-Custom-API` (separato da quello di ASPEN, stessa logica di segregazione del §6.4). Logica:
- **Livello 1 (Sezione):** PERC = fascicoli assegnati in quella categoria / n. magistrati sezione, tra le sezioni attive compatibili con la specializzazione richiesta; ordina per PERC crescente (tie-break: numero sezione); sceglie il minimo tra le prime 4 sezioni (approssimazione "turno", da raffinare).
- **Livello 2 (Magistrato):** tra i magistrati della sezione assegnata, sceglie quello con meno fascicoli nella categoria (tie-break: anzianità di nomina); esclude i Presidenti se la categoria lo richiede e i magistrati con astensione 100%; un magistrato "nuovo" (0 fascicoli mai assegnati) va in coda (trattato come massimo, non minimo) — scelta intenzionalmente diversa da ASPEN, commentata nel codice.
- Guardia di idempotenza: rifiuta l'esecuzione su un fascicolo già assegnato (serve una Variazione per modificarlo).
- Testato end-to-end su dati demo temporanei (poi rimossi) in `ASSPECAPOC`.

**Ancora da fare (P0):** form/viste/sitemap per le nuove tabelle (consigliato via Maker Portal), dati demo magistrati/fascicoli, stampa PDF giornaliera, cruscotto/PCF carico per categoria.

**Ribbon "Assegna Fascicolo" — ✅ implementato (09/09/2026):** pulsante su form e vista/griglia di `agc_fascicoloappello` che invoca la Custom API `agc_AssegnaFascicoloAppello` (`Xrm.WebApi.online.execute`) e mostra il risultato in un alert dialog. Implementato con lo stesso pattern classico `RibbonDiffXml` usato da ASPEN per "Modifica Carico": web resource JS [`agc_assegnafascicoloappello.js`](05%20-%20Power%20Platform/AssegnaFascicolo/WebResources/agc_assegnafascicoloappello.js), solution unmanaged dedicata `AgicAssegnaFascicoloAppelloRibbon` (root component = sola entità `agc_fascicoloappello`, `behavior=2` per non toccare altri componenti), impacchettata e importata via `pac solution pack` / `pac solution import --publish-changes`. Il pulsante si disabilita automaticamente (EnableRule custom) se il fascicolo risulta già assegnato — client-side, con la stessa guardia di idempotenza applicata anche server-side dal plugin.

**Sicurezza — ✅ implementata (09/09/2026):** ruolo di sicurezza `Operatore ASSPECA` creato alla Business Unit radice (propagato automaticamente a tutte le BU, incluse "Corte di Appello di Napoli", per comportamento standard Dataverse). Privilege: framework/condiviso clonato da `Operatore ASPEN` (91 privilege, stesse depth) **escludendo** le tabelle ASPEN-only (`agc_fascicolo2`, `agc_fascicolo`, `agc_giudice`, `agc_rgnr`, `agc_canestro*`, `agc_modificacarico`); **aggiunte** le privilege sulle tabelle ASSPECA: `agc_fascicoloappello`/`agc_variazione` (CRUD completo a livello Local/BU) e `agc_sezione`/`agc_categoria`/`agc_specializzazione`/`agc_motivo` (sola lettura, livello Global — tabelle di configurazione condivise, gestite solo da amministratori). Team `Corte di Appello di Napoli - ASSPECA` creato nella BU dedicata con il ruolo associato — i futuri utenti magistrato andranno aggiunti a questo team.

> Dettaglio completo, fonti e alternative in [`02 - Analisi Comparativa ASSPECA-vs-ASPEN.md`](02%20-%20Analisi%20Comparativa%20ASSPECA-vs-ASPEN.md), sezioni 3–5.

---

## POC — stato attuale

**Fase:** Analisi e progettazione completate (09/09/2026). Modello dati, motore di assegnazione a 2 livelli, sicurezza e ribbon "Assegna Fascicolo" implementati e testati in Dataverse (solution `ASSPECAPOC`, ambiente `LCC-MINISTEROGIUSTIZIA-DEMO`).

Vedi [`SESSION_NOTES.md`](SESSION_NOTES.md) per il log dettagliato delle sessioni di sviluppo (stesso modello adottato in ASPEN).
