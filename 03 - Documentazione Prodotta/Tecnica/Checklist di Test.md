# Checklist di Test — ASSPECA (end-to-end, dall'inizio del progetto)

> Checklist cumulativa di tutto ciò che è stato realizzato nel POC, da spuntare manualmente durante
> una sessione di collaudo con l'utente. Organizzata per area funzionale. Ogni voce corrisponde a
> qualcosa di già implementato e (salvo diversa indicazione) già testato dal team di sviluppo — questa
> checklist serve a farlo ri-validare/ri-verificare dall'utente finale.

---

## A. Anagrafiche e dati di riferimento

- [ ] Le 6 **Sezioni** sono presenti con le quote corrette di magistrati (8/9/8/8/5/5 = 43)
- [ ] Le **Categorie** di complessità (22 codici demo D01-D10/L01-L12) sono visibili e consultabili
- [ ] Le **Specializzazioni** (S1/S2/S3) sono presenti con la sezione di destinazione fissa associata
- [ ] I **Motivi di variazione** (Errore materiale, Incompatibilità, Scardinamento, Stralcio, Verifica)
  sono presenti e selezionabili
- [ ] I 43 **magistrati** demo sono presenti, ciascuno associato alla sezione corretta, con tipo
  (Presidente/Consigliere) e data di nomina valorizzati
- [ ] Un magistrato con **esonero/astensione** (es. 50% o 100%) è presente e riconoscibile

## B. Gestione Fascicolo d'appello

- [ ] È possibile creare un nuovo **Fascicolo d'appello** con categoria, specializzazione (opzionale),
  imputato e data di deposito
- [ ] Il pulsante **"Assegna Fascicolo"** è visibile sia sulla scheda del fascicolo sia nella
  griglia/elenco fascicoli
- [ ] Il pulsante **"Assegna Fascicolo"** è **disabilitato** su un fascicolo già assegnato

## C. Motore di assegnazione — Livello 1 (Sezione)

- [ ] Assegnando un fascicolo di una categoria con storico esistente, viene scelta la sezione con
  minor carico proporzionale (PERC) in quella categoria
- [ ] Assegnando più fascicoli consecutivi di una categoria **nuova** (mai vista prima, PERC=0 per
  tutte le sezioni), le assegnazioni **ruotano** tra sezioni diverse (non si concentrano sempre sulla
  stessa sezione) — verificare su almeno 8-10 fascicoli consecutivi
- [ ] Un fascicolo con **specializzazione** viene assegnato solo a una sezione compatibile con quella
  specializzazione
- [ ] L'indice di rotazione (`agc_configurazione` / "IndiceTurnoSezione") avanza ad ogni assegnazione

## D. Motore di assegnazione — Livello 2 (Magistrato)

- [ ] Tra i magistrati della sezione scelta, viene selezionato quello con **meno fascicoli** aperti in
  quella categoria
- [ ] A parità di conteggio, viene preferito il magistrato **più anziano** (nominato prima)
- [ ] Un **Presidente** viene escluso dall'assegnazione per le categorie che lo richiedono
- [ ] Un magistrato con **astensione 100%** non riceve mai un'assegnazione
- [ ] Un magistrato "nuovo" (0 fascicoli nella categoria) NON riceve automaticamente priorità assoluta
  (va in coda rispetto a chi ha già un conteggio basso ma non zero) — comportamento intenzionale da
  ri-validare col cliente (vedi "Punti Aperti da Validare con il Cliente", §2.2)

## E. Idempotenza e guardie

- [ ] Tentare di eseguire di nuovo "Assegna Fascicolo" su un fascicolo già assegnato viene **rifiutato**
  con un messaggio chiaro (nessuna doppia assegnazione)

## F. Variazione — Sezionale

- [ ] Creare una Variazione di tipo **Sezionale** su un fascicolo assegnato, indicando la nuova sezione
- [ ] Il magistrato relatore nella nuova sezione viene **ricalcolato automaticamente**
- [ ] Il fascicolo risulta aggiornato con la nuova sezione/magistrato
- [ ] Il record di Variazione mostra correttamente i valori "prima" e "dopo" (sezione e magistrato)

## G. Variazione — Magistrato

