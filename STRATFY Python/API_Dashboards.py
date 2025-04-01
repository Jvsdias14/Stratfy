import pandas as pd
import matplotlib.pyplot as plt
import streamlit as st
from io import BytesIO

# Configurar a largura da página e o tema
st.set_page_config(page_title="Dashboard de Entradas e Saídas", layout="wide")

# CSS para fixar o título no topo e ajustar os cartões
st.markdown(
    """
    <style>
    /* Fixar o título no topo */
    .main > div:first-child {
        position: sticky;
        top: 0;
        background-color: white;
        z-index: 100;
        padding-top: 10px;
        padding-bottom: 10px;
        border-bottom: 1px solid #ddd;
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    /* Ajustar o título */
    .main > div:first-child h1 {
        margin: 0;
        padding-left: 20px; /* Ajustar o espaçamento à esquerda */
    }

    /* Aumentar os cartões */
    .stMetric {
        font-size: 1.5rem; /* Tamanho do texto */
        padding: 10px; /* Espaçamento interno */
        border: 1px solid #ddd; /* Borda */
        border-radius: 10px; /* Bordas arredondadas */
        background-color: #f9f9f9; /* Fundo claro */
        text-align: center; /* Centralizar o texto */
    }
    </style>
    """,
    unsafe_allow_html=True,
)

# Título da página
st.title("Dashboard de Entradas e Saídas")

# Função para criar gráficos dinâmicos
def criar_grafico_filtrado(df, tipo, eixo_x, eixo_y, titulo, cor):
    fig, ax = plt.subplots(figsize=(8, 6))  # Aumentar o tamanho do gráfico
    df.plot(kind=tipo, x=eixo_x, y=eixo_y, ax=ax, color=cor, title=titulo)
    ax.set_xlabel(eixo_x)
    ax.set_ylabel(eixo_y)
    plt.tight_layout()

    # Salvar o gráfico em memória
    buf = BytesIO()
    plt.savefig(buf, format='png')
    buf.seek(0)
    return buf

# Carregar o arquivo
file_path = "Exemplo Extrato.csv"
df = pd.read_csv(file_path) if file_path.endswith(".csv") else pd.read_excel(file_path)
df.columns = df.columns.str.strip()
df.rename(columns={df.columns[0]: "Data", df.columns[1]: "Descrição", df.columns[2]: "Valor"}, inplace=True)
df["Data"] = pd.to_datetime(df["Data"], errors="coerce")
df["Valor"] = df["Valor"].astype(float)

# Criar a dashboard com Streamlit

# Filtros Dinâmicos em um Expander
with st.sidebar.expander("Filtros", expanded=True):
    st.header("Filtros")
    data_inicio = st.date_input("Data de Início", value=df["Data"].min())
    data_fim = st.date_input("Data de Fim", value=df["Data"].max())
    tipo_grafico = st.selectbox("Tipo de Gráfico", ["line", "bar"])
    mostrar_entradas = st.checkbox("Mostrar Entradas", value=True)
    mostrar_saidas = st.checkbox("Mostrar Saídas", value=True)

# Aplicar os filtros
df_filtrado = df[(df["Data"] >= pd.Timestamp(data_inicio)) & (df["Data"] <= pd.Timestamp(data_fim))]
df_positive = df_filtrado[df_filtrado["Valor"] > 0].groupby("Data")["Valor"].sum()
df_negative = df_filtrado[df_filtrado["Valor"] < 0].groupby("Data")["Valor"].sum()

# Calcular os totais
total_positive = df_positive.sum()
total_negative = df_negative.sum()

# Exibir os cartões com os valores totais
col1, col2 = st.columns(2)
with col1:
    st.metric(label="Total de Entradas", value=f"R$ {total_positive:,.2f}")
with col2:
    st.metric(label="Total de Saídas", value=f"R$ {total_negative:,.2f}")

# Exibir os gráficos lado a lado
if mostrar_entradas or mostrar_saidas:
    st.subheader("Gráficos de Entradas e Saídas")
    col1, col2 = st.columns(2)

    if mostrar_entradas:
        with col1:
            st.image(
                criar_grafico_filtrado(
                    df_positive.reset_index(), tipo_grafico, "Data", "Valor", "Entradas por Data", "green"
                ),
                use_container_width=True,
            )

    if mostrar_saidas:
        with col2:
            st.image(
                criar_grafico_filtrado(
                    df_negative.reset_index(), tipo_grafico, "Data", "Valor", "Saídas por Data", "red"
                ),
                use_container_width=True,
            )