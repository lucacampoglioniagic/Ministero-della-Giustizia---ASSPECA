import { execSync } from 'child_process';

const envUrl = 'https://lccministerogiustiziademo.crm4.dynamics.com';
const token = execSync(`az account get-access-token --resource "${envUrl}" --query accessToken -o tsv`, { encoding: 'utf8' }).trim();

const headers = {
  Authorization: `Bearer ${token}`,
  'Content-Type': 'application/json',
  Accept: 'application/json',
  'OData-MaxVersion': '4.0',
  'OData-Version': '4.0',
  Prefer: 'return=representation',
};

async function post(entitySet, body) {
  const res = await fetch(`${envUrl}/api/data/v9.2/${entitySet}`, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) {
    console.error(`FAIL ${entitySet}:`, res.status, text);
    return null;
  }
  const json = JSON.parse(text);
  return json;
}

// Lookup ids gathered earlier
const categoria = {
  D05: '0be44430-58ac-f111-aaac-002248a420f7',
  D07: '0fe44430-58ac-f111-aaac-002248a420f7',
  D10: '15e44430-58ac-f111-aaac-002248a420f7',
  L01: '18e44430-58ac-f111-aaac-002248a420f7',
  L02: '1de44430-58ac-f111-aaac-002248a420f7',
  L08: '27e44430-58ac-f111-aaac-002248a420f7',
  L11: '29e44430-58ac-f111-aaac-002248a420f7',
  D03: '6f3b4630-58ac-f111-aaab-7ced8d71a68d',
  D04: '723b4630-58ac-f111-aaab-7ced8d71a68d',
  D08: '793b4630-58ac-f111-aaab-7ced8d71a68d',
};

const sezione = {
  S1: 'e059e01e-58ac-f111-aaab-7ced8d757277',
  S2: 'e0c5ca23-58ac-f111-aaab-7ced8d71a68d',
  S3: '4262ae23-58ac-f111-aaab-7ced8d775612',
  S4: '13501124-58ac-f111-aaac-002248a420f7',
  S5: '4662ae23-58ac-f111-aaab-7ced8d775612',
  S6: '13c6ca23-58ac-f111-aaab-7ced8d71a68d',
};

const specializzazione = {
  S1: '1ae7232a-58ac-f111-aaac-002248a420f7',
  S2: '4bf9ad29-58ac-f111-aaab-7ced8d775612',
  S3: '4cf9ad29-58ac-f111-aaab-7ced8d775612',
};

const motivo = {
  Incompatibilita: '1ce7232a-58ac-f111-aaac-002248a420f7',
  Scardinamento: '439ad129-58ac-f111-aaab-7ced8d71a68d',
  Verifica: '5f9ad129-58ac-f111-aaab-7ced8d71a68d',
  Stralcio: 'aa646c2b-58ac-f111-aaab-7ced8d757277',
  ErroreMateriale: '4ef9ad29-58ac-f111-aaab-7ced8d775612',
};

const STATO = { Proposto: 10000, Validato: 10001, Chiuso: 10002 };
const TIPO_VARIAZIONE = { Sezionale: 10000, Magistrato: 10001 };

