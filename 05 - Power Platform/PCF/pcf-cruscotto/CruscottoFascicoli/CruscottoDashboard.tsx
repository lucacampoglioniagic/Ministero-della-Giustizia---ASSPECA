import * as React from 'react';
import { Bar, Pie } from 'react-chartjs-2';
import {
    Chart as ChartJS, CategoryScale, LinearScale, BarElement, ArcElement,
    Title, Tooltip, Legend, ChartOptions
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, BarElement, ArcElement, Title, Tooltip, Legend);

export interface ICruscottoDashboardProps {
    webAPI: ComponentFramework.WebApi;
}

interface ICount {
    label: string;
    value: number;
}

interface IDashboardState {
    loading: boolean;
    error?: string;
    totaleFascicoli: number;
    totaleVariazioni: number;
    perStato: ICount[];
    perSezione: ICount[];
    perCategoria: ICount[];
    perMagistrato: ICount[];
    perMotivoVariazione: ICount[];
    perTipoVariazione: ICount[];
}

const PALETTE = ['#0078D4', '#107C10', '#D83B01', '#5C2D91', '#008272', '#FFB900', '#E81123', '#00B7C3', '#8764B8', '#A80000'];

// Legge il valore visualizzato di un campo Picklist (annotazione FormattedValue)
// oppure di un campo Lookup (annotazione FormattedValue sul campo _xxx_value).
function getFormattedValue(record: ComponentFramework.WebApi.Entity, field: string, isLookup: boolean): string | undefined {
    const key = isLookup
        ? `_${field}_value@OData.Community.Display.V1.FormattedValue`
        : `${field}@OData.Community.Display.V1.FormattedValue`;
    return record[key] as string | undefined;
}

function groupBy(records: ComponentFramework.WebApi.Entity[], field: string, isLookup: boolean): ICount[] {
    const map = new Map<string, number>();
    records.forEach((r) => {
        const raw = getFormattedValue(r, field, isLookup);
        const label = (raw === null || raw === undefined || raw === '') ? '(non specificato)' : String(raw);
        map.set(label, (map.get(label) ?? 0) + 1);
    });
    return Array.from(map.entries())
        .map(([label, value]) => ({ label, value }))
        .sort((a, b) => b.value - a.value);
}

const barOptions: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
};

const pieOptions: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'right' } }
};

export class CruscottoDashboard extends React.Component<ICruscottoDashboardProps, IDashboardState> {
    constructor(props: ICruscottoDashboardProps) {
        super(props);
        this.state = {
            loading: true,
            totaleFascicoli: 0,
            totaleVariazioni: 0,
            perStato: [],
            perSezione: [],
            perCategoria: [],
            perMagistrato: [],
            perMotivoVariazione: [],
            perTipoVariazione: []
        };
    }

    public componentDidMount(): void {
        void this.loadData();
    }

    private async loadData(): Promise<void> {
        try {
            const fascicoliResult = await this.props.webAPI.retrieveMultipleRecords(
                'agc_fascicoloappello',
                '?$select=agc_stato,_agc_sezioneassegnata_value,_agc_categoria_value,_agc_magistratoassegnato_value&$top=5000'
            );
            const variazioniResult = await this.props.webAPI.retrieveMultipleRecords(
                'agc_variazione',
                '?$select=_agc_motivo_value,agc_tipovariazione&$top=5000'
            );

            const fascicoli = fascicoliResult.entities;
            const variazioni = variazioniResult.entities;

            this.setState({
                loading: false,
                totaleFascicoli: fascicoli.length,
                totaleVariazioni: variazioni.length,
                perStato: groupBy(fascicoli, 'agc_stato', false),
                perSezione: groupBy(fascicoli, 'agc_sezioneassegnata', true),
                perCategoria: groupBy(fascicoli, 'agc_categoria', true),
                perMagistrato: groupBy(fascicoli, 'agc_magistratoassegnato', true),
                perMotivoVariazione: groupBy(variazioni, 'agc_motivo', true),
                perTipoVariazione: groupBy(variazioni, 'agc_tipovariazione', false)
            });
        } catch (err) {
            this.setState({ loading: false, error: (err as Error).message });
        }
    }

