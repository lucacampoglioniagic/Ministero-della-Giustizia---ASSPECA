# Session Notes — ASSPECA / Ministero della Giustizia

> Note di sessione in ordine cronologico inverso (più recente in cima). Stesso modello adottato in ASPEN.

---

## Session 2026-09-09 — Analisi, decisione di sicurezza, schema dati, motore di assegnazione, sicurezza, ribbon

### Obiettivo di sessione
Partire da zero: analizzare i requisiti ASSPECA (cartella `Documentazione/`) confrontandoli con ASPEN,
decidere cosa riusare, progettare lo schema dati, e implementare il primo blocco funzionante in
Dataverse (ambiente `LCC-MINISTEROGIUSTIZIA-DEMO`, solution `ASSPECAPOC`).

### 1. Analisi e progettazione
- Analizzati tutti i documenti in `Documentazione/` (manuale legacy, criteri di assegnazione, verbali
  incontri, tabelle sezione penale) e confrontati con l'AS-IS di ASPEN.
- Prodotto [`02 - Analisi Comparativa ASSPECA-vs-ASPEN.md`](02%20-%20Analisi%20Comparativa%20ASSPECA-vs-ASPEN.md):
  requisiti, confronto, componenti riutilizzabili, rischi, piano POC.
- **Differenze chiave rispetto ad ASPEN**: assegnazione a **2 livelli** (Sezione → Magistrato relatore,
  non diretta al magistrato), carico basato su **conteggio per categoria** (non peso sommato), stampa
  lotto giornaliero con firma Presidente, "Variazione" motivata al posto del "tappo" di ASPEN.

### 2. Decisione architetturale — tabella fascicoli dedicata
- L'utente ha sollevato un dubbio corretto: riusare `agc_fascicolo2` (di ASPEN) rischiava data leakage
  tra le due app se nella stessa Business Unit, perché la profondità del ruolo di sicurezza
  (User/BU/Parent-Child/Organization) e i confini dell'app model-driven filtrano solo la UI, non i dati
  sottostanti (Advanced Find, export Excel, Power BI vedono comunque tutte le righe).
- **Decisione**: tabella dedicata **`agc_fascicoloappello`**, non condivisa con ASPEN. Motivazione
  documentata in dettaglio nel par. 6.4 dell'analisi comparativa.

### 3. Schema dati implementato (Web API dirette: `az account get-access-token` + `Invoke-RestMethod`,
   stesso pattern documentato in ASPEN `SESSION_NOTES.md`)
- Business Unit **"Corte di Appello di Napoli"** (figlia di "Ministero della Giustizia").
- Tabelle: `agc_categoria`, `agc_specializzazione`, `agc_motivo`, `agc_sezione`, `agc_fascicoloappello`
  (+ 4 lookup: categoria/specializzazione/sezione assegnata/magistrato assegnato), `agc_variazione`
  (+ 6 lookup: fascicolo/sezione prima-dopo/magistrato prima-dopo/motivo).
- `contact` esteso con `agc_sezionemagistrato`, `agc_tipomagistrato`, `agc_datanominamagistrato`,
  `agc_percentualeastensione` (il campo `agc_ismagistrato` esisteva già, condiviso con ASPEN).
- Dati di riferimento popolati: 6 Sezioni (quote 8/9/8/8/5/5), 22 Categorie (D01-D10/L01-L12, demo/TBD),
  3 Specializzazioni (S1/S2/S3), 5 Motivi variazione.
- **Gotcha metadata cache lag**: la prima creazione di una relationship (lookup `agc_motivo` su
  `agc_variazione`) è tornata 204 ma un controllo immediato ha dato 404 falso negativo → creato per
  errore un duplicato, poi ripulito. Lezione: aspettare/ritentare la verifica prima di ricreare.

### 4. Motore di assegnazione a 2 livelli (Custom API + Plugin)
- Nuovo progetto C# `05 - Power Platform/Plugin-Custom-API/` (net462, firmato con lo stesso `.snk` di
  ASPEN), **assembly Dataverse dedicato** `ASSPECA-Plugin-Custom-API` (nome distinto da quello di ASPEN
  per evitare collisione nello stesso ambiente e mantenere la stessa segregazione del par. 6.4).
- Custom API **`agc_AssegnaFascicoloAppello`** (bound su `agc_fascicoloappello`), plugin
  `AssegnaFascicoloAppelloPlugin.cs`:
  - **Livello 1 (Sezione)**: PERC = fascicoli/categoria assegnati alla sezione ÷ n. magistrati sezione,
    tra le sezioni attive compatibili con la specializzazione; ordina per PERC crescente (tie-break:
    numero sezione); sceglie il minimo tra le prime 4 (approssimazione del "turno", da raffinare).
  - **Livello 2 (Magistrato)**: tra i magistrati della sezione scelta, minimo conteggio fascicoli nella
    categoria (tie-break: anzianità di nomina); esclude Presidenti se la categoria lo richiede e
    astensioni 100%; un magistrato "nuovo" (0 fascicoli mai assegnati) va in coda (trattato come
    massimo, non minimo) — scelta intenzionale diversa da ASPEN, commentata nel codice.
  - Guardia di idempotenza: rifiuta l'esecuzione su un fascicolo già assegnato.
