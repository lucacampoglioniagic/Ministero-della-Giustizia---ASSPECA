# Session Notes — ASSPECA / Ministero della Giustizia

> Note di sessione in ordine cronologico inverso (più recente in cima). Stesso modello adottato in ASPEN.

---

## Session 2026-09-10 (continua) — Documentazione finale: punti aperti, guida funzionale, checklist di test, riordino repo

### Obiettivo
Chiudere la sessione producendo la documentazione mancante e riordinando il materiale in repo:
1. un documento dei punti aperti da validare con il cliente;
2. una guida funzionale in linguaggio semplice per chi non conosce l'app;
3. una checklist di test end-to-end (dall'inizio del progetto ad oggi), da spuntare manualmente;
4. riordino/aggiornamento di tutta la documentazione di repo.

### Documenti prodotti
- [`03 - Documentazione Prodotta/Funzionale/Punti Aperti da Validare con il Cliente.md`](03%20-%20Documentazione%20Prodotta/Funzionale/Punti%20Aperti%20da%20Validare%20con%20il%20Cliente.md) —
  consolida tutte le assunzioni/semplificazioni prese durante il POC (rotazione "in turno", tie-break
  anzianità, categorie escluse Presidenti, trimestre/mese nascita imputato non implementato, sezioni
  specializzate fisse vs rotazione semestrale, Variazione Sezionale sempre automatica, connettore
  Mail bloccato dalla piattaforma, dati demo vs reali, ruoli di sicurezza).
- [`03 - Documentazione Prodotta/Funzionale/Guida Funzionale ASSPECA.md`](03%20-%20Documentazione%20Prodotta/Funzionale/Guida%20Funzionale%20ASSPECA.md) —
  spiegazione non tecnica di cosa fa l'app e come si usa (fascicolo, assegnazione a 2 livelli,
  variazione, cruscotto, notifica giornaliera, modelli Word, sicurezza).
- [`03 - Documentazione Prodotta/Tecnica/Checklist di Test.md`](03%20-%20Documentazione%20Prodotta/Tecnica/Checklist%20di%20Test.md) —
  checklist cumulativa (14 aree, dalle anagrafiche alla sicurezza) di tutto quanto realizzato
  dall'inizio del progetto, con caselle da spuntare manualmente in una prossima sessione di collaudo.

### Riordino repository
- Spostati i file sciolti alla radice nelle rispettive cartelle sotto `05 - Power Platform/`:
  `Fascicolo Appello - Modello Word.docx` e `Variazione - Modello Word.docx` → nuova cartella
  `Word-Templates/`; cartella `pcf-cruscotto/` (progetto del componente PCF `CruscottoFascicoli`,
  fino ad ora mai documentato in queste note pur essendo già implementato) → `PCF/pcf-cruscotto/`;
  `seed-demo-data.mjs` (script di popolamento dati demo) → `Dataverse/`.
- Aggiornato `README.md`: struttura repository, tabella documenti di riferimento (nuovi 3 documenti),
  nuova sezione "Funzionalità completate dopo l'analisi iniziale" (app model-driven, modelli Word,
  cruscotto PCF, rotazione, Variazione, notifica giornaliera, dati demo — funzionalità già realizzate
  ma non ancora riflesse nel README, la cui sezione "stato attuale" risultava disallineata) e stato
  POC aggiornato.

### Nota sulla tracciabilità
Alcune funzionalità risultavano già implementate (app model-driven, cruscotto PCF, modelli Word) ma
non erano mai state documentate in questo file con una propria voce di sessione: sono ora tracciate
retroattivamente nel README e nella nuova documentazione prodotta, così da avere una base coerente
per la checklist di test e per i punti aperti.

### Prossimi passi
- [ ] Eseguire la Checklist di Test con l'utente, una voce alla volta
- [ ] Rivedere il documento Punti Aperti con il cliente e annotare le risposte ricevute

---

## Session 2026-09-10 (continua) — Plugin "Variazione Fascicolo" (Sezionale / Magistrato)

### Obiettivo
Backlog item "Flusso/plugin Variazione": implementare la logica che applica una rettifica motivata
di un'assegnazione già effettuata (Variazione Sezionale o Variazione Magistrato), come da manuale
legacy ASSPECA (F1 p.17-18, F7 slide 5-8).

### Stato di partenza
La tabella `agc_variazione` esisteva già (creata in una sessione precedente, N7 del modello dati:
fascicolo, sezione/magistrato prima e dopo, motivo, nota, data provvedimento, tipo variazione
Sezionale/Magistrato) ma non c'era ancora nessuna logica server-side collegata: creare un record non
aveva alcun effetto sul fascicolo.