    private renderKpiCard(title: string, value: number): React.ReactNode {
        return (
            <div style={{
                background: '#fff', border: '1px solid #edebe9', borderRadius: 4,
                padding: '16px 24px', minWidth: 160, boxShadow: '0 1px 2px rgba(0,0,0,0.08)'
            }}>
                <div style={{ fontSize: 13, color: '#605e5c' }}>{title}</div>
                <div style={{ fontSize: 32, fontWeight: 600, color: '#201f1e' }}>{value}</div>
            </div>
        );
    }

    private renderBarChart(title: string, data: ICount[]): React.ReactNode {
        const chartData = {
            labels: data.map((d) => d.label),
            datasets: [{
                data: data.map((d) => d.value),
                backgroundColor: PALETTE[0],
                borderRadius: 4
            }]
        };
        return (
            <div style={{
                background: '#fff', border: '1px solid #edebe9', borderRadius: 4,
                padding: 16, flex: '1 1 420px', minWidth: 380
            }}>
                <div style={{ fontSize: 15, fontWeight: 600, marginBottom: 8, color: '#201f1e' }}>{title}</div>
                <div style={{ height: 260 }}>
                    <Bar data={chartData} options={barOptions} />
                </div>
            </div>
        );
    }

    private renderPieChart(title: string, data: ICount[]): React.ReactNode {
        const chartData = {
            labels: data.map((d) => d.label),
            datasets: [{
                data: data.map((d) => d.value),
                backgroundColor: data.map((_d, i) => PALETTE[i % PALETTE.length])
            }]
        };
        return (
            <div style={{
                background: '#fff', border: '1px solid #edebe9', borderRadius: 4,
                padding: 16, flex: '1 1 380px', minWidth: 340
            }}>
                <div style={{ fontSize: 15, fontWeight: 600, marginBottom: 8, color: '#201f1e' }}>{title}</div>
                <div style={{ height: 260 }}>
                    <Pie data={chartData} options={pieOptions} />
                </div>
            </div>
        );
    }

    public render(): React.ReactNode {
        if (this.state.loading) {
            return <div style={{ padding: 24 }}>Caricamento dati in corso...</div>;
        }
        if (this.state.error) {
            return <div style={{ padding: 24, color: '#a4262c' }}>Errore nel caricamento dati: {this.state.error}</div>;
        }

        return (
            <div style={{ padding: 16, background: '#faf9f8', fontFamily: 'Segoe UI, sans-serif' }}>
                <h2 style={{ marginTop: 0, color: '#201f1e' }}>Cruscotto Statistico - Fascicoli Appello</h2>
                <div style={{ display: 'flex', gap: 16, marginBottom: 16, flexWrap: 'wrap' }}>
                    {this.renderKpiCard('Totale Fascicoli', this.state.totaleFascicoli)}
                    {this.renderKpiCard('Totale Variazioni', this.state.totaleVariazioni)}
                </div>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 16 }}>
                    {this.renderPieChart('Fascicoli per Stato', this.state.perStato)}
                    {this.renderBarChart('Fascicoli per Sezione Assegnata', this.state.perSezione)}
                </div>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 16 }}>
                    {this.renderBarChart('Fascicoli per Categoria', this.state.perCategoria)}
                    {this.renderBarChart('Fascicoli per Magistrato Assegnato', this.state.perMagistrato)}
                </div>
                <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
                    {this.renderBarChart('Variazioni per Motivo', this.state.perMotivoVariazione)}
                    {this.renderPieChart('Variazioni per Tipo', this.state.perTipoVariazione)}
                </div>
            </div>
        );
    }
}
