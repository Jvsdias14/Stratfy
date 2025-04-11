import streamlit as st
import pandas as pd
import requests
import matplotlib.pyplot as plt  # Importa o matplotlib

st.set_page_config(layout="wide")

# Captura parâmetros da URL
query = st.query_params
st.write("Parâmetros recebidos:", query)

dashboard_id = str(query.get("dashboardId", "")).strip()

if dashboard_id:
    url = f"http://localhost:5211/api/dashboarddata/{dashboard_id}"
    st.write("Tentando acessar:", url)

    response = requests.get(url)
    if response.status_code == 200:
        data = response.json()

        df = pd.DataFrame(data["movimentacoes"])

        # Padroniza todas as colunas para minúsculas
        df.columns = df.columns.str.lower()

        st.title(f"Dashboard: {data['dashboard']['descricao']}")

        col1, col2 = st.columns(2)

        with col1:
            for graf in data["graficos"]:
                st.write("Processando gráfico:", graf)
                campo1 = graf["campo1"].lower()
                campo2 = graf["campo2"].lower()

                if campo1 == "data":
                    campo1 = "datamovimentacao"

                if campo1 in df.columns and campo2 in df.columns:
                    st.subheader(graf["titulo"])
                    if graf["tipo"].lower() == "barra":
                        st.bar_chart(df.groupby(campo1)[campo2].sum())
                    elif graf["tipo"].lower() == "pizza":
                        # Agrupa e soma os valores
                        dados = df.groupby(campo1)[campo2].sum()

                        # Gera gráfico de pizza com matplotlib
                        fig, ax = plt.subplots()
                        ax.pie(dados, labels=dados.index, autopct='%1.1f%%', startangle=90)
                        ax.axis('equal')  # Deixa o gráfico redondo

                        # Exibe no Streamlit
                        st.pyplot(fig)
                else:
                    st.warning(f"Campos inválidos no gráfico: {campo1}, {campo2}")

        with col2:
            for card in data["cartoes"]:
                st.write("Processando cartão:", card)
                campo = card["campo"].lower()

                if campo in df.columns:
                    valor = df[campo].sum() if card["tipoAgregacao"].lower() == "soma" else df[campo].nunique()
                    st.metric(label=card["nome"], value=valor)
                else:
                    st.warning(f"Campo inválido no cartão: {campo}")
    else:
        st.error("Dashboard não encontrada.")
else:
    st.warning("Nenhum ID de dashboard foi informado na URL.")