### Implementazione
- **Refactor**: estratta la logica di selezione del magistrato (Livello 2 del motore) dal plugin
  `AssegnaFascicoloAppelloPlugin` in una nuova classe condivisa `MotoreAssegnazione` (nuovo file
  `MotoreAssegnazione.cs`), riutilizzabile anche dal nuovo plugin Variazione. Aggiunto un parametro
  opzionale `escludiMagistratoId` per poter escludere il magistrato uscente dal ricalcolo.
- **Nuovo plugin `VariazioneFascicoloPlugin`** (`VariazioneFascicoloPlugin.cs`), registrato su
  `Create` (PostOperation, sincrono) di `agc_variazione`:
  - **Variazione Sezionale** (`agc_tipovariazione = 10000`): richiede `agc_sezionedopo` in input; il
    magistrato nella nuova sezione è SEMPRE ricalcolato automaticamente dal motore (Livello 2), come
    da manuale ("il magistrato è ricalcolato automaticamente nella nuova sezione").
  - **Variazione Magistrato** (`agc_tipovariazione = 10001`): la sezione resta invariata;
    `agc_magistratodopo` può essere scelto manualmente (se valorizzato in input) oppure, se vuoto,
    selezionato automaticamente escludendo il magistrato uscente (per garantire un cambio reale).
  - I valori "prima" (`agc_sezioneprima`, `agc_magistratoprima`) sono sempre presi dallo stato
    corrente del fascicolo al momento della variazione (snapshot per l'audit "vista prima/dopo"),
    ignorando eventuali valori indicati in input.
  - Precondizione: il fascicolo deve essere già assegnato (Sezione e Magistrato non nulli),
    altrimenti l'operazione viene rifiutata con errore esplicito (si usa prima la Custom API
    `agc_AssegnaFascicoloAppello`).
  - Aggiorna sia il fascicolo (`agc_sezioneassegnata`/`agc_magistratoassegnato`) sia il record
    Variazione stesso (con i valori "prima/dopo" effettivi).

### Deploy e registrazione
- Build (`dotnet build`, 0 errori) + `pac plugin push` per aggiornare l'assembly (stesso comando
  della sessione precedente).
- **`pac plugin push` NON registra automaticamente i nuovi plugin type/step**: è stato necessario
  creare manualmente, via Web API, il record `plugintype` per `VariazioneFascicoloPlugin` e il
  relativo `sdkmessageprocessingstep` (messaggio `Create`, entità `agc_variazione`, stage 40
  PostOperation, mode 0 sincrono, rank 1).

### Verifica
- **Variazione Magistrato** su un fascicolo assegnato a Sezione 3/Marco Bianchi → magistrato
  ricalcolato automaticamente su Patrizia Gatti (diverso da Marco Bianchi, sezione invariata).
- **Variazione Sezionale** su un fascicolo assegnato a Sezione 3/Marco Bianchi con `sezionedopo` =
  Sezione 4 → sezione cambiata correttamente, magistrato ricalcolato automaticamente su Paolo Russo
  (magistrato di Sezione 4).
- **Validazione precondizione**: creata una Variazione su un fascicolo NON ancora assegnato → 400
  rifiutato con messaggio esplicito, nessun record orfano (rollback automatico della transazione).

### Todo aggiornato
- [x] Flusso/plugin Variazione (Sezionale/Magistrato) con ricalcolo automatico e audit prima/dopo

---

## Session 2026-09-10 — Flusso "Notifica Lotto Giornaliero Assegnazioni"

### Obiettivo di sessione
Backlog item "Stampa PDF giornaliera del lotto di assegnazione con firma del Presidente". Non essendo
disponibile in ambiente alcuna connessione O365/Word/OneDrive né la possibilità di automatizzare una
firma, concordato con l'utente un approccio semplificato: **email HTML giornaliera** al posto di
PDF firmato.

### Flusso implementato
Cloud Flow **"Notifica Lotto Giornaliero Assegnazioni"** (Power Automate, solution "ASSPECA POC",
workflow id `32794773-1aad-f111-aaab-7ced8d775612`), stato: **salvato e attivo**.
- **Recurrence** (giornaliera, 18:00 UTC)
- **Elenca righe** (Microsoft Dataverse) su `agc_fascicoloappellos`, filtro:
  `_agc_sezioneassegnata_value ne null and modifiedon` nell'intervallo "oggi" (proxy per "assegnato
  oggi", non esiste un campo dedicato "data assegnazione")
