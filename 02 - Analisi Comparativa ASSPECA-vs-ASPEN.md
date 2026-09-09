# ASSPECA vs ASPEN – Analisi comparativa e progettazione di massima POC

**Cliente:** Ministero della Giustizia – Corte di Appello di Napoli (ufficio pilota) e Corte di Appello di Firenze (secondo ufficio di riferimento)
**Fornitore:** AGIC Technology
**Data:** 09/09/2026
**Scadenza POC:** 15/09/2026
**Stato:** Analisi documentale – nessuna implementazione eseguita, nessuna modifica all'ambiente Dataverse `LCC-MINISTEROGIUSTIZIA-DEMO` né al repository ASPEN

---

## Fonti analizzate

| # | Documento | Cartella | Leggibilità | Note |
|---|---|---|---|---|
| F1 | `MANUALE ASSPECA_DOC.pdf` (28 pag., Corte di Appello di Napoli – Reparto Informatica, 2018, P. Montella) | ASSPECA/Documentazione | ✅ testo completo + screenshot | Manuale ufficiale dell'applicativo AS-IS |
| F2 | `criteri assegnazione.docx` | ASSPECA/Documentazione | ✅ | Estratto tabellare Napoli: codici di difficoltà 1–20 (+7bis, 12bis), codice S3, regole per sezioni ordinarie/semispecializzate |
| F3 | `ASPECA _ Incontro con AGIC e Microsoft.docx` (trascrizione Teams 06/08/2026, 1h05) | ASSPECA/Documentazione | ✅ | Demo live di ASSPECA da parte di E. Galano (Registro penale CA Napoli), con A. Maddalena (magistrato CA Napoli) e G. Borraccia (CA Firenze, II sez. penale) |
| F4 | `Meeting ASPEN-ASPENCA - 2026-08-04 - Sintesi meeting.DOCX` | ASSPECA/Documentazione | ✅ | Sintesi AI del meeting 04/08 (demo ASPEN al cliente CA) |
| F5 | `Meeting ASPEN-ASPENCA - 2026-08-04 - Transcript.DOCX` (1h02) | ASSPECA/Documentazione | ✅ letto integralmente | Trascrizione completa; fonte primaria dei requisiti "target" |
| F6 | `STAMPA ASSPECA.pdf` (1 pag., scansione CamScanner) | ASSPECA/Documentazione | ⚠️ solo immagine – letta visivamente, nessun OCR | Report "Settore Penale – ASSEGNAZIONE – PROCEDIMENTI – lunedì 6 maggio 2024" |
| F7 | `Screen ASPECA.pptx` (8 slide) | ASSPECA/Documentazione | ⚠️ solo immagini – lette visivamente | Screenshot dell'app VB6 v.5.a: Anagrafica Magistrati, Inserimento Processi, Assegnazione Detenuti, Modifica dati, Variazione Sezionale (3 slide), Variazione Magistrati |
| F8 | `tabelle seconda sezione penale.pdf` (7 pag., pagg. 87–93 di 152, firmato A. Nencini 14/07/2025) | ASSPECA/Documentazione | ✅ | **Tabelle della Corte di Appello di FIRENZE** (3 sezioni penali), non di Napoli – cfr. §1.3 |
| F9 | `Link Video_Trascrizione_Sintesi_Overview ASPECA.txt` | ASSPECA/Documentazione | ✅ | Solo link Teams alla registrazione del 06/08 (contenuto = F3) |
| A1 | `README.md` | ASPEN | ✅ | Stato POC ASPEN al 09/09/2026: modello dati, PCF, ribbon, plugin, custom page |
| A2 | `02 - Analisi/AS-IS/ASPEN - Analisi AS-IS.md` | ASPEN | ✅ | AS-IS ASPEN2 Palermo (canestri, pesatura, tappo, GIP virtuale) |
| A3 | `SESSION_NOTES.md` (sessioni 07/2026 → 09/09/2026) | ASPEN | ✅ sezioni recenti | Esoneri, RGNR, carico monotono, riserva GUP, Modifica Carico, migrazione contact |
| A4 | `aspen_resoconto_modifiche.pdf` | ASPEN | ✅ | Gap analysis 3.1–3.10 + roadmap Fase 1–4 |
| A5 | `03 - Documentazione Prodotta/Funzionale/ASPEN - Analisi e documentazione.docx` | ASPEN | ✅ | Documento funzionale AGIC 04/06/2026 – §2.4 e §3.5 già citano ASSPECA |
| A6 | `05 - Power Platform/*` (struttura file) | ASPEN | ✅ | Componenti effettivamente costruiti |
| — | `02 - Analisi/TO-BE/`, `03 - Documentazione Prodotta/Tecnica/`, `05 - Power Platform/Dataverse/`, `Power-Automate/` | ASPEN | ⚠️ **vuote** (solo `.gitkeep`) | Nessun documento TO-BE/tecnico formale; la fonte tecnica è README + SESSION_NOTES |

---

## 1. Executive summary

### 1.1 Cos'è ASSPECA
**ASSPECA** ("procedura di ASSEGNAZIONE dei fascicoli penali", da F1 p.1) è l'applicativo in uso presso la **Corte di Appello di Napoli – Settore Penale** per l'assegnazione automatica dei procedimenti penali di secondo grado **prima alle sezioni e successivamente ai magistrati**, "in base a dei criteri di trasparenza designati dallo stesso Presidente della Corte" (F1 p.1). Fornisce inoltre un **registro delle assegnazioni** con ricerche, statistiche e stampe da distribuire alle sezioni (F1 p.1).

Caratteristiche tecniche AS-IS (F1 p.1, F7): VB6 + ADO, client-server, DB **Microsoft Access** su server virtualizzato, backup giornaliero; versione in uso **V.5.a**; sviluppato nel 2018 e "non modificabile" (F5, A. Maddalena 36:20: "è datato, non è modificabile … se si vuole cambiare qualcosa non si può fare, quindi questo è il limite principale"). È "l'unico ufficio [in Italia] che utilizza" un sistema di assegnazione automatica in Corte d'Appello (F5 37:08).

Dimensioni (F3 12:02, F7 slide 1): **6 sezioni penali**, ~40 magistrati attivi (107 record in anagrafica incl. cancellati), **20 categorie operative / 22 parametrizzate**, distinte per **Presidenti (P), Consiglieri (C), Facenti Funzioni (F), Magrif (M)**.

### 1.2 Per chi
- **Ufficio pilota:** Corte di Appello di Napoli. Interlocutori: dott.ssa **Alessandra Maddalena** (magistrato, membro della commissione che verifica il funzionamento del sistema, F5 36:20), sig. **Ernesto Galano** (Registro penale / amministratore applicativo, F3). Gli utenti effettivi oggi sono **solo il Registro penale e l'amministratore di sistema** ("due uffici"); i magistrati **non accedono** all'applicativo (F3 47:33–48:01).
- **Secondo ufficio di riferimento:** Corte di Appello di **Firenze**, dott. **Giampiero Borraccia** (II sezione penale), che ha fornito le proprie tabelle (F8) e **non dispone oggi di assegnazione automatica** (F3 5:54, 1:02:25 "nel tempo non c'è una perequazione e questo sarebbe importante per noi").
- Lato AGIC: Chiara D'Innocenzi, Gloria Violi, Riccardo Vedovato, Leonardo Nanni.

### 1.3 Ambito applicativo e chiarimento sul file "tabelle seconda sezione penale.pdf"
Il file F8 **non riguarda Napoli**: è firmato da Alessandro Nencini (Presidente CA Firenze), parla di "**tre sezioni penali**" (Napoli ne ha sei) ed è il documento che Borraccia annuncia di condividere in F3 56:24 ("vi faccio vedere velocemente come è organizzato il nostro ufficio … ve lo mando"). Descrive: competenza tabellare della II sezione penale di Firenze per elenco reati (§102), criteri integrativi temporali per materie trasversali (§105), esame preliminare delle impugnazioni con **valore ponderale 1–7** (§87). Va quindi trattato come **secondo set di regole di configurazione**, non come specifica dell'ufficio pilota.