- **Testato end-to-end** su dati demo temporanei (2 magistrati, 1 fascicolo, poi rimossi): assegnazione
  corretta verificata (sezione con PERC minimo, magistrato più anziano a parità di conteggio zero).
- Tutte le assunzioni/semplificazioni sono commentate nel codice con riferimento alle ambiguità aperte
  documentate nell'analisi comparativa (par. 6.1).

### 5. Sicurezza — ruolo e team
- Ruolo `Operatore ASSPECA` creato alla BU radice (propagato automaticamente a tutte le BU per
  comportamento standard Dataverse, incl. "Corte di Appello di Napoli").
- Privilege: clonate quelle framework/condivise da `Operatore ASPEN` (91, stesse depth, recuperate via
  `RetrieveRolePrivilegesRole`) **escludendo** le tabelle ASPEN-only; **aggiunte** quelle sulle tabelle
  ASSPECA — `agc_fascicoloappello`/`agc_variazione` CRUD completo a livello Local/BU,
  `agc_sezione`/`agc_categoria`/`agc_specializzazione`/`agc_motivo` sola lettura a livello Global
  (tabelle di configurazione, scrittura riservata agli amministratori). Assegnate via
  `AddPrivilegesRole` (109 privilege totali).
- Team `Corte di Appello di Napoli - ASSPECA` creato nella BU dedicata con il ruolo associato — i futuri
  utenti magistrato andranno aggiunti a questo team.

### 6. Ribbon "Assegna Fascicolo"
- Pulsante su form e griglia di `agc_fascicoloappello` che invoca la Custom API via
  `Xrm.WebApi.online.execute` e mostra il risultato in un alert dialog; si disabilita automaticamente se
  il fascicolo è già assegnato (stessa guardia lato client, difesa in profondità col plugin).
- Implementato con lo stesso pattern classico `RibbonDiffXml` usato da ASPEN per "Modifica Carico":
  web resource `agc_assegnafascicoloappello.js` + solution unmanaged dedicata
  `AgicAssegnaFascicoloAppelloRibbon` (root component = sola entità, `behavior=2`), impacchettata e
  importata via `pac solution pack` / `pac solution import --publish-changes`.
- Verificato via `RetrieveEntityRibbon` che il pulsante è presente nel ribbon live.

### Repository
- Repo GitHub creata: `lucacampoglioniagic/Ministero-della-Giustizia---ASSPECA` (privata), stessa
  convenzione di ASPEN.

---

## ✅ Todo completati in questa sessione

- [x] Analisi comparativa ASSPECA vs ASPEN (documento completo con fonti)
- [x] Decisione architetturale: tabella fascicoli dedicata (non condivisa con ASPEN) + rationale
  documentato
- [x] Struttura repository (README.md, cartelle `03`-`06`)
- [x] Business Unit "Corte di Appello di Napoli"
- [x] Schema dati: `agc_categoria`, `agc_specializzazione`, `agc_motivo`, `agc_sezione`,
  `agc_fascicoloappello`, `agc_variazione` + estensione `contact`
- [x] Dati di riferimento: Sezioni, Categorie, Specializzazioni, Motivi
- [x] Motore di assegnazione a 2 livelli: Custom API + Plugin C#, assembly dedicato, testato end-to-end
- [x] Ruolo di sicurezza `Operatore ASSPECA` + Team `Corte di Appello di Napoli - ASSPECA`
- [x] Ribbon "Assegna Fascicolo" (form + griglia) con web resource JS

## ⏳ Todo aperti / prossimi passi

- [ ] **Form/viste/sitemap** per le nuove tabelle e **app model-driven ASSPECA** — consigliato via Maker
  Portal (rischio di corruzione troppo alto per farlo via Web API grezze, come per l'app "Modern" di
  ASPEN che usa un `descriptor` JSON non documentato)
- [ ] **Dati demo definitivi**: magistrati (contact con `agc_ismagistrato`, sezione, data nomina, tipo)
  e fascicoli di test realistici per validare il motore con un dataset più ampio
- [ ] **Variazione**: al momento la tabella `agc_variazione` esiste ma non c'è ancora un flusso/plugin
  che la usi per modificare sezione/magistrato di un fascicolo già assegnato
- [ ] **Stampa PDF giornaliera** del lotto di assegnazione con firma Presidente (Power Automate?)
- [ ] **Cruscotto/PCF** per il carico per categoria (sezione e magistrato)
- [ ] **Validazione con il cliente** delle assunzioni aperte (documentate in par. 6.1 dell'analisi
  comparativa): formula PERC esatta, semantica "data deposito", direzione tie-break anzianità,
  numero/codici esatti delle categorie (20 vs 22), quali categorie escludono i Presidenti, logica
  "prime 4 su 6 in turno" (rotazione storica non ancora modellata)
- [ ] Estendere `agc_configurazione`/`agc_esonero` (riusate da ASPEN) con eventuali chiavi/mappature
  specifiche ASSPECA, se necessario