- **Crea tabella HTML** dal risultato
- **Condizione**: se ci sono righe (`length(...) > 0`) → **Invia una notifica di posta elettronica
  (V3)** (connettore "Posta", basato su SendGrid nativo Microsoft) con oggetto/corpo dinamici che
  includono la tabella HTML e una nota sui limiti (niente PDF/firma automatica).

### Bug scoperto e corretto durante il test
Il filtro dell'azione "Elenca righe" referenziava `agc_sezioneassegnata` (nome logico del lookup),
ma Dataverse Web API richiede il nome della proprietà OData del valore del lookup
**`_agc_sezioneassegnata_value`** per i filtri — il nome "nudo" non è filtrabile e dava 400. Corretto
via PATCH diretto su `workflows.clientdata` (stesso pattern usato per le espressioni). Dopo la
correzione, il test manuale del flusso ha confermato: List rows, filtro e Condizione funzionano
correttamente (branch "Vero" raggiunto con righe trovate).

### Limite scoperto: connettore "Mail" disabilitato per il tenant
Il test end-to-end dell'azione email ha restituito `Unauthorized`. Il dettaglio dell'errore rivela la
causa reale (non di configurazione): **"The Mail connector is currently restricted for new tenants.
Microsoft is working on enabling this connector. In the meantime, please consider using alternatives
like Office 365 Outlook, Gmail, SendGrid connector instead."** — è un blocco lato piattaforma per i
tenant nuovi, non risolvibile lato flusso.

**Decisione utente**: lasciare il flusso così com'è (struttura corretta e verificata, azione email
pronta ma non eseguibile finché Microsoft non abilita il connettore per questo tenant). Il flusso si
attiverà automaticamente non appena il connettore sarà sbloccato, senza ulteriori modifiche.

### Todo aggiornato
- [x] Stampa PDF giornaliera → sostituita con notifica email HTML (limite firma/PDF documentato)
- [ ] Riattivare/verificare l'invio email non appena il connettore "Mail" sarà abilitato dal tenant
  (o valutare in futuro un connettore alternativo: Office 365 Outlook, Gmail, SendGrid con account
  esterno)

---

## Session 2026-09-10 (continua) — Dati demo definitivi: magistrati e fascicoli realistici

### Obiettivo
Popolare il dataset demo con un numero realistico di magistrati (coerente con le quote per sezione) e
fascicoli di test, per validare il motore di assegnazione a 2 livelli su scala reale.

### Magistrati
- Le 6 `Sezioni` hanno quote (`agc_numeromagistrati`) 8/9/8/8/5/5 = **43 magistrati totali**.
- I 6 contact magistrato demo preesistenti (Laura Verdi, Anna Greco, Marco Bianchi, Paolo Russo,
  Alessia Gialli, Chiara Marini) sono stati aggiornati come **Presidente (P)**, uno per sezione, con
  data di nomina "storica" (1998-2003) per simulare anzianità.
- Creati **37 nuovi contact "Consigliere (C)"** (nomi italiani realistici) per completare esattamente
  le quote di ciascuna sezione, con data di nomina distribuita 2003-2020 (per testare i tie-break di
  anzianità) e `agc_percentualeastensione` per lo più 0, con alcuni casi 50%/100% per testare
  l'esclusione per astensione.
- **Gotcha Web API**: il bind del lookup `agc_sezionemagistrato` su `contact` richiede il nome della
  **navigation property** `agc_SezioneMagistrato` (PascalCase, diverso dal nome logico
  minuscolo) nell'annotazione `@odata.bind` — usare il nome logico minuscolo dà 400
  ("undeclared property"). Recuperato il nome corretto via
  `EntityDefinitions(LogicalName='contact')/ManyToOneRelationships`.
- Verificato via Web API: distribuzione finale magistrati per sezione esattamente 8/9/8/8/5/5 = 43.

### Fascicoli di test
- Creati **20 nuovi fascicoli** (`agc_fascicoloappello`) non assegnati, con categorie/specializzazioni
  variate, imputati e date deposito realistici.
- Eseguita la Custom API `agc_AssegnaFascicoloAppello` su tutti e 20 (sequenzialmente, per evitare
  condizioni di gara nel conteggio) → **20/20 assegnazioni completate senza errori**, confermando che
  il motore funziona correttamente con il nuovo dataset di 43 magistrati.

