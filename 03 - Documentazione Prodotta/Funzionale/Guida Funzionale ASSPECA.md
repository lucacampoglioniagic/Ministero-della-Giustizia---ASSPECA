# Guida Funzionale ASSPECA (spiegata in modo semplice)

> Questo documento spiega cosa fa l'applicazione ASSPECA e come funziona, senza dare per scontato
> nessuna conoscenza tecnica pregressa. È pensato per chi deve capire "a cosa serve" e "come si usa",
> non per chi deve svilupparla.

---

## 1. Di cosa si occupa ASSPECA

ASSPECA è l'applicazione che la **Corte di Appello di Napoli – Settore Penale** usa per decidere, in
modo automatico e tracciabile, **a quale Sezione e a quale Magistrato relatore assegnare ogni nuovo
fascicolo (procedimento penale d'appello)** che arriva in cancelleria.

Prima, questa scelta veniva fatta manualmente (o con un vecchio programma, ASSPECA legacy) seguendo
regole scritte in un manuale. ASSPECA nuova versione automatizza queste regole dentro Microsoft
Power Platform, in modo che:
- l'assegnazione sia sempre coerente con le regole ufficiali (nessun errore umano o preferenza);
- il carico di lavoro resti bilanciato tra le sezioni e tra i magistrati;
- ogni assegnazione e ogni successiva modifica restino **tracciate** (chi, quando, perché).

---

## 2. I concetti base (il "vocabolario" dell'app)

| Termine | Cosa significa |
|---|---|
| **Fascicolo (d'appello)** | Il procedimento penale che deve essere assegnato. Ha una categoria di complessità, un imputato, una data di deposito. |
| **Sezione** | Una delle sezioni penali della Corte d'Appello (nel POC ce ne sono 6). Ogni sezione ha un numero fisso di magistrati assegnati (le "quote"). |
| **Magistrato** | Un giudice della Corte. Può essere **Presidente** di sezione o **Consigliere**. Ogni magistrato appartiene a una sola sezione. |
| **Categoria** | Il tipo/complessità del procedimento (es. reati specifici, gravità, materia). Serve per contare il carico di lavoro "per categoria" (non un numero unico di punti, ma un conteggio per ogni categoria). |
| **Specializzazione** | Alcune materie (es. reati particolari) sono riservate a sezioni specifiche indipendentemente dal carico. |
| **Variazione** | Una correzione motivata di un'assegnazione già fatta (cambio sezione o cambio magistrato), con un motivo ufficiale (es. incompatibilità, errore materiale). |
| **Esonero** | Una riduzione (parziale o totale) della disponibilità di un magistrato a ricevere nuovi fascicoli (es. per malattia, altri incarichi). |

---

## 3. Come funziona l'assegnazione automatica (motore a 2 livelli)

Quando arriva un nuovo fascicolo, l'operatore preme un pulsante **"Assegna Fascicolo"** (presente sia
sulla scheda del fascicolo che nell'elenco/griglia). A quel punto l'applicazione decide da sola, in due
passi:

### Passo 1 — Scelta della Sezione
L'applicazione guarda, tra le sezioni compatibili con la specializzazione del fascicolo, quale sezione
ha proporzionalmente **meno fascicoli di quella categoria** rispetto al numero di magistrati che ha
(più magistrati ha una sezione, più fascicoli "le spettano"). Sceglie la sezione più scarica.

Per evitare che la stessa sezione venga scelta troppe volte di fila quando i conti sono in pareggio
(ad esempio con categorie nuove, mai assegnate a nessuno), l'applicazione fa anche **ruotare** un
piccolo gruppo di sezioni "in turno", così il carico si distribuisce nel tempo anche nei casi limite.

### Passo 2 — Scelta del Magistrato
Una volta scelta la sezione, l'applicazione guarda **tra i magistrati di quella sezione** e sceglie
quello che ha **meno fascicoli aperti in quella stessa categoria**. Se più magistrati sono in
pareggio, viene preferito quello più anziano (nominato prima). I Presidenti vengono esclusi per le
categorie che lo richiedono, così come i magistrati con un esonero totale (100%).

### Cosa succede se il fascicolo è già assegnato
Il pulsante "Assegna Fascicolo" si disabilita automaticamente sui fascicoli già assegnati: per
cambiare una decisione già presa **non si riassegna da capo**, si usa lo strumento di **Variazione**
(vedi sotto), che tiene traccia del cambiamento.

---

## 4. La Variazione: correggere un'assegnazione già fatta

A volte, dopo l'assegnazione automatica, serve correggere la decisione (es. il magistrato è
incompatibile con quel procedimento, oppure c'è stato un errore materiale). Per questo esiste la
**Variazione**, che si crea compilando: fascicolo interessato, tipo di variazione, motivo ufficiale
(scelto da un elenco predefinito) ed eventuali note.

Esistono due tipi di variazione:

- **Variazione Sezionale** — si cambia la sezione del fascicolo. Il nuovo magistrato relatore, nella
  nuova sezione, viene scelto **automaticamente** dall'applicazione (stessa logica del Passo 2 sopra).
- **Variazione Magistrato** — la sezione resta la stessa, cambia solo il magistrato relatore. Si può
  scegliere manualmente il nuovo magistrato, oppure lasciare che l'applicazione lo scelga in automatico
  (escludendo comunque il magistrato uscente, per garantire che il cambio sia reale).

Ogni Variazione registra sia la situazione "prima" che quella "dopo" (sezione e magistrato), creando
uno storico completo e verificabile di ogni correzione fatta su un fascicolo — utile per audit e per
rispondere a eventuali contestazioni.

Non è possibile creare una Variazione su un fascicolo che non è ancora stato assegnato: va prima
usato il pulsante "Assegna Fascicolo".

---

## 5. Il cruscotto (dashboard) del carico di lavoro

Per dare visibilità immediata sulla distribuzione del lavoro, l'applicazione include un **cruscotto**
(dashboard) integrato nella scheda applicativa, che mostra il carico di fascicoli per sezione e per
magistrato, aiutando a verificare a colpo d'occhio che il bilanciamento funzioni come atteso, senza
dover contare manualmente i fascicoli.

---

## 6. La notifica giornaliera dei fascicoli assegnati

Ogni giorno, in automatico, l'applicazione invia un **riepilogo** (via email, con una tabella HTML)
di tutti i fascicoli assegnati in giornata. Questo sostituisce, in versione semplificata, la vecchia
"stampa del lotto giornaliero con firma del Presidente" del sistema legacy (per la quale non è
disponibile, nell'ambiente demo, un meccanismo di firma digitale automatizzato — vedi il documento
"Punti Aperti da Validare con il Cliente" per i dettagli). L'invio effettivo dell'email è oggi
temporaneamente bloccato da una restrizione della piattaforma Microsoft per i tenant nuovi, ma il
flusso è completo e pronto: si attiverà automaticamente non appena la restrizione sarà rimossa.

---

## 7. I modelli di stampa Word

Sono disponibili due modelli di documento Word, generabili direttamente dalla scheda del fascicolo o
della variazione, per produrre in automatico un documento formattato pronto per la stampa/archiviazione:
- **Fascicolo Appello** — riepilogo completo del fascicolo e della sua assegnazione;
- **Variazione** — provvedimento di variazione, con i dati "prima/dopo" e il motivo ufficiale.

---

## 8. Chi può fare cosa (sicurezza)

Gli utenti dell'applicazione (personale della Corte d'Appello) fanno parte di un gruppo dedicato
("Corte di Appello di Napoli - ASSPECA") a cui è associato un ruolo (**Operatore ASSPECA**) che
permette di: creare/gestire fascicoli e variazioni della propria sezione/ufficio, e consultare (in
sola lettura) le tabelle di configurazione condivise (Sezioni, Categorie, Specializzazioni, Motivi),
che possono essere modificate solo dagli amministratori di sistema.

---

## 9. In sintesi: il percorso di un fascicolo

1. Il fascicolo arriva e viene registrato in ASSPECA (categoria, imputato, data deposito, eventuale
   specializzazione).
2. L'operatore preme **"Assegna Fascicolo"** → l'app sceglie automaticamente Sezione e Magistrato.
3. Il fascicolo compare nella **notifica email giornaliera** del lotto assegnato.
4. Se necessario, si registra una **Variazione** (Sezionale o Magistrato) con motivo ufficiale — la
   modifica aggiorna il fascicolo e resta tracciata nello storico.
5. In ogni momento, il **cruscotto** mostra la situazione aggiornata del carico per sezione e per
   magistrato.
6. Su richiesta, si può generare il **documento Word** di riepilogo del fascicolo o della variazione.

---

## 10. Cosa NON fa (ancora) l'applicazione

Per completezza, alcuni aspetti del manuale legacy non sono (ancora) implementati nel POC attuale e
sono elencati nel dettaglio nel documento **"Punti Aperti da Validare con il Cliente"**: ad esempio la
stampa PDF firmata digitalmente, il criterio "trimestre/mese di nascita dell'imputato" per alcune
categorie, e la rotazione semestrale delle sezioni specializzate.