### 1.4 Relazione con ASPEN / ASPENCA
- La famiglia è documentata in A2 §1 e A5 §2: ASPEN (Milano GIP, dismesso), ASPEN2 (Palermo GIP, in produzione), **ASPENCA** ("installato, non usato" a Palermo, "pensato per Corte d'Appello"), **ASSPECA** ("Napoli – estensione di ASPENCA", "22 categorie D/L, livelli di profilazione 0,1,2,3,254,255,256" – A5 §2.4). Il presente lavoro conferma tutti questi dati (F1 p.3, p.28).
- Radice comune confermata dal cliente: "il Presidente della Corte di appello che ha ideato questo sistema … veniva proprio dall'Ufficio GIP di Napoli, quindi era un conoscitore esperto del sistema ASPEN del GIP" (F3 38:52). Meccanismi identici: perequazione del carico, nuovo magistrato inserito "in coda"/al valore più basso (F3 38:21 Vedovato: "anche nell'assegnazione GIP, quando arriva un nuovo magistrato gli viene dato il punteggio più basso").
- **Differenza strutturale principale**: in ASPEN (GIP/GUP) l'assegnazione è **diretta al magistrato per canestro/materia** con carico = **somma dei pesi**; in ASSPECA (Corte d'Appello) l'assegnazione è **a due livelli, prima alla sezione poi al magistrato**, il carico è un **conteggio di fascicoli per categoria** (non una somma pesata), e intervengono il **trimestre di nascita del primo imputato**, la **data di deposito**, l'**anzianità del magistrato** e le **percentuali di astensione** (F1 p.8–9, F3 14:07–20:33).
- Il cliente stesso posiziona la nuova app come "reingegnerizzazione" partendo da ASPEN + ASSPECA + criticità (F5 34:22 Chiara D'Innocenzi: "la commistione di tutti e tre i contesti … realizzare l'applicazione ad hoc per voi"). Riccardo Vedovato in F5 17:57: "il concetto di sezione e di appartenenza di un magistrato alla sezione … questo non c'è oggi. Quindi questo è un nuovo requisito … per ASPENCA dovremmo svilupparlo".

---

## 2. Requisiti funzionali di ASSPECA

### 2.1 Entità / tabelle dati necessarie (AS-IS → target)

| Entità | Attributi rilevati | Fonte |
|---|---|---|
| **Sezione** (`Tab_Sezioni`) | Numero sezione (1–6), **numero magistrati anche frazionario/percentuale** (es. 7,75 → 8,75 dopo inserimento di un consigliere; 0,125/… nei PERC), flag CANC; il numero magistrati va aggiornato **manualmente** ad ogni variazione di anagrafica (criticità) | F1 p.5; F3 16:55, 34:27–34:41; F7 slide 5 (griglia SEZ/MAG: 008, 009, 008, 008, 050, 005) |
| **Magistrato** (`Anagrafe Magistrati`) | ID_MAG, Cognome, **DATA_POS** (data di nomina/"posizione", usata come tie-break: a parità "viene scelto come primo magistrato quello più giovane"), **TIPO** P=Presidente / C=Consigliere / F=Facente Funzioni / M=Magrif, SEZ (una sola), CANC (flag cancellazione), **ASTENSIONE %** (es. 25% Magrif settore penale, 50% membro Consiglio giudiziario), sospensione temporanea | F1 p.6; F3 12:36, 28:14, 30:03–31:13; F7 slide 1 |
| **Categoria** (`Tab_Categorie`, 22 parametriche) | Codice (D01…D05 detenuti, L06…L20 liberi, + 7bis, 12bis), descrizione, tipo **D/L**, eventuale legame a **specializzazione**, esclusione per Presidenti sulle categorie più pesanti; è la "pesatura" del fascicolo | F1 p.10, p.28; F2; F3 20:33; F6 ("L11 - Liberi fino a 5 e almeno uno (art.416,416bis,74"); F7 slide 7 ("D01 - Detenuti fino a 5 imputati") |
| **Specializzazione / materia** | Codici **S1, S2, S3** con mappatura fissa sezione↔semestre di nascita (S1: sez.1 gen-giu / sez.2 lug-dic; S2: sez.3 / sez.6; S3: sez.4 / sez.5); S3 = reati ambientali, prostituzione L.75/1958, reati sessuali 609bis-octies; "la quinta sezione penale tratta tutti gli altri reati non rientranti nella specializzazione" | F1 p.8; F2 |
| **Fascicolo / Registro Assegnazioni** | ID_Progressivo (auto), **N° Reg. Appello + Anno**, **N° Reg. PM + Anno + Ufficio** (combo), **Categoria** + Cod_Cat, **Data Deposito** (+ "Ultima Data Deposito" della categoria), **Data Iscrizione**, **Mese di nascita** del 1° imputato in ordine alfabetico, **Imputato** (cognome nome), **SPECIALIZZATE** (codice + flag), Note, **Sezione assegnata**, **Magistrato** (codice + nome), User, Data Sistema; controlli di duplicazione su N° Reg + Data Deposito; campi obbligatori | F1 p.12–13; F7 slide 2, 4 |
| **Storico movimentazioni** ("scheda storica", `D.Storico`) | Tutte le modifiche apportate nel tempo dai diversi utenti sull'istanza (utente, data/ora); storico categorie per DATA_LAV | F1 p.11, p.13; F7 slide 4/6 pulsante "D.Storico" |
| **Variazione (sezionale / magistrato)** | N° Reg. Appello, Anno, Data iscrizione, Spec, Sezione → Nuova Sez., **Motivo** (tabella `Tab_Motivi`: *Errore materiale, Incompatibilità, Scardinamento, Stralcio, Verifica*), Nota (es. data del provvedimento del Presidente/coordinatore), Categoria, Magistrato prima/dopo; visualizzazione **tabelle PERC prima/dopo** | F1 p.17–18, p.28; F3 44:38–47:12; F7 slide 5–8 |
| **Tabelle di appoggio per il calcolo** (`CAT_n`, `CAT_APP_n`, `CAT_APP_nDEF`; `MAG_CATn`, `MAG_CATn_APP`, `MAG_CATn_DEF`) per ciascuna categoria | Sezione: Id, SEZ, **PERC**, **MAGI** (n. magistrati), **FASC**, **DATA_DEP**, **FASC_INI**, **PERC1**; Magistrato: SEZ, ID_MAG, Cognome, DATA_POS, MAX_GIU, FASC, PERC. Pulsanti "Attiva scrittura", "Ricalcola", "Calcola perc iniziale", "Ins fasc", "Astensioni" | F1 p.7, p.9 (screenshot); F3 31:13–37:07 |
| **Controllo Data Deposito** | Per ogni categoria: data deposito "aggiornata" (= data dell'ultimo lotto di assegnazione per quella categoria) + flag inizializzazione | F1 p.10; F3 23:28–27:27 |
| **Utenti** (`Tab_utenti` / `USERS_DESC`) | User, password criptata, **LIVELLO** 0/1/2/3/254/255/256 | F1 p.2–3 |
| **Avvocati/ricerche** | Consultazione per cognome imputato o n° PM su **copia di backup** separata | F1 p.24 |

### 2.2 Logica di pesatura

In ASSPECA la "pesatura" **non è un punteggio sommabile**: è la **classificazione del fascicolo in una categoria**, e il carico è il **numero di fascicoli ricevuti per categoria** (F3 40:40–41:32; F5 50:35 Maddalena: "il peso è dato dal numero degli imputati, detenuti, faldoni eccetera, non dalla materia"; "le specializzate non hanno un peso diverso, hanno solo una destinazione diversa").

**Codici di difficoltà Napoli (F2)** – dimensioni: *stato dell'imputato* (detenuti/liberi) × *numero imputati* (≤5, 6–10, 11–30, >30) × *allegati* (con allegati; 3+ allegati; >5 allegati) × *reati DDA* (artt. 416, 416bis, 74 DPR 309/90, aggravante art.7 L.203/91):

| Cod. | Descrizione | Cod. | Descrizione |
|---|---|---|---|
| 1 | Detenuti fino a 5 imputati | 11 | Liberi fino a 5, reati DDA |
| 2 | Detenuti fino a 5 con allegati | 12 / 12bis | Liberi fino a 5, DDA, con allegati / >5 allegati |
| 3 | Detenuti 6–10 | 13 | Liberi 6–10, DDA |
| 4 | Detenuti 11–30 | 14 | Liberi 11–30, DDA |
| 5 | Detenuti >30 | 15 | Liberi >30, DDA |
| 6 | Liberi fino a 5 (**categoria più usata**, F3 17:56) | 16 | Detenuti fino a 5, DDA |
| 7 / 7bis | Liberi fino a 5 con allegati / con 3+ allegati | 17 | Detenuti fino a 5 con allegati, DDA |
| 8 | Liberi 6–10 | 18 | Detenuti 6–10, DDA |
| 9 | Liberi 11–30 | 19 | Detenuti 11–30, DDA |
| 10 | Liberi >30 | 20 | Detenuti >30, DDA |

Regole aggiuntive (F2): concorso di reati specializzati → il più grave determina la sezione; se **uno solo** è specializzato è quello a determinare la competenza; giudizi di **rinvio dalla Cassazione** trattati come appelli ordinari, ma se l'assegnazione ricadrebbe sulla sezione che ha emesso la sentenza cassata → sezione immediatamente successiva.

**Pesatura Firenze (F8 §87.3–87.6)** – completamente diversa: **valore ponderale 1–7** attribuito in sede di esame preliminare dal Presidente di sezione/consiglieri delegati, basato su n. appellanti, complessità dei motivi di impugnazione e struttura della sentenza; **+1** se misura cautelare personale (1–5 appellanti; oltre, +1 ogni 5); **+1** se parti civili costituite; **+1/+2 discrezionali** per mole documenti, n. difensori, complessità fonti, n. parti, novità della materia. L'attività di esame preliminare a turno **concorre al carico mensile** del consigliere (§88.2).

**Requisito target (F5 26:55–30:55, F4)**: *pesatura multi-criterio configurabile per ufficio*: numero imputati, numero detenuti, faldoni/allegati, **motivi di appello**, **parte civile**, reati DDA, materia; alcuni criteri automatizzabili, altri con "possibilità di intervenire manualmente e dire questo è più complesso" (F3 1:02:40). Il "peso" non è necessariamente attributo della materia/canestro ma del fascicolo (F5 27:53).

### 2.3 Logica di assegnazione (algoritmo ASSPECA – F1 p.8–9, p.15–16; F2; F3)

**Livello 1 – Sezione (per ogni categoria, separatamente):**
1. Parametri: mese di nascita 1° imputato (ordine alfabetico), data deposito, n. fascicoli assegnati per sezione, n. magistrati per sezione (netto astensioni/Presidente).
2. `PERC = N° fascicoli assegnati alla sezione (per quella categoria, dalla data di avvio 2018) / N° magistrati della sezione`.
3. Ordinamento sezioni per `PERC, DATA_DEP, SEZ` (parità → numero d'ordine sezione).
4. **Solo le prime 4 sezioni** su 6 entrano in turno quel giorno; le altre 2 sono escluse.
5. Alle 4 sezioni in turno si abbina un **trimestre di nascita** in ordine: 1ª più scarica → gen-mar, 2ª → apr-giu, 3ª → lug-set, 4ª → ott-dic.
6. **Assegnazioni specializzate** (S1/S2/S3): *non* dipendono dall'ordinamento; sezione fissa in base al semestre di nascita. Il maggior carico delle specializzate è compensato "con la materia semispecializzata"/ordinaria nella stessa giornata (F2; F5 39:43–40:14).
7. Riequilibrio **quotidiano**; l'assegnazione avviene **a lotti per categoria e data deposito** ("oggi assegniamo tutta la categoria 6 arrivata, domani la categoria 1", F3 42:42), separando i form **Assegnazioni Detenuti** e **Assegnazioni Liberi** (F1 p.15–16; F7 slide 3).

**Livello 2 – Magistrato (all'interno della sezione, per categoria):**
- Ordinamento `[SEZ, FASC, DATA_DEP, DATA_POS]`: il fascicolo va al magistrato con **meno fascicoli in quella categoria**; a parità decide la **data di nomina** (F1 p.9; F3 30:03, 37:07–40:19).
- Il **Presidente** è conteggiato come magistrato ma **escluso dalle categorie più pesanti** (F3 20:33).
- Le **astensioni %** riducono la quota del magistrato nel conteggio della sezione (F3 12:36; F1 p.6, p.9 "Permette di gestire le astensioni").
- **Nuovo magistrato**: "viene messo in coda" = gli si attribuisce fittiziamente lo stesso numero di fascicoli dell'ultimo in classifica per ogni categoria, così non riceve tutto subito (F3 31:29–38:33; F5 41:13).
- Il fascicolo **resta incardinato nella sezione** anche se il magistrato cambia sezione: si riassegna a un altro magistrato della stessa sezione (F5 18:26–19:02).

**Operatività**: i dati sono inseriti **manualmente** dal Registro penale (nessun collegamento con SICP/registri/CCP – F4, F5 38:09); l'ufficio individua la categoria; poi "protocollazione" = assegnazione automatica di lotto; **stampa dell'assegnazione** in ordine data deposito/categoria/sezione/magistrato che "accompagna i fascicoli in sezione" e serve al cancelliere per verifica (F1 p.15; F3 49:16); comunicazione al Registro Generale di sezione e relatore (F3 44:20).

**Assegnazioni manuali / rettifiche (F1 p.17–18, F3 43:56–47:12, F7 slide 5–8):**
- *Modifica categoria* (errore in inserimento) → riassegnazione automatica.
- *Variazione Sezionale* (su provvedimento del Presidente/coordinatore): cambia sezione, motivo tabellato, il magistrato è ricalcolato automaticamente nella nuova sezione secondo l'algoritmo; vista "Prima/Dopo" delle tabelle PERC.
- *Variazione Magistrato* (la più usata, es. incompatibilità): stessa sezione, scelta manuale da lista magistrati della sezione o automatica.

**Requisiti target emersi nei meeting (F4, F5, F3):**
- Assegnazione **prima per sezione, poi per magistrato** (F5 18:10 Maddalena: "per sezioni più che per magistrato e poi magistrato all'interno della sezione"; posizione Borraccia 48:15/49:47: si potrebbe assegnare direttamente al magistrato "in quanto sta in quella sezione", ma la perequazione a monte tra sezioni evita udienze sovraccariche – 52:08). **Decisione ancora aperta**, da simulare con dati reali (F5 53:21).
- Il **collegio** è irrilevante per l'assegnazione automatica (F5 54:22–54:35).
- **Co-assegnazione** a più magistrati per processi complessi: **peso pieno a ciascun coassegnatario** (F5 21:53–23:43).
- **Assise** fuori perimetro (sistema di assegnazione distinto, F5 55:23–55:34).
- **Data udienza** fuori perimetro (gestita in CCP; evitare doppio inserimento, F5 56:05–57:19); evoluzione futura: aggregazione fascicoli in "udienze omogenee per peso" (F5 58:24).
- **Perequazione su arco temporale / periodo di servizio** del magistrato (F5 8:19 Borraccia); chiusura fascicolo **opzionale/configurabile** per ufficio (F5 7:25).
- **Configurabilità per ufficio** come requisito primario: "l'applicativo che deve adeguarsi all'organizzazione, non il contrario" (F3 2:07), variabile "ufficio" nelle configurazioni (F3 0:43 D'Innocenzi).
- **Simulazioni con dati reali** prima della fase operativa (F5 53:21).

### 2.4 Ruoli utente (F1 p.3, F3 48:11–49:09)

| Livello | Descrizione | Note target |
|---|---|---|
| 0 | Lettura | |
| 1 | Inserimento, Modifiche, Visualizza e Stampa (**Cancelleria/Registro penale**) | Utente operativo principale |
| 2 | Inserimento, Modifica, Visualizza e Stampa (**Magistrati**) | Oggi i magistrati **non accedono** (F3 48:01); il target prevede accesso personale (F4) con visibilità dei carichi "personalizzabile in base alle scelte dell'ufficio" (F4, F5 4:19–4:42) |
| 3 | Visualizza e Stampa | |
| 254 | Superuser | |
| 255 | Vice Amministratore | Utility (annulla base dati, crea categorie di appoggio, sblocco record, gestione tabelle) riservate ad admin/supervisore (F1 p.25–28) |
| 256 | Amministratore | |

### 2.5 Stampe e reportistica

| Report / funzione | Contenuto | Fonte |
|---|---|---|
| **Stampa assegnazione giornaliera** "Settore Penale – ASSEGNAZIONE – PROCEDIMENTI – <giorno data>" | Intestazione con stemma; per riga: **Id, Categoria (es. "L11 - Liberi fino a 5 e almeno uno (art.416,416bis,74"), Anno, N°RG, Sez., Spec, Data Dep., Magistrato**; sottoriga **Imputato** e **Mese_Nas**; riga **Note** (es. "ACR - NO 4^ - PERV. DA CASSA 1 FASC. IL 01/3/2023", "1 FALDONE"); piè di pagina con totale record ("8 Totale"), pagina, data, firma "**Il Presidente**" | F6; F1 p.15–16 |
| **Stampe Registro Assegnazioni** | Filtri: data deposito, sezione, categoria, id fascicolo; raggruppamento per categoria e sezione; campi categoria, sezione, magistrato, fascicolo, data deposito e iscrizione | F1 p.20 |
| Report "**Assegnazione Procedimenti Penali – Pervenuti in data**" | Per categoria (banda rossa "D03 - Detenuti oltre 5 e fino a 10 imputati") → per sezione (badge n.) → righe Cognome magistrato, Sezione, Cod_Mag, RG, Anno, Data_Dep, Data_Isc; "TOT sez" | F1 p.21 (screenshot) |
| **Stampe Commesse** | Per data iscrizione, categoria, id | F1 p.23 |
| **Statistiche (Pivot)** | Per data deposito, anno iscrizione, categorie; estrazioni per sezione/magistrato/categoria/periodo con totale | F1 p.19; F3 47:16–52:31 |
| **Formato/archiviazione** | Layout orizzontale e verticale; **PDF archiviato in cartella condivisa**; anche export **Excel** | F1 p.1; F3 52:54–53:19 |
| **Ricerche Avvocati** | Per cognome imputato o n° PM, su copia di backup | F1 p.24 |
| Verifica prima/dopo | Tabelle PERC sezione/magistrato prima e dopo una variazione | F1 p.17–18; F7 slide 7 |

### 2.6 Specificità della sezione penale di Corte d'Appello (Firenze, F8) da supportare come configurazione
- **Competenza per materia** con elenco articolato di reati (§102.1): PA (314–335 c.p.), attività giudiziaria, fede pubblica, economia pubblica, famiglia, libertà individuale, stupefacenti 73/74/79/81–83 DPR 309/90, furti 624–626, rinvii dalla Cassazione già di competenza della I sezione, incidenti di esecuzione, revisioni, riparazioni per ingiusta detenzione, estradizioni/MAE, rogatorie, liquidazione spese.
- **Criteri integrativi temporali** (§105.1): art.73 → II sez. nei mesi gen/apr/lug/ott, III sez. negli altri; art.74 → rotazione quadrimestrale sulle 3 sezioni; furti → mesi predefiniti per sezione; associazioni 416/416bis "pure" → I sez.; con reati-fine → sezione del reato-fine più grave; rogatorie/estradizioni → rotazione mensile; nuove leggi → rotazione per ordine di iscrizione; esecuzione condanne civili → sezione "incrociata".
- **Criterio di attribuzione basato sul mese di iscrizione** (non sul mese di nascita dell'imputato come a Napoli) — F3 5:34 Borraccia: "i furti che arrivano nel primo trimestre vanno … alla prima sezione"; dichiara che, con l'assegnazione automatica, le tabelle potrebbero essere adeguate per usare la pesatura (F3 5:54–6:38).
- **Nessuna assegnazione automatica al magistrato**: oggi Presidente di sezione/collegio distribuisce (F5 46:33), perequazione manuale solo sui processi grossi in riunione di sezione (F3 1:01:27).

---

## 3. Confronto puntuale ASSPECA vs ASPEN

Legenda: **=** UGUALE · **≈** SIMILE (con differenze) · **≠** COMPLETAMENTE DIVERSO

| Area | ASPEN (AS-IS legacy + POC attuale) | ASSPECA (AS-IS + target richiesto) | Esito |
|---|---|---|---|
| **Contesto organizzativo** | Tribunale, Ufficio GIP/GUP: assegnazione **diretta al magistrato** (A2 §5; F5 19:16 "il GIP l'assegnazione è al magistrato perché è singolo") | Corte d'Appello: assegnazione **alla sezione e poi al magistrato relatore**; il fascicolo "rimane incardinato nella sezione" (F1 p.1; F5 18:26) | **≠** |
| **Modello dati – fascicolo** | `agc_fascicolo2`: Numero RG, N. imputati, N. imputazioni, Canestro, Punti imputati/imputazioni, Peso calcolato, Magistrato (contact), Stato (Validato/Proposto/Chiuso), Data, RGNR (lookup padre `agc_rgnr`), Ruolo assegnazione GIP/GUP (A1, A3 28/08) | N° Reg. Appello+Anno, N° Reg. PM+Anno+Ufficio, Categoria, Data Deposito, Data Iscrizione, **Mese nascita 1° imputato**, Imputato, **Specializzata**, Note, **Sezione assegnata**, Magistrato, storico (F1 p.12–13; F7) | **≠** – **decisione architetturale (09/09/2026): NON estendere `agc_fascicolo2`**, ma creare una **tabella dedicata `agc_fascicoloappello`**. Motivo: segregazione dati/sicurezza (v. §6.4) e modello semanticamente diverso (~8 attributi mutuamente esclusivi, nessun peso numerico) |
| **Modello dati – magistrato** | `contact` con `agc_ismagistrato`, `agc_caricoattuale` (persistito), `agc_utenteassociato`→systemuser, `agc_ruolomagistrato`, BU per tribunale (A1, A3 27–28/08) | Cognome, **DATA_POS** (anzianità), **TIPO P/C/F/M**, **SEZ** (una sola), **ASTENSIONE %**, CANC (F1 p.6; F7 slide 1) | **≈** – manca Sezione, Tipo, Data nomina; carico singolo vs **carico per categoria** |
| **Modello dati – sezione** | **Non esiste** (segregazione solo per BU/tribunale; "sezioni interne al tribunale" rinviate – A4 §3.8) | Entità centrale: n. magistrati (frazionario), specializzazioni, ordine (F1 p.5; F7 slide 5) | **≠** – da costruire |
| **Canestri vs Categorie** | Canestro = **materia/tipo reato** con **peso** (`agc_canestrofascicolo`: nome + peso; 13 record), contributo additivo al peso (A1, A2 §3–4) | Categoria = **classe di complessità** (D/L × n. imputati × allegati × DDA), 22 codici parametrici; la **materia** è separata (S1/S2/S3) e determina solo la **destinazione**, non il peso (F2; F5 50:35) | **≠** – semantica diversa; la tabella nome+peso è riusabile solo come contenitore |
| **Pesatura** | Peso = 1 + fasce imputazioni + fasce imputati + peso canestro (A2 §4; A1 `agc_pesocalcolato`); carico = **somma pesi** (`agc_caricoattuale`) | Nessun peso numerico: **conteggio fascicoli per categoria**, con classifiche separate per ognuna delle 20/22 categorie (F3 24:39–25:39); target: multi-criterio configurabile (imputati, detenuti, allegati, motivi appello, parte civile) (F5 28:27–30:37); Firenze: valore ponderale 1–7 + incrementi (F8 §87) | **≠** |
| **Algoritmo di assegnazione** | Magistrato con **carico minore in assoluto** (POC, F5 13:20) / per canestro (legacy Palermo, A2 §5); assegnazione singola + **massiva sequenziale**; continuità RGNR; esclusione esonero Totale; coefficiente esonero Parziale; riserva GUP (A1, A3 28/08–08/09) | **Due livelli**: (1) sezione: PERC = fascicoli/magistrati per categoria, ordinamento PERC/DATA_DEP/SEZ, **prime 4 su 6**, **trimestre di nascita**, specializzate a sezione fissa per semestre; (2) magistrato: meno fascicoli nella categoria, tie-break **anzianità**; lotti giornalieri per categoria, Detenuti/Liberi separati (F1 p.8–9, 15–16; F2) | **≠** – solo il pattern "min carico + aggiornamento sequenziale" è riusabile |
| **Nuovo magistrato** | Legacy Palermo: valore minimo −15% (A2 §5.2); POC: baseline non definita, carico monotono (A1 punto 21) | "Messo in coda": fittiziamente pari al **massimo** della classifica per ogni categoria (F3 31:29; F5 41:13) | **≈** – stesso principio (evitare che riceva tutto), regola opposta (min vs max) |
| **Esoneri / astensioni** | `agc_esonero` (Totale/Parziale, %, date, stato), `EsoneroRientroPlugin` (foto carico + riallineamento al "collega più simile"), flow chiusura automatica, blocco assegnazione con esonero Totale (A3 08–09/09) | **Astensione %** stabile sull'anagrafica (25% Magrif, 50% Consiglio giudiziario) che riduce la quota del magistrato nel conteggio sezione; sospensione temporanea; CANC (F3 12:36, 30:33–31:13) | **≈** – riusabile la tabella Esoneri; differente l'effetto (riduce il denominatore "n. magistrati sezione" anziché moltiplicare il peso) |
| **Incompatibilità / esclusioni** | Legacy: "tappo" 1000 / 99.999.999 (A2 §6; A5 §3.4); POC: selezione incompatibili nel dialog (non persistite), esonero Totale blocca (A1) | Gestite **ex post** come *Variazione Magistrato* con **motivo tabellato** (Incompatibilità, Errore materiale, Scardinamento, Stralcio, Verifica) su provvedimento del Presidente; nessun tappo (F3 44:38–47:12; F7 slide 6, 8) | **≈** – concetto comune, workflow diverso (rettifica motivata post-assegnazione con audit, richiesta anche in A4 §3.7) |
| **Riassegnazione / cambio sezione** | Modifica lookup magistrato sul form + handler `addOnSave` che decrementa/incrementa carico (A3 28/08) | *Variazione Sezionale*: nuova sezione + ricalcolo automatico del magistrato nella nuova sezione + vista prima/dopo delle tabelle PERC (F1 p.17; F7 slide 7) | **≠** |
| **Co-assegnazione** | Non prevista (1 lookup magistrato) | Richiesta per casi complessi, peso pieno a ciascuno (F5 21:53–23:43) | **≠** – da costruire (N:N o tabella Assegnazione) |
| **Stato fascicolo / chiusura** | Validato/Proposto/Chiuso; "Chiudi Caso"; carico monotono (non decrementa alla chiusura) (A1) | Solo **prima assegnazione**; "tutto quello che avviene dopo … non riguarda più questo sistema" (F3 50:57); chiusura opzionale per ufficio (F5 7:25) | **≈** – stati riusabili, chiusura da rendere configurabile |
| **Gestione utenti / ruoli** | Ruolo "Operatore ASPEN", "System Administrator", BU per tribunale, Entra ID, contact→systemuser (A1) | 7 livelli gerarchici (0–256); oggi solo Registro penale + admin; target: accesso magistrati con visibilità carichi configurabile (F1 p.3; F3 48:11; F4) | **≈** – mappabile su security roles Dataverse |
| **Reportistica / stampe** | Dashboard "Cruscotto ASPEN" (PCF barre carico magistrati, torta stati, carico per canestro), custom page Home con KPI; **nessuna stampa** (A1; A4 §3.9 "manca tabella Template stampa") | **Stampa ufficiale giornaliera** con stemma e firma del Presidente che accompagna i fascicoli, PDF archiviato in cartella condivisa, Excel, statistiche pivot, report per categoria/sezione, ricerche avvocati (F6; F1 p.19–24) | **≠** – la stampa è requisito core, oggi assente in ASPEN |
| **Integrazioni** | SICP/Regiweb in roadmap Fase 4 (A4) | Nessuna (inserimento manuale; CCP separato, F5 38:09, 57:02) | **=** (entrambi manuali in POC) |
| **UI/UX** | Model-driven app + custom page Home + dialog HTML per assegnazione | VB6 menu a discesa: Aggiorna Schemi, Gestione Fascicoli, Assegnazioni Automatiche (Detenuti/Liberi), Assegnazioni Manuali, Statistiche, Moduli di Stampa, Ricerche Avvocati, Utility (F1 p.4; F7) | **≈** – la struttura del sitemap ASPEN (Operatività/Anagrafiche/Impostazioni) copre concettualmente le voci; mancano viste "Prima/Dopo" e lotti |
| **Multi-ufficio / configurabilità** | Ambiente unico + BU per tribunale, `agc_configurazione` con soglie (A1; A4 §3.8) | Variabile "ufficio" nelle regole: Napoli (6 sez., trimestre nascita, 22 cat.) vs Firenze (3 sez., mese iscrizione, valore ponderale 1–7) (F3 0:43–3:31) | **≈** – architettura BU riusabile, ma il **motore** deve essere parametrico su regole molto diverse |
| **Sicurezza dati storici** | Audit nativo Dataverse (A5 §6.4) | Storico movimentazioni per istanza, storico categorie per DATA_LAV, log utente/data su ogni salvataggio (F1 p.11–13) | **≈** |

---

## 4. Componenti Power Platform riutilizzabili da ASPEN

Fonte: A1 (README), A3 (SESSION_NOTES), A6 (struttura `05 - Power Platform`).

| Componente ASPEN | Riuso per ASSPECA | Adattamento necessario | Perché riusabile |
|---|---|---|---|
| **Tabella `contact` come Magistrato** (`agc_ismagistrato`, `agc_caricoattuale`, `agc_utenteassociato`→systemuser, form "Contatto - Magistrato", tab Esoneri) | ✅ as-is + estensioni | Aggiungere: lookup **Sezione**, **Tipo** (P/C/F/M), **Data nomina** (DATA_POS), **% Astensione** (o derivarla da `agc_esonero`), flag attivo/CANC | Decisione architetturale già presa e validata (Opzione C, A3 27–28/08): magistrati = utenti Entra ID; l'anagrafica ASSPECA (F7 slide 1) è un sottoinsieme + 4 campi |
| **`agc_esonero` + `EsoneroRientroPlugin.cs` + flow schedulato di chiusura** | ✅ con adattamento | Mappare *Astensione 25%/50%* → esonero **Parziale** permanente (senza data fine); *sospensione temporanea* → esonero **Totale**; rivedere l'effetto: in ASSPECA l'astensione riduce la quota nel conteggio **n. magistrati sezione** (F3 34:27: 7,75 → 8,75) | Il requisito "esoneri parziali già previsti dall'applicativo" è confermato dal cliente (F3 12:56 Vedovato) |
| **`agc_modificacarico` + Custom API `agc_ModificaCaricoMagistrato` + ribbon "Modifica Carico" (solo System Administrator)** | ✅ as-is | Eventuale estensione a "carico per categoria" | Copre il caso ASSPECA di inizializzazione/forzatura fascicoli per magistrato ("INS FASC", "Calcola perc iniziale", F1 p.7, p.9) con audit |
| **`agc_configurazione`** (parametri chiave/valore) | ✅ as-is | Nuove chiavi: n. sezioni in turno (4), criterio trimestre/semestre, regola nuovo magistrato, ufficio | Già usata per soglie PCF |
| **`agc_canestrofascicolo`** (nome + peso) | ⚠️ solo come contenitore → rinominare/estendere in **Categoria** | Aggiungere: codice (D01…L20), tipo Detenuti/Liberi, flag specializzata, esclusione Presidenti, criteri (n. imputati min/max, allegati, DDA) | Il cliente stesso parla di categorie "che possono essere paragonate ai canestri" (F5 31:49); ma la semantica è diversa (§3) |
| ~~`agc_fascicolo2` (+ form, viste, ribbon)~~ | ❌ **NON riusata** — tabella **non condivisa** tra i due applicativi | Creare **nuova tabella dedicata `agc_fascicoloappello`**, con form/viste/ribbon propri, ricalcati sul pattern di `agc_fascicolo2` (struttura, non l'istanza) | **Decisione architetturale (09/09/2026)**: v. §6.4. La sola segregazione per Business Unit **non garantisce l'isolamento dei dati** se la tabella è condivisa — un privilegio di lettura a livello Organization (es. su un ruolo reportistica/admin) o l'uso di Advanced Find/viste/Power BI bypassano il filtro di BU e un magistrato potrebbe vedere fascicoli ASPEN dentro ASSPECA e viceversa. Il confine di sicurezza deve stare **a livello di tabella** (0 privilegi sull'altra tabella per i ruoli dell'altro applicativo), non solo di BU. Si riusa comunque il **pattern** (schema colonne, form, viste, ribbon, comandi moderni) come base di partenza, copiato e adattato nella nuova tabella |
| **Ribbon `AgicAspenRibbon` – "Assegna Fascicolo" + dialog HTML con selezione incompatibili, "Chiudi Caso", refresh** (`agc_assignfascicolo.js`, `agc_assignfascicolodialog.html`) | ✅ struttura/UX (da duplicare sulla nuova tabella) | Sostituire il motore "min carico assoluto" con il motore a 2 livelli; il dialog può mostrare **sezione proposta + magistrato proposto** e la vista "prima/dopo"; ribbon/comandi registrati su `agc_fascicoloappello`, non su `agc_fascicolo2` | Pattern deploy già collaudato (pack/import, A1 punto 18); UX incompatibilità già validata col cliente (F4) |
| **Comando moderno "Assegnazione massiva"** (`openBulkAssignFromGrid`, sequenziale con aggiornamento carico in memoria) | ✅ pattern | Diventa "**Assegnazione di lotto per categoria/data deposito**" (Detenuti/Liberi) – F1 p.15–16 | Stesso schema: ciclo sui fascicoli non assegnati, ricalcolo dopo ciascuno |
| **PCF `CaricoMagistratiChart`** (barre orizzontali, soglie colore, drill-down) | ✅ as-is / minimo | Dataset bindato a una view; per ASSPECA aggiungere filtro per sezione/categoria; il "peso" diventa conteggio | Il cliente ha apprezzato i cruscotti e chiesto di segnalare KPI (F4) |
| **PCF `StatoFascicoliChart`** (torta stati, selettore anno) | ✅ as-is | Nessuno se si mantengono gli stati | |
| **PCF `CaricoPerCanestro`** (form magistrato, WebAPI) | ✅ con rename | "Carico per **Categoria**" del magistrato = tabella MAG_CAT (F1 p.9) | Query OData già parametrica su lookup |
| **Plugin `SetOwnerTeamPlugin`** (owner team alla creazione) | ✅ as-is | Registrare su nuove tabelle (Sezione, Variazione) | Supporta la segregazione per BU |
| **Architettura BU per ufficio + ruolo "Operatore ASPEN"** | ✅ | Creare BU "Corte di Appello di Napoli" (e "Firenze"); ruoli: Registro penale (liv.1), Magistrato (liv.2), Sola lettura (liv.0/3), Admin (254–256) | Coerente con A4 §3.8 e con i 7 livelli F1 p.3 |
| **Custom page "Home"** (card + KPI Power Fx) | ✅ as-is | Cambiare titoli/KPI (fascicoli assegnati oggi per sezione, ecc.) | Ricostruita e pubblicata il 08/09 (A3) |
| **Sitemap** Operatività / Anagrafiche / Impostazioni | ✅ | Aggiungere Sezioni, Categorie, Specializzazioni, Variazioni, Stampe | Copre le voci menu F1 p.4 |
| **Modello RGNR padre → fascicoli figli + continuità** | ⚠️ parziale | Potenzialmente utile per "N° Reg. PM + Ufficio" e per il caso "rinvio dalla Cassazione → sezione diversa da quella cassata" (F2) | Pattern "fascicolo fratello già assegnato" riusabile per regole di continuità/esclusione |
| **Know-how tecnico** (A1/A3): deploy webresource via editor UI; `pac solution pack`+`import`; `CountIf` vs `CountRows`; navigation property case-sensitive; form duplicate; publish custom page da Studio | ✅ | — | Riduce drasticamente il rischio nei 6 giorni disponibili |

---

## 5. Cosa va costruito da zero per ASSPECA

| # | Componente / logica | Descrizione | Fonte requisito |
|---|---|---|---|
| N0 | **Tabella dedicata `agc_fascicoloappello`** (NON estensione di `agc_fascicolo2`) | Nuova entità Dataverse per i fascicoli d'appello, con schema colonne ricalcato/adattato da `agc_fascicolo2` (N° Reg. Appello/Anno, N° Reg. PM/Anno/Ufficio, Categoria, Data Deposito, Data Iscrizione, Mese nascita 1° imputato, Imputato, Specializzata, Note, lookup Sezione e Magistrato) + form/viste/ribbon propri | Decisione architetturale 09/09/2026 (v. §6.4): garantisce segregazione a livello di tabella tra ASPEN e ASSPECA, indipendente dalla configurazione di Business Unit |
| N1 | **Tabella `Sezione`** | Numero/ordine, nome, BU/ufficio, **n. magistrati effettivo calcolato** (somma quote 1 − astensione%, Presidente incluso/escluso per categoria), flag attiva, note; subgrid magistrati | F1 p.5; F3 16:55, 34:27 |
| N2 | **Tabella `Categoria`** (o estensione profonda di `agc_canestrofascicolo`) | Codice, descrizione, tipo D/L, flag specializzata, esclusione Presidenti, regole di classificazione (range imputati, allegati, DDA), **ordine di priorità** | F2; F1 p.28; F7 slide 7 |
| N3 | **Tabella `Specializzazione/Materia`** + **regola di destinazione** | S1/S2/S3 con mappa sezione ↔ semestre di nascita (Napoli) o ↔ mese di iscrizione (Firenze); elenco reati per competenza tabellare (Firenze §102) | F1 p.8; F2; F8 |
| N4 | **Carico per categoria** (matrice) | Tabelle `CaricoSezioneCategoria` (FASC, MAGI, PERC, DATA_DEP, FASC_INI) e `CaricoMagistratoCategoria` (FASC, PERC, DATA_POS) — equivalente Dataverse delle CAT_n / MAG_CATn | F1 p.7, p.9 |
| N5 | **Motore di assegnazione a 2 livelli** (Plugin/Custom API, come raccomandato in A5 §6.2) | Livello sezione: classifica giornaliera per categoria, prime N (4) sezioni, abbinamento trimestre di nascita, specializzate a sezione fissa, compensazione; livello magistrato: min FASC nella categoria, tie-break DATA_POS, esclusioni Presidente/astensione/esonero, nuovo magistrato "in coda" | F1 p.8–9; F2; F3 |
| N6 | **Assegnazione di lotto** (Detenuti / Liberi) per categoria e data deposito | Form/dialog "Controllo Assegnazioni" + "Assegna Sezioni e Magistrati" + stampa; gestione "Ultima Data Deposito" per categoria | F1 p.14–16; F7 slide 3 |
| N7 | **Variazione Sezionale / Variazione Magistrato** con tabella `Motivo` | Entità `Variazione` (fascicolo, sezione/magistrato prima e dopo, motivo, nota, provvedimento, utente, data), ricalcolo automatico del magistrato nella nuova sezione, vista "Prima/Dopo" delle classifiche | F1 p.17–18; F7 slide 5–8 |
| N8 | **Storico movimentazioni** strutturato | Oltre all'audit nativo: tabella di storico consultabile ("D.Storico") e storico categorie per data lavorazione | F1 p.11, p.13 |
| N9 | **Stampa ufficiale di assegnazione** | Template PDF (stemma, "Settore Penale – ASSEGNAZIONE – PROCEDIMENTI – <data>", righe Id/Categoria/Anno/N°RG/Sez/Spec/Data Dep/Magistrato/Imputato/Mese_Nas/Note, totale, "Il Presidente"), generazione via Power Automate (Word template → PDF) o Report SSRS/Power BI paginato; archiviazione PDF in SharePoint/cartella condivisa; export Excel | F6; F1 p.1, p.20–23 |
| N10 | **Statistiche / pivot** per data deposito, anno iscrizione, categoria, sezione, magistrato | Viste + grafici model-driven o Power BI | F1 p.19; F3 50:35 |
| N11 | **Pesatura multi-criterio configurabile** (target) | Tabella `CriterioPeso` per ufficio (imputati, detenuti, allegati/faldoni, motivi di appello, parte civile, DDA, materia) con classificazione automatica in categoria/valore ponderale + override manuale motivato | F5 26:55–30:55; F8 §87 |
| N12 | **Co-assegnazione** | Relazione N:N fascicolo↔magistrato (o tabella `Assegnazione`) con peso pieno a ciascuno; impatti su PCF e motore | F5 21:53–23:43 |
| N13 | **Configurazione per ufficio del motore** | Parametri: n. sezioni in turno, criterio (trimestre nascita / mese iscrizione / rotazione), granularità (giornaliera), regola nuovo magistrato, chiusura fascicolo on/off, visibilità carichi | F3 0:43–3:31; F5 7:25 |
| N14 | **Ricerche Avvocati** (fuori scope POC) | Vista pubblica/limitata per cognome imputato o n° PM su dati in sola lettura | F1 p.24 |
| N15 | **Regole Firenze** (fase 2) | Competenza per articolo di reato, criteri temporali per mese di iscrizione, rotazioni, valore ponderale 1–7 con incrementi, esame preliminare a turno nel carico mensile | F8 |
| N16 | **Import dati storici da Access** (fase 2) | Anagrafiche, categorie, storico assegnazioni dal 2018 (necessario per far partire le classifiche PERC dai valori reali o decidere un reset) | F3 21:18–22:52 |

---

## 6. Rischi e aree da chiarire con il cliente

### 6.1 Ambiguità funzionali
1. **"Data deposito"**: il cliente stesso non è certo del significato (data di pervenimento vs data-etichetta del lotto di assegnazione per categoria). "Su questo andrebbe un attimo approfondito … potremmo lasciarlo in sospeso" (F3 25:39–27:31). Impatta la chiave di ordinamento del motore.
2. **Formula PERC**: F1 p.8 dice `fascicoli / magistrati`; negli screenshot compaiono sia PERC che PERC1 e FASC_INI (F1 p.7); Galano: "questo calcolo poi lo dobbiamo vedere bene … se c'è una formulina" (F3 41:20). Da verificare con lo sviluppatore originale (P. Montella) o via dump Access.
3. **Tie-break per anzianità**: F1 p.15 parla di "anzianità di servizio", Galano dice "il magistrato più giovane" (F3 30:03). Chiarire direzione dell'ordinamento su DATA_POS.
4. **Categorie escluse per i Presidenti**: quali (F3 20:33 "alcune categorie che sono quelle più pesanti").
5. **Numero categorie**: 20 (Galano), 22 (manuale/A5), 1–20 + 7bis + 12bis (F2). Verificare l'elenco definitivo con codici D/L.
6. **Astensione %**: riduce il denominatore della sezione (F3 34:27) — riduce anche la quota nel conteggio del singolo magistrato? Come si combina con "Presidente conteggiato come magistrato in più"?
7. **Ordine sezione→magistrato vs magistrato diretto**: posizioni non del tutto allineate tra Maddalena (sezione prima) e Borraccia (magistrato in quanto appartenente alla sezione), da risolvere con simulazioni (F5 47:32–53:27). Impatta il disegno del motore.
8. **Squilibrio storico** della categoria 6 dal 2018 (F3 21:18): in POC, reset a zero o import dei totali storici? Perequazione su arco temporale (F5 8:19)?
9. **Specializzate**: le regole F2 (S1/S2/S3 con 2 sezioni per semestre) vs F1 p.8; confermare che il codice "Spec" e la flag siano coerenti con le tabelle 2025–2026 (F2 potrebbe essere datato).
10. **Rinvii dalla Cassazione**: regola "sezione successiva a quella cassata" (F2) — è gestita oggi dall'applicativo o manualmente?

### 6.2 Scope e governance
11. **Due uffici con regole molto diverse** (Napoli vs Firenze): rischio di "over-configurazione"; Vedovato stesso ipotizza "forse vale la pena fare due applicativi separati" (F3 2:28). Proposta: POC = Napoli; Firenze come test di configurabilità in fase 2.
12. **Cliente indisponibile fino al ~10/09** (F3 1:04:38–1:05:06); la POC del 15/09 deve reggersi su assunzioni documentate qui, senza validazione intermedia.
13. **Sessione operativa reale non ancora vista**: l'inserimento e la protocollazione di lotto non sono stati mostrati (F3 41:47–43:16); proposta visita in loco a Napoli (F3 54:41–55:24).
14. **Manca dump/backup Access** e la struttura tabelle reale (solo screenshot). Necessario per import e per validare PERC.
15. **Ambiente Dataverse condiviso con ASPEN**: decidere se ASSPECA vive nella stessa solution `ASPENPOC` (rischio regressioni sulla demo ASPEN) o in solution separata `ASSPECAPOC` sullo stesso ambiente con nuova BU. **Risolto in parte (09/09/2026)**: solution separata `ASSPECAPOC`, nuova BU dedicata, **tabella fascicoli non condivisa** (`agc_fascicoloappello`, v. §6.4) — restano condivise solo `contact`, `agc_configurazione`, `agc_esonero`. Ambiente afflitto dal **bug Microsoft di metadata orfani** (`agc_canestro`/`agc_giudice`, A3 08/09) che blocca alcune operazioni di schema.
16. **Divergenze già note in ASPEN**: `.pa.yaml` non allineato alla custom page live; deploy webresource solo via editor UI; `pac solution export` rotto (A1, A3) → tempi di deploy da non sottovalutare.
17. **Nessuna integrazione** con registri (SICP/CCP) — coerente con ASPEN, ma il cliente Firenze ha chiesto esplicitamente se sia possibile importare (F5 37:57).
18. **Accesso dei magistrati**: oggi assente; il target lo prevede ma "dipende dall'ufficio" (F4). Da confermare per la definizione dei ruoli.
19. **Requisiti fuori perimetro confermati**: Assise, fissazione udienze, aggregazione udienze omogenee (evoluzione), Ricerche Avvocati — da esplicitare nella demo come roadmap.
20. **Documenti non leggibili con OCR**: F6 (stampa) e F7 (screenshot) sono stati interpretati visivamente; alcune diciture (es. note "ACR - NO 4^") potrebbero contenere abbreviazioni interne da verificare.

### 6.4 Segregazione dati ASPEN / ASSPECA (decisione 09/09/2026)
Sollevato dal cliente/team interno: riusare `agc_fascicolo2` per ASSPECA avrebbe creato un rischio concreto di **fuga di dati tra applicativi**. In Dataverse la sicurezza per Business Unit agisce sul **livello di accesso di un privilegio** (User/BU/BU e sub-BU/Organization); se anche un solo ruolo di sicurezza assegnato a un utente (es. un ruolo di reportistica, un ruolo "Sola lettura" cross-ufficio, o semplicemente un ruolo con privilegio Read a livello Organization) ha accesso alla tabella, quell'utente vede **tutte** le righe indipendentemente dalla BU — inclusi i fascicoli dell'altro applicativo. Inoltre l'app model-driven filtra solo la navigazione UI, non i dati: Advanced Find, viste, export Excel, dashboard e Power Automate interrogano comunque la tabella sottostante. Il rischio si applica in particolare se uno stesso magistrato/operatore dovesse avere accesso a entrambi gli applicativi, o se un ruolo con scope Organization venisse introdotto in futuro (es. per statistiche ministeriali).

**Decisione:** i fascicoli ASSPECA vivranno in una **tabella Dataverse dedicata** (`agc_fascicoloappello`, N0), separata da `agc_fascicolo2`. Il confine di sicurezza diventa così **strutturale a livello di tabella** (privilegio 0 sull'altra tabella per i ruoli dell'applicativo opposto), robusto anche in presenza di ruoli con scope Organization mal configurati — a differenza di un confine basato solo sulla profondità BU su una tabella condivisa. Le tabelle "trasversali" per natura (`contact` come Magistrato, `agc_configurazione`, `agc_esonero`, `SetOwnerTeamPlugin`) restano condivise perché modellano la stessa persona/parametro fisico, non il dato sensibile (fascicolo) da segregare.

### 6.3 Assunzioni adottate in questo documento
- L'ufficio pilota della POC è la **Corte di Appello di Napoli**, regole = F1 + F2 + F3.
- Il "peso" ASSPECA è modellato come **conteggio per categoria**, non come somma pesata (salvo evoluzione N11).
- La stampa di assegnazione (F6) è **requisito di demo**, non opzionale.
- La chiusura del fascicolo non è richiesta a Napoli (solo prima assegnazione) ma resta disponibile/configurabile.

---

## 7. Proposta di piano di massima per la POC (scadenza 15/09/2026)

> Solo macro-attività: **nessuna implementazione è stata eseguita** in questa fase.

### Priorità P0 – indispensabile per la demo del 15/09
| # | Macro-attività | Output | Riuso ASPEN |
|---|---|---|---|
| 1 | **Decisioni di scoping** (interne AGIC, 1 gg): ufficio pilota Napoli; solution separata `ASSPECAPOC` nello stesso ambiente con BU "Corte di Appello di Napoli"; scelta motore lato client (JS/dialog, più rapido) vs plugin (più robusto) per la demo | Verbale decisioni | — |
| 2 | **Modello dati minimo**: nuova tabella **`agc_fascicoloappello`** (N0, dedicata, non condivisa con ASPEN), `Sezione` (N1), `Categoria` (N2, 22 record da F2), `Specializzazione` (N3, S1–S3), estensione `contact` (sezione, tipo, data nomina, astensione), `CaricoSezioneCategoria` e `CaricoMagistratoCategoria` (N4), `Variazione` + `Motivo` (N7) | Tabelle, form, viste, sitemap | contact, agc_configurazione, agc_esonero, SetOwnerTeamPlugin, schema/pattern di agc_fascicolo2 (non la tabella stessa) |
| 3 | **Dati demo realistici**: 6 sezioni con quote (8/9/8/8/5/5 magistrati, da F7 slide 5), ~40 magistrati fittizi con tipo/anzianità/astensioni, 22 categorie, ~10 fascicoli per lotto in 3–4 categorie, mesi di nascita distribuiti | Dataset anonimizzato | pattern import già usato |
| 4 | **Motore assegnazione a 2 livelli** (N5) per il caso ordinario + specializzate: classifica sezioni per categoria, prime 4, trimestre nascita, S1–S3 fisse; magistrato per min FASC + DATA_POS; nuovo magistrato in coda; Presidente escluso su categorie configurate | Dialog "Assegna" con sezione+magistrato proposti; "Assegnazione lotto" (N6) | agc_assignfascicolo.js / dialog, openBulkAssignFromGrid |
| 5 | **Variazione Sezionale / Magistrato** con motivo e vista prima/dopo semplificata (N7) | Form Variazione + ricalcolo | handler addOnSave, agc_modificacarico pattern |
| 6 | **Cruscotto**: carico per sezione e per magistrato per categoria; stato fascicoli | Dashboard | PCF CaricoMagistratiChart, StatoFascicoliChart, CaricoPerCanestro (rename) |
| 7 | **Stampa di assegnazione giornaliera** (N9) fedele a F6 (PDF con stemma e firma "Il Presidente") | Flow Power Automate Word→PDF + archiviazione SharePoint, o report | — (nuovo; connessione Dataverse-Power Automate da stabilire, A3 08/09) |
| 8 | **Script demo** e mapping menu ASSPECA → app (Aggiorna Schemi → Anagrafiche; Gestione Fascicoli → Fascicoli; Assegnazioni Automatiche → Assegnazione lotto; Assegnazioni Manuali → Variazioni; Statistiche/Stampe → Cruscotto/Stampe) | Documento + video breve | Script voiceover ASPEN come modello |

### Priorità P1 – da mostrare come roadmap il 15/09 (post-POC)
| # | Macro-attività |
|---|---|
| 9 | Pesatura multi-criterio configurabile per ufficio (N11) e classificazione automatica in categoria |
| 10 | Configurazione Firenze (N15): 3 sezioni, competenza per reato, criteri per mese di iscrizione, valore ponderale 1–7 |
| 11 | Co-assegnazione (N12) |
| 12 | Statistiche/pivot ed export Excel (N10); report per categoria/sezione (F1 p.21); Stampe Commesse |
| 13 | Storico strutturato (N8) e audit completo; incompatibilità persistite (allineato ad A4 §3.7) |
| 14 | Import storico da Access (N16) e strategia di reset/perequazione su arco temporale |
| 15 | Accesso magistrati con visibilità carichi configurabile; Ricerche Avvocati (N14) |
| 16 | Simulazioni con dati reali (richiesta esplicita F5 53:21) e sessione on-site a Napoli |
| 17 | Integrazione registri (SICP/CCP) – solo disegno evolutivo, coerente con A4 Fase 4 |

### Sequenza suggerita (6 giorni lavorativi)
1. **G1**: decisioni (1), modello dati (2), dati demo (3) – in parallelo preparazione template stampa (7).
2. **G2–G3**: motore livello sezione + livello magistrato (4), assegnazione lotto (6 del P0).
3. **G4**: variazioni (5), cruscotto (6), stampa (7).
4. **G5**: test end-to-end con scenario "lunedì 6 maggio 2024" ricostruito da F6 (8 fascicoli L11), correzioni.
5. **G6**: script demo (8), buffer e congelamento ambiente.

---

## Appendice A – Glossario ASSPECA
- **Categoria / Codice di difficoltà**: classe di complessità del fascicolo (D = detenuti, L = liberi) che sostituisce il "peso".
- **Specializzata (S1/S2/S3)**: materia riservata a coppie di sezioni, destinazione fissa per semestre di nascita.
- **PERC**: rapporto fascicoli assegnati / n. magistrati della sezione, per categoria.
- **DATA_POS**: data di nomina/posizione del magistrato (anzianità).
- **DATA_DEP**: data deposito (usata anche come etichetta del lotto di assegnazione).
- **MAGRIF (M)**: magistrato di riferimento per l'informatica, con astensione 25%.
- **Variazione Sezionale / Magistrato**: riassegnazione manuale motivata (Errore materiale, Incompatibilità, Scardinamento, Stralcio, Verifica).
- **Registro penale**: ufficio che inserisce i fascicoli e lancia l'assegnazione.

## Appendice B – Corrispondenza voci menu ASSPECA (F1 p.4) → aree app target
| Menu ASSPECA | Sotto-voci (F1) | Area app target |
|---|---|---|
| Aggiorna Schemi | Tabella Sezioni, Anagrafe Magistrati, Verifica Categorie, Verifica Magistrati, Controllo Data Deposito, Storico Categorie | Anagrafiche (Sezioni, Magistrati, Categorie) + viste carico per categoria |
| Gestione Fascicoli | Inserimento / Modifica Registro Assegnazioni | Fascicoli (form, viste, storico) |
| Assegnazioni Automatiche | Controllo Assegnazioni, Assegnazioni Detenuti, Assegnazioni Liberi | Comando "Assegnazione lotto" + dialog "Assegna Fascicolo" |
| Assegnazioni Manuali | Variazioni Sezionali, Modifica Magistrato | Entità Variazione + comandi form |
| Statistiche | Pivot | Cruscotto / Power BI |
| Moduli di Stampa/Report | Stampe Registro Assegnazioni, Stampe Commesse | Flow stampa PDF + viste esportabili |
| Ricerche Avvocati | — | Fase 2 |
| Utility | Password, Storico/Cancellazioni, Annulla base dati, Crea categorie di appoggio, Blocchi record, Gestione tabelle | Impostazioni (agc_configurazione, Motivi, Categorie) + funzioni admin (Modifica Carico) |