### Limite reale scoperto grazie al test su scala
La distribuzione risultante è fortemente sbilanciata: quasi tutti i fascicoli sono finiti su
**Sezione 1 / Laura Verdi**. Causa: molte categorie di test sono "nuove" per tutte le sezioni (nessun
fascicolo storico in quella categoria), quindi il PERC (livello 1) è 0 per tutte le sezioni compatibili
→ pareggio → il tie-break attuale sceglie la sezione con **numero più basso**, sempre la stessa. Allo
stesso modo, a livello 2, il magistrato più anziano con 0 fascicoli nella categoria vince sempre.
Questo conferma quanto già annotato come rischio aperto nell'analisi comparativa (par. 6.1): la
rotazione storica "prime 4 su 6 a turno" non è ancora modellata, e con dati demo scarsi/nuovi il
tie-break deterministico produce concentrazione. **Da validare con il cliente** prima del rilascio: il
meccanismo di turno reale va implementato per evitare questo comportamento in produzione.

### Todo aggiornato
- [x] Dati demo definitivi: 43 magistrati (quote rispettate) + 20 fascicoli di test assegnati
- [x] Implementare la rotazione storica "prime 4 su 6 a turno" per il livello 1 (Sezione)

---

## Session 2026-09-10 (continua) — Rotazione "prime 4 su 6 a turno" (Livello 1)

### Problema
Il test su dati demo aveva rivelato che, a parità di PERC=0 (categorie "nuove" senza storico), il
tie-break sceglieva sempre la sezione con numero più basso → concentrazione su Sezione 1.

### Soluzione implementata
`AssegnaFascicoloAppelloPlugin.SelezionaSezione` ora mantiene una **finestra rotante** delle sezioni
"in turno" (dimensione 4, come da assunzione POC), il cui indice di partenza è persistito nella
tabella chiave/valore `agc_configurazione` (record `agc_nome = "IndiceTurnoSezione"`,
`agc_valore` = indice 0..N-1). Ad ogni assegnazione:
1. le sezioni compatibili vengono ordinate per `agc_numero`;
2. si estrae la finestra circolare di 4 sezioni a partire dall'indice corrente;
3. si sceglie, tra queste, quella con PERC minimo (tie-break: numero sezione);
4. l'indice viene incrementato di 1 (modulo il numero di sezioni candidate) e ripersistito, cosi la
   sezione di partenza ruota ad ogni chiamata successiva.

### Deploy
- Build: `dotnet build` (0 errori) sul progetto `05 - Power Platform/Plugin-Custom-API`.
- Pubblicazione assembly aggiornato in Dataverse via **`pac plugin push`**:
  `pac plugin push --environment https://lccministerogiustiziademo.crm4.dynamics.com/ --pluginId 58748bb9-5aac-f111-aaab-7ced8d775612 --pluginFile "bin\Debug\net462\ASSPECA-Plugin-Custom-API.dll" --type Assembly`
  (comando comodo per i prossimi aggiornamenti del plugin: non richiede Plugin Registration Tool).

### Verifica
Creati 12 nuovi fascicoli di test (categorie varie, mai assegnate prima) ed eseguita la Custom API in
sequenza: le assegnazioni ora **ruotano correttamente tra le sezioni 2, 3, 4, 5** (pattern osservato:
3,3,4,5,2,2,3,3,4,5,2,2), invece di concentrarsi tutte sulla Sezione 1 come nel test precedente.
Conferma che la rotazione funziona; nel tempo, con altre assegnazioni, anche le sezioni 1 e 6
rientreranno nella finestra.

### Todo aggiornato
- [ ] Validare con il cliente la dimensione della finestra "in turno" (4 su 6) e la logica esatta di
  rotazione (l'assunzione POC è documentata nel codice del plugin, par. 6.1 dell'Analisi Comparativa)

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
- [x] **Stampa PDF giornaliera** del lotto di assegnazione con firma Presidente → sostituita con
  notifica email HTML via flusso "Notifica Lotto Giornaliero Assegnazioni" (v. Session 2026-09-10)
- [ ] **Cruscotto/PCF** per il carico per categoria (sezione e magistrato)
- [ ] **Validazione con il cliente** delle assunzioni aperte (documentate in par. 6.1 dell'analisi
  comparativa): formula PERC esatta, semantica "data deposito", direzione tie-break anzianità,
  numero/codici esatti delle categorie (20 vs 22), quali categorie escludono i Presidenti, logica
  "prime 4 su 6 in turno" (rotazione storica non ancora modellata)
- [ ] Estendere `agc_configurazione`/`agc_esonero` (riusate da ASPEN) con eventuali chiavi/mappature
  specifiche ASSPECA, se necessario
