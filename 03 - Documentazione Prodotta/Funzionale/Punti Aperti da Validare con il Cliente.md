# Punti Aperti da Validare con il Cliente — ASSPECA

> Elenco delle assunzioni fatte durante il POC in assenza di una risposta esplicita del cliente, delle
> semplificazioni note e dei limiti di piattaforma emersi. Da rivedere insieme al cliente prima del
> passaggio in produzione. Riferimenti al documento [`02 - Analisi Comparativa ASSPECA-vs-ASPEN.md`](../../02%20-%20Analisi%20Comparativa%20ASSPECA-vs-ASPEN.md)
> e a `SESSION_NOTES.md` dove indicato.

---

## 1. Motore di assegnazione — Livello 1 (Sezione)

### 1.1 Rotazione "in turno" (finestra 4 su 6 sezioni)
Il manuale legacy parla di un meccanismo di "turno" tra le sezioni, ma non ne descrive in modo
inequivocabile l'algoritmo esatto. Nel POC è stata implementata un'**approssimazione**: una finestra
circolare di **4 sezioni su 6**, che ruota di 1 posizione ad ogni assegnazione (indice persistito in
`agc_configurazione`), e all'interno della finestra si sceglie la sezione con PERC minimo.

**Da validare:**
- La dimensione della finestra è davvero 4 su 6, o dipende da altri fattori (es. sezioni escluse
  temporaneamente per carico, assenze, ecc.)?
- La rotazione deve avanzare ad ogni fascicolo assegnato, oppure con un'altra cadenza (giornaliera,
  per categoria, ecc.)?
- Il turno è unico per tutte le categorie o va tracciato separatamente per specializzazione/categoria?

### 1.2 Formula PERC esatta
PERC = fascicoli assegnati alla sezione in quella categoria ÷ numero di magistrati della sezione. Da
confermare che sia esattamente questa la formula usata nel legacy (denominatore = tutti i magistrati
della sezione o solo quelli non esonerati/attivi?).

### 1.3 Tie-break tra sezioni a parità di PERC
In caso di pareggio, il POC sceglie la sezione con numero più basso all'interno della finestra di
turno. Da confermare se esiste un criterio diverso (es. anzianità del Presidente, data ultima
assegnazione).

---

## 2. Motore di assegnazione — Livello 2 (Magistrato relatore)

### 2.1 Direzione del tie-break per anzianità
Il manuale legacy contiene due indicazioni non perfettamente coerenti tra loro sulla direzione del
tie-break per anzianità di nomina (F1 pag. 15 vs F3 30:03). Nel POC si è scelta una direzione
(magistrato più anziano preferito a parità di conteggio) — **da confermare esplicitamente con il
cliente** quale sia il criterio corretto.

### 2.2 Magistrato "nuovo" (0 fascicoli nella categoria)
Un magistrato senza alcun fascicolo pregresso nella categoria viene trattato nel POC come
**ultimo in coda** (non come "minimo assoluto", cioè non riceve priorità automatica). È una scelta
intenzionale diversa dal comportamento di ASPEN, per evitare che i nuovi magistrati vengano subito
sommersi di fascicoli in una categoria mai vista. **Da validare che questo comportamento sia quello
desiderato** anche per ASSPECA.

### 2.3 Esclusioni dal calcolo
Attualmente vengono esclusi dal Livello 2: i Presidenti (solo per le categorie che lo richiedono) e i
magistrati con `agc_percentualeastensione = 100%`. Da confermare se vanno gestite anche astensioni
parziali (es. 50%) come fattore di **ponderazione** del carico (oggi non incidono sul conteggio, sono
solo escluse se al 100%).

---

## 3. Categorie di complessità

- Il manuale/i documenti ricevuti riportano un numero di categorie non perfettamente coerente tra le
  fonti (20 vs 22 codici D/L). Nel POC sono state caricate **22 categorie demo (D01-D10, L01-L12)**,
  da considerarsi **segnaposto**: vanno sostituite con l'elenco ufficiale e definitivo fornito dal
  cliente, comprensivo di quali categorie escludono i Presidenti dall'assegnazione.
- **Trimestre/mese di nascita del primo imputato**: nel manuale legacy è citato come possibile
  criterio di ripartizione per alcune categorie specifiche, ma **non è stato implementato** nel POC
  (nessun campo/logica dedicata). Da chiarire se è realmente in uso e per quali categorie.

---

## 4. Specializzazioni e sezioni fisse