- [ ] Creare una Variazione di tipo **Magistrato** su un fascicolo assegnato, lasciando vuoto il nuovo
  magistrato → viene scelto automaticamente un magistrato **diverso** da quello uscente, stessa sezione
- [ ] Creare una Variazione di tipo **Magistrato** indicando manualmente il nuovo magistrato → il
  fascicolo viene aggiornato con quel magistrato specifico
- [ ] Il record di Variazione mostra correttamente i valori "prima" e "dopo"

## H. Variazione — validazioni

- [ ] Tentare di creare una Variazione su un fascicolo **non ancora assegnato** viene rifiutato con un
  errore esplicito
- [ ] Dopo un tentativo di Variazione rifiutato, non restano record "orfani" (nessuna variazione a metà,
  nessun fascicolo modificato parzialmente)

## I. Cruscotto (dashboard PCF)

- [ ] Il cruscotto è visibile nella scheda applicativa e mostra il carico di fascicoli per sezione
- [ ] Il cruscotto mostra il carico di fascicoli per magistrato
- [ ] I dati del cruscotto si aggiornano dopo una nuova assegnazione/variazione (verificare refresh)

## J. Notifica giornaliera del lotto assegnato

- [ ] Il flusso "Notifica Lotto Giornaliero Assegnazioni" è **attivo** e schedulato
- [ ] Il flusso individua correttamente i fascicoli assegnati/modificati nella giornata corrente
- [ ] La tabella HTML generata elenca correttamente i fascicoli del giorno
- [ ] Il ramo "nessun fascicolo assegnato oggi" non genera invii inutili (verificare la condizione)
- [ ] **Nota**: l'invio email è bloccato da una restrizione della piattaforma Microsoft sui tenant
  nuovi (connettore "Mail" non abilitato) — da riverificare quando Microsoft lo sblocca, oppure da
  sostituire con un altro connettore (Office 365 Outlook / Gmail / SendGrid esterno)

## K. Modelli di stampa Word

- [ ] Il modello Word **"Fascicolo Appello"** genera correttamente un documento con i dati del
  fascicolo assegnato
- [ ] Il modello Word **"Variazione"** genera correttamente un documento con i dati prima/dopo e il
  motivo della variazione

## L. Sicurezza

- [ ] Un utente associato al team **"Corte di Appello di Napoli - ASSPECA"** con ruolo
  **"Operatore ASSPECA"** può creare/consultare fascicoli e variazioni
- [ ] Lo stesso utente può **consultare** (ma non modificare) Sezioni, Categorie, Specializzazioni,
  Motivi
- [ ] Un utente non associato al team/ruolo **non vede/non accede** ai dati ASSPECA
- [ ] Verificare che i dati ASSPECA (`agc_fascicoloappello`, `agc_variazione`) siano effettivamente
  **isolati** da quelli di ASPEN (tabelle dedicate, non condivise)

## M. Applicazione model-driven

- [ ] L'app model-driven ASSPECA si apre correttamente e la sitemap mostra le voci attese (Fascicoli,
  Variazioni, Sezioni, Categorie, Magistrati, Cruscotto)
- [ ] Le viste principali (elenco fascicoli, elenco variazioni) mostrano le colonne rilevanti e sono
  filtrabili/ordinabili
- [ ] I form di Fascicolo e Variazione mostrano tutti i campi attesi, in sola lettura dove opportuno
  (es. campi "prima" della variazione)

## N. Dati demo

- [ ] I 20+12 fascicoli di test creati nelle sessioni di collaudo sono presenti e correttamente
  assegnati (o eliminabili/ignorabili se si vuole ripartire con dati puliti)
- [ ] Verificare se si desidera **ripulire** i dati demo prima di eventuali demo ufficiali al cliente,
  oppure tenerli come esempio

---

## Come usare questa checklist

Da rivedere una voce alla volta nella prossima sessione, spuntando quelle confermate funzionanti e
annotando eventuali difformità riscontrate accanto alla singola voce. Le voci relative a scelte/
assunzioni ancora da confermare con il cliente sono anche riportate nel documento **"Punti Aperti da
Validare con il Cliente"**: un test "funziona come previsto" non equivale automaticamente a un
"comportamento validato dal cliente" per quelle voci.