const fascicoli = [
  { name: '2024/00145', categoria: categoria.D05, sezione: sezione.S1, stato: STATO.Proposto, dataDeposito: '2024-01-15', imputato: 'Rossi Mario', ufficioPm: 'Procura di Napoli', anno: 2024 },
  { name: '2024/00187', categoria: categoria.D07, sezione: sezione.S2, stato: STATO.Validato, dataDeposito: '2024-02-03', imputato: 'Bianchi Luca', ufficioPm: 'Procura di Napoli', anno: 2024 },
  { name: '2024/00212', categoria: categoria.D10, sezione: sezione.S3, stato: STATO.Proposto, dataDeposito: '2024-02-20', imputato: 'Verdi Anna', ufficioPm: 'Procura di Torre Annunziata', anno: 2024 },
  { name: '2024/00256', categoria: categoria.L01, sezione: sezione.S4, stato: STATO.Chiuso, dataDeposito: '2024-03-05', imputato: 'Russo Paolo', ufficioPm: 'Procura di Napoli', anno: 2024 },
  { name: '2024/00301', categoria: categoria.L02, sezione: sezione.S5, stato: STATO.Validato, dataDeposito: '2024-03-18', imputato: 'Ferrari Giulia', ufficioPm: 'Procura di Nola', anno: 2024, specializzazione: specializzazione.S1 },
  { name: '2024/00334', categoria: categoria.L08, sezione: sezione.S6, stato: STATO.Proposto, dataDeposito: '2024-04-02', imputato: 'Esposito Marco', ufficioPm: 'Procura di Napoli', anno: 2024 },
  { name: '2024/00378', categoria: categoria.L11, sezione: sezione.S1, stato: STATO.Validato, dataDeposito: '2024-04-25', imputato: 'Romano Chiara', ufficioPm: 'Procura di Torre Annunziata', anno: 2024, specializzazione: specializzazione.S2 },
  { name: '2024/00405', categoria: categoria.D03, sezione: sezione.S2, stato: STATO.Proposto, dataDeposito: '2024-05-10', imputato: 'Colombo Davide', ufficioPm: 'Procura di Napoli', anno: 2024 },
  { name: '2024/00452', categoria: categoria.D04, sezione: sezione.S3, stato: STATO.Chiuso, dataDeposito: '2024-05-28', imputato: 'Ricci Sara', ufficioPm: 'Procura di Nola', anno: 2024, specializzazione: specializzazione.S3 },
  { name: '2024/00489', categoria: categoria.D08, sezione: sezione.S4, stato: STATO.Validato, dataDeposito: '2024-06-12', imputato: 'Marino Luca', ufficioPm: 'Procura di Napoli', anno: 2024 },
];

const results = { fascicoli: [], variazioni: [] };

async function run() {
  const fascicoloIds = [];
  for (const f of fascicoli) {
    const body = {
      agc_name: f.name,
      'agc_Categoria@odata.bind': `/agc_categorias(${f.categoria})`,
      'agc_SezioneAssegnata@odata.bind': `/agc_seziones(${f.sezione})`,
      agc_stato: f.stato,
      agc_datadeposito: f.dataDeposito,
      agc_imputato: f.imputato,
      agc_ufficiopm: f.ufficioPm,
      agc_annoregistroappello: f.anno,
    };
    if (f.specializzazione) body['agc_Specializzazione@odata.bind'] = `/agc_specializzaziones(${f.specializzazione})`;
    const created = await post('agc_fascicoloappellos', body);
    if (created) {
      fascicoloIds.push(created.agc_fascicoloappelloid);
      results.fascicoli.push({ name: f.name, id: created.agc_fascicoloappelloid, status: 'OK' });
    } else {
      results.fascicoli.push({ name: f.name, status: 'FAIL' });
    }
  }

  const variazioni = [
    { name: 'VAR-001', fascicoloIdx: 0, motivo: motivo.Incompatibilita, tipo: TIPO_VARIAZIONE.Magistrato, data: '2024-07-01' },
    { name: 'VAR-002', fascicoloIdx: 1, motivo: motivo.Scardinamento, tipo: TIPO_VARIAZIONE.Sezionale, data: '2024-07-05' },
    { name: 'VAR-003', fascicoloIdx: 2, motivo: motivo.Verifica, tipo: TIPO_VARIAZIONE.Sezionale, data: '2024-07-10' },
    { name: 'VAR-004', fascicoloIdx: 4, motivo: motivo.Stralcio, tipo: TIPO_VARIAZIONE.Magistrato, data: '2024-07-15' },
    { name: 'VAR-005', fascicoloIdx: 6, motivo: motivo.ErroreMateriale, tipo: TIPO_VARIAZIONE.Sezionale, data: '2024-07-20' },
  ];

  for (const v of variazioni) {
    const fascId = fascicoloIds[v.fascicoloIdx];
    if (!fascId) {
      results.variazioni.push({ name: v.name, status: 'SKIPPED (fascicolo mancante)' });
      continue;
    }
    const body = {
      agc_name: v.name,
      'agc_Fascicolo@odata.bind': `/agc_fascicoloappellos(${fascId})`,
      'agc_Motivo@odata.bind': `/agc_motivos(${v.motivo})`,
      agc_tipovariazione: v.tipo,
      agc_dataprovvedimento: v.data,
    };
    const created = await post('agc_variaziones', body);
    if (created) {
      results.variazioni.push({ name: v.name, id: created.agc_variazioneid, status: 'OK' });
    } else {
      results.variazioni.push({ name: v.name, status: 'FAIL' });
    }
  }

  console.log(JSON.stringify(results, null, 2));
}

run();