- Il modello dati POC gestisce specializzazioni (S1/S2/S3) con destinazione **fissa** a una sezione.
  Il manuale accenna anche a una possibile **rotazione semestrale** delle sezioni specializzate
  (sezione "fissa" che cambia ogni semestre) — **non implementata** nel POC (assunzione: assegnazione
  statica sezione↔specializzazione). Da validare se serve gestire la variabilità temporale.

---

## 5. Variazione (Sezionale / Magistrato)

- Nella **Variazione Sezionale**, il magistrato nella nuova sezione viene **sempre ricalcolato
  automaticamente** dal motore (Livello 2), senza possibilità di scelta manuale — interpretazione
  letterale del manuale ("il magistrato è ricalcolato automaticamente nella nuova sezione"). Da
  confermare se in casi particolari serva invece consentire la scelta manuale anche in questo tipo di
  variazione (come già previsto per la Variazione Magistrato).
- L'elenco dei **Motivi** di variazione caricati (Errore materiale, Incompatibilità, Scardinamento,
  Stralcio, Verifica) è stato dedotto dai documenti ricevuti: da validare che sia completo e che le
  etichette siano quelle ufficiali.
- Una variazione può essere richiesta solo su un fascicolo **già assegnato** (guardia implementata nel
  plugin). Da confermare che non esistano casi limite in cui serva "annullare" un'assegnazione
  riportando il fascicolo allo stato non assegnato, anziché fare una nuova variazione.

---

## 6. Stampa/notifica lotto giornaliero

- Non essendo disponibile in ambiente demo alcuna integrazione O365/Word/OneDrive né un meccanismo di
  firma digitale automatizzabile, la "stampa PDF giornaliera con firma del Presidente" prevista dal
  manuale legacy è stata **sostituita** nel POC con una notifica email HTML riepilogativa (flusso
  "Notifica Lotto Giornaliero Assegnazioni"). **Da validare con il cliente** se questo soddisfa il
  requisito reale o se serve realmente il documento PDF con firma (in tal caso va progettata
  un'integrazione ad hoc, es. con un servizio di firma digitale esterno).
- Il connettore email nativo ("Mail"/SendGrid) risulta **bloccato dalla piattaforma Microsoft per i
  tenant nuovi** ("The Mail connector is currently restricted for new tenants"): il flusso è pronto e
  testato in tutte le sue parti tranne l'invio effettivo dell'email, che si sbloccherà automaticamente
  quando Microsoft abiliterà il connettore per questo tenant, oppure può essere sostituito con Office
  365 Outlook / Gmail / SendGrid con account esterno. **Non è un limite applicativo**, ma va comunque
  segnalato al cliente come dipendenza esterna.
- Il flusso individua i fascicoli "assegnati oggi" tramite `modifiedon` (non esiste un campo dedicato
  "data assegnazione"): da valutare se serve un campo esplicito per tracciare separatamente data di
  assegnazione da data di ultima modifica generica del fascicolo.

---

## 7. Dati demo e collaudo

- I 43 magistrati e i fascicoli di test caricati in ambiente demo sono **dati fittizi** creati per
  validare il motore di assegnazione, non dati reali del cliente. Da concordare un piano di
  migrazione/caricamento dei dati reali (magistrati effettivi, quote reali per sezione, storico
  fascicoli) prima del passaggio in produzione.
- Il test su ampia scala ha mostrato che, in assenza di storico su una categoria (PERC=0 per tutte le
  sezioni), la rotazione implementata distribuisce correttamente il carico — ma questo comportamento
  andrebbe **ri-validato con dati reali** (storico multi-anno), che potrebbero produrre dinamiche di
  turno diverse da quelle osservate in demo.

---

## 8. Sicurezza e ruoli

- Il ruolo `Operatore ASSPECA` e il team `Corte di Appello di Napoli - ASSPECA` sono stati creati con
  un set di privilege dedotto per analogia da ASPEN. Da validare con il cliente se sono necessari
  ruoli aggiuntivi (es. un ruolo "Presidente di Sezione" con permessi più ampi, o un ruolo di sola
  lettura per il personale amministrativo) non ancora previsti nel POC.

---

## Come usare questo documento

Ogni punto qui elencato rappresenta una **decisione presa in autonomia per poter procedere con il
POC**, motivata e documentata nel codice/nei log di sessione. Nessuno di questi punti blocca la
demo, ma **tutti richiedono conferma esplicita del cliente** prima di considerare la logica
"production-ready". Si suggerisce di rivedere il documento punto per punto in una call dedicata e
annotare qui accanto a ciascun punto la risposta ricevuta.
